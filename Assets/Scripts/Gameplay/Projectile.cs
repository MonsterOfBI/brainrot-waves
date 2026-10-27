using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>Летящий снаряд: бананы, бомбы, кокосы, магические шары боссов.</summary>
    public class Projectile : MonoBehaviour
    {
        public enum Kind { Banana, Bomb, Coconut, Orb }

        Vector3 velocity;
        float gravity;
        float damage;
        float areaRadius;
        bool fromPlayer;
        Kind kind;
        Color color;
        float life = 8f;
        Vector3 spin;
        bool hitObjective;

        public static Projectile Spawn(Vector3 position, Vector3 velocity, float gravity, float damage, float areaRadius,
            bool fromPlayer, Kind kind, Color? orbColor = null)
        {
            var go = new GameObject("Projectile_" + kind);
            go.transform.position = position;
            var p = go.AddComponent<Projectile>();
            p.velocity = velocity;
            p.gravity = gravity;
            p.damage = damage;
            p.areaRadius = areaRadius;
            p.fromPlayer = fromPlayer;
            p.kind = kind;
            p.color = orbColor ?? Color.white;
            p.BuildVisual();
            return p;
        }

        void BuildVisual()
        {
            var t = transform;
            switch (kind)
            {
                case Kind.Banana:
                    color = GameAssets.Hex("#FFD93B");
                    BrainrotModels.Part(t, PrimitiveType.Capsule, new Vector3(0f, 0.08f, 0f), new Vector3(0.18f, 0.22f, 0.18f), color, new Vector3(0f, 0f, 25f), 0.6f, false);
                    BrainrotModels.Part(t, PrimitiveType.Capsule, new Vector3(0f, -0.12f, 0f), new Vector3(0.17f, 0.2f, 0.17f), color, new Vector3(0f, 0f, -25f), 0.6f, false);
                    BrainrotModels.Part(t, PrimitiveType.Sphere, new Vector3(0.05f, 0.33f, 0f), Vector3.one * 0.07f, GameAssets.Hex("#6B4226"), default, 0.3f, false);
                    spin = new Vector3(0f, 0f, 720f);
                    break;
                case Kind.Bomb:
                    color = GameAssets.Hex("#FF7043");
                    BrainrotModels.Part(t, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.3f, 0.35f, 0.3f), GameAssets.Hex("#3B3B3B"), default, 0.6f, false);
                    BrainrotModels.Part(t, PrimitiveType.Sphere, new Vector3(0f, -0.32f, 0f), Vector3.one * 0.16f, GameAssets.Hex("#D32F2F"), default, 0.5f, false);
                    BrainrotModels.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(0.35f, 0.15f, 0.03f), GameAssets.Hex("#556B2F"), default, 0.3f, false);
                    BrainrotModels.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(0.03f, 0.15f, 0.35f), GameAssets.Hex("#556B2F"), default, 0.3f, false);
                    spin = new Vector3(0f, 90f, 0f);
                    break;
                case Kind.Coconut:
                    color = GameAssets.Hex("#795548");
                    BrainrotModels.Part(t, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.38f, color, default, 0.2f, false);
                    BrainrotModels.Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0.15f), Vector3.one * 0.07f, GameAssets.Hex("#3E2723"), default, 0.2f, false);
                    spin = new Vector3(400f, 200f, 0f);
                    break;
                default:
                    var orb = BrainrotModels.Part(t, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.45f, color, default, 0.5f, false);
                    orb.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(color, 3f);
                    break;
            }
        }

        void Update()
        {
            float dt = fromPlayer ? Time.deltaTime : Time.deltaTime * Enemy.TimeScale;
            if (dt <= 0f) return;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }

            Vector3 previous = transform.position;
            velocity.y -= gravity * dt;
            Vector3 next = previous + velocity * dt;
            transform.Rotate(spin * dt, Space.Self);

            if (Physics.Linecast(previous, next, out RaycastHit hit, Combat.EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                Impact(hit.point, false);
                return;
            }

            if (fromPlayer)
            {
                if (Physics.CheckSphere(next, 0.6f, Combat.EnemyMask, QueryTriggerInteraction.Ignore))
                {
                    Impact(next, true);
                    return;
                }
            }
            else
            {
                var player = PlayerController.Instance;
                if (player != null)
                {
                    Vector3 body = player.transform.position + Vector3.up * 0.9f;
                    Vector3 closest = ClosestPointOnSegment(previous, next, body);
                    if (Vector3.Distance(closest, body) < 0.85f)
                    {
                        Impact(closest, true);
                        return;
                    }
                }
                // Защищаемый объект (режим защиты)
                var objective = DefenseObjective.Instance;
                if (objective != null && !objective.Destroyed)
                {
                    Vector3 c = objective.Center;
                    Vector3 closest = ClosestPointOnSegment(previous, next, c);
                    if (Vector3.Distance(closest, c) < objective.Radius)
                    {
                        hitObjective = true;
                        Impact(closest, true);
                        return;
                    }
                }
            }

            if (next.y <= 0.02f)
            {
                Impact(new Vector3(next.x, 0.02f, next.z), false);
                return;
            }

            transform.position = next;
        }

        static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return a + ab * t;
        }

        static readonly Collider[] hitBuffer = new Collider[8];

        void Impact(Vector3 point, bool direct)
        {
            if (areaRadius > 0f)
            {
                Combat.Explode(point, areaRadius, damage, fromPlayer, kind == Kind.Banana ? GameAssets.Hex("#FFE082") : GameAssets.Hex("#FF8A3D"));
            }
            else
            {
                if (direct && !fromPlayer)
                {
                    if (hitObjective && DefenseObjective.Instance != null) DefenseObjective.Instance.Damage(damage);
                    else if (PlayerController.Instance != null) PlayerController.Instance.Health.TakeDamageFrom(damage, transform.position - velocity.normalized * 2f);
                }
                else if (direct && fromPlayer)
                {
                    int n = Physics.OverlapSphereNonAlloc(point, 0.9f, hitBuffer, Combat.EnemyMask, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < n; i++)
                    {
                        var e = hitBuffer[i].GetComponentInParent<Enemy>();
                        if (e == null || e.IsDead) continue;
                        bool crit = Random.value < RunState.stats.critChance;
                        e.TakeHit(damage * (crit ? 2f : 1f), crit, point);
                        break;
                    }
                }
                FX.Burst(point, color, 3, 4f, 0.12f, 0.5f);
                Particles.Sparkle(point, color, 8, 3f, 0.18f);
            }
            Destroy(gameObject);
        }
    }
}
