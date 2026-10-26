using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// «Скелет» игрушечной модели: какие части качать при ходьбе, крутить, махать.
    /// Анимация процедурная (считается кодом), без файлов анимаций:
    /// походка с покачиванием, замах и удар, реакция на попадание (вспышка + отклонение), рык босса.
    /// </summary>
    public class BrainrotRig : MonoBehaviour
    {
        public Transform body;
        public Transform head;
        public Transform tail;
        public readonly List<Transform> legs = new List<Transform>();
        public readonly List<Transform> arms = new List<Transform>();
        public readonly List<Transform> spinners = new List<Transform>();
        public readonly List<Transform> wings = new List<Transform>();
        public bool flying;

        [Tooltip("0 — стоит, 1 — бежит")]
        public float moveAmount;
        public float animSpeed = 1f;
        public float timeScale = 1f;

        readonly Dictionary<Transform, Quaternion> restRotations = new Dictionary<Transform, Quaternion>();
        Vector3 bodyRest;
        Quaternion bodyRestRotation = Quaternion.identity;
        float phase;
        float attackTimer;
        float hurtTimer;
        float windup;
        float roarTimer;
        float roarDuration = 1f;
        Vector3 hurtAxis;
        Vector3 baseScale;
        bool initialized;
        float smoothMove;

        // Белая вспышка при попадании
        Renderer[] renderers;
        Material[] originalMaterials;
        float flashTimer;
        bool flashing;
        static Material flashMaterial;

        public void Init()
        {
            restRotations.Clear();
            foreach (var t in legs) restRotations[t] = t.localRotation;
            foreach (var t in arms) restRotations[t] = t.localRotation;
            foreach (var t in wings) restRotations[t] = t.localRotation;
            if (tail != null) restRotations[tail] = tail.localRotation;
            if (head != null) restRotations[head] = head.localRotation;
            if (body != null)
            {
                bodyRest = body.localPosition;
                bodyRestRotation = body.localRotation;
            }
            baseScale = transform.localScale;
            phase = Random.value * 10f;

            renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) originalMaterials[i] = renderers[i].sharedMaterial;
            initialized = true;
        }

        public void Attack() => attackTimer = 0.35f;
        public void SetWindup(float amount) => windup = amount;

        public void Hurt() => Hurt(Vector3.zero);

        /// <summary>Попадание: модель «вздрагивает», отклоняется от удара и вспыхивает белым.</summary>
        public void Hurt(Vector3 worldDirection)
        {
            hurtTimer = 0.18f;
            flashTimer = 0.07f;
            if (worldDirection.sqrMagnitude > 0.001f)
            {
                Vector3 local = transform.InverseTransformDirection(worldDirection);
                local.y = 0f;
                hurtAxis = Vector3.Cross(Vector3.up, local.normalized);
            }
            else hurtAxis = Vector3.right;
        }

        /// <summary>Рык (заставка босса): руки вверх, голова назад, тело раздувается.</summary>
        public void Roar(float duration)
        {
            roarTimer = duration;
            roarDuration = Mathf.Max(0.1f, duration);
        }

        public float StepPhase => phase;

        void SetFlash(bool on)
        {
            if (renderers == null || flashing == on) return;
            flashing = on;
            if (on && flashMaterial == null) flashMaterial = GameAssets.Glow(Color.white, 1.4f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].sharedMaterial = on ? flashMaterial : originalMaterials[i];
            }
        }

        void OnDisable() => SetFlash(false);

        void Update()
        {
            if (!initialized) return;
            float dt = Time.deltaTime * timeScale;
            smoothMove = Mathf.MoveTowards(smoothMove, moveAmount, Time.deltaTime * 5f);
            float move = smoothMove;
            phase += dt * (3f + move * 9f) * animSpeed;
            attackTimer = Mathf.Max(0f, attackTimer - dt);
            hurtTimer = Mathf.Max(0f, hurtTimer - Time.deltaTime);
            roarTimer = Mathf.Max(0f, roarTimer - Time.deltaTime);
            flashTimer -= Time.deltaTime;
            SetFlash(flashTimer > 0f);

            float roar = roarTimer > 0f ? Mathf.Sin(Mathf.Clamp01(1f - roarTimer / roarDuration) * Mathf.PI) : 0f;
            float roarShake = roar > 0f ? Mathf.Sin(Time.time * 50f) * roar : 0f;

            // Ноги: шаг вперёд-назад + подъём колена
            float swing = Mathf.Sin(phase) * 38f * move;
            for (int i = 0; i < legs.Count; i++)
            {
                float side = (i % 2 == 0) ? 1f : -1f;
                legs[i].localRotation = restRotations[legs[i]] * Quaternion.Euler(swing * side, 0f, 0f);
            }

            // Руки: размахиваем в противофазе, замах и удар, рык — вверх
            float attack = attackTimer > 0f ? Mathf.Sin((1f - attackTimer / 0.35f) * Mathf.PI) : 0f;
            for (int i = 0; i < arms.Count; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float armSwing = swing * side * 0.8f + Mathf.Sin(phase * 0.5f) * 4f;
                float strike = -attack * 110f - windup * 60f - roar * 140f;
                arms[i].localRotation = restRotations[arms[i]] * Quaternion.Euler(armSwing + strike, 0f, roar * 25f * side);
            }

            for (int i = 0; i < wings.Count; i++)
            {
                float side = (i % 2 == 0) ? 1f : -1f;
                float flap = Mathf.Sin(phase * 1.5f) * 6f + roar * 20f;
                wings[i].localRotation = restRotations[wings[i]] * Quaternion.Euler(0f, 0f, flap * side);
            }

            foreach (var s in spinners) s.Rotate(0f, 0f, 1400f * dt, Space.Self);

            if (tail != null) tail.localRotation = restRotations[tail] * Quaternion.Euler(0f, Mathf.Sin(phase * 1.3f) * (20f + move * 15f), 0f);
            if (head != null)
            {
                float nod = Mathf.Sin(phase * 2f) * 4f * move;
                head.localRotation = restRotations[head] * Quaternion.Euler(Mathf.Sin(phase * 0.7f) * 3f - nod - roar * 30f, Mathf.Sin(phase * 0.4f) * 6f + roarShake * 6f, 0f);
            }

            float punch = hurtTimer > 0f ? hurtTimer / 0.18f : 0f;
            if (body != null)
            {
                // Покачивание из стороны в сторону и подпрыгивание на каждом шаге
                float step = Mathf.Abs(Mathf.Sin(phase));
                float bob = flying
                    ? Mathf.Sin(phase * 0.6f) * 0.15f
                    : step * 0.12f * move + Mathf.Sin(phase * 0.5f) * 0.02f;
                float roll = flying ? Mathf.Sin(phase * 0.5f) * 8f : Mathf.Cos(phase) * 6f * move;
                float lean = attack * 18f + windup * -12f + move * 7f - roar * 12f;
                body.localPosition = bodyRest + new Vector3(0f, bob - windup * 0.15f + roar * 0.1f, attack * 0.3f);
                Quaternion hurtTilt = Quaternion.AngleAxis(punch * 22f, hurtAxis);
                body.localRotation = hurtTilt * bodyRestRotation * Quaternion.Euler(lean, roarShake * 3f, roll);
            }

            // «Пружинка»: попадание, замах, приземление шага, рык
            float footfall = flying ? 0f : Mathf.Pow(1f - Mathf.Abs(Mathf.Sin(phase)), 6f) * 0.05f * move;
            float stretch = punch * 0.12f + windup * 0.12f + footfall - roar * 0.1f;
            float widen = punch * 0.15f + windup * 0.1f + footfall * 0.5f + roar * 0.12f;
            transform.localScale = new Vector3(baseScale.x * (1f + widen), baseScale.y * (1f - stretch), baseScale.z * (1f + widen));
        }
    }
}
