using System;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>Здоровье игрока или врага.</summary>
    public class Health : MonoBehaviour
    {
        public float maxHealth = 100f;
        [Tooltip("Множитель получаемого урона (броня)")]
        public float damageTakenMultiplier = 1f;
        [Tooltip("Неуязвимость после удара, сек")]
        public float invulnerabilityTime;
        /// <summary>Временный множитель урона (щит навыка, фаза босса).</summary>
        [System.NonSerialized] public float tempMultiplier = 1f;

        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public float Fraction => maxHealth > 0f ? Current / maxHealth : 0f;

        public event Action Changed;
        /// <summary>Урон (сколько, критический ли).</summary>
        public event Action<float, bool> Damaged;
        public event Action<Health> Died;
        /// <summary>Урон с известным источником (для индикатора направления).</summary>
        public event Action<Vector3> DamagedFrom;

        float invulnerableUntil;

        void Awake()
        {
            Current = maxHealth;
        }

        public void SetMax(float value, bool refill)
        {
            float delta = value - maxHealth;
            maxHealth = value;
            Current = refill ? value : Mathf.Clamp(Current + Mathf.Max(0f, delta), 0f, value);
            Changed?.Invoke();
        }

        public bool TakeDamage(float amount, bool crit = false)
        {
            if (IsDead || amount <= 0f) return false;
            if (Time.time < invulnerableUntil) return false;
            amount *= damageTakenMultiplier * tempMultiplier;
            Current = Mathf.Max(0f, Current - amount);
            if (invulnerabilityTime > 0f) invulnerableUntil = Time.time + invulnerabilityTime;
            Damaged?.Invoke(amount, crit);
            Changed?.Invoke();
            if (IsDead) Died?.Invoke(this);
            return true;
        }

        /// <summary>Урон от источника в точке source (индикатор покажет направление).</summary>
        public bool TakeDamageFrom(float amount, Vector3 source)
        {
            bool hit = TakeDamage(amount);
            if (hit) DamagedFrom?.Invoke(source);
            return hit;
        }

        /// <summary>Воскрешение (улучшение «Второй шанс»).</summary>
        public void Revive(float fraction, float invulnerableSeconds)
        {
            Current = Mathf.Max(1f, maxHealth * fraction);
            invulnerableUntil = Time.time + invulnerableSeconds;
            Changed?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke();
        }

        public void HealFull()
        {
            if (IsDead) return;
            Current = maxHealth;
            Changed?.Invoke();
        }
    }
}
