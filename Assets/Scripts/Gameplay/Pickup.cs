using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>Подбираемые предметы: монеты, аптечки, ящики с оружием.</summary>
    public class Pickup : MonoBehaviour
    {
        public enum Kind { Coin, Health, WeaponCrate }

        public static readonly List<Pickup> Active = new List<Pickup>();

        public Kind kind;
        public int value = 1;
        public WeaponId weapon;

        Vector3 velocity;
        float age;
        bool magnetized;
        Transform visual;

        public static Pickup Spawn(Kind kind, Vector3 position, int value = 1, WeaponId weapon = WeaponId.Rifle)
        {
            var go = new GameObject("Pickup_" + kind);
            go.transform.position = position;
            var p = go.AddComponent<Pickup>();
            p.kind = kind;
            p.value = value;
            p.weapon = weapon;
            p.velocity = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(4f, 7f), Random.Range(-2.5f, 2.5f));
            p.BuildVisual();
            return p;
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public void Magnetize() => magnetized = true;

        void BuildVisual()
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            switch (kind)
            {
                case Kind.Coin:
                    var coin = BrainrotModels.Part(visual, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.38f, 0.04f, 0.38f), GameAssets.Hex("#FFC107"), new Vector3(90f, 0f, 0f), 0.9f, false);
                    coin.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(GameAssets.Hex("#FFC107"), 0.6f);
                    BrainrotModels.Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.02f), new Vector3(0.24f, 0.03f, 0.24f), GameAssets.Hex("#FFE082"), new Vector3(90f, 0f, 0f), 0.9f, false);
                    break;
                case Kind.Health:
                    BrainrotModels.Part(visual, PrimitiveType.Cube, Vector3.zero, new Vector3(0.6f, 0.45f, 0.45f), Color.white, default, 0.6f);
                    var c1 = BrainrotModels.Part(visual, PrimitiveType.Cube, new Vector3(0f, 0f, 0.23f), new Vector3(0.35f, 0.1f, 0.02f), Color.red, default, 0.5f, false);
                    c1.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(new Color(1f, 0.15f, 0.2f), 1.5f);
                    var c2 = BrainrotModels.Part(visual, PrimitiveType.Cube, new Vector3(0f, 0f, 0.23f), new Vector3(0.1f, 0.35f, 0.02f), Color.red, default, 0.5f, false);
                    c2.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(new Color(1f, 0.15f, 0.2f), 1.5f);
                    break;
                case Kind.WeaponCrate:
                    BrainrotModels.Part(visual, PrimitiveType.Cube, Vector3.zero, new Vector3(1f, 0.6f, 0.6f), GameAssets.Hex("#8D6E63"), default, 0.2f);
                    BrainrotModels.Part(visual, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(1.04f, 0.1f, 0.64f), GameAssets.Hex("#FFB300"), default, 0.4f);
                    var glow = BrainrotModels.Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.31f, 0f), new Vector3(0.5f, 0.02f, 0.3f), Color.white, default, 0.4f, false);
                    glow.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(GameAssets.Hex("#FFD54F"), 2f);
                    velocity = Vector3.zero;
                    break;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            var player = PlayerController.Instance;

            if (velocity != Vector3.zero || transform.position.y > 0.5f)
            {
                velocity.y -= 20f * dt;
                transform.position += velocity * dt;
                if (transform.position.y < 0.5f)
                {
                    transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
                    velocity = Vector3.zero;
                }
            }

            visual.localPosition = new Vector3(0f, Mathf.Sin(age * 3f) * 0.12f, 0f);
            visual.Rotate(0f, 120f * dt, 0f, Space.World);

            if (player == null || player.Health.IsDead) return;
            Vector3 target = player.transform.position + Vector3.up * 0.9f;
            float distance = Vector3.Distance(transform.position, target);

            float magnet = kind == Kind.Coin ? RunState.stats.coinMagnetRadius : 2.5f;
            if (kind == Kind.WeaponCrate) magnet = 0f;
            if ((distance < magnet || magnetized) && age > 0.4f)
            {
                velocity = Vector3.zero;
                float speed = Mathf.Lerp(18f, 8f, Mathf.Clamp01(distance / 10f)) + age * 2f;
                transform.position = Vector3.MoveTowards(transform.position, target, speed * dt);
            }

            if (distance < (kind == Kind.WeaponCrate ? 1.6f : 1.1f) && age > 0.3f)
                Collect(player);
        }

        void Collect(PlayerController player)
        {
            switch (kind)
            {
                case Kind.Coin:
                    RunState.coins += value;
                    SoundFX.Play(SoundFX.Sfx.Coin, 0.35f, 0.08f);
                    break;
                case Kind.Health:
                    if (player.Health.Current >= player.Health.maxHealth) return;
                    player.Health.Heal(30f);
                    SoundFX.Play(SoundFX.Sfx.Heal, 0.6f);
                    FX.Text(transform.position + Vector3.up, "+30", new Color(0.4f, 1f, 0.5f));
                    break;
                case Kind.WeaponCrate:
                    var weapons = player.GetComponentInChildren<WeaponSystem>();
                    if (weapons != null)
                    {
                        bool had = weapons.Has(weapon);
                        weapons.Give(weapon, true);
                        FX.Text(transform.position + Vector3.up * 1.5f, had ? "Патроны!" : WeaponDef.Get(weapon).name + "!", new Color(1f, 0.85f, 0.3f), 0.09f);
                    }
                    SoundFX.Play(SoundFX.Sfx.Upgrade, 0.7f);
                    break;
            }
            Destroy(gameObject);
        }
    }
}
