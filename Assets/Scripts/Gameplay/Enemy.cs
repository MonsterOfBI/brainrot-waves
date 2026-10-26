using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BrainrotWaves
{
    /// <summary>
    /// Враг-брейнрот. Поведение зависит от вида (бегун, метатель, летун...).
    /// Босс — увеличенная версия со своими особыми атаками.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> Active = new List<Enemy>();
        /// <summary>Замедление всех врагов (способность Капучино).</summary>
        public static float TimeScale = 1f;
        public static event Action<Enemy> AnyDied;

        enum State { Move, Windup, Leap, Dash }
        enum Special { Melee, Leap, Dash, Throw }

        public BrainrotInfo Info { get; private set; }
        public bool IsBoss { get; private set; }
        public Health Health { get; private set; }
        public bool IsDead => Health == null || Health.IsDead;
        public float Size { get; private set; }

        BrainrotRig rig;
        Transform modelRoot;
        NavMeshAgent agent;
        float speed;
        float damage;
        float attackRange;
        float attackCooldown;
        float specialCooldown;
        float repathTimer;
        float rootTimer;
        float rootDamagePerSecond;
        GameObject rootVisual;
        Vector3 knockback;
        float spawnTimer = 0.45f;
        float hoverAngle;
        Vector3 lastHitDirection;

        State state = State.Move;
        Special pendingSpecial;
        float stateTimer;
        float stateDuration;
        Vector3 leapFrom, leapTo;
        Vector3 dashDirection;
        bool dashHit;

        float bossTimer = 4f;
        int bossPattern;

        // ---------- Создание ----------

        public static Enemy Create(BrainrotId id, Vector3 position, bool boss, float health, float damageMultiplier)
        {
            var info = Brainrots.Get(id);
            float size = boss ? info.enemySize * 2.6f : info.enemySize * UnityEngine.Random.Range(0.92f, 1.08f);
            bool flyer = info.behavior == EnemyBehavior.Flyer;

            var go = new GameObject((boss ? "Boss_" : "Enemy_") + id);
            go.layer = Combat.EnemyLayer;

            Vector3 spawnPosition = position;
            if (!flyer && NavMesh.SamplePosition(position, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
                spawnPosition = navHit.position;
            if (flyer) spawnPosition.y = boss ? 9f : 6.5f;
            go.transform.position = spawnPosition;

            var hp = go.AddComponent<Health>();
            hp.SetMax(health, true); // Awake уже выставил 100, поэтому задаём максимум и заполняем заново

            var collider = go.AddComponent<CapsuleCollider>();
            collider.radius = 0.6f * size;
            collider.height = 2.2f * size;
            collider.center = new Vector3(0f, (flyer ? 1.3f : 1.1f) * size, 0f);
            if (flyer)
            {
                collider.direction = 2; // вдоль Z
                collider.height = 3f * size;
                collider.radius = 0.7f * size;
            }

            var enemy = go.AddComponent<Enemy>();
            enemy.Info = info;
            enemy.IsBoss = boss;
            enemy.Size = size;
            enemy.Health = hp;
            enemy.speed = info.enemySpeed * (boss ? 0.75f : 1f);
            enemy.damage = info.enemyDamage * damageMultiplier * (boss ? 1.6f : 1f);
            enemy.attackRange = 1.4f + size * 0.7f;
            enemy.specialCooldown = UnityEngine.Random.Range(1.5f, 3.5f);
            enemy.attackCooldown = 0.8f;
            enemy.hoverAngle = UnityEngine.Random.value * 360f;

            var modelRoot = new GameObject("ModelRoot").transform;
            modelRoot.SetParent(go.transform, false);
            modelRoot.localScale = Vector3.one * size;
            enemy.rig = BrainrotModels.Build(id, modelRoot);
            enemy.modelRoot = modelRoot;
            modelRoot.localScale = Vector3.one * 0.01f;

            if (!flyer)
            {
                var agent = go.AddComponent<NavMeshAgent>();
                agent.speed = enemy.speed;
                agent.angularSpeed = 600f;
                agent.acceleration = 40f;
                agent.radius = Mathf.Min(0.45f * size, 1.2f);
                agent.height = 2f * size;
                agent.stoppingDistance = enemy.attackRange * 0.75f;
                agent.autoBraking = true;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.avoidancePriority = boss ? 10 : UnityEngine.Random.Range(30, 70);
                enemy.agent = agent;
                if (agent.isOnNavMesh) agent.Warp(spawnPosition);
            }

            FX.Burst(spawnPosition + Vector3.up * 0.5f, GameAssets.Hex("#7E57C2"), 4, 5f, 0.25f, 0.6f);
            Particles.Sparkle(spawnPosition + Vector3.up * 0.8f, GameAssets.Hex("#B388FF"), 12, 3f, 0.2f);
            if (!flyer) Particles.Dust(spawnPosition, 0.8f * size, 8, GameAssets.Hex("#8D6E63"));
            return enemy;
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void Start()
        {
            Health.Died += _ => Die();
        }

        // ---------- Получение урона ----------

        public void TakeHit(float amount, bool crit, Vector3 point)
        {
            if (IsDead) return;
            Vector3 from = PlayerController.Instance != null ? PlayerController.Instance.transform.position : point - transform.forward;
            lastHitDirection = transform.position - from;
            if (Health.TakeDamage(amount, crit))
            {
                rig.Hurt(lastHitDirection);
                FX.DamageNumber(point, amount, crit);
                Vector3 normal = (from + Vector3.up * 1.5f - point).normalized;
                Particles.Sparks(point, normal, Info.themeColor, crit ? 10 : 5, crit ? 9f : 6f);
                if (crit)
                {
                    Particles.Flash(point, new Color(1f, 0.9f, 0.3f), 1.2f, 0.1f);
                    FX.Cfx("CFXR3 Hit Light B (Air)", point, 0.6f);
                }
            }
        }

        /// <summary>Рык босса для заставки.</summary>
        public void Roar(float duration)
        {
            if (rig != null) rig.Roar(duration);
        }

        public void Knockback(Vector3 force)
        {
            knockback += force / (IsBoss ? 5f : 1f);
        }

        public void Root(float duration, float damagePerSecond)
        {
            if (IsDead) return;
            rootTimer = Mathf.Max(rootTimer, IsBoss ? duration * 0.4f : duration);
            rootDamagePerSecond = damagePerSecond;
            if (rootVisual == null)
            {
                rootVisual = new GameObject("Roots");
                rootVisual.transform.SetParent(transform, false);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 72f;
                    var q = Quaternion.Euler(0f, a, 0f);
                    BrainrotModels.Part(rootVisual.transform, PrimitiveType.Capsule, q * new Vector3(0f, 0f, 0.55f * Size) + Vector3.up * 0.5f * Size,
                        new Vector3(0.15f, 0.6f, 0.15f) * Size, GameAssets.Hex("#5D4037"), new Vector3(-20f, a, 0f), 0.2f, false);
                }
            }
        }

        void Die()
        {
            Active.Remove(this);
            if (rootVisual != null) Destroy(rootVisual);
            Particles.Sparkle(transform.position + Vector3.up * Size, Color.white, IsBoss ? 20 : 6, 5f, 0.18f);
            DeathAnimation.Play(this, modelRoot, rig, lastHitDirection);
            SoundFX.Play(SoundFX.Sfx.EnemyDie, IsBoss ? 1f : 0.55f, 0.15f);
            if (IsBoss) CameraShake.Add(0.5f);

            int coins = IsBoss ? 40 + RunState.arenaIndex * 10 : Info.coins;
            Vector3 dropPosition = transform.position + Vector3.up * (Info.behavior == EnemyBehavior.Flyer ? 0f : 1f);
            if (Info.behavior == EnemyBehavior.Flyer) dropPosition.y = Mathf.Min(dropPosition.y, 3f);
            for (int i = 0; i < Mathf.Min(coins, 25); i++)
                Pickup.Spawn(Pickup.Kind.Coin, dropPosition, coins > 25 && i == 0 ? coins - 24 : 1);
            if (IsBoss || UnityEngine.Random.value < 0.07f)
                Pickup.Spawn(Pickup.Kind.Health, dropPosition);

            if (RunState.stats.healPerKill > 0f && PlayerController.Instance != null)
                PlayerController.Instance.Health.Heal(RunState.stats.healPerKill);

            RunState.kills++;
            AnyDied?.Invoke(this);
            Destroy(gameObject);
        }

        // ---------- Поведение ----------

        void Update()
        {
            if (IsDead) return;
            float dt = Time.deltaTime * TimeScale;
            rig.timeScale = TimeScale;

            // Появление из-под земли
            if (spawnTimer > 0f)
            {
                spawnTimer -= Time.deltaTime;
                float s = 1f - Mathf.Clamp01(spawnTimer / 0.45f);
                modelRoot.localScale = Vector3.one * Size * Mathf.Max(0.01f, 1f - Mathf.Pow(1f - s, 3f));
                return;
            }

            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                rig.moveAmount = 0f;
                return;
            }

            attackCooldown -= dt;
            specialCooldown -= dt;

            if (rootTimer > 0f)
            {
                rootTimer -= Time.deltaTime;
                Health.TakeDamage(rootDamagePerSecond * Time.deltaTime);
                if (IsDead) return;
                if (rootTimer <= 0f && rootVisual != null) Destroy(rootVisual);
            }

            Vector3 playerPosition = player.transform.position;
            Vector3 toPlayer = playerPosition - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            if (Info.behavior == EnemyBehavior.Flyer)
            {
                UpdateFlyer(dt, player, toPlayer, distance);
            }
            else
            {
                UpdateGround(dt, player, toPlayer, distance);
            }

            if (IsBoss) UpdateBoss(dt, player, distance);
        }

        bool Rooted => rootTimer > 0f;

        void UpdateGround(float dt, PlayerController player, Vector3 toPlayer, float distance)
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                if (agent != null && state != State.Leap && NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                if (state != State.Leap) return;
            }

            if (knockback.sqrMagnitude > 0.01f && agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(knockback * dt);
                knockback = Vector3.MoveTowards(knockback, Vector3.zero, 40f * dt);
            }

            switch (state)
            {
                case State.Move:
                    UpdateMove(dt, player, toPlayer, distance);
                    break;

                case State.Windup:
                    agent.isStopped = true;
                    FaceTowards(toPlayer, dt * 3f);
                    stateTimer -= dt;
                    rig.SetWindup(1f - Mathf.Clamp01(stateTimer / stateDuration));
                    if (stateTimer <= 0f)
                    {
                        rig.SetWindup(0f);
                        ExecuteSpecial(player, toPlayer, distance);
                    }
                    break;

                case State.Leap:
                    stateTimer += dt;
                    float t = Mathf.Clamp01(stateTimer / stateDuration);
                    Vector3 p = Vector3.Lerp(leapFrom, leapTo, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * (IsBoss ? 7f : 4f);
                    transform.position = p;
                    if (t >= 1f) Land(player);
                    break;

                case State.Dash:
                    stateTimer -= dt;
                    agent.Move(dashDirection * (IsBoss ? 26f : 20f) * dt);
                    rig.moveAmount = 1f;
                    if (Time.frameCount % 2 == 0) Particles.Dust(transform.position, 0.4f * Size, 1, GameAssets.Hex("#BCAAA4"));
                    if (!dashHit && Vector3.Distance(transform.position, player.transform.position) < 1.6f + Size * 0.5f)
                    {
                        dashHit = true;
                        player.Health.TakeDamageFrom(damage * 1.3f, transform.position);
                        CameraShake.Add(0.3f);
                    }
                    if (stateTimer <= 0f) state = State.Move;
                    break;
            }
        }

        void UpdateMove(float dt, PlayerController player, Vector3 toPlayer, float distance)
        {
            agent.speed = speed * TimeScale;
            agent.isStopped = Rooted;
            rig.moveAmount = Rooted ? 0f : Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(0.1f, speed * TimeScale));
            rig.animSpeed = speed / 4f;

            repathTimer -= dt;
            if (repathTimer <= 0f && !Rooted)
            {
                repathTimer = 0.2f;
                Vector3 destination = player.transform.position;
                if (Info.behavior == EnemyBehavior.Thrower && !IsBoss)
                {
                    if (distance < 8f) destination = transform.position - toPlayer.normalized * 6f;
                    else if (distance < 14f) destination = transform.position + Vector3.Cross(Vector3.up, toPlayer.normalized) * Mathf.Sin(Time.time + hoverAngle) * 4f;
                }
                agent.SetDestination(destination);
            }

            if (distance < attackRange * 1.5f) FaceTowards(toPlayer, dt * 6f);
            if (Rooted) return;

            switch (Info.behavior)
            {
                case EnemyBehavior.Leaper:
                    if (specialCooldown <= 0f && distance > 4f && distance < 16f) BeginWindup(Special.Leap, 0.6f);
                    else TryMelee(distance);
                    break;
                case EnemyBehavior.Dasher:
                    if (specialCooldown <= 0f && distance < 11f && distance > 2.5f) BeginWindup(Special.Dash, 0.55f);
                    else TryMelee(distance);
                    break;
                case EnemyBehavior.Thrower:
                    if (attackCooldown <= 0f && distance < 24f && HasLineOfSight(player)) BeginWindup(Special.Throw, 0.35f);
                    else TryMelee(distance);
                    break;
                default:
                    TryMelee(distance);
                    break;
            }
        }

        void TryMelee(float distance)
        {
            if (attackCooldown <= 0f && distance < attackRange) BeginWindup(Special.Melee, IsBoss ? 0.4f : 0.22f);
        }

        void BeginWindup(Special special, float duration)
        {
            state = State.Windup;
            pendingSpecial = special;
            stateTimer = duration;
            stateDuration = duration;
            if (agent.isOnNavMesh) agent.isStopped = true;
        }

        void ExecuteSpecial(PlayerController player, Vector3 toPlayer, float distance)
        {
            state = State.Move;
            if (agent.isOnNavMesh) agent.isStopped = false;
            switch (pendingSpecial)
            {
                case Special.Melee:
                    rig.Attack();
                    attackCooldown = IsBoss ? 1.2f : 1f;
                    if (Vector3.Distance(transform.position, player.transform.position) < attackRange + 0.6f)
                    {
                        player.Health.TakeDamageFrom(damage, transform.position);
                        if (Info.id == BrainrotId.Patapim) player.ApplySlow(0.55f, 1.5f);
                    }
                    break;

                case Special.Leap:
                    Vector3 target = player.transform.position;
                    if (NavMesh.SamplePosition(target, out var hit, 4f, NavMesh.AllAreas)) target = hit.position;
                    leapFrom = transform.position;
                    leapTo = target;
                    agent.enabled = false;
                    state = State.Leap;
                    stateTimer = 0f;
                    stateDuration = IsBoss ? 1f : 0.75f;
                    specialCooldown = UnityEngine.Random.Range(4f, 6.5f);
                    SoundFX.Play(SoundFX.Sfx.Jump, 0.6f);
                    break;

                case Special.Dash:
                    dashDirection = toPlayer.normalized;
                    dashHit = false;
                    state = State.Dash;
                    stateTimer = IsBoss ? 0.5f : 0.4f;
                    specialCooldown = UnityEngine.Random.Range(2.5f, 4f);
                    rig.Attack();
                    SoundFX.Play(SoundFX.Sfx.Dash, 0.5f);
                    break;

                case Special.Throw:
                    rig.Attack();
                    attackCooldown = UnityEngine.Random.Range(1.8f, 2.6f);
                    ThrowAt(player, Info.id == BrainrotId.Chimpanzini ? Projectile.Kind.Banana : Projectile.Kind.Coconut, damage);
                    break;
            }
        }

        void Land(PlayerController player)
        {
            agent.enabled = true;
            agent.Warp(leapTo);
            state = State.Move;
            float radius = IsBoss ? 6f : 3.2f;
            FX.Ring(leapTo, radius, Info.themeColor, 0.4f);
            FX.Burst(leapTo + Vector3.up * 0.2f, GameAssets.Hex("#8D6E63"), 5, 6f, 0.2f, 0.7f);
            Particles.Dust(leapTo, radius * 0.5f, 16, GameAssets.Hex("#A1887F"));
            FX.Cfx("CFXR2 Ground Hit", leapTo, IsBoss ? 2f : 1f);
            CameraShake.AddAt(leapTo, IsBoss ? 0.7f : 0.35f, 20f);
            SoundFX.Play(SoundFX.Sfx.Explosion, IsBoss ? 0.8f : 0.4f, 0.2f);
            if (Vector3.Distance(leapTo, player.transform.position) < radius)
                player.Health.TakeDamageFrom(damage * 1.4f, leapTo);
        }

        void ThrowAt(PlayerController player, Projectile.Kind kind, float dmg, float spreadDegrees = 0f)
        {
            Vector3 start = transform.position + Vector3.up * 1.8f * Size + transform.forward * 0.5f * Size;
            Vector3 target = player.transform.position + Vector3.up * 0.8f;
            float flatDistance = Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(target.x, 0f, target.z));
            float time = Mathf.Clamp(flatDistance / 15f, 0.5f, 1.4f);
            // Небольшое упреждение
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) target += new Vector3(cc.velocity.x, 0f, cc.velocity.z) * time * 0.6f;
            const float gravity = 20f;
            Vector3 velocity = (target - start) / time + Vector3.up * 0.5f * gravity * time;
            if (spreadDegrees != 0f) velocity = Quaternion.Euler(0f, spreadDegrees, 0f) * velocity;
            Projectile.Spawn(start, velocity, gravity, dmg, 0f, false, kind);
            SoundFX.Play(SoundFX.Sfx.Throw, 0.4f, 0.15f);
        }

        void UpdateFlyer(float dt, PlayerController player, Vector3 toPlayer, float distance)
        {
            hoverAngle += dt * (IsBoss ? 25f : 40f);
            float radius = IsBoss ? 9f : 5f;
            Vector3 orbit = Quaternion.Euler(0f, hoverAngle, 0f) * Vector3.forward * radius;
            Vector3 target = player.transform.position + orbit;
            target.y = (IsBoss ? 9f : 6.5f) + Mathf.Sin(Time.time * 1.3f + hoverAngle) * 0.6f;

            float limit = GameManager.ArenaHalfSize - 3f;
            target.x = Mathf.Clamp(target.x, -limit, limit);
            target.z = Mathf.Clamp(target.z, -limit, limit);

            Vector3 velocity = Vector3.zero;
            if (!Rooted)
            {
                velocity = (target - transform.position);
                if (velocity.magnitude > 1f) velocity = velocity.normalized;
                velocity *= speed;
                transform.position += velocity * dt;
            }
            transform.position += knockback * dt * 0.3f;
            knockback = Vector3.MoveTowards(knockback, Vector3.zero, 40f * dt);

            Vector3 look = velocity.sqrMagnitude > 0.5f ? velocity : toPlayer;
            look.y = 0f;
            FaceTowards(look, dt * 3f);
            rig.moveAmount = 1f;

            if (attackCooldown <= 0f && distance < (IsBoss ? 10f : 6f) && !Rooted)
            {
                attackCooldown = IsBoss ? 1.2f : UnityEngine.Random.Range(2f, 3f);
                Vector3 drop = transform.position + Vector3.down * 0.8f * Size;
                var cc = player.GetComponent<CharacterController>();
                Vector3 lead = cc != null ? new Vector3(cc.velocity.x, 0f, cc.velocity.z) * 0.5f : Vector3.zero;
                Vector3 horizontal = (player.transform.position + lead - transform.position);
                horizontal.y = 0f;
                Projectile.Spawn(drop, horizontal * 0.9f, 20f, damage, IsBoss ? 3.5f : 2.8f, false, Projectile.Kind.Bomb);
                rig.Attack();
            }
        }

        void UpdateBoss(float dt, PlayerController player, float distance)
        {
            bossTimer -= dt;
            if (bossTimer > 0f || state != State.Move) return;
            bossTimer = UnityEngine.Random.Range(5f, 7f);
            bossPattern = (bossPattern + 1) % 3;

            switch (bossPattern)
            {
                case 0:
                    // Призыв помощников
                    FX.Ring(transform.position, 5f, Info.themeColor, 0.6f);
                    SoundFX.Play(SoundFX.Sfx.BossRoar, 0.7f);
                    if (GameManager.Instance != null) GameManager.Instance.SummonMinions(Info.id, transform.position, 3);
                    break;
                case 1:
                    // Кольцо из шаров
                    SoundFX.Play(SoundFX.Sfx.Ability, 0.7f);
                    int count = 14;
                    Vector3 center = transform.position + Vector3.up * (Info.behavior == EnemyBehavior.Flyer ? 0f : 1.4f);
                    for (int i = 0; i < count; i++)
                    {
                        float a = i * 360f / count + UnityEngine.Random.Range(-5f, 5f);
                        Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                        Vector3 vel = dir * 11f;
                        if (Info.behavior == EnemyBehavior.Flyer) vel += Vector3.down * 3f;
                        Projectile.Spawn(center + dir * Size, vel, Info.behavior == EnemyBehavior.Flyer ? 2f : 0f, damage * 0.6f, 0f, false, Projectile.Kind.Orb, Info.themeColor);
                    }
                    break;
                default:
                    // Фирменная атака
                    switch (Info.behavior)
                    {
                        case EnemyBehavior.Thrower:
                            for (int i = -2; i <= 2; i++) ThrowAt(player, Projectile.Kind.Banana, damage * 0.8f, i * 12f);
                            rig.Attack();
                            break;
                        case EnemyBehavior.Flyer:
                            for (int i = 0; i < 8; i++)
                            {
                                Vector3 p = player.transform.position + new Vector3(UnityEngine.Random.Range(-7f, 7f), 14f + i, UnityEngine.Random.Range(-7f, 7f));
                                Projectile.Spawn(p, Vector3.down * 3f, 20f, damage, 3f, false, Projectile.Kind.Bomb);
                            }
                            break;
                        case EnemyBehavior.Leaper:
                            specialCooldown = 0f;
                            if (agent != null && agent.isOnNavMesh) BeginWindup(Special.Leap, 0.5f);
                            break;
                        default:
                            if (agent != null && agent.isOnNavMesh) BeginWindup(Special.Dash, 0.6f);
                            break;
                    }
                    break;
            }
        }

        bool HasLineOfSight(PlayerController player)
        {
            Vector3 from = transform.position + Vector3.up * 1.6f * Size;
            Vector3 to = player.transform.position + Vector3.up * 1.2f;
            return !Physics.Linecast(from, to, Combat.EnvironmentMask, QueryTriggerInteraction.Ignore);
        }

        void FaceTowards(Vector3 direction, float speedFactor)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Mathf.Clamp01(speedFactor));
        }
    }
}
