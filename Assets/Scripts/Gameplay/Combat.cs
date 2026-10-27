using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>Общие правила боя: слои, взрывы, урон по площади.</summary>
    public static class Combat
    {
        public const int EnemyLayer = 8;
        public const int PlayerLayer = 9;
        public const int EnvironmentMask = 1 << 0;
        public const int EnemyMask = 1 << EnemyLayer;
        public const int ShootMask = EnvironmentMask | EnemyMask;

        static readonly Collider[] overlap = new Collider[64];
        static readonly HashSet<Enemy> hitSet = new HashSet<Enemy>();

        /// <summary>Наносит урон всем врагам в радиусе. Возвращает число задетых.</summary>
        public static int DamageEnemiesInRadius(Vector3 center, float radius, float damage, float knockback = 0f, bool falloff = true)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, overlap, EnemyMask, QueryTriggerInteraction.Ignore);
            hitSet.Clear();
            int hits = 0;
            for (int i = 0; i < count; i++)
            {
                var enemy = overlap[i].GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead || !hitSet.Add(enemy)) continue;
                float d = Vector3.Distance(center, enemy.transform.position);
                float k = falloff ? Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(d / radius)) : 1f;
                enemy.TakeHit(damage * k, false, enemy.transform.position + Vector3.up);
                if (knockback > 0f)
                {
                    Vector3 dir = enemy.transform.position - center;
                    dir.y = 0f;
                    enemy.Knockback(dir.normalized * knockback);
                }
                hits++;
            }
            return hits;
        }

        /// <summary>Взрыв: эффект + урон врагам (если от игрока) или игроку (если от врага).</summary>
        public static void Explode(Vector3 center, float radius, float damage, bool fromPlayer, Color color)
        {
            FX.Explosion(center, radius, color);
            if (fromPlayer)
            {
                DamageEnemiesInRadius(center, radius, damage * RunState.stats.explosionDamageMultiplier, 6f);
            }
            else
            {
                var objective = DefenseObjective.Instance;
                if (objective != null && !objective.Destroyed && Vector3.Distance(center, objective.Center) < radius + objective.Radius)
                    objective.Damage(damage * 0.8f);
                var player = PlayerController.Instance;
                if (player == null) return;
                float d = Vector3.Distance(center, player.transform.position + Vector3.up * 0.9f);
                if (d < radius + 0.4f)
                    player.Health.TakeDamageFrom(damage * Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(d / radius)), center);
            }
        }
    }
}
