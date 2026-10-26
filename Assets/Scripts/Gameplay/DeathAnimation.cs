using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Анимация смерти врага: модель отделяется от врага и красиво погибает
    /// (падает навзничь, крутится, сплющивается, летун падает штопором, босс взрывается серией).
    /// </summary>
    public class DeathAnimation : MonoBehaviour
    {
        enum Style { FallBack, Spin, Squash, Crash, Boss }

        Style style;
        Transform model;
        BrainrotRig rig;
        Color color;
        float size;
        float age;
        float duration;
        Vector3 axis;
        Vector3 velocity;
        Vector3 startPosition;
        Vector3 startScale;
        Quaternion startRotation;
        float nextBoom;
        bool finished;

        public static void Play(Enemy enemy, Transform modelRoot, BrainrotRig rig, Vector3 hitDirection)
        {
            var go = new GameObject("Death_" + enemy.Info.id);
            go.transform.position = enemy.transform.position;
            go.transform.rotation = enemy.transform.rotation;
            modelRoot.SetParent(go.transform, true);

            var d = go.AddComponent<DeathAnimation>();
            d.model = modelRoot;
            d.rig = rig;
            d.color = enemy.Info.themeColor;
            d.size = enemy.Size;
            d.startPosition = go.transform.position;
            d.startScale = go.transform.localScale;
            d.startRotation = go.transform.rotation;

            if (rig != null)
            {
                rig.moveAmount = 0f;
                rig.timeScale = 0.15f;
                rig.Hurt(hitDirection);
            }

            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude < 0.001f) hitDirection = -enemy.transform.forward;
            hitDirection.Normalize();
            d.axis = Vector3.Cross(Vector3.up, hitDirection);

            if (enemy.IsBoss)
            {
                d.style = Style.Boss;
                d.duration = 2.2f;
                if (rig != null) rig.Roar(2.2f);
            }
            else if (enemy.Info.behavior == EnemyBehavior.Flyer)
            {
                d.style = Style.Crash;
                d.duration = 3f;
                d.velocity = hitDirection * 4f + Vector3.up * 2f;
            }
            else
            {
                float r = Random.value;
                d.style = r < 0.5f ? Style.FallBack : r < 0.8f ? Style.Spin : Style.Squash;
                d.duration = d.style == Style.FallBack ? 0.75f : d.style == Style.Spin ? 0.6f : 0.4f;
                d.velocity = hitDirection * 3f;
            }
        }

        void Update()
        {
            if (finished) return;
            float dt = Time.deltaTime;
            age += dt;
            float t = Mathf.Clamp01(age / duration);
            var tr = transform;

            switch (style)
            {
                case Style.FallBack:
                {
                    // Падает навзничь от удара, подпрыгивает и исчезает
                    float fall = Mathf.Clamp01(age / 0.38f);
                    float angle = 88f * fall * fall;
                    if (fall >= 1f) angle = 88f - Mathf.Sin(Mathf.Clamp01((age - 0.38f) / 0.15f) * Mathf.PI) * 10f;
                    tr.rotation = Quaternion.AngleAxis(angle, axis) * startRotation;
                    velocity = Vector3.MoveTowards(velocity, Vector3.zero, dt * 8f);
                    tr.position += velocity * dt;
                    float shrink = Mathf.Clamp01((t - 0.7f) / 0.3f);
                    tr.localScale = startScale * (1f - shrink * shrink);
                    break;
                }
                case Style.Spin:
                {
                    tr.Rotate(0f, 900f * dt, 0f, Space.World);
                    tr.position = startPosition + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.2f * size + velocity * t * 0.5f;
                    tr.localScale = startScale * (1f - Ease.InOutCubic(t) * 0.9f);
                    break;
                }
                case Style.Squash:
                {
                    float s = Ease.OutCubic(t);
                    tr.localScale = new Vector3(startScale.x * (1f + s * 0.6f), startScale.y * (1f - s * 0.85f), startScale.z * (1f + s * 0.6f));
                    break;
                }
                case Style.Crash:
                {
                    velocity.y -= 14f * dt;
                    tr.position += velocity * dt;
                    tr.Rotate(0f, 0f, 400f * dt, Space.Self);
                    tr.Rotate(0f, 120f * dt, 0f, Space.World);
                    if (Time.frameCount % 3 == 0) Particles.Smoke(tr.position + Vector3.up * size, 0.5f * size, 1, new Color(0.2f, 0.2f, 0.2f, 0.6f));
                    if (Time.frameCount % 2 == 0) Particles.Flash(tr.position + Vector3.up * size, new Color(1f, 0.6f, 0.2f), 0.6f * size, 0.15f);
                    if (tr.position.y <= 0.2f) t = 1f;
                    break;
                }
                case Style.Boss:
                {
                    // Трясётся, раздувается, по телу бегут взрывы
                    Vector3 jitter = Random.insideUnitSphere * 0.15f * size * t;
                    jitter.y = 0f;
                    tr.position = startPosition + jitter;
                    tr.localScale = startScale * (1f + t * 0.15f);
                    nextBoom -= dt;
                    if (nextBoom <= 0f && t < 0.9f)
                    {
                        nextBoom = Mathf.Lerp(0.3f, 0.08f, t);
                        Vector3 p = tr.position + Vector3.up * Random.Range(0.6f, 2.4f) * size + Random.insideUnitSphere * size;
                        Particles.Fireball(p, 0.6f * size, Random.value < 0.5f ? color : new Color(1f, 0.7f, 0.3f));
                        SoundFX.Play(SoundFX.Sfx.Explosion, 0.5f, 0.3f);
                        CameraShake.Add(0.15f);
                    }
                    break;
                }
            }

            if (t >= 1f) Finish();
        }

        void Finish()
        {
            finished = true;
            Vector3 center = transform.position + Vector3.up * size * (style == Style.FallBack ? 0.4f : 1f);
            if (style == Style.Boss)
            {
                FX.Explosion(center, 4f, color);
                FX.Burst(center, color, 30, 14f, 0.5f, 1.6f);
                Particles.Sparkle(center, Color.white, 60, 12f, 0.3f);
                Particles.Sparkle(center, color, 40, 9f, 0.4f);
                CameraShake.Add(0.9f);
                PostFX.Impact(1f);
            }
            else if (style == Style.Crash)
            {
                FX.Explosion(new Vector3(center.x, 0.3f, center.z), 2.2f, new Color(1f, 0.55f, 0.2f));
            }
            else
            {
                FX.Burst(center, color, 6, 6f, 0.2f, 0.8f);
                Particles.Sparkle(center, color, 12, 4f, 0.2f);
                Particles.Smoke(center, 0.6f * size, 4, new Color(0.9f, 0.9f, 0.9f, 0.5f));
            }
            Destroy(gameObject);
        }
    }
}
