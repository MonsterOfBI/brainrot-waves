using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrainrotWaves
{
    /// <summary>Особая способность героя на клавишу Q.</summary>
    public class HeroAbility : MonoBehaviour
    {
        public BrainrotInfo hero;
        public Camera cam;

        float cooldownLeft;
        float slowTimeLeft;
        readonly HashSet<Enemy> dashHits = new HashSet<Enemy>();
        static readonly Collider[] buffer = new Collider[32];

        public float Cooldown => hero.abilityCooldown * RunState.stats.abilityCooldownMultiplier;
        public float CooldownFraction => Cooldown > 0f ? Mathf.Clamp01(cooldownLeft / Cooldown) : 0f;
        public bool Ready => cooldownLeft <= 0f;
        public float SlowTimeLeft => slowTimeLeft;

        public event System.Action Activated;

        public static bool SlowTimeActive => Enemy.TimeScale < 0.99f;

        void OnDisable()
        {
            Enemy.TimeScale = 1f;
        }

        void Update()
        {
            if (Time.timeScale == 0f) return;
            cooldownLeft = Mathf.Max(0f, cooldownLeft - Time.deltaTime);

            if (slowTimeLeft > 0f)
            {
                slowTimeLeft -= Time.deltaTime;
                if (slowTimeLeft <= 0f) Enemy.TimeScale = 1f;
            }

            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead) return;

            if (player.IsDashing && hero.id == BrainrotId.Tralalero) DashDamage(player.transform.position + Vector3.up);

            if (!player.InputEnabled || !PlayerController.IsCursorLocked) return;
            if (Controls.Down(GameAction.Ability))
            {
                if (Ready) Activate(player);
                else SoundFX.Play(SoundFX.Sfx.Empty, 0.4f);
            }
        }

        public void ResetCooldown() => cooldownLeft = 0f;

        void Activate(PlayerController player)
        {
            cooldownLeft = Cooldown;
            SoundFX.Play(SoundFX.Sfx.Ability, 0.8f);
            Particles.Sparkle(player.transform.position + Vector3.up, hero.themeColor, 20, 5f, 0.25f);
            Activated?.Invoke();
            float dmg = RunState.stats.damageMultiplier;
            Vector3 forward = cam.transform.forward;
            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;
            Vector3 feet = player.transform.position;

            switch (hero.id)
            {
                case BrainrotId.Tralalero:
                    dashHits.Clear();
                    player.Dash(flatForward, 26f, 0.32f);
                    SoundFX.Play(SoundFX.Sfx.Dash, 0.8f);
                    break;

                case BrainrotId.Bombardiro:
                    StartCoroutine(CarpetBomb(feet + flatForward * 9f, flatForward, dmg));
                    break;

                case BrainrotId.TungTung:
                    FX.Ring(feet, 8f, hero.themeColor, 0.5f);
                    FX.Burst(feet + Vector3.up * 0.2f, GameAssets.Hex("#8D6E63"), 8, 9f, 0.25f, 0.9f);
                    Particles.Dust(feet, 3f, 30, GameAssets.Hex("#A1887F"));
                    FX.Cfx("CFXR2 Ground Hit", feet, 2.5f);
                    FX.Cfx("CFXR _POW_", feet + Vector3.up * 2f, 1.3f);
                    CameraShake.Add(0.6f);
                    SoundFX.Play(SoundFX.Sfx.Explosion, 0.8f);
                    Combat.DamageEnemiesInRadius(feet, 8f, 90f * dmg, 18f);
                    break;

                case BrainrotId.Patapim:
                    FX.Ring(feet, 14f, hero.themeColor, 0.7f);
                    int count = Physics.OverlapSphereNonAlloc(feet, 14f, buffer, Combat.EnemyMask, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < count; i++)
                    {
                        var enemy = buffer[i].GetComponentInParent<Enemy>();
                        if (enemy != null) enemy.Root(4f, 18f * dmg);
                    }
                    break;

                case BrainrotId.Cappuccino:
                    Enemy.TimeScale = 0.3f;
                    slowTimeLeft = 5f;
                    FX.Ring(feet, 20f, hero.themeColor, 0.8f);
                    break;

                case BrainrotId.Chimpanzini:
                    Vector3 start = cam.transform.position + forward * 0.8f;
                    Projectile.Spawn(start, forward * 20f + Vector3.up * 5f, 20f, 100f * dmg, 5f, true, Projectile.Kind.Banana);
                    SoundFX.Play(SoundFX.Sfx.Throw, 0.8f);
                    break;
            }
        }

        IEnumerator CarpetBomb(Vector3 center, Vector3 forward, float dmg)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int i = 0; i < 12; i++)
            {
                Vector3 target = center + forward * Random.Range(-5f, 6f) + right * Random.Range(-5f, 5f);
                Projectile.Spawn(target + Vector3.up * 14f, Vector3.down * 4f, 25f, 55f * dmg, 3.5f, true, Projectile.Kind.Bomb);
                yield return new WaitForSeconds(0.08f);
            }
        }

        void DashDamage(Vector3 position)
        {
            int count = Physics.OverlapSphereNonAlloc(position, 2.2f, buffer, Combat.EnemyMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var enemy = buffer[i].GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead || !dashHits.Add(enemy)) continue;
                enemy.TakeHit(45f * RunState.stats.damageMultiplier, false, enemy.transform.position + Vector3.up);
                enemy.Knockback((enemy.transform.position - position).normalized * 10f);
            }
        }
    }
}
