using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Декорации из бесплатных паков Kenney (CC0): лежат в Resources/Decor/<пак>/.
    /// Внутри арены — укрытия (с коллайдерами) и мелочь (без коллайдеров),
    /// за стенами — крупные объекты для фона (корабли, дома, деревья, небоскрёбы).
    /// Если файлов нет, арена строится как раньше.
    /// </summary>
    public partial class ArenaGenerator
    {
        struct Prop
        {
            public string path;
            public float height;
            public bool solid;
            public float radius;

            public Prop(string path, float height, bool solid = true, float radius = 1f)
            {
                this.path = path;
                this.height = height;
                this.solid = solid;
                this.radius = radius;
            }
        }

        static readonly Dictionary<string, GameObject> propCache = new Dictionary<string, GameObject>();

        static GameObject LoadProp(string path)
        {
            if (propCache.TryGetValue(path, out var go)) return go;
            go = Resources.Load<GameObject>("Decor/" + path);
            propCache[path] = go;
            return go;
        }

        void AddKitDecor(ArenaTheme theme)
        {
            Prop[] inner, small, outer;
            int innerCount, smallCount, outerCount;
            switch (theme)
            {
                case ArenaTheme.Beach:
                    inner = new[] { new Prop("pirate/barrel", 1.2f), new Prop("pirate/crate", 1.2f), new Prop("pirate/chest", 0.9f), new Prop("pirate/cannon", 1.3f, true, 1.4f),
                        new Prop("pirate/rocks-sand-a", 1.3f, true, 1.5f), new Prop("pirate/boat-row-small", 1.1f, true, 2f), new Prop("pirate/crate-bottles", 1.1f) };
                    small = new[] { new Prop("pirate/patch-sand-foliage", 0.6f, false), new Prop("pirate/grass-plant", 0.8f, false) };
                    outer = new[] { new Prop("pirate/ship-wreck", 7f, false, 6f), new Prop("pirate/ship-pirate-medium", 12f, false, 8f), new Prop("pirate/tower-watch", 10f, false, 3f),
                        new Prop("pirate/palm-detailed-bend", 8f, false, 2f), new Prop("pirate/palm-straight", 8f, false, 2f), new Prop("nature/tree_palmTall", 9f, false, 2f), new Prop("pirate/flag-pirate-high", 7f, false, 1f) };
                    innerCount = 9; smallCount = 14; outerCount = 22;
                    break;
                case ArenaTheme.Desert:
                    inner = new[] { new Prop("nature/cactus_tall", 3f, true, 0.8f), new Prop("nature/cactus_short", 1.6f, true, 0.7f), new Prop("nature/rock_largeA", 2.2f, true, 1.8f),
                        new Prop("nature/rock_tallB", 3f, true, 1.2f), new Prop("nature/stone_largeC", 1.8f, true, 1.5f), new Prop("nature/campfire_stones", 0.6f, true, 1f) };
                    small = new[] { new Prop("nature/cactus_short", 0.9f, false), new Prop("nature/stone_largeC", 0.5f, false) };
                    outer = new[] { new Prop("nature/cliff_large_rock", 12f, false, 7f), new Prop("nature/rock_tallJ", 9f, false, 3f), new Prop("nature/statue_obelisk", 8f, false, 2f),
                        new Prop("nature/statue_column", 6f, false, 1.5f), new Prop("nature/tent_detailedClosed", 3.5f, false, 3f), new Prop("nature/cactus_tall", 6f, false, 1.5f) };
                    innerCount = 10; smallCount = 12; outerCount = 22;
                    break;
                case ArenaTheme.NightVillage:
                    inner = new[] { new Prop("graveyard/gravestone-cross", 1.5f, true, 0.7f), new Prop("graveyard/gravestone-round", 1.1f, true, 0.7f), new Prop("graveyard/gravestone-decorative", 1.4f, true, 0.7f),
                        new Prop("graveyard/crypt-small", 3f, true, 2f), new Prop("graveyard/coffin-old", 0.6f, true, 1.2f), new Prop("town/cart", 1.6f, true, 1.5f), new Prop("town/stall-red", 2.6f, true, 1.6f) };
                    small = new[] { new Prop("graveyard/pumpkin-carved", 0.7f, false), new Prop("graveyard/pumpkin-tall-carved", 0.9f, false), new Prop("graveyard/candle-multiple", 0.5f, false),
                        new Prop("graveyard/lightpost-single", 3.2f, false), new Prop("graveyard/fire-basket", 1.2f, false), new Prop("town/lantern", 1.6f, false) };
                    outer = new[] { new Prop("town/windmill", 12f, false, 4f), new Prop("town/watermill", 8f, false, 4f), new Prop("town/tree-high-crooked", 9f, false, 2f),
                        new Prop("graveyard/pine-crooked", 9f, false, 2f), new Prop("graveyard/pine-fall-crooked", 8f, false, 2f), new Prop("graveyard/crypt-large", 7f, false, 4f) };
                    innerCount = 12; smallCount = 16; outerCount = 22;
                    break;
                case ArenaTheme.Forest:
                    inner = new[] { new Prop("nature/stump_oldTall", 1.4f, true, 0.9f), new Prop("nature/log_large", 1.1f, true, 1.8f), new Prop("nature/rock_largeB", 2f, true, 1.6f),
                        new Prop("nature/tree_fat", 5f, true, 1f), new Prop("nature/tree_detailed", 6f, true, 1f) };
                    small = new[] { new Prop("nature/mushroom_redTall", 1.4f, false), new Prop("nature/mushroom_tanGroup", 0.6f, false), new Prop("nature/flower_redA", 0.5f, false),
                        new Prop("nature/flower_yellowB", 0.5f, false), new Prop("nature/grass_large", 0.6f, false) };
                    outer = new[] { new Prop("nature/tree_oak", 10f, false, 3f), new Prop("nature/tree_detailed", 11f, false, 3f), new Prop("nature/tree_fat", 9f, false, 3f),
                        new Prop("nature/tree_pineTallA_detailed", 13f, false, 3f) };
                    innerCount = 9; smallCount = 22; outerCount = 30;
                    break;
                case ArenaTheme.Jungle:
                    inner = new[] { new Prop("nature/log", 0.9f, true, 1.4f), new Prop("nature/rock_largeB", 1.8f, true, 1.5f), new Prop("nature/tree_palmBend", 6f, true, 0.8f),
                        new Prop("nature/stump_oldTall", 1.2f, true, 0.8f) };
                    small = new[] { new Prop("nature/plant_bushLarge", 1.6f, false), new Prop("nature/plant_bushDetailed", 1.2f, false), new Prop("nature/flower_redA", 0.6f, false),
                        new Prop("nature/crops_bambooStageB", 2.5f, false) };
                    outer = new[] { new Prop("nature/tree_palmDetailedTall", 11f, false, 2f), new Prop("nature/tree_palmBend", 8f, false, 2f), new Prop("nature/plant_bushDetailed", 3.5f, false, 2f),
                        new Prop("nature/crops_bambooStageB", 6f, false, 1.5f) };
                    innerCount = 8; smallCount = 24; outerCount = 32;
                    break;
                default: // Город
                    inner = new[] { new Prop("roads/dumpster", 1.4f, true, 1.3f), new Prop("roads/construction-barrier", 1f, true, 1.2f), new Prop("town/stall-red", 2.6f, true, 1.6f),
                        new Prop("pirate/crate", 1.1f) };
                    small = new[] { new Prop("roads/construction-cone", 0.7f, false), new Prop("city/detail-parasol-a", 2.6f, false), new Prop("roads/light-square", 5f, false),
                        new Prop("roads/road-sign-stop", 2.4f, false) };
                    outer = new[] { new Prop("city/building-a", 12f, false, 6f), new Prop("city/building-c", 14f, false, 6f), new Prop("city/building-e", 12f, false, 6f),
                        new Prop("city/building-g", 16f, false, 6f), new Prop("city/building-i", 13f, false, 6f), new Prop("city/building-k", 15f, false, 6f),
                        new Prop("city/building-skyscraper-a", 30f, false, 7f), new Prop("city/building-skyscraper-c", 34f, false, 7f), new Prop("city/building-skyscraper-e", 28f, false, 7f) };
                    innerCount = 8; smallCount = 12; outerCount = 26;
                    break;
            }

            // Проверяем, что пак на месте (иначе тихо выходим)
            if (LoadProp(inner[0].path) == null) return;

            var group = Group("KitDecor", Vector3.zero);
            for (int i = 0; i < innerCount; i++)
            {
                var p = inner[RI(0, inner.Length)];
                if (FindSpot(p.radius, out var pos, 4f)) Place(group, p, pos, true);
            }
            for (int i = 0; i < smallCount; i++)
            {
                var p = small[RI(0, small.Length)];
                var pos2 = new Vector2(R(-half + 2f, half - 2f), R(-half + 2f, half - 2f));
                if (!IsFree(pos2, 0.3f)) continue;
                Place(group, p, new Vector3(pos2.x, 0f, pos2.y), false);
            }
            // Фон за стенами: кольцо крупных объектов
            for (int i = 0; i < outerCount; i++)
            {
                var p = outer[RI(0, outer.Length)];
                float a = (i + R(-0.3f, 0.3f)) / outerCount * Mathf.PI * 2f;
                float d = half + 6f + p.radius + R(0f, 14f);
                Place(group, p, new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d), false, true);
            }
        }

        void Place(Transform parent, Prop p, Vector3 position, bool collider, bool faceCenter = false)
        {
            var prefab = LoadProp(p.path);
            if (prefab == null) return;
            var holder = new GameObject(prefab.name).transform;
            holder.SetParent(parent, false);
            holder.localPosition = position;
            float yaw = faceCenter ? Mathf.Atan2(-position.x, -position.z) * Mathf.Rad2Deg + R(-25f, 25f) : R(0f, 360f);
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var model = Object.Instantiate(prefab, holder, false);

            // Размер по нужной высоте, низ модели — на земле
            var b = ModelBounds(holder, model);
            float scale = p.height / Mathf.Max(0.01f, b.size.y) * R(0.85f, 1.15f);
            model.transform.localScale *= scale;
            b = ModelBounds(holder, model);
            model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);

            bool castShadows = p.height > 1.5f;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;

            if (collider && p.solid)
            {
                b = ModelBounds(holder, model);
                var box = holder.gameObject.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = new Vector3(b.size.x * 0.85f, b.size.y, b.size.z * 0.85f);
            }
        }

        static Bounds ModelBounds(Transform space, GameObject model)
        {
            var b = new Bounds();
            bool first = true;
            var toSpace = space.worldToLocalMatrix;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var m = toSpace * mf.transform.localToWorldMatrix;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var pt = m.MultiplyPoint3x4(c);
                    if (first) { b = new Bounds(pt, Vector3.zero); first = false; }
                    else b.Encapsulate(pt);
                }
            }
            return b;
        }
    }
}
