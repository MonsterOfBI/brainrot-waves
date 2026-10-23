using System;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Здоровье. Вешается и на игрока, и на врагов.
    /// </summary>
    public class Health : MonoBehaviour
    {
        public float maxHealth = 100f;

        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>Вызывается при любом изменении здоровья.</summary>
        public event Action Changed;
        /// <summary>Вызывается один раз, когда здоровье падает до нуля.</summary>
        public event Action<Health> Died;

        void Awake()
        {
            Current = maxHealth;
        }

        public void SetMax(float value)
        {
            maxHealth = value;
            Current = value;
            Changed?.Invoke();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Changed?.Invoke();
            if (IsDead) Died?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke();
        }
    }
}
