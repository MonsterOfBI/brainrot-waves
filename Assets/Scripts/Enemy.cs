using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Враг: бежит к игроку и бьёт его вблизи. Пока это цветные «колобки»,
    /// позже заменим их на модели брейнротов.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        public float speed = 3.5f;
        public float attackDamage = 10f;
        public float attackRange = 1.8f;
        public float attackCooldown = 1f;
        [Tooltip("Сколько монет даёт (пригодится для магазина)")]
        public int coinReward = 1;

        Transform target;
        Health targetHealth;
        CharacterController controller;
        Health health;
        float nextAttackTime;
        float verticalVelocity;
        Vector3 baseScale;
        float hitPunch;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            baseScale = transform.localScale;
            health.Changed += () => hitPunch = 0.15f;
            health.Died += _ => Destroy(gameObject);
        }

        public void Init(Transform player, float moveSpeed, float maxHealth, float damage)
        {
            target = player;
            targetHealth = player != null ? player.GetComponent<Health>() : null;
            speed = moveSpeed;
            attackDamage = damage;
            health.SetMax(maxHealth);
            hitPunch = 0f;
            nextAttackTime = Time.time + 0.5f;
        }

        void Update()
        {
            // «Подпрыгивание» при попадании
            hitPunch = Mathf.MoveTowards(hitPunch, 0f, Time.deltaTime);
            transform.localScale = baseScale * (1f + hitPunch);

            if (target == null || targetHealth == null || targetHealth.IsDead) return;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), Time.deltaTime * 10f);

            Vector3 move = Vector3.zero;
            if (distance > attackRange * 0.7f) move = toTarget.normalized * speed;

            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - 20f * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);

            if (distance <= attackRange && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                targetHealth.TakeDamage(attackDamage);
            }
        }
    }
}
