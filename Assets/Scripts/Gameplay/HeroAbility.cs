using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Навыки героя: Q и E (как в Доте — у каждого героя свои).
    /// Навык описывается данными (AbilityDef), а здесь собраны 19 типов эффектов.
    /// </summary>
    public class HeroAbility : MonoBehaviour
    {
        public BrainrotInfo hero;
        public Camera cam;

        readonly float[] cooldownLeft = new float[2];
        float slowTimeLeft;
        float shieldLeft;
        float rageLeft;
        float whirlLeft;
        float whirlTick;
        AbilityDef whirlDef;
        GameObject shieldVisual;
        Transform whirlVisual;
        readonly HashSet<Enemy> dashHits = new HashSet<Enemy>();
        float dashDamage;
        static readonly Collider[] buffer = new Collider[48];

        /// <summary>Временные усиления от навыков (читает оружие и игрок).</summary>
        public static float FireRateBoost = 1f;
        public static float DamageBoost = 1f;
        public static float SpeedBoost = 1f;

        public static bool SlowTimeActive => Enemy.TimeScale < 0.99f && Enemy.TimeScale > 0f;
        public event System.Action Activated;

        public AbilityDef Def(int slot) => slot == 0 ? hero.q : hero.e;
        public float Cooldown(int slot) => Def(slot).cooldown * RunState.stats.abilityCooldownMultiplier;
        public float Fraction(int slot) => Mathf.Clamp01(cooldownLeft[slot] / Mathf.Max(0.01f, Cooldown(slot)));
        public bool Ready(int slot) => cooldownLeft[slot] <= 0f;
        public float CooldownFraction => Fraction(0);

        static float Dmg => RunState.stats.damageMultiplier * DamageBoost;

        void OnDisable()
        {
            if (slowTimeLeft > 0f) Enemy.TimeScale = 1f;
            FireRateBoost = DamageBoost = SpeedBoost = 1f;
        }

        void OnDestroy()
        {
            FireRateBoost = DamageBoost = SpeedBoost = 1f;
        }

        /// <summary>Бонус «Ярость» с поля.</summary>
        public void GrantRage(float seconds)
        {
            rageLeft = Mathf.Max(rageLeft, seconds);
            FireRateBoost = Mathf.Max(FireRateBoost, 1.5f);
            DamageBoost = Mathf.Max(DamageBoost, 1.3f);
        }

        /// <summary>Бонус «Щит» с поля.</summary>
        public void GrantShield(float seconds)
        {
            Run(new AbilityDef { type = AbilityType.Shield, duration = seconds, power = 1.1f }, PlayerController.Instance);
        }

        public void ResetCooldown()
        {
            cooldownLeft[0] = cooldownLeft[1] = 0f;
        }

        void Update()
        {
            if (Time.timeScale == 0f) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < 2; i++) cooldownLeft[i] = Mathf.Max(0f, cooldownLeft[i] - dt);

            if (slowTimeLeft > 0f)
            {
                slowTimeLeft -= dt;
                if (slowTimeLeft <= 0f && Enemy.TimeScale > 0f) Enemy.TimeScale = 1f;
            }

            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead) return;

            UpdateBuffs(player, dt);
            if (player.IsDashing) DashDamage(player.transform.position + Vector3.up);

            if (!player.InputEnabled || !PlayerController.IsCursorLocked) return;
            if (Controls.Down(GameAction.Ability)) TryActivate(player, 0);
            if (Controls.Down(GameAction.Ability2)) TryActivate(player, 1);
        }

        void TryActivate(PlayerController player, int slot)
        {
            if (Ready(slot)) ActivateSlot(player, slot);
            else SoundFX.Play(SoundFX.Sfx.Empty, 0.4f);
        }

        /// <summary>Для автопроверки: навык Q.</summary>
        void Activate(PlayerController player) => ActivateSlot(player, 0);

        public void ActivateSlot(PlayerController player, int slot)
        {
            var def = Def(slot);
            if (def == null) return;
            cooldownLeft[slot] = Cooldown(slot);
            SoundFX.Play(SoundFX.Sfx.Ability, 0.8f);
            Particles.Sparkle(player.transform.position + Vector3.up, hero.themeColor, 20, 5f, 0.25f);
            Activated?.Invoke();
            Run(def, player);
        }

        // ---------- Эффекты ----------

        Vector3 Forward
        {
            get
            {
                Vector3 f = cam != null ? cam.transform.forward : transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
            }
        }

        /// <summary>Точка на земле, куда смотрит игрок (не дальше maxDistance).</summary>
        Vector3 AimPoint(float maxDistance)
        {
            if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, maxDistance, Combat.ShootMask, QueryTriggerInteraction.Ignore))
                return new Vector3(hit.point.x, 0.05f, hit.point.z);
            var p = transform.position + Forward * maxDistance * 0.6f;
            p.y = 0.05f;
            return p;
        }

        static float Or(float value, float fallback) => value > 0f ? value : fallback;

        void Run(AbilityDef def, PlayerController player)
        {
            Color color = hero.themeColor;
            Vector3 feet = player.transform.position;
            float power = def.power;

            switch (def.type)
            {
                case AbilityType.Dash:
                    dashHits.Clear();
                    dashDamage = 45f * power;
                    player.Dash(Forward, 26f, Or(def.duration, 0.32f));
                    SoundFX.Play(SoundFX.Sfx.Dash, 0.8f);
                    break;

                case AbilityType.Roots:
                {
                    float r = Or(def.radius, 14f);
                    FX.Ring(feet, r, color, 0.7f);
                    int count = Physics.OverlapSphereNonAlloc(feet, r, buffer, Combat.EnemyMask, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < count; i++)
                    {
                        var enemy = buffer[i].GetComponentInParent<Enemy>();
                        if (enemy != null) enemy.Root(Or(def.duration, 4f), 18f * power * Dmg);
                    }
                    break;
                }

                case AbilityType.Grenade:
                {
                    Vector3 forward = cam.transform.forward;
                    Vector3 start = cam.transform.position + forward * 0.8f;
                    Projectile.Spawn(start, forward * 20f + Vector3.up * 5f, 20f, 100f * power * Dmg, Or(def.radius, 5f), true, def.projectile);
                    SoundFX.Play(SoundFX.Sfx.Throw, 0.8f);
                    break;
                }

                case AbilityType.CarpetBomb:
                    StartCoroutine(CarpetBomb(feet + Forward * 9f, Forward, 55f * power * Dmg, def.count > 0 ? def.count : 12, def.projectile == Projectile.Kind.Orb ? Projectile.Kind.Bomb : def.projectile));
                    break;

                case AbilityType.SlowTime:
                    Enemy.TimeScale = def.duration > 0f && def.duration < 4f ? 0.08f : 0.3f;
                    slowTimeLeft = Or(def.duration, 5f);
                    FX.Ring(feet, 20f, color, 0.8f);
                    PostFX.Impact(0.6f);
                    break;

                case AbilityType.Slam:
                {
                    float r = Or(def.radius, 8f);
                    FX.Ring(feet, r, color, 0.5f);
                    FX.Burst(feet + Vector3.up * 0.2f, GameAssets.Hex("#8D6E63"), 8, 9f, 0.25f, 0.9f);
                    Particles.Dust(feet, 3f, 30, GameAssets.Hex("#A1887F"));
                    FX.Cfx("CFXR2 Ground Hit", feet, 2.5f);
                    FX.Cfx("CFXR _POW_", feet + Vector3.up * 2f, 1.3f);
                    CameraShake.Add(0.6f);
                    SoundFX.Play(SoundFX.Sfx.Explosion, 0.8f);
                    Combat.DamageEnemiesInRadius(feet, r, 90f * power * Dmg, 18f);
                    break;
                }

                case AbilityType.Heal:
                    player.Health.Heal(player.Health.maxHealth * 0.4f * power);
                    FX.Ring(feet, 4f, UIKit.Green, 0.6f);
                    Particles.Sparkle(feet + Vector3.up, UIKit.Green, 40, 4f, 0.3f);
                    SoundFX.Play(SoundFX.Sfx.Heal, 1f);
                    break;

                case AbilityType.Shield:
                    shieldLeft = Or(def.duration, 5f);
                    player.Health.tempMultiplier = Mathf.Clamp01(1f - 0.7f * power);
                    if (shieldVisual == null)
                    {
                        var s = BrainrotModels.Part(player.transform, PrimitiveType.Sphere, new Vector3(0f, 1f, 0f), Vector3.one * 2.6f, color, default, 0.9f, false);
                        s.GetComponent<Renderer>().sharedMaterial = GameAssets.Transparent(new Color(color.r, color.g, color.b, 0.18f));
                        shieldVisual = s.gameObject;
                    }
                    shieldVisual.SetActive(true);
                    FX.Ring(feet, 3f, color, 0.5f);
                    break;

                case AbilityType.Nova:
                {
                    int n = def.count > 0 ? def.count : 16;
                    Vector3 center = feet + Vector3.up * 1.1f;
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, i * 360f / n, 0f) * Vector3.forward;
                        Projectile.Spawn(center + dir * 0.8f, dir * 22f + Vector3.up * 1.5f, 3f, 30f * power * Dmg, 0f, true, def.projectile, color);
                    }
                    FX.Ring(feet, 4f, color, 0.4f);
                    SoundFX.Play(SoundFX.Sfx.Throw, 0.9f);
                    break;
                }

                case AbilityType.Chain:
                    StartCoroutine(ChainLightning(def.count > 0 ? def.count : 6, 70f * power * Dmg, color));
                    break;

                case AbilityType.Turret:
                    Turret.Spawn(feet + Forward * 2f, Or(def.duration, 12f), 18f * power, color);
                    break;

                case AbilityType.Blink:
                {
                    float dist = Or(def.radius, 11f);
                    Vector3 from = feet + Vector3.up;
                    Vector3 dir = Forward;
                    if (Physics.SphereCast(from, 0.4f, dir, out var hit, dist, Combat.EnvironmentMask, QueryTriggerInteraction.Ignore))
                        dist = Mathf.Max(0f, hit.distance - 0.6f);
                    FX.Cfx("CFXR Magic Poof", feet + Vector3.up, 1f);
                    Vector3 to = feet + dir * dist;
                    player.MoveTo(to);
                    FX.Ring(to, 4f, color, 0.4f);
                    FX.Cfx("CFXR2 Ground Hit", to, 1.4f);
                    Combat.DamageEnemiesInRadius(to, 4.5f, 60f * power * Dmg, 14f);
                    CameraShake.Add(0.3f);
                    SoundFX.Play(SoundFX.Sfx.Dash, 0.9f);
                    break;
                }

                case AbilityType.Freeze:
                {
                    float r = Or(def.radius, 9f);
                    FX.Ring(feet, r, GameAssets.Hex("#B3E5FC"), 0.6f);
                    Particles.Sparkle(feet + Vector3.up, GameAssets.Hex("#E1F5FE"), 40, 8f, 0.3f);
                    int count = Physics.OverlapSphereNonAlloc(feet, r, buffer, Combat.EnemyMask, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < count; i++)
                    {
                        var enemy = buffer[i].GetComponentInParent<Enemy>();
                        if (enemy == null || enemy.IsDead) continue;
                        enemy.Freeze(Or(def.duration, 3f));
                        enemy.TakeHit(25f * power * Dmg, false, enemy.transform.position + Vector3.up);
                    }
                    SoundFX.Play(SoundFX.Sfx.Ability, 0.7f);
                    break;
                }

                case AbilityType.Vortex:
                    StartCoroutine(Vortex(AimPoint(16f), Or(def.radius, 8f), Or(def.duration, 2.5f), 25f * power * Dmg, color));
                    break;

                case AbilityType.Rage:
                    rageLeft = Or(def.duration, 6f);
                    FireRateBoost = 1.6f * Mathf.Max(1f, power);
                    DamageBoost = 1.3f;
                    FX.Cfx("CFXR3 Hit Light B (Air)", feet + Vector3.up, 1.5f);
                    PostFX.Impact(0.4f);
                    break;

                case AbilityType.Poison:
                    StartCoroutine(PoisonCloud(AimPoint(18f), Or(def.radius, 6f), Or(def.duration, 6f), 30f * power * Dmg, color));
                    break;

                case AbilityType.Beam:
                    Beam(150f * power * Dmg, color);
                    break;

                case AbilityType.Whirlwind:
                    whirlDef = def;
                    whirlLeft = Or(def.duration, 3f);
                    whirlTick = 0f;
                    SpeedBoost = 1.3f;
                    if (whirlVisual == null)
                    {
                        whirlVisual = new GameObject("Whirl").transform;
                        whirlVisual.SetParent(player.transform, false);
                        for (int i = 0; i < 3; i++)
                        {
                            var blade = BrainrotModels.Part(whirlVisual, PrimitiveType.Cube, Quaternion.Euler(0f, i * 120f, 0f) * new Vector3(0f, 0.9f, 2f), new Vector3(0.25f, 0.25f, 1.4f), color, new Vector3(0f, i * 120f, 0f), 0.5f, false);
                            blade.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(color, 1.5f);
                        }
                    }
                    whirlVisual.gameObject.SetActive(true);
                    break;

                case AbilityType.Mines:
                {
                    int n = def.count > 0 ? def.count : 6;
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 p = feet + Quaternion.Euler(0f, i * 360f / n + Random.Range(-15f, 15f), 0f) * Vector3.forward * Random.Range(2.5f, 6f);
                        Mine.Spawn(p, 90f * power * Dmg, color);
                    }
                    SoundFX.Play(SoundFX.Sfx.Throw, 0.8f);
                    break;
                }
            }
        }

        void UpdateBuffs(PlayerController player, float dt)
        {
            if (shieldLeft > 0f)
            {
                shieldLeft -= dt;
                if (shieldVisual != null) shieldVisual.transform.localScale = Vector3.one * (2.6f + Mathf.Sin(Time.time * 8f) * 0.05f);
                if (shieldLeft <= 0f)
                {
                    player.Health.tempMultiplier = 1f;
                    if (shieldVisual != null) shieldVisual.SetActive(false);
                }
            }
            if (rageLeft > 0f)
            {
                rageLeft -= dt;
                if (Time.frameCount % 4 == 0) Particles.Flash(player.transform.position + Vector3.up * 1.2f + Random.insideUnitSphere * 0.6f, new Color(1f, 0.4f, 0.2f), 0.4f, 0.2f);
                if (rageLeft <= 0f) { FireRateBoost = 1f; DamageBoost = 1f; }
            }
            if (whirlLeft > 0f)
            {
                whirlLeft -= dt;
                if (whirlVisual != null) whirlVisual.Rotate(0f, 900f * dt, 0f);
                whirlTick -= dt;
                if (whirlTick <= 0f)
                {
                    whirlTick = 0.25f;
                    float r = Or(whirlDef.radius, 3.8f);
                    Combat.DamageEnemiesInRadius(player.transform.position + Vector3.up, r, 20f * whirlDef.power * Dmg, 6f, false);
                    SoundFX.Play(SoundFX.Sfx.Dash, 0.25f, 0.3f);
                }
                if (whirlLeft <= 0f)
                {
                    SpeedBoost = 1f;
                    if (whirlVisual != null) whirlVisual.gameObject.SetActive(false);
                }
            }
        }

        IEnumerator CarpetBomb(Vector3 center, Vector3 forward, float dmg, int count, Projectile.Kind kind)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int i = 0; i < count; i++)
            {
                Vector3 target = center + forward * Random.Range(-5f, 6f) + right * Random.Range(-5f, 5f);
                Projectile.Spawn(target + Vector3.up * 14f, Vector3.down * 4f, 25f, dmg, 3.5f, true, kind);
                yield return new WaitForSeconds(0.08f);
            }
        }

        IEnumerator ChainLightning(int jumps, float dmg, Color color)
        {
            var hit = new HashSet<Enemy>();
            Vector3 from = cam.transform.position + cam.transform.forward * 0.6f + Vector3.down * 0.3f;
            // Первая цель: ближе всего к прицелу
            Enemy current = null;
            float best = float.MaxValue;
            foreach (var e in Enemy.Active)
            {
                if (e == null || e.IsDead) continue;
                Vector3 to = e.transform.position - cam.transform.position;
                if (to.magnitude > 28f) continue;
                float angle = Vector3.Angle(cam.transform.forward, to);
                float score = angle * 2f + to.magnitude;
                if (angle < 50f && score < best) { best = score; current = e; }
            }
            SoundFX.Play(SoundFX.Sfx.Crit, 0.8f);
            for (int j = 0; j < jumps && current != null; j++)
            {
                hit.Add(current);
                Vector3 to = current.transform.position + Vector3.up * current.Size;
                Lightning.Draw(from, to, color);
                current.TakeHit(dmg, false, to);
                from = to;
                Enemy next = null;
                float nd = 10f;
                Vector3 here = to;
                foreach (var e in Enemy.Active)
                {
                    if (e == null || e.IsDead || hit.Contains(e)) continue;
                    float d = Vector3.Distance(e.transform.position, here);
                    if (d < nd) { nd = d; next = e; }
                }
                current = next;
                yield return new WaitForSeconds(0.07f);
            }
        }

        IEnumerator Vortex(Vector3 center, float radius, float duration, float dps, Color color)
        {
            FX.Ring(center, radius, color, 0.6f);
            float t = 0f;
            float tick = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                tick -= Time.deltaTime;
                if (Time.frameCount % 2 == 0)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 p = center + new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)) * radius;
                    Particles.Sparkle(p, color, 2, 1f, 0.25f);
                }
                foreach (var e in Enemy.Active)
                {
                    if (e == null || e.IsDead) continue;
                    Vector3 d = center - e.transform.position;
                    d.y = 0f;
                    if (d.magnitude > radius) continue;
                    e.Pull(d.normalized * Mathf.Min(d.magnitude * 3f, 14f));
                }
                if (tick <= 0f)
                {
                    tick = 0.25f;
                    Combat.DamageEnemiesInRadius(center, radius, dps * 0.25f, 0f, false);
                }
                yield return null;
            }
            FX.Cfx("CFXR Explosion 1", center, 1.2f);
            Combat.DamageEnemiesInRadius(center, radius * 0.6f, dps * 2.5f, 10f);
            SoundFX.Play(SoundFX.Sfx.Explosion, 0.7f);
        }

        IEnumerator PoisonCloud(Vector3 center, float radius, float duration, float dps, Color color)
        {
            float t = 0f, tick = 0f;
            Color c = Color.Lerp(color, new Color(0.6f, 1f, 0.4f), 0.4f);
            while (t < duration)
            {
                t += Time.deltaTime;
                tick -= Time.deltaTime;
                if (Time.frameCount % 3 == 0)
                    Particles.Smoke(center + Random.insideUnitSphere * radius * 0.6f + Vector3.up * 0.8f, radius * 0.35f, 1, new Color(c.r, c.g, c.b, 0.35f));
                if (tick <= 0f)
                {
                    tick = 0.5f;
                    Combat.DamageEnemiesInRadius(center, radius, dps * 0.5f, 0f, false);
                }
                yield return null;
            }
        }

        void Beam(float dmg, Color color)
        {
            Vector3 origin = cam.transform.position;
            Vector3 dir = cam.transform.forward;
            float length = 40f;
            if (Physics.Raycast(origin, dir, out var wall, length, Combat.EnvironmentMask, QueryTriggerInteraction.Ignore)) length = wall.distance;
            var hits = Physics.SphereCastAll(origin, 0.9f, dir, length, Combat.EnemyMask, QueryTriggerInteraction.Ignore);
            var done = new HashSet<Enemy>();
            foreach (var h in hits)
            {
                var e = h.collider.GetComponentInParent<Enemy>();
                if (e == null || e.IsDead || !done.Add(e)) continue;
                e.TakeHit(dmg, true, h.point == Vector3.zero ? e.transform.position + Vector3.up : h.point);
            }
            Lightning.Beam(origin + dir * 0.6f + Vector3.down * 0.25f, origin + dir * length, color);
            CameraShake.Add(0.25f);
            SoundFX.Play(SoundFX.Sfx.Shotgun, 0.9f);
        }

        void DashDamage(Vector3 position)
        {
            if (dashDamage <= 0f) return;
            int count = Physics.OverlapSphereNonAlloc(position, 2.2f, buffer, Combat.EnemyMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var enemy = buffer[i].GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead || !dashHits.Add(enemy)) continue;
                enemy.TakeHit(dashDamage * Dmg, false, enemy.transform.position + Vector3.up);
                enemy.Knockback((enemy.transform.position - position).normalized * 10f);
            }
        }
    }

    /// <summary>Молния и луч (линии, которые быстро гаснут).</summary>
    public class Lightning : MonoBehaviour
    {
        LineRenderer line;
        float life, age;
        float width;

        public static void Draw(Vector3 a, Vector3 b, Color color)
        {
            var l = Create(color, 0.12f, 0.25f);
            int seg = 8;
            l.line.positionCount = seg + 1;
            for (int i = 0; i <= seg; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)seg);
                if (i > 0 && i < seg) p += Random.insideUnitSphere * 0.35f;
                l.line.SetPosition(i, p);
            }
            Particles.Flash(b, color, 1.2f, 0.15f);
        }

        public static void Beam(Vector3 a, Vector3 b, Color color)
        {
            var l = Create(color, 0.45f, 0.35f);
            l.line.positionCount = 2;
            l.line.SetPosition(0, a);
            l.line.SetPosition(1, b);
            for (int i = 0; i < 12; i++) Particles.Flash(Vector3.Lerp(a, b, i / 11f), color, 0.8f, 0.2f);
        }

        static Lightning Create(Color color, float width, float life)
        {
            var go = new GameObject("Lightning");
            var l = go.AddComponent<Lightning>();
            l.line = go.AddComponent<LineRenderer>();
            l.line.useWorldSpace = true;
            l.line.sharedMaterial = GameAssets.Glow(Color.Lerp(color, Color.white, 0.5f), 3f);
            l.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.width = width;
            l.life = life;
            l.line.startWidth = width;
            l.line.endWidth = width * 0.6f;
            return l;
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(1f - age / life);
            line.startWidth = width * k;
            line.endWidth = width * 0.6f * k;
            if (age >= life) Destroy(gameObject);
        }
    }

    /// <summary>Турель героя: сама стреляет в ближайшего врага.</summary>
    public class Turret : MonoBehaviour
    {
        float life, damage, fireTimer, tracerTime;
        Transform head;
        Transform muzzle;
        Color color;
        LineRenderer tracer;

        public static void Spawn(Vector3 position, float life, float damage, Color color)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) position = hit.position;
            var go = new GameObject("Turret");
            go.transform.position = position;
            var t = go.AddComponent<Turret>();
            t.life = life;
            t.damage = damage;
            t.color = color;
            BrainrotModels.Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.35f, 0.9f), GameAssets.Hex("#455A64"), default, 0.5f);
            t.head = BrainrotModels.Pivot(go.transform, "Head", new Vector3(0f, 0.95f, 0f));
            BrainrotModels.Part(t.head, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.7f, color, default, 0.6f);
            BrainrotModels.Part(t.head, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.5f), new Vector3(0.18f, 0.35f, 0.18f), GameAssets.Hex("#263238"), new Vector3(90f, 0f, 0f), 0.6f);
            t.muzzle = BrainrotModels.Pivot(t.head, "Muzzle", new Vector3(0f, 0f, 0.9f));
            t.tracer = go.AddComponent<LineRenderer>();
            t.tracer.positionCount = 2;
            t.tracer.startWidth = 0.06f;
            t.tracer.endWidth = 0.02f;
            t.tracer.sharedMaterial = GameAssets.Glow(Color.Lerp(color, Color.white, 0.6f), 2.5f);
            t.tracer.enabled = false;
            FX.Cfx("CFXR Magic Poof", position + Vector3.up, 0.8f);
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                FX.Cfx("CFXR Magic Poof", transform.position + Vector3.up, 0.8f);
                Destroy(gameObject);
                return;
            }
            tracerTime -= Time.deltaTime;
            if (tracerTime <= 0f) tracer.enabled = false;

            Enemy target = null;
            float best = 22f;
            foreach (var e in Enemy.Active)
            {
                if (e == null || e.IsDead) continue;
                float d = Vector3.Distance(e.transform.position, transform.position);
                if (d < best) { best = d; target = e; }
            }
            if (target == null) { head.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World); return; }
            Vector3 aim = target.transform.position + Vector3.up * target.Size - head.position;
            head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(aim), Time.deltaTime * 12f);
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                fireTimer = 0.22f;
                Vector3 hitPoint = target.transform.position + Vector3.up * target.Size;
                target.TakeHit(damage * RunState.stats.damageMultiplier, false, hitPoint);
                tracer.SetPosition(0, muzzle.position);
                tracer.SetPosition(1, hitPoint);
                tracer.enabled = true;
                tracerTime = 0.05f;
                Particles.Flash(muzzle.position, color, 0.4f, 0.06f);
                SoundFX.Play(SoundFX.Sfx.Pistol, 0.25f, 0.15f);
            }
        }
    }

    /// <summary>Мина: взрывается, когда рядом враг.</summary>
    public class Mine : MonoBehaviour
    {
        float damage, life = 25f, arm = 0.6f;
        Transform blink;

        public static void Spawn(Vector3 position, float damage, Color color)
        {
            position.y = 0.05f;
            var go = new GameObject("Mine");
            go.transform.position = position;
            var m = go.AddComponent<Mine>();
            m.damage = damage;
            BrainrotModels.Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.6f, 0.08f, 0.6f), GameAssets.Hex("#37474F"), default, 0.6f, false);
            m.blink = BrainrotModels.Part(go.transform, PrimitiveType.Sphere, new Vector3(0f, 0.18f, 0f), Vector3.one * 0.16f, color, default, 0.5f, false);
            m.blink.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(new Color(1f, 0.2f, 0.2f), 3f);
        }

        void Update()
        {
            life -= Time.deltaTime;
            arm -= Time.deltaTime;
            blink.gameObject.SetActive(Mathf.Repeat(Time.time * 3f, 1f) < 0.5f);
            if (life <= 0f) { Destroy(gameObject); return; }
            if (arm > 0f) return;
            foreach (var e in Enemy.Active)
            {
                if (e == null || e.IsDead) continue;
                if (Vector3.Distance(e.transform.position, transform.position) < 2.5f + e.Size * 0.5f)
                {
                    Combat.Explode(transform.position + Vector3.up * 0.3f, 3.5f, damage, true, new Color(1f, 0.55f, 0.2f));
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
