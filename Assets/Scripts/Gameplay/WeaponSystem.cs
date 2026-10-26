using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrainrotWaves
{
    /// <summary>
    /// Оружие героя. ЛКМ — стрелять, R — перезарядка, 1/2/3 или колесо — сменить оружие.
    /// Анимации: смена оружия (убрать/достать), перезарядка с выпадающим магазином, отдача.
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        class Slot
        {
            public WeaponDef def;
            public int ammo;
            public Transform model;
            public Transform muzzle;
            public Transform mag;        // магазин (у дробовика — цевьё)
            public Vector3 magRest;
        }

        public Camera cam;
        public Color accentColor = Color.cyan;

        public event Action Hit;
        public event Action<bool> HitCrit;

        /// <summary>Слой оружия в руках (рисуется отдельной камерой).</summary>
        public const int ViewmodelLayer = 10;

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        static readonly Vector3 RestPosition = new Vector3(0.26f, -0.24f, 0.42f);
        const float HolsterTime = 0.14f;
        const float DrawTime = 0.3f;

        readonly List<Slot> slots = new List<Slot>();
        int current;
        int pending = -1;
        bool reloading;
        float reloadTimer;
        float reloadDuration;
        float nextFireTime;
        float recoil;
        float switchTimer;
        float holsterTimer;
        bool magDropped;
        int shellsShown;
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
        public bool IsSwitching => switchTimer > 0f || holsterTimer > 0f;
        public float ReloadProgress => reloading && reloadDuration > 0f ? 1f - reloadTimer / reloadDuration : 0f;
        public int SlotCount => slots.Count;
        public int CurrentIndex => current;
        public WeaponId SlotId(int i) => slots[i].def.id;
        public int SlotAmmo(int i) => slots[i].ammo;

        static int MagSize(WeaponDef def) => Mathf.CeilToInt(def.magazine * RunState.stats.magazineMultiplier);

        void Awake()
        {
            holder = new GameObject("WeaponHolder").transform;
            holder.SetParent(transform, false);
            holder.localPosition = RestPosition;

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
                line.sharedMaterial = GameAssets.Glow(new Color(1f, 0.92f, 0.5f), 2.5f);
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
            slot.model = BuildModel(def, out slot.muzzle, out slot.mag);
            if (slot.mag != null) slot.magRest = slot.mag.localPosition;
            SetLayer(holder, ViewmodelLayer);
            slot.model.gameObject.SetActive(false);
            slots.Add(slot);
            if (!RunState.ownedWeapons.Contains(id)) RunState.ownedWeapons.Add(id);
            if (slots.Count == 1) DoSelect(0);
            else if (select) Select(slots.Count - 1);
        }

        public void RefillAll()
        {
            foreach (var s in slots) s.ammo = MagSize(s.def);
        }

        /// <summary>Начать смену оружия: сначала убираем текущее, потом достаём новое.</summary>
        void Select(int index)
        {
            if (index < 0 || index >= slots.Count) return;
            if (index == current && holsterTimer <= 0f) return;
            pending = index;
            if (holsterTimer <= 0f) holsterTimer = HolsterTime;
            CancelReload();
        }

        void DoSelect(int index)
        {
            if (index < 0 || index >= slots.Count) return;
            if (current < slots.Count) slots[current].model.gameObject.SetActive(false);
            current = index;
            slots[current].model.gameObject.SetActive(true);
            CancelReload();
            switchTimer = DrawTime;
            SoundFX.Play(SoundFX.Sfx.Reload, 0.4f);
        }

        void CancelReload()
        {
            reloading = false;
            magDropped = false;
            if (slots.Count > 0 && slots[current].mag != null)
            {
                slots[current].mag.localPosition = slots[current].magRest;
                slots[current].mag.gameObject.SetActive(true);
            }
        }

        void Update()
        {
            UpdateTracers();
            UpdateFlash();
            if (slots.Count == 0) return;

            if (holsterTimer > 0f)
            {
                holsterTimer -= Time.deltaTime;
                if (holsterTimer <= 0f)
                {
                    holsterTimer = 0f;
                    DoSelect(pending);
                }
            }

            var player = PlayerController.Instance;
            bool canControl = Time.timeScale > 0f && player != null && player.InputEnabled && !player.Health.IsDead && PlayerController.IsCursorLocked;
            var mouse = Mouse.current;

            if (canControl && mouse != null)
            {
                if (Controls.Down(GameAction.Weapon1)) Select(0);
                if (Controls.Down(GameAction.Weapon2) && slots.Count > 1) Select(1);
                if (Controls.Down(GameAction.Weapon3) && slots.Count > 2) Select(2);
                float scroll = mouse.scroll.ReadValue().y;
                int baseIndex = holsterTimer > 0f ? pending : current;
                if (Mathf.Abs(scroll) > 0.1f && slots.Count > 1)
                    Select((baseIndex + (scroll < 0f ? 1 : slots.Count - 1)) % slots.Count);

                if (Controls.Down(GameAction.Reload)) StartReload();

                var def = CurrentDef;
                bool trigger = def.automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
                if (trigger && !IsSwitching && Time.time >= nextFireTime)
                {
                    if (reloading)
                    {
                        // Дробовик можно прервать выстрелом, если есть патроны
                        if (def.id == WeaponId.Shotgun && slots[current].ammo > 0) CancelReload();
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
            if (reloading || IsSwitching || slot.ammo >= MagSize(slot.def)) return;
            reloading = true;
            magDropped = false;
            shellsShown = 0;
            reloadDuration = slot.def.reloadTime * RunState.stats.reloadTimeMultiplier;
            reloadTimer = reloadDuration;
            SoundFX.Play(SoundFX.Sfx.Reload, 0.6f);
        }

        void UpdateReload()
        {
            if (!reloading) return;
            reloadTimer -= Time.deltaTime;
            var slot = slots[current];
            float p = ReloadProgress;

            // Шаги перезарядки со звуками
            if (slot.def.id == WeaponId.Shotgun)
            {
                int shells = Mathf.FloorToInt(Mathf.InverseLerp(0.15f, 0.8f, p) * 3f);
                if (shells > shellsShown && shellsShown < 3)
                {
                    shellsShown = shells;
                    SoundFX.Play(SoundFX.Sfx.Empty, 0.5f, 0.15f);
                }
            }
            else if (!magDropped && p > 0.3f)
            {
                magDropped = true;
                DropMagazine(slot);
                SoundFX.Play(SoundFX.Sfx.Empty, 0.45f, 0.1f);
            }

            if (reloadTimer <= 0f)
            {
                reloading = false;
                slot.ammo = MagSize(slot.def);
                if (slot.mag != null)
                {
                    slot.mag.localPosition = slot.magRest;
                    slot.mag.gameObject.SetActive(true);
                }
                recoil = Mathf.Max(recoil, 0.8f); // «передёрнуть затвор»
                SoundFX.Play(SoundFX.Sfx.Reload, 0.6f);
            }
        }

        /// <summary>Пустой магазин выпадает и падает вниз.</summary>
        void DropMagazine(Slot slot)
        {
            if (slot.mag == null) return;
            var copy = Instantiate(slot.mag.gameObject);
            copy.layer = 0;
            copy.transform.SetPositionAndRotation(slot.mag.position, slot.mag.rotation);
            copy.transform.localScale = slot.mag.lossyScale;
            var debris = copy.AddComponent<Debris>();
            debris.velocity = cam.transform.TransformDirection(new Vector3(0.3f, -1.5f, 0.6f));
            debris.life = 0.9f;
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
            muzzleFlash.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            muzzleLight.transform.position = slot.muzzle.position;
            Particles.Flash(slot.muzzle.position, new Color(1f, 0.75f, 0.3f), def.id == WeaponId.Shotgun ? 0.5f : 0.3f, 0.05f);
            Particles.Smoke(slot.muzzle.position + cam.transform.forward * 0.1f, def.id == WeaponId.Shotgun ? 0.18f : 0.08f, def.id == WeaponId.Shotgun ? 3 : 1, new Color(0.85f, 0.85f, 0.85f, 0.25f));

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
                        if (!enemy.IsDead) enemy.Knockback(dir * (def.id == WeaponId.Shotgun ? 1.2f : 0.5f));
                        anyHit = true;
                        anyCrit |= crit;
                    }
                    else
                    {
                        FX.Burst(hit.point + hit.normal * 0.05f, new Color(0.85f, 0.8f, 0.7f), 1, 3f, 0.06f, 0.4f);
                        Particles.Sparks(hit.point + hit.normal * 0.03f, hit.normal, new Color(1f, 0.85f, 0.5f), 4, 5f);
                        Particles.Dust(hit.point, 0.25f, 2, new Color(0.7f, 0.65f, 0.6f));
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

        static float Bump(float p, float a, float b) => Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(a, b, p)) * Mathf.PI);

        void AnimateModel()
        {
            var player = PlayerController.Instance;
            float move = player != null ? player.MoveFraction : 0f;
            float t = Time.time * (6f + move * 6f);
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 7f);
            switchTimer = Mathf.Max(0f, switchTimer - Time.deltaTime);

            Vector3 bob = new Vector3(Mathf.Cos(t * 0.5f) * 0.012f, Mathf.Abs(Mathf.Sin(t * 0.5f)) * 0.015f, 0f) * move;
            Vector3 position = RestPosition + bob + new Vector3(sway.x, sway.y, 0f);
            position += new Vector3(0f, recoil * 0.015f, -recoil * 0.06f);
            Vector3 euler = new Vector3(-recoil * 6f, sway.x * 40f, -sway.x * 60f);

            // Смена оружия: старое уходит вниз, новое поднимается с поворотом
            if (holsterTimer > 0f)
            {
                float h = 1f - holsterTimer / HolsterTime;
                position.y -= Ease.InOutCubic(h) * 0.35f;
                euler += new Vector3(Ease.InOutCubic(h) * 35f, 0f, -h * 20f);
            }
            else if (switchTimer > 0f)
            {
                float d = 1f - Ease.OutBack(1f - switchTimer / DrawTime);
                position.y -= d * 0.35f;
                euler += new Vector3(d * 35f, 0f, d * 25f);
            }

            // Перезарядка
            if (reloading && slots.Count > 0)
            {
                var slot = slots[current];
                float p = ReloadProgress;
                float tilt = Bump(p, 0f, 1f);
                if (slot.def.id == WeaponId.Shotgun)
                {
                    // Поворот набок, по одному патрону — толчок, в конце — передёрнуть цевьё
                    euler += new Vector3(-10f * tilt, 0f, 35f * tilt);
                    position += new Vector3(-0.04f, 0.02f, 0f) * tilt;
                    float shell = Mathf.Repeat(Mathf.InverseLerp(0.15f, 0.8f, p) * 3f, 1f);
                    if (p > 0.15f && p < 0.8f) position.z -= Mathf.Sin(shell * Mathf.PI) * 0.025f;
                    if (slot.mag != null) slot.mag.localPosition = slot.magRest + new Vector3(0f, 0f, -0.12f * Bump(p, 0.82f, 1f));
                }
                else
                {
                    // Наклон, магазин выезжает вниз, новый вставляется, затвор
                    euler += new Vector3(-15f * tilt, 10f * tilt, 28f * tilt);
                    position += new Vector3(-0.03f, 0.03f, 0.02f) * tilt;
                    if (slot.mag != null)
                    {
                        if (p < 0.3f)
                        {
                            slot.mag.gameObject.SetActive(true);
                            slot.mag.localPosition = slot.magRest + Vector3.down * Ease.InOutCubic(Mathf.InverseLerp(0.1f, 0.3f, p)) * 0.08f;
                        }
                        else if (p < 0.45f) slot.mag.gameObject.SetActive(false);
                        else
                        {
                            slot.mag.gameObject.SetActive(true);
                            float insert = Ease.OutCubic(Mathf.InverseLerp(0.45f, 0.72f, p));
                            slot.mag.localPosition = slot.magRest + Vector3.down * (1f - insert) * 0.25f;
                            if (p > 0.72f) position.y += Bump(p, 0.72f, 0.8f) * 0.02f;
                        }
                    }
                    position.z -= Bump(p, 0.82f, 0.95f) * 0.04f;
                }
            }

            holder.localPosition = position;
            holder.localRotation = Quaternion.Euler(euler);
        }

        // ---------- Модели оружия ----------

        /// <summary>Модель из Kenney Blaster Kit (Resources/Weapons) для оружия.</summary>
        static string KitModel(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Rifle: return "blaster-p";
                case WeaponId.Shotgun: return "blaster-l";
                default: return "blaster-k";
            }
        }

        static float KitLength(WeaponId id) => id == WeaponId.Rifle ? 0.5f : id == WeaponId.Shotgun ? 0.44f : 0.3f;

        /// <summary>Поворот моделей Kenney, чтобы ствол смотрел вперёд (+Z).</summary>
        public static float KitYaw = 180f;

        static bool id0(WeaponDef def) => def.id == WeaponId.Pistol;

        static Bounds LocalBounds(Transform root, GameObject model)
        {
            var b = new Bounds();
            bool first = true;
            var toRoot = root.worldToLocalMatrix;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var m = toRoot * mf.transform.localToWorldMatrix;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(c);
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        Transform BuildKitModel(WeaponDef def, GameObject prefab, out Transform muzzle, out Transform mag)
        {
            var root = new GameObject(def.name.en).transform;
            root.SetParent(holder, false);
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root, false);
            pivot.localRotation = Quaternion.Euler(0f, KitYaw, 0f);
            var model = Instantiate(prefab, pivot, false);
            foreach (var r in model.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Масштаб по длине и центровка: рукоять примерно в точке крепления
            var b = LocalBounds(root, model);
            float length = Mathf.Max(0.01f, Mathf.Max(b.size.z, b.size.y * 1.7f));
            float scale = KitLength(def.id) / length;
            pivot.localScale = Vector3.one * scale;
            b = LocalBounds(root, model);
            pivot.localPosition = -new Vector3(b.center.x, b.center.y - (id0(def) ? b.size.y * 0.12f : 0f), b.center.z - b.size.z * 0.25f);
            b = LocalBounds(root, model);

            muzzle = Muzzle(root, new Vector3(0f, b.center.y + b.size.y * 0.15f, b.max.z));
            mag = null;
            if (def.id != WeaponId.Shotgun)
            {
                var clip = Resources.Load<GameObject>("Weapons/clip-small");
                if (clip != null)
                {
                    var holderT = new GameObject("Mag").transform;
                    holderT.SetParent(root, false);
                    var c = Instantiate(clip, holderT, false);
                    foreach (var r in c.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var cb = LocalBounds(holderT, c);
                    float cs = 0.09f / Mathf.Max(0.01f, cb.size.y);
                    c.transform.localScale = Vector3.one * cs;
                    cb = LocalBounds(holderT, c);
                    c.transform.localPosition = -cb.center;
                    holderT.localPosition = new Vector3(0f, b.min.y + 0.025f, b.center.z - b.size.z * 0.05f);
                    mag = holderT;
                }
            }
            return root;
        }

        Transform BuildModel(WeaponDef def, out Transform muzzle, out Transform mag)
        {
            var kit = Resources.Load<GameObject>("Weapons/" + KitModel(def.id));
            if (kit != null) return BuildKitModel(def, kit, out muzzle, out mag);

            var root = new GameObject(def.name.en).transform;
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
                    mag = P(root, new Vector3(0f, -0.13f, 0.08f), new Vector3(0.06f, 0.18f, 0.08f), dark, new Vector3(12f, 0f, 0f));
                    P(root, new Vector3(0f, -0.11f, -0.07f), new Vector3(0.06f, 0.14f, 0.07f), wood, new Vector3(-15f, 0f, 0f));
                    P(root, new Vector3(0f, -0.02f, -0.25f), new Vector3(0.07f, 0.1f, 0.18f), wood);
                    P(root, new Vector3(0f, 0.08f, 0.05f), new Vector3(0.03f, 0.04f, 0.12f), accent);
                    muzzle = Muzzle(root, new Vector3(0f, 0.015f, 0.62f));
                    break;
                case WeaponId.Shotgun:
                    P(root, new Vector3(0f, 0f, 0.1f), new Vector3(0.09f, 0.1f, 0.42f), dark);
                    P(root, new Vector3(-0.022f, 0.02f, 0.45f), new Vector3(0.04f, 0.04f, 0.4f), mid);
                    P(root, new Vector3(0.022f, 0.02f, 0.45f), new Vector3(0.04f, 0.04f, 0.4f), mid);
                    mag = P(root, new Vector3(0f, -0.04f, 0.38f), new Vector3(0.08f, 0.06f, 0.18f), wood);
                    P(root, new Vector3(0f, -0.05f, -0.2f), new Vector3(0.08f, 0.12f, 0.3f), wood, new Vector3(8f, 0f, 0f));
                    P(root, new Vector3(0f, 0.06f, 0.1f), new Vector3(0.1f, 0.02f, 0.3f), accent);
                    muzzle = Muzzle(root, new Vector3(0f, 0.02f, 0.66f));
                    break;
                default:
                    P(root, new Vector3(0f, 0.02f, 0.05f), new Vector3(0.07f, 0.08f, 0.26f), dark);
                    P(root, new Vector3(0f, 0.03f, 0.2f), new Vector3(0.045f, 0.045f, 0.1f), mid);
                    P(root, new Vector3(0f, -0.08f, -0.03f), new Vector3(0.06f, 0.15f, 0.08f), accent, new Vector3(-12f, 0f, 0f));
                    mag = P(root, new Vector3(0f, -0.15f, -0.045f), new Vector3(0.045f, 0.05f, 0.06f), mid, new Vector3(-12f, 0f, 0f));
                    P(root, new Vector3(0f, 0.07f, -0.04f), new Vector3(0.02f, 0.02f, 0.05f), accent);
                    muzzle = Muzzle(root, new Vector3(0f, 0.03f, 0.27f));
                    break;
            }
            return root;
        }

        static Transform P(Transform parent, Vector3 position, Vector3 scale, Color color, Vector3 euler = default)
        {
            return BrainrotModels.Part(parent, PrimitiveType.Cube, position, scale, color, euler, 0.6f, false);
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
