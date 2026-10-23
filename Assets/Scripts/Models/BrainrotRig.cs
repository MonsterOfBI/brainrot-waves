using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// «Скелет» игрушечной модели: какие части качать при ходьбе, крутить, махать.
    /// Анимация процедурная (считается кодом), без файлов анимаций.
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
        float phase;
        float attackTimer;
        float hurtTimer;
        float windup;
        Vector3 baseScale;
        bool initialized;

        public void Init()
        {
            foreach (var t in legs) restRotations[t] = t.localRotation;
            foreach (var t in arms) restRotations[t] = t.localRotation;
            foreach (var t in wings) restRotations[t] = t.localRotation;
            if (tail != null) restRotations[tail] = tail.localRotation;
            if (head != null) restRotations[head] = head.localRotation;
            if (body != null) bodyRest = body.localPosition;
            baseScale = transform.localScale;
            phase = Random.value * 10f;
            initialized = true;
        }

        public void Attack() => attackTimer = 0.35f;
        public void Hurt() => hurtTimer = 0.15f;
        public void SetWindup(float amount) => windup = amount;

        void Update()
        {
            if (!initialized) return;
            float dt = Time.deltaTime * timeScale;
            phase += dt * (3f + moveAmount * 9f) * animSpeed;
            attackTimer = Mathf.Max(0f, attackTimer - dt);
            hurtTimer = Mathf.Max(0f, hurtTimer - Time.deltaTime);

            float swing = Mathf.Sin(phase) * 35f * moveAmount;
            for (int i = 0; i < legs.Count; i++)
            {
                float side = (i % 2 == 0) ? 1f : -1f;
                legs[i].localRotation = restRotations[legs[i]] * Quaternion.Euler(swing * side, 0f, 0f);
            }

            float attack = attackTimer > 0f ? Mathf.Sin((1f - attackTimer / 0.35f) * Mathf.PI) : 0f;
            for (int i = 0; i < arms.Count; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float armSwing = swing * side * 0.8f + Mathf.Sin(phase * 0.5f) * 4f;
                float strike = -attack * 110f - windup * 60f;
                arms[i].localRotation = restRotations[arms[i]] * Quaternion.Euler(armSwing + strike, 0f, 0f);
            }

            for (int i = 0; i < wings.Count; i++)
            {
                float side = (i % 2 == 0) ? 1f : -1f;
                wings[i].localRotation = restRotations[wings[i]] * Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 1.5f) * 6f * side);
            }

            foreach (var s in spinners) s.Rotate(0f, 0f, 1400f * dt, Space.Self);

            if (tail != null) tail.localRotation = restRotations[tail] * Quaternion.Euler(0f, Mathf.Sin(phase * 1.3f) * 20f, 0f);
            if (head != null) head.localRotation = restRotations[head] * Quaternion.Euler(Mathf.Sin(phase * 0.7f) * 3f, Mathf.Sin(phase * 0.4f) * 6f, 0f);

            if (body != null)
            {
                float bob = flying
                    ? Mathf.Sin(phase * 0.6f) * 0.15f
                    : Mathf.Abs(Mathf.Sin(phase)) * 0.1f * moveAmount + Mathf.Sin(phase * 0.5f) * 0.02f;
                float lean = attack * 18f + windup * -12f;
                body.localPosition = bodyRest + new Vector3(0f, bob - windup * 0.15f, attack * 0.3f);
                body.localRotation = Quaternion.Euler(lean + moveAmount * 6f, 0f, flying ? Mathf.Sin(phase * 0.5f) * 8f : 0f);
            }

            // «Пружинка» при попадании и при замахе
            float punch = hurtTimer > 0f ? hurtTimer / 0.15f : 0f;
            transform.localScale = new Vector3(
                baseScale.x * (1f + punch * 0.15f + windup * 0.1f),
                baseScale.y * (1f - punch * 0.12f - windup * 0.12f),
                baseScale.z * (1f + punch * 0.15f + windup * 0.1f));
        }
    }
}
