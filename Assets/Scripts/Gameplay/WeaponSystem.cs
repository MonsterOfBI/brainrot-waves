using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrainrotWaves
{
    /// <summary>
    /// Оружие героя. ЛКМ — стрелять, R — перезарядка, 1/2/3 или колесо — сменить оружие.
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        class Slot
        {
            public WeaponDef def;
            public int ammo;
            public Transform model;
            public Transform muzzle;
        }

        public Camera cam;
        public Color accentColor = Color.cyan;

        public event Action Hit;
        public event Action<bool> HitCrit;

        readonly List<Slot> slots = new List<Slot>();
        int current;
        bool reloading;
        float reloadTimer;
        float reloadDuration;
        float nextFireTime;
        float recoil;
        float switchTimer;
        Vector2 sway;
        Transform holder;
        Light muzzleLight;
        Transform muzzleFlash;
        float flashTimer;
        readonly List<LineRenderer> tracers = new List<LineRenderer>();
        readonly List<float> tracerTimers = new List<float>();

        public WeaponDef CurrentDef => slots.Count > 0 ? slots[current].def : WeaponDef.Pistol;
        public int Ammo => slots.Count > 0 ? slots[current].ammo : 0;
        public int MagazineSize => MagSize(CurrentDef);
        public bool IsReloading => reloading;
        public float ReloadProgress => reloading && reloadDuration > 0f ? 1f - reloadTimer / reloadDuration : 0f;
        public int SlotCount => slots.Count;
        public int CurrentIndex => current;
        public WeaponId SlotId(int i) => slots[i].def.id;

        static int MagSize(WeaponDef def) => Mathf.CeilToInt(def.magazine * RunState.stats.magazineMultiplier);

        void Awake()
        {
            holder = new GameObject("WeaponHolder").transform;
            holder.SetParent(transform, false);
            holder.localPosition = new Vector3(0.26f, -0.24f, 0.42f);

            var flash = BrainrotModels.Part(holder, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.15f, Color.white, default, 0.5f, false);
            flash.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(new Color(1f, 0.8f, 0.3f), 4f);
            muzzleFlash = flash;
            muzzleFlash.gameObject.SetActive(false);

            var lightObject = new GameObject("MuzzleLight");
            lightObject.transform.SetParent(holder, false);
            muzzleLight = lightObject.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.color = new Color(1f, 0.75f, 0.35f);
            muzzleLight.range = 8f;
            muzzleLight.intensity = 0f;

            for (int i = 0; i < 12; i++)
            {
                var tracerObject = new GameObject("Tracer");
                var line = tracerObject.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = 0.035f;
                line.endWidth = 0.01f;
                line.useWorldSpace = true;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.sharedMaterial = GameAssets.Unlit(new Color(1f, 0.92f, 0.5f));
                line.enabled = false;
                tracers.Add(line);
                tracerTimers.Add(0f);
            }
        }

        void OnDestroy()
        {
            foreach (var t in tracers) if (t != null) Destroy(t.gameObject);
        }

        public bool Has(WeaponId id) => slots.Exists(s => s.def.id == id);

        public void Give(WeaponId id, bool select = true)
        {
            var existing = slots.Find(s => s.def.id == id);
            if (existing != null)
            {
                existing.ammo = MagSize(existing.def);
                return;
            }
            var def = WeaponDef.Get(id);
            var slot = new Slot { def = def, ammo = MagSize(def) };
            slot.model = BuildModel(def, out slot.muzzle);
            slot.model.gameObject.SetActive(false);
            slots.Add(slot);
            if (!RunState.ownedWeapons.Contains(id)) RunState.ownedWeapons.Add(id);
            if (select || slots.Count == 1) Select(slots.Count - 1);
        }

        public void RefillAll()
        {
            foreach (var s in slots) s.ammo = MagSize(s.def);
        }

        void Select(int index)
        {
            if (index < 0 || index >= slots.Count) return;
            if (slots.Count > 0 && current < slots.Count) slots[current].model.gameObject.SetActive(false);
            current = index;
            slots[current].model.gameObject.SetActive(true);
            reloading = false;
            switchTimer = 0.25f;
            SoundFX.Play(SoundFX.Sfx.Reload, 0.4f);
        }

        void Update()
        {
            UpdateTracers();
            UpdateFlash();
            if (slots.Count == 0) return;

            var player = PlayerController.Instance;
            bool canControl = Time.timeScale > 0f && player != null && player.InputEnabled && !player.Health.IsDead && PlayerController.IsCursorLocked;
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if (canControl && keyboard != null && mouse != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) Select(0);
                if (keyboard.digit2Key.wasPressedThisFrame && slots.Count > 1) Select(1);
                if (keyboard.digit3Key.wasPressedThisFrame && slots.Count > 2) Select(2);
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.1f && slots.Count > 1)
                    Select((current + (scroll < 0f ? 1 : slots.Count - 1)) % slots.Count);

                if (keyboard.rKey.wasPressedThisFrame) StartReload();

                var def = CurrentDef;
                bool trigger = def.automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
                if (trigger && switchTimer <= 0f && Time.time >= nextFireTime)
                {
                    if (reloading)
                    {
                        // Дробовик можно прервать выстрелом, если есть патроны
                        if (def.id == WeaponId.Shotgun && slots[current].ammo > 0) reloading = false;
                    }
                    if (!reloading)
                    {
                        if (slots[current].ammo > 0) Fire();
                        else if (mouse.leftButton.wasPressedThisFrame) { SoundFX.Play(SoundFX.Sfx.Empty, 0.5f); StartReload(); }
                        else StartReload();
                    }
                }
                Vector2 delta = mouse.delta.ReadValue();
                sway = Vector2.Lerp(sway, -delta * 0.0015f, Time.deltaTime * 10f);
            }

            UpdateReload();
            AnimateModel();
        }

        void StartReload()
        {
            var slot = slots[current];
            if (reloading || slot.ammo >= MagSize(slot.def)) return;
            reloading = true;
            reloadDuration = slot.def.reloadTime * RunState.stats.reloadTimeMultiplier;
            reloadTimer = reloadDuration;
            SoundFX.Play(SoundFX.Sfx.Reload, 0.6f);
        }

        void UpdateReload()
        {
            if (!reloading) return;
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                reloading = false;
                slots[current].ammo = MagSize(slots[current].def);
                SoundFX.Play(SoundFX.Sfx.Reload, 0.6f);
            }
        }

        void Fire()
        {
            var slot = slots[current];
            var def = slot.def;
            var stats = RunState.stats;
            slot.ammo--;
            nextFireTime = Time.time + 1f / (def.fireRate * stats.fireRateMultiplier);
            recoil = Mathf.Min(recoil + def.recoil, 2.5f);
            CameraShake.Add(def.shake);

            switch (def.id)
            {
                case WeaponId.Rifle: SoundFX.Play(SoundFX.Sfx.Rifle, 0.55f, 0.08f); break;
                case WeaponId.Shotgun: SoundFX.Play(SoundFX.Sfx.Shotgun, 0.9f, 0.05f); break;
                default: SoundFX.Play(SoundFX.Sfx.Pistol, 0.7f, 0.08f); break;
            }

            flashTimer = 0.05f;
            muzzleFlash.position = slot.muzzle.position;
            muzzleFlash.localScale = Vector3.one * (def.id == WeaponId.Shotgun ? 0.28f : 0.16f) * UnityEngine.Random.Range(0.8f, 1.2f);
            muzzleLight.transform.position = slot.muzzle.position;

            bool anyHit = false, anyCrit = false;
            for (int p = 0; p < def.pellets; p++)
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * def.spread;
                Vector3 dir = cam.transform.rotation * Quaternion.Euler(spread.y, spread.x, 0f) * Vector3.forward;
                Vector3 origin = cam.transform.position;
                Vector3 end = origin + dir * 120f;

                if (Physics.Raycast(origin, dir, out RaycastHit hit, 120f, Combat.ShootMask, QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;
                    var enemy = hit.collider.GetComponentInParent<Enemy>();
                    if (enemy != null && !enemy.IsDead)
                    {
                        bool crit = UnityEngine.Random.value < stats.critChance;
                        float damage = def.damage * stats.damageMultiplier * (crit ? 2f : 1f);
                        enemy.TakeHit(damage, crit, hit.point);
                        anyHit = true;
                        anyCrit |= crit;
                    }
                    else
                    {
                        FX.Burst(hit.point + hit.normal * 0.05f, new Color(0.85f, 0.8f, 0.7f), 2, 3f, 0.06f, 0.4f);
                    }
                }
                ShowTracer(slot.muzzle.position, end);
            }

            if (anyHit)
            {
                Hit?.Invoke();
                HitCrit?.Invoke(anyCrit);
                SoundFX.Play(anyCrit ? SoundFX.Sfx.Crit : SoundFX.Sfx.Hit, 0.45f, 0.1f);
            }

            if (slot.ammo <= 0) StartReload();
        }

        void ShowTracer(Vector3 from, Vector3 to)
        {
            for (int i = 0; i < tracers.Count; i++)
            {
                if (tracerTimers[i] > 0f) continue;
                tracers[i].SetPosition(0, from);
                tracers[i].SetPosition(1, to);
                tracers[i].enabled = true;
                tracerTimers[i] = 0.045f;
                return;
            }
        }

        void UpdateTracers()
        {
            for (int i = 0; i < tracers.Count; i++)
            {
                if (tracerTimers[i] <= 0f) continue;
                tracerTimers[i] -= Time.deltaTime;
                if (tracerTimers[i] <= 0f) tracers[i].enabled = false;
            }
        }

        void UpdateFlash()
        {
            flashTimer -= Time.deltaTime;
            bool on = flashTimer > 0f;
            if (muzzleFlash.gameObject.activeSelf != on) muzzleFlash.gameObject.SetActive(on);
            muzzleLight.intensity = on ? 4f : 0f;
        }

        void AnimateModel()
        {
            var player = PlayerController.Instance;
            float move = player != null ? player.MoveFraction : 0f;
            float t = Time.time * (6f + move * 6f);
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 7f);
            switchTimer = Mathf.Max(0f, switchTimer - Time.deltaTime);

            Vector3 bob = new Vector3(Mathf.Cos(t * 0.5f) * 0.012f, Mathf.Abs(Mathf.Sin(t * 0.5f)) * 0.015f, 0f) * move;
            Vector3 position = new Vector3(0.26f, -0.24f, 0.42f) + bob + new Vector3(sway.x, sway.y, 0f);
            position += new Vector3(0f, recoil * 0.015f, -recoil * 0.06f);
            position.y -= switchTimer * 1.2f;

            float reloadTilt = reloading ? Mathf.Sin(ReloadProgress * Mathf.PI) * 40f : 0f;
            holder.localPosition = position;
            holder.localRotation = Quaternion.Euler(-recoil * 6f + reloadTilt, sway.x * 40f, -sway.x * 60f + reloadTilt * 0.5f);
        }

        // ---------- Модели оружия ----------

        Transform BuildModel(WeaponDef def, out Transform muzzle)
        {
            var root = new GameObject(def.name).transform;
            root.SetParent(holder, false);
            Color dark = GameAssets.Hex("#2B2D33");
            Color mid = GameAssets.Hex("#4A4E57");
            Color wood = GameAssets.Hex("#8D5A35");
            Color accent = accentColor;

            switch (def.id)
            {
                case WeaponId.Rifle:
                    P(root, new Vector3(0f, 0f, 0.05f), new Vector3(0.08f, 0.11f, 0.5f), dark);
                    P(root, new Vector3(0f, 0.015f, 0.42f), new Vector3(0.04f, 0.04f, 0.35f), mid);
                    P(root, new Vector3(0f, -0.02f, 0.3f), new Vector3(0.07f, 0.07f, 0.2f), accent);
                    P(root, new Vector3(0f, -0.13f, 0.08f), new Vector3(0.06f, 0.18f, 0.08f), dark, new Vector3(12f, 0f, 0f));
                    P(root, new Vector3(0f, -0.11f, -0.07f), new Vector3(0.06f, 0.14f, 0.07f), wood, new Vector3(-15f, 0f, 0f));
                    P(root, new Vector3(0f, -0.02f, -0.25f), new Vector3(0.07f, 0.1f, 0.18f), wood);
                    P(root, new Vector3(0f, 0.08f, 0.05f), new Vector3(0.03f, 0.04f, 0.12f), accent);
                    muzzle = Muzzle(root, new Vector3(0f, 0.015f, 0.62f));
                    break;
                case WeaponId.Shotgun:
                    P(root, new Vector3(0f, 0f, 0.1f), new Vector3(0.09f, 0.1f, 0.42f), dark);
                    P(root, new Vector3(-0.022f, 0.02f, 0.45f), new Vector3(0.04f, 0.04f, 0.4f), mid);
                    P(root, new Vector3(0.022f, 0.02f, 0.45f), new Vector3(0.04f, 0.04f, 0.4f), mid);
                    P(root, new Vector3(0f, -0.04f, 0.38f), new Vector3(0.08f, 0.06f, 0.18f), wood);
                    P(root, new Vector3(0f, -0.05f, -0.2f), new Vector3(0.08f, 0.12f, 0.3f), wood, new Vector3(8f, 0f, 0f));
                    P(root, new Vector3(0f, 0.06f, 0.1f), new Vector3(0.1f, 0.02f, 0.3f), accent);
                    muzzle = Muzzle(root, new Vector3(0f, 0.02f, 0.66f));
                    break;
                default:
                    P(root, new Vector3(0f, 0.02f, 0.05f), new Vector3(0.07f, 0.08f, 0.26f), dark);
                    P(root, new Vector3(0f, 0.03f, 0.2f), new Vector3(0.045f, 0.045f, 0.1f), mid);
                    P(root, new Vector3(0f, -0.08f, -0.03f), new Vector3(0.06f, 0.15f, 0.08f), accent, new Vector3(-12f, 0f, 0f));
                    P(root, new Vector3(0f, 0.07f, -0.04f), new Vector3(0.02f, 0.02f, 0.05f), accent);
                    muzzle = Muzzle(root, new Vector3(0f, 0.03f, 0.27f));
                    break;
            }
            return root;
        }

        static void P(Transform parent, Vector3 position, Vector3 scale, Color color, Vector3 euler = default)
        {
            BrainrotModels.Part(parent, PrimitiveType.Cube, position, scale, color, euler, 0.6f, false);
        }

        static Transform Muzzle(Transform parent, Vector3 position)
        {
            var m = new GameObject("Muzzle").transform;
            m.SetParent(parent, false);
            m.localPosition = position;
            return m;
        }
    }
}
