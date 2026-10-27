using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BrainrotWaves
{
    /// <summary>
    /// Враг-брейнрот. Поведение зависит от архетипа (бегун, танк, снайпер, камикадзе...).
    /// Элитный враг — крупнее, живучее, с короной. Босс — гигант с фазами ярости и щитом.
    /// В режиме защиты враги идут к пиццерии, а на игрока отвлекаются, только если он рядом или стреляет в них.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> Active = new List<Enemy>();
        /// <summary>Замедление всех врагов (навыки замедления времени, заставка босса).</summary>
        public static float TimeScale = 1f;
        public static event Action<Enemy> AnyDied;

        enum State { Move, Windup, Leap, Dash }
        enum Special { Melee, Leap, Dash, Throw, Snipe, Explode, Summon, Teleport }

        public BrainrotInfo Info { get; private set; }
        public bool IsBoss { get; private set; }
        public bool IsElite { get; private set; }
        public Health Health { get; private set; }
        public bool IsDead => Health == null || Health.IsDead;
        public float Size { get; private set; }
        /// <summary>Щит босса включён (урон почти не проходит).</summary>
        public bool Shielded => shieldTimer > 0f;
        public int BossPhase => bossPhase;

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
        float freezeTimer;
        GameObject rootVisual;
        GameObject iceVisual;
        Vector3 knockback;
        float spawnTimer = 0.45f;
        float hoverAngle;
        Vector3 lastHitDirection;
        float playerAggro;   // режим защиты: сколько ещё секунд злиться на игрока

        State state = State.Move;
        Special pendingSpecial;
        float stateTimer;
        float stateDuration;
        Vector3 leapFrom, leapTo;
        Vector3 dashDirection;
        float dashSpeed;
        bool dashHit;
        LineRenderer laser;

        // Босс
        float bossTimer = 4f;
        int bossPattern;
        int bossPhase;            // 0, 1 (после 66%), 2 (после 33%)
        float shieldTimer;
        GameObject shieldVisual;
        float rage = 1f;

        // ---------- Создание ----------

        public static Enemy Create(BrainrotId id, Vector3 position, bool boss, float health, float damageMultiplier, bool elite = false)
        {
            var info = Brainrots.Get(id);
            if (boss) elite = false;
            float size = boss ? info.enemySize * 2.6f : info.enemySize * UnityEngine.Random.Range(0.92f, 1.08f) * (elite ? 1.35f : 1f);
            bool flyer = info.behavior == EnemyBehavior.Flyer;

            var go = new GameObject((boss ? "Boss_" : elite ? "Elite_" : "Enemy_") + id);
            go.layer = Combat.EnemyLayer;

            Vector3 spawnPosition = position;
            if (!flyer && NavMesh.SamplePosition(position, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
                spawnPosition = navHit.position;
            if (flyer) spawnPosition.y = boss ? 9f : 6.5f;
            go.transform.position = spawnPosition;

            var hp = go.AddComponent<Health>();
            hp.SetMax(health * (elite ? 3f : 1f), true);

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
            enemy.IsElite = elite;
            enemy.Size = size;
            enemy.Health = hp;
            enemy.speed = info.enemySpeed * (boss ? 0.75f : 1f) * (elite ? 0.9f : 1f);
            enemy.damage = info.enemyDamage * damageMultiplier * (boss ? 1.6f : 1f) * (elite ? 1.3f : 1f);
            enemy.attackRange = 1.4f + size * 0.7f;
            enemy.specialCooldown = UnityEngine.Random.Range(1.5f, 3.5f);
            enemy.attackCooldown = 0.8f;
            enemy.hoverAngle = UnityEngine.Random.value * 360f;

            var modelRoot = new GameObject("ModelRoot").transform;
            modelRoot.SetParent(go.transform, false);
            modelRoot.localScale = Vector3.one * size;
            enemy.rig = BrainrotModels.Build(id, modelRoot);
            enemy.modelRoot = modelRoot;
            if (elite) AddCrown(modelRoot, info);
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
            Particles.Sparkle(spawnPosition + Vector3.up * 0.8f, elite ? UIKit.Accent : GameAssets.Hex("#B388FF"), elite ? 24 : 12, 3f, 0.2f);
            if (!flyer) Particles.Dust(spawnPosition, 0.8f * size, 8, GameAssets.Hex("#8D6E63"));
            return enemy;
        }

        /// <summary>Золотая корона элитного врага.</summary>
        static void AddCrown(Transform modelRoot, BrainrotInfo info)
        {
            float top = info.behavior == EnemyBehavior.Flyer ? 2.3f : 2.6f;
            var crown = BrainrotModels.Pivot(modelRoot, "Crown", new Vector3(0f, top, 0f));
            Color gold = GameAssets.Hex("#FFC93C");
            var band = BrainrotModels.Part(crown, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.6f, 0.1f, 0.6f), gold, default, 0.9f, false);
            band.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(gold, 1.4f);
            for (int i = 0; i < 5; i++)
            {
                var q = Quaternion.Euler(0f, i * 72f, 0f);
                var spike = BrainrotModels.Part(crown, PrimitiveType.Cube, q * new Vector3(0f, 0.18f, 0.26f), new Vector3(0.1f, 0.22f, 0.1f), gold, new Vector3(0f, i * 72f, 45f), 0.9f, false);
                spike.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(gold, 1.4f);
            }
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void Start()
        {
            Health.Died += _ => Die();
            Health.Changed += OnHealthChanged;
        }

        // ---------- Получение урона ----------

        public void TakeHit(float amount, bool crit, Vector3 point)
        {
            if (IsDead) return;
            Vector3 from = PlayerController.Instance != null ? PlayerController.Instance.transform.position : point - transform.forward;
            lastHitDirection = transform.position - from;
            playerAggro = 4f;
            if (Shielded)
            {
                amount *= 0.1f;
                Particles.Sparks(point, (from - point).normalized, GameAssets.Hex("#80D8FF"), 4, 5f);
            }
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
            float resist = IsBoss ? 5f : Info.behavior == EnemyBehavior.Tank ? 3f : IsElite ? 2f : 1f;
            knockback += force / resist;
        }

        /// <summary>Притяжение (воронка): сила на этот кадр.</summary>
        public void Pull(Vector3 velocity)
        {
            if (IsBoss) velocity *= 0.25f;
            if (Info.behavior == EnemyBehavior.Flyer) { transform.position += velocity * Time.deltaTime * 0.3f; return; }
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Move(velocity * Time.deltaTime);
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

        /// <summary>Заморозка: стоит на месте во льду.</summary>
        public void Freeze(float duration)
        {
            if (IsDead) return;
            freezeTimer = Mathf.Max(freezeTimer, IsBoss ? duration * 0.35f : duration);
            if (iceVisual == null)
            {
                var ice = BrainrotModels.Part(transform, PrimitiveType.Cube, new Vector3(0f, 1.1f * Size, 0f), new Vector3(1.4f, 2.3f, 1.4f) * Size, GameAssets.Hex("#B3E5FC"), new Vector3(0f, 20f, 0f), 0.9f, false);
                ice.GetComponent<Renderer>().sharedMaterial = GameAssets.Transparent(new Color(0.7f, 0.9f, 1f, 0.45f));
                iceVisual = ice.gameObject;
            }
        }

        void Die()
        {
            Active.Remove(this);
            if (rootVisual != null) Destroy(rootVisual);
            if (iceVisual != null) Destroy(iceVisual);
            if (shieldVisual != null) Destroy(shieldVisual);
            if (laser != null) Destroy(laser.gameObject);
            Particles.Sparkle(transform.position + Vector3.up * Size, Color.white, IsBoss ? 20 : 6, 5f, 0.18f);
            DeathAnimation.Play(this, modelRoot, rig, lastHitDirection);
            SoundFX.Play(SoundFX.Sfx.EnemyDie, IsBoss ? 1f : 0.55f, 0.15f);
            if (IsBoss) CameraShake.Add(0.5f);

            int coins = IsBoss ? 50 + RunState.arenaIndex * 10 : Info.coins * (IsElite ? 4 : 1);
            Vector3 dropPosition = transform.position + Vector3.up * (Info.behavior == EnemyBehavior.Flyer ? 0f : 1f);
            if (Info.behavior == EnemyBehavior.Flyer) dropPosition.y = Mathf.Min(dropPosition.y, 3f);
            for (int i = 0; i < Mathf.Min(coins, 25); i++)
                Pickup.Spawn(Pickup.Kind.Coin, dropPosition, coins > 25 && i == 0 ? coins - 24 : 1);
            if (IsBoss || UnityEngine.Random.value < (IsElite ? 0.5f : 0.06f))
                Pickup.Spawn(Pickup.Kind.Health, dropPosition);
            // Временные бонусы (как сундуки в Vampire Survivors): чаще с элитных
            if (IsElite || UnityEngine.Random.value < 0.025f)
                Pickup.Spawn(Pickup.RandomPower(), dropPosition);

            if (RunState.stats.healPerKill > 0f && PlayerController.Instance != null)
                PlayerController.Instance.Health.Heal(RunState.stats.healPerKill);

            RunState.kills++;
            AnyDied?.Invoke(this);
            Destroy(gameObject);
        }

        // ---------- Цель: игрок или защищаемый объект ----------

        Vector3 targetPos;
        bool targetIsPlayer = true;

        void ChooseTarget(PlayerController player)
        {
            var objective = DefenseObjective.Instance;
            targetIsPlayer = true;
            targetPos = player.transform.position;
            if (objective == null || objective.Destroyed) return;
            float toPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (playerAggro > 0f || toPlayer < 7f) return;
            targetIsPlayer = false;
            Vector3 c = objective.Center;
            Vector3 away = transform.position - c;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
            targetPos = new Vector3(c.x, 0f, c.z) + away.normalized * (objective.Radius + 0.6f);
        }

        /// <summary>Нанести урон текущей цели.</summary>
        void HitTarget(PlayerController player, float amount)
        {
            if (targetIsPlayer) player.Health.TakeDamageFrom(amount, transform.position);
            else if (DefenseObjective.Instance != null) DefenseObjective.Instance.Damage(amount);
        }

        float DistanceToTarget()
        {
            Vector3 d = targetPos - transform.position;
            d.y = 0f;
            return d.magnitude;
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
            playerAggro -= dt;

            if (rootTimer > 0f)
            {
                rootTimer -= Time.deltaTime;
                Health.TakeDamage(rootDamagePerSecond * Time.deltaTime);
                if (IsDead) return;
                if (rootTimer <= 0f && rootVisual != null) Destroy(rootVisual);
            }
            if (freezeTimer > 0f)
            {
                freezeTimer -= Time.deltaTime;
                rig.timeScale = 0f;
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                if (freezeTimer <= 0f && iceVisual != null) Destroy(iceVisual);
                else return;
            }

            if (IsBoss) UpdateBossShield(dt);

            ChooseTarget(player);
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (Info.behavior == EnemyBehavior.Flyer) UpdateFlyer(dt, player, toTarget, distance);
            else UpdateGround(dt, player, toTarget, distance);

            if (IsBoss) UpdateBoss(dt, player, distance);
        }

        bool Rooted => rootTimer > 0f;

        void UpdateGround(float dt, PlayerController player, Vector3 toTarget, float distance)
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
                    UpdateMove(dt, player, toTarget, distance);
                    break;

                case State.Windup:
                    agent.isStopped = true;
                    FaceTowards(toTarget, dt * 3f);
                    stateTimer -= dt;
                    rig.SetWindup(1f - Mathf.Clamp01(stateTimer / stateDuration));
                    UpdateTelegraph(player);
                    if (stateTimer <= 0f)
                    {
                        rig.SetWindup(0f);
                        ExecuteSpecial(player, toTarget, distance);
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
                    agent.Move(dashDirection * dashSpeed * dt);
                    rig.moveAmount = 1f;
                    if (Time.frameCount % 2 == 0) Particles.Dust(transform.position, 0.4f * Size, 1, GameAssets.Hex("#BCAAA4"));
                    if (!dashHit && Vector3.Distance(transform.position, targetPos) < 1.6f + Size * 0.5f + (targetIsPlayer ? 0f : 0.8f))
                    {
                        dashHit = true;
                        HitTarget(player, damage * 1.3f);
                        CameraShake.Add(0.3f);
                    }
                    if (stateTimer <= 0f) state = State.Move;
                    break;
            }
        }

        bool KeepsDistance => !IsBoss && (Info.behavior == EnemyBehavior.Thrower || Info.behavior == EnemyBehavior.Sniper || Info.behavior == EnemyBehavior.Summoner);

        void UpdateMove(float dt, PlayerController player, Vector3 toTarget, float distance)
        {
            agent.speed = speed * TimeScale * rage;
            agent.isStopped = Rooted;
            rig.moveAmount = Rooted ? 0f : Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(0.1f, speed * TimeScale));
            rig.animSpeed = speed / 4f;

            repathTimer -= dt;
            if (repathTimer <= 0f && !Rooted)
            {
                repathTimer = 0.2f;
                Vector3 destination = targetPos;
                if (KeepsDistance && targetIsPlayer)
                {
                    float near = Info.behavior == EnemyBehavior.Sniper ? 13f : 8f;
                    float far = Info.behavior == EnemyBehavior.Sniper ? 20f : 14f;
                    if (distance < near) destination = transform.position - toTarget.normalized * 6f;
                    else if (distance < far) destination = transform.position + Vector3.Cross(Vector3.up, toTarget.normalized) * Mathf.Sin(Time.time + hoverAngle) * 4f;
                }
                agent.SetDestination(destination);
            }

            if (distance < attackRange * 1.5f || KeepsDistance) FaceTowards(toTarget, dt * 6f);
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
                case EnemyBehavior.Tank:
                    if (specialCooldown <= 0f && distance < 14f && distance > 4f) BeginWindup(Special.Dash, 0.9f);
                    else TryMelee(distance);
                    break;
                case EnemyBehavior.Thrower:
                    if (attackCooldown <= 0f && distance < 24f && HasLineOfSight()) BeginWindup(Special.Throw, 0.35f);
                    else TryMelee(distance);
                    break;
                case EnemyBehavior.Sniper:
                    if (attackCooldown <= 0f && distance < 30f && HasLineOfSight()) BeginWindup(Special.Snipe, 1.1f);
                    else TryMelee(distance);
                    break;
                case EnemyBehavior.Summoner:
                    if (specialCooldown <= 0f && Active.Count < 26) BeginWindup(Special.Summon, 0.8f);
                    else if (attackCooldown <= 0f && distance < 22f && HasLineOfSight()) BeginWindup(Special.Throw, 0.4f);
                    else TryMelee(distance);
                    break;
                case EnemyBehavior.Exploder:
                    if (distance < 2.4f + Size * 0.5f + (targetIsPlayer ? 0f : 0.8f)) BeginWindup(Special.Explode, 0.55f);
                    break;
                case EnemyBehavior.Teleporter:
                    if (specialCooldown <= 0f && distance > 6f && distance < 30f) BeginWindup(Special.Teleport, 0.5f);
                    else TryMelee(distance);
                    break;
                default:
                    TryMelee(distance);
                    break;
            }
        }

        void TryMelee(float distance)
        {
            if (attackCooldown <= 0f && distance < attackRange + (targetIsPlayer ? 0f : 0.8f)) BeginWindup(Special.Melee, IsBoss ? 0.4f : 0.22f);
        }

        void BeginWindup(Special special, float duration)
        {
            state = State.Windup;
            pendingSpecial = special;
            stateTimer = duration / rage;
            stateDuration = stateTimer;
            if (agent.isOnNavMesh) agent.isStopped = true;
        }

        /// <summary>Подсказка игроку перед атакой: красный лазер снайпера, мигание камикадзе.</summary>
        void UpdateTelegraph(PlayerController player)
        {
            if (pendingSpecial == Special.Snipe)
            {
                if (laser == null)
                {
                    var go = new GameObject("SniperLaser");
                    laser = go.AddComponent<LineRenderer>();
                    laser.positionCount = 2;
                    laser.startWidth = 0.05f;
                    laser.endWidth = 0.05f;
                    laser.sharedMaterial = GameAssets.Glow(new Color(1f, 0.1f, 0.1f), 3f);
                    laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                laser.enabled = true;
                laser.SetPosition(0, Muzzle);
                laser.SetPosition(1, targetIsPlayer ? player.transform.position + Vector3.up * 1.2f : targetPos + Vector3.up);
            }
            else if (pendingSpecial == Special.Explode)
            {
                if (Time.frameCount % 6 < 3) rig.Hurt();
                modelRoot.localScale = Vector3.one * Size * (1f + (1f - stateTimer / stateDuration) * 0.35f);
            }
        }

        Vector3 Muzzle => transform.position + Vector3.up * 1.6f * Size + transform.forward * 0.6f * Size;

        void ExecuteSpecial(PlayerController player, Vector3 toTarget, float distance)
        {
            state = State.Move;
            if (laser != null) laser.enabled = false;
            if (agent.isOnNavMesh) agent.isStopped = false;
            switch (pendingSpecial)
            {
                case Special.Melee:
                    rig.Attack();
                    attackCooldown = (IsBoss ? 1.2f : 1f) / rage;
                    if (DistanceToTarget() < attackRange + 0.6f + (targetIsPlayer ? 0f : 0.8f))
                    {
                        HitTarget(player, damage);
                        if (targetIsPlayer && (Info.id == BrainrotId.Patapim || Info.id == BrainrotId.FrigoCamelo)) player.ApplySlow(0.55f, 1.5f);
                    }
                    break;

                case Special.Leap:
                    Vector3 target = targetPos;
                    if (NavMesh.SamplePosition(target, out var hit, 4f, NavMesh.AllAreas)) target = hit.position;
                    leapFrom = transform.position;
                    leapTo = target;
                    agent.enabled = false;
                    state = State.Leap;
                    stateTimer = 0f;
                    stateDuration = IsBoss ? 1f : 0.75f;
                    specialCooldown = UnityEngine.Random.Range(4f, 6.5f) / rage;
                    SoundFX.Play(SoundFX.Sfx.Jump, 0.6f);
                    break;

                case Special.Dash:
                    dashDirection = toTarget.normalized;
                    dashHit = false;
                    state = State.Dash;
                    bool tank = Info.behavior == EnemyBehavior.Tank;
                    dashSpeed = IsBoss ? 26f : tank ? 16f : 20f;
                    stateTimer = IsBoss ? 0.5f : tank ? 0.8f : 0.4f;
                    specialCooldown = (tank ? UnityEngine.Random.Range(5f, 7f) : UnityEngine.Random.Range(2.5f, 4f)) / rage;
                    rig.Attack();
                    SoundFX.Play(SoundFX.Sfx.Dash, 0.5f);
                    break;

                case Special.Throw:
                    rig.Attack();
                    attackCooldown = UnityEngine.Random.Range(1.8f, 2.6f) / rage;
                    ThrowAt(player, Info.id == BrainrotId.Chimpanzini || Info.id == BrainrotId.Orangutini ? Projectile.Kind.Banana : Projectile.Kind.Coconut, damage);
                    break;

                case Special.Snipe:
                {
                    rig.Attack();
                    attackCooldown = UnityEngine.Random.Range(2.6f, 3.4f) / rage;
                    Vector3 aim = (targetIsPlayer ? player.transform.position + Vector3.up * 1.1f : targetPos + Vector3.up) - Muzzle;
                    Projectile.Spawn(Muzzle, aim.normalized * 38f, 0f, damage * 1.4f, 0f, false, Projectile.Kind.Orb, Info.id == BrainrotId.Blueberrini ? GameAssets.Hex("#3949AB") : new Color(1f, 0.3f, 0.2f));
                    SoundFX.Play(SoundFX.Sfx.Rifle, 0.5f, 0.1f);
                    break;
                }

                case Special.Explode:
                    Combat.Explode(transform.position + Vector3.up * Size, 3.6f * Mathf.Max(1f, Size), damage * 1.6f, false, Info.themeColor);
                    FX.Cfx("CFXR _BOOM_", transform.position + Vector3.up * 2f, 1f);
                    Health.TakeDamage(99999f);
                    break;

                case Special.Summon:
                    specialCooldown = UnityEngine.Random.Range(7f, 10f) / rage;
                    rig.Roar(0.8f);
                    FX.Ring(transform.position, 3f, Info.themeColor, 0.5f);
                    if (GameManager.Instance != null) GameManager.Instance.SummonMinions(BrainrotId.TrippiTroppi, transform.position, IsBoss ? 4 : 2);
                    break;

                case Special.Teleport:
                {
                    specialCooldown = UnityEngine.Random.Range(4f, 6f) / rage;
                    Vector3 side = Quaternion.Euler(0f, UnityEngine.Random.Range(-70f, 70f), 0f) * (-toTarget.normalized);
                    Vector3 dest = targetPos + side * UnityEngine.Random.Range(3f, 5f);
                    if (NavMesh.SamplePosition(dest, out var th, 4f, NavMesh.AllAreas))
                    {
                        FX.Cfx("CFXR Magic Poof", transform.position + Vector3.up, Size);
                        agent.Warp(th.position);
                        FX.Cfx("CFXR Magic Poof", th.position + Vector3.up, Size);
                        SoundFX.Play(SoundFX.Sfx.Dash, 0.5f, 0.2f);
                        attackCooldown = 0.3f;
                    }
                    break;
                }
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
            var objective = DefenseObjective.Instance;
            if (objective != null && !objective.Destroyed && Vector3.Distance(leapTo, objective.Center) < radius + objective.Radius)
                objective.Damage(damage * 1.4f);
        }

        void ThrowAt(PlayerController player, Projectile.Kind kind, float dmg, float spreadDegrees = 0f)
        {
            Vector3 start = transform.position + Vector3.up * 1.8f * Size + transform.forward * 0.5f * Size;
            Vector3 target = (targetIsPlayer ? player.transform.position : targetPos) + Vector3.up * 0.8f;
            float flatDistance = Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(target.x, 0f, target.z));
            float time = Mathf.Clamp(flatDistance / 15f, 0.5f, 1.4f);
            if (targetIsPlayer)
            {
                // Небольшое упреждение
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) target += new Vector3(cc.velocity.x, 0f, cc.velocity.z) * time * 0.6f;
            }
            const float gravity = 20f;
            Vector3 velocity = (target - start) / time + Vector3.up * 0.5f * gravity * time;
            if (spreadDegrees != 0f) velocity = Quaternion.Euler(0f, spreadDegrees, 0f) * velocity;
            Projectile.Spawn(start, velocity, gravity, dmg, 0f, false, kind);
            SoundFX.Play(SoundFX.Sfx.Throw, 0.4f, 0.15f);
        }

        void UpdateFlyer(float dt, PlayerController player, Vector3 toTarget, float distance)
        {
            hoverAngle += dt * (IsBoss ? 25f : 40f);
            float radius = IsBoss ? 9f : 5f;
            Vector3 orbit = Quaternion.Euler(0f, hoverAngle, 0f) * Vector3.forward * radius;
            Vector3 center = targetIsPlayer ? player.transform.position : DefenseObjective.Instance.Center;
            Vector3 target = center + orbit;
            target.y = (IsBoss ? 9f : 6.5f) + Mathf.Sin(Time.time * 1.3f + hoverAngle) * 0.6f;

            float limit = GameManager.ArenaHalfSize - 3f;
            target.x = Mathf.Clamp(target.x, -limit, limit);
            target.z = Mathf.Clamp(target.z, -limit, limit);

            Vector3 velocity = Vector3.zero;
            if (!Rooted)
            {
                velocity = (target - transform.position);
                if (velocity.magnitude > 1f) velocity = velocity.normalized;
                velocity *= speed * rage;
                transform.position += velocity * dt;
            }
            transform.position += knockback * dt * 0.3f;
            knockback = Vector3.MoveTowards(knockback, Vector3.zero, 40f * dt);

            Vector3 look = velocity.sqrMagnitude > 0.5f ? velocity : toTarget;
            look.y = 0f;
            FaceTowards(look, dt * 3f);
            rig.moveAmount = 1f;

            Vector3 flat = center - transform.position;
            flat.y = 0f;
            if (attackCooldown <= 0f && flat.magnitude < (IsBoss ? 10f : 6f) && !Rooted)
            {
                attackCooldown = (IsBoss ? 1.2f : UnityEngine.Random.Range(2f, 3f)) / rage;
                Vector3 drop = transform.position + Vector3.down * 0.8f * Size;
                Vector3 lead = Vector3.zero;
                if (targetIsPlayer)
                {
                    var cc = player.GetComponent<CharacterController>();
                    if (cc != null) lead = new Vector3(cc.velocity.x, 0f, cc.velocity.z) * 0.5f;
                }
                Vector3 horizontal = (center + lead - transform.position);
                horizontal.y = 0f;
                Projectile.Spawn(drop, horizontal * 0.9f, 20f, damage, IsBoss ? 3.5f : 2.8f, false, Projectile.Kind.Bomb);
                rig.Attack();
            }
        }

        // ---------- Босс: фазы, щит, ярость ----------

        void OnHealthChanged()
        {
            if (!IsBoss || IsDead) return;
            float f = Health.Fraction;
            int phase = f < 0.34f ? 2 : f < 0.67f ? 1 : 0;
            if (phase > bossPhase)
            {
                bossPhase = phase;
                EnterRage();
            }
        }

        /// <summary>Босс злится: включает щит, зовёт помощников и становится быстрее.</summary>
        void EnterRage()
        {
            rage = 1f + 0.2f * bossPhase;
            shieldTimer = 6f;
            if (shieldVisual == null)
            {
                var s = BrainrotModels.Part(transform, PrimitiveType.Sphere, new Vector3(0f, 1.2f * Size, 0f), Vector3.one * 3.2f * Size, Color.white, default, 0.9f, false);
                s.GetComponent<Renderer>().sharedMaterial = GameAssets.Transparent(new Color(0.5f, 0.85f, 1f, 0.22f));
                shieldVisual = s.gameObject;
            }
            shieldVisual.SetActive(true);
            rig.Roar(1.4f);
            SoundFX.Play(SoundFX.Sfx.BossRoar, 1f, 0f);
            CameraShake.Add(0.5f);
            PostFX.Impact(0.8f);
            FX.Cfx("CFXR2 _WHAM_ 3", transform.position + Vector3.up * 3f * Size, 2f);
            FX.Ring(transform.position, 8f, Info.themeColor, 0.6f);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SummonMinions(Info.id, transform.position, 3 + bossPhase);
                GameManager.Instance.OnBossRage(this);
            }
        }

        void UpdateBossShield(float dt)
        {
            if (shieldTimer <= 0f) return;
            shieldTimer -= Time.deltaTime;
            if (shieldVisual != null) shieldVisual.transform.localScale = Vector3.one * 3.2f * Size * (1f + Mathf.Sin(Time.time * 6f) * 0.03f);
            if (shieldTimer <= 0f && shieldVisual != null)
            {
                shieldVisual.SetActive(false);
                FX.Cfx("CFXR Magic Poof", transform.position + Vector3.up * Size, Size);
            }
        }

        void UpdateBoss(float dt, PlayerController player, float distance)
        {
            bossTimer -= dt;
            if (bossTimer > 0f || state != State.Move) return;
            bossTimer = UnityEngine.Random.Range(4.5f, 6.5f) / rage;
            bossPattern = (bossPattern + 1) % 3;

            switch (bossPattern)
            {
                case 0:
                    // Призыв помощников
                    FX.Ring(transform.position, 5f, Info.themeColor, 0.6f);
                    SoundFX.Play(SoundFX.Sfx.BossRoar, 0.7f);
                    if (GameManager.Instance != null) GameManager.Instance.SummonMinions(Info.id, transform.position, 3 + bossPhase);
                    break;
                case 1:
                {
                    // Кольцо из шаров (в ярости — два кольца)
                    SoundFX.Play(SoundFX.Sfx.Ability, 0.7f);
                    int rings = bossPhase >= 2 ? 2 : 1;
                    for (int ring = 0; ring < rings; ring++)
                    {
                        int count = 14 + bossPhase * 4;
                        Vector3 center = transform.position + Vector3.up * (Info.behavior == EnemyBehavior.Flyer ? 0f : 1.4f + ring * 0.8f);
                        for (int i = 0; i < count; i++)
                        {
                            float a = i * 360f / count + ring * 180f / count + UnityEngine.Random.Range(-5f, 5f);
                            Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                            Vector3 vel = dir * (11f + ring * 3f);
                            if (Info.behavior == EnemyBehavior.Flyer) vel += Vector3.down * 3f;
                            Projectile.Spawn(center + dir * Size, vel, Info.behavior == EnemyBehavior.Flyer ? 2f : 0f, damage * 0.6f, 0f, false, Projectile.Kind.Orb, Info.themeColor);
                        }
                    }
                    break;
                }
                default:
                    // Фирменная атака
                    switch (Info.behavior)
                    {
                        case EnemyBehavior.Thrower:
                        case EnemyBehavior.Sniper:
                            for (int i = -2; i <= 2; i++) ThrowAt(player, Projectile.Kind.Banana, damage * 0.8f, i * 12f);
                            rig.Attack();
                            break;
                        case EnemyBehavior.Flyer:
                            for (int i = 0; i < 8 + bossPhase * 3; i++)
                            {
                                Vector3 p = player.transform.position + new Vector3(UnityEngine.Random.Range(-7f, 7f), 14f + i, UnityEngine.Random.Range(-7f, 7f));
                                Projectile.Spawn(p, Vector3.down * 3f, 20f, damage, 3f, false, Projectile.Kind.Bomb);
                            }
                            break;
                        case EnemyBehavior.Leaper:
                            specialCooldown = 0f;
                            if (agent != null && agent.isOnNavMesh) BeginWindup(Special.Leap, 0.5f);
                            break;
                        case EnemyBehavior.Summoner:
                            if (agent != null && agent.isOnNavMesh) BeginWindup(Special.Summon, 0.6f);
                            break;
                        case EnemyBehavior.Teleporter:
                            if (agent != null && agent.isOnNavMesh) BeginWindup(Special.Teleport, 0.4f);
                            break;
                        default:
                            if (agent != null && agent.isOnNavMesh) BeginWindup(Special.Dash, 0.6f);
                            break;
                    }
                    break;
            }
        }

        bool HasLineOfSight()
        {
            if (!targetIsPlayer) return true;
            Vector3 from = transform.position + Vector3.up * 1.6f * Size;
            Vector3 to = targetPos + Vector3.up * 1.2f;
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
