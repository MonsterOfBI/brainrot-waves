using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Собирает игрушечные модели брейнротов из простых фигур.
    /// Модель смотрит вперёд (+Z), стоит ногами на y = 0, высота около 2 метров.
    /// </summary>
    public static partial class BrainrotModels
    {
        // ---------- Инструменты ----------

        public static Transform Pivot(Transform parent, string name, Vector3 localPosition, Vector3 localEuler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            return go.transform;
        }

        public static Transform Part(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale,
            Color color, Vector3 localEuler = default, float smoothness = 0.3f, bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = GameAssets.Lit(color, smoothness);
            if (!shadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static Transform Glow(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color, float intensity = 2f)
        {
            var t = Part(parent, type, localPosition, localScale, color, default, 0.5f, false);
            t.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(color, intensity);
            return t;
        }

        /// <summary>Мультяшный глаз: белок, зрачок и блик.</summary>
        static Transform Eye(Transform parent, Vector3 localPosition, float size, Color iris, Vector3 lookEuler = default, bool slit = false)
        {
            var eye = Part(parent, PrimitiveType.Sphere, localPosition, Vector3.one * size, Color.white, lookEuler, 0.8f, false);
            var pupil = Part(eye, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.36f), slit ? new Vector3(0.18f, 0.6f, 0.4f) : new Vector3(0.55f, 0.55f, 0.4f), iris, default, 0.9f, false);
            if (slit)
            {
                Part(eye, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.36f), new Vector3(0.6f, 0.6f, 0.3f), new Color(1f, 0.85f, 0.2f), default, 0.6f, false);
                pupil.localPosition = new Vector3(0f, 0f, 0.42f);
                pupil.GetComponent<Renderer>().sharedMaterial = GameAssets.Lit(Color.black, 0.9f);
            }
            Glow(eye, PrimitiveType.Sphere, new Vector3(0.14f, 0.16f, 0.45f), Vector3.one * 0.16f, Color.white, 1.5f);
            return eye;
        }

        static BrainrotRig NewRig(Transform parent, BrainrotId id)
        {
            var root = new GameObject("Model_" + id);
            root.transform.SetParent(parent, false);
            var rig = root.AddComponent<BrainrotRig>();
            rig.body = Pivot(root.transform, "Body", Vector3.zero);
            return rig;
        }

        public static BrainrotRig Build(BrainrotId id, Transform parent)
        {
            BrainrotRig rig;
            switch (id)
            {
                case BrainrotId.Tralalero: rig = BuildTralalero(parent); break;
                case BrainrotId.Patapim: rig = BuildPatapim(parent); break;
                case BrainrotId.Chimpanzini: rig = BuildChimpanzini(parent); break;
                case BrainrotId.Bombardiro: rig = BuildBombardiro(parent); break;
                case BrainrotId.Cappuccino: rig = BuildCappuccino(parent); break;
                case BrainrotId.TungTung: rig = BuildTungTung(parent); break;
                default: rig = BuildNew(id, parent); break;
            }
            rig.Init();
            return rig;
        }

        // ---------- Тралалеро Тралала: акула на трёх ногах в синих кроссовках ----------

        static BrainrotRig BuildTralalero(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Tralalero);
            Color blue = GameAssets.Hex("#4A8FC9");
            Color darkBlue = GameAssets.Hex("#3A72A3");
            Color belly = GameAssets.Hex("#F1F6F9");
            Color skin = GameAssets.Hex("#E8B98A");
            Color shoe = GameAssets.Hex("#1F5BD9");

            var torso = Pivot(rig.body, "Torso", new Vector3(0f, 1.3f, 0f));
            Part(torso, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.95f, 0.9f, 2.2f), blue, default, 0.55f);
            Part(torso, PrimitiveType.Sphere, new Vector3(0f, -0.17f, 0.08f), new Vector3(0.8f, 0.62f, 1.95f), belly, default, 0.5f);

            var head = Pivot(torso, "Head", new Vector3(0f, 0f, 0.75f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0.25f), new Vector3(0.78f, 0.62f, 0.85f), blue, default, 0.55f);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.12f, 0.3f), new Vector3(0.66f, 0.38f, 0.75f), belly, default, 0.5f);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.42f), new Vector3(0.52f, 0.14f, 0.42f), GameAssets.Hex("#8B1E2B"));
            for (int i = 0; i < 6; i++)
            {
                float x = -0.22f + i * 0.088f;
                Part(head, PrimitiveType.Cube, new Vector3(x, -0.11f, 0.62f), new Vector3(0.06f, 0.08f, 0.05f), Color.white, new Vector3(0f, 0f, 45f), 0.7f, false);
                Part(head, PrimitiveType.Cube, new Vector3(x + 0.04f, -0.23f, 0.6f), new Vector3(0.05f, 0.07f, 0.05f), Color.white, new Vector3(0f, 0f, 45f), 0.7f, false);
            }
            Eye(head, new Vector3(-0.3f, 0.18f, 0.32f), 0.22f, Color.black, new Vector3(0f, -35f, 0f));
            Eye(head, new Vector3(0.3f, 0.18f, 0.32f), 0.22f, Color.black, new Vector3(0f, 35f, 0f));
            // Брови «бро»
            Part(head, PrimitiveType.Cube, new Vector3(-0.28f, 0.33f, 0.36f), new Vector3(0.2f, 0.04f, 0.04f), darkBlue, new Vector3(0f, -30f, -12f));
            Part(head, PrimitiveType.Cube, new Vector3(0.28f, 0.33f, 0.36f), new Vector3(0.2f, 0.04f, 0.04f), darkBlue, new Vector3(0f, 30f, 12f));

            // Плавники
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.62f, -0.15f), new Vector3(0.08f, 0.7f, 0.55f), darkBlue, new Vector3(-32f, 0f, 0f));
            Part(torso, PrimitiveType.Cube, new Vector3(-0.55f, -0.25f, 0.3f), new Vector3(0.6f, 0.06f, 0.32f), darkBlue, new Vector3(0f, 20f, 28f));
            Part(torso, PrimitiveType.Cube, new Vector3(0.55f, -0.25f, 0.3f), new Vector3(0.6f, 0.06f, 0.32f), darkBlue, new Vector3(0f, -20f, -28f));

            var tail = Pivot(torso, "Tail", new Vector3(0f, 0.05f, -0.95f));
            rig.tail = tail;
            Part(tail, PrimitiveType.Sphere, new Vector3(0f, 0f, -0.15f), new Vector3(0.38f, 0.38f, 0.6f), blue, default, 0.55f);
            Part(tail, PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.48f), new Vector3(0.08f, 0.75f, 0.32f), darkBlue, new Vector3(-38f, 0f, 0f));
            Part(tail, PrimitiveType.Cube, new Vector3(0f, -0.18f, -0.42f), new Vector3(0.08f, 0.45f, 0.28f), darkBlue, new Vector3(38f, 0f, 0f));

            // Три ноги в кроссовках
            Vector3[] legPositions = { new Vector3(-0.3f, -0.38f, 0.35f), new Vector3(0.3f, -0.38f, 0.35f), new Vector3(0f, -0.38f, -0.45f) };
            foreach (var p in legPositions)
            {
                var leg = Pivot(torso, "Leg", p);
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.33f, 0f), new Vector3(0.15f, 0.33f, 0.15f), skin);
                Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.78f, 0.08f), new Vector3(0.3f, 0.2f, 0.48f), shoe, default, 0.5f);
                Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.88f, 0.08f), new Vector3(0.32f, 0.07f, 0.52f), Color.white);
                Part(leg, PrimitiveType.Cube, new Vector3(-0.155f, -0.78f, 0.05f), new Vector3(0.02f, 0.05f, 0.3f), Color.white, new Vector3(18f, 0f, 0f), 0.5f, false);
                Part(leg, PrimitiveType.Cube, new Vector3(0.155f, -0.78f, 0.05f), new Vector3(0.02f, 0.05f, 0.3f), Color.white, new Vector3(18f, 0f, 0f), 0.5f, false);
                Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.66f, 0f), new Vector3(0.2f, 0.06f, 0.2f), Color.white);
            }
            return rig;
        }

        // ---------- Брр Брр Патапим: дерево с лицом обезьяны и огромными ногами ----------

        static BrainrotRig BuildPatapim(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Patapim);
            Color bark = GameAssets.Hex("#7A5230");
            Color barkDark = GameAssets.Hex("#5C3B1F");
            Color leaf = GameAssets.Hex("#4CAF50");
            Color leafLight = GameAssets.Hex("#7CCB5B");
            Color face = GameAssets.Hex("#E9C9A0");
            Color skin = GameAssets.Hex("#D9A27A");

            var trunk = Pivot(rig.body, "Trunk", Vector3.zero);
            Part(trunk, PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), new Vector3(0.62f, 0.6f, 0.62f), bark);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                var q = Quaternion.Euler(0f, a, 0f);
                Part(trunk, PrimitiveType.Cube, q * new Vector3(0f, 0f, 0.3f) + new Vector3(0f, 1.15f, 0f), new Vector3(0.05f, 1.1f, 0.05f), barkDark, new Vector3(0f, a, 0f));
            }
            // Корни у основания
            for (int i = 0; i < 4; i++)
            {
                float a = 45f + i * 90f;
                var q = Quaternion.Euler(0f, a, 0f);
                Part(trunk, PrimitiveType.Capsule, q * new Vector3(0f, 0f, 0.32f) + new Vector3(0f, 0.68f, 0f), new Vector3(0.14f, 0.18f, 0.14f), barkDark, new Vector3(35f, a, 0f));
            }

            var head = Pivot(trunk, "Head", new Vector3(0f, 1.5f, 0f));
            rig.head = head;
            // Крона
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.6f, -0.05f), new Vector3(1.15f, 0.85f, 1.05f), leaf, default, 0.2f);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.42f, 0.45f, -0.1f), new Vector3(0.7f, 0.6f, 0.7f), leafLight, default, 0.2f);
            Part(head, PrimitiveType.Sphere, new Vector3(0.42f, 0.5f, -0.05f), new Vector3(0.7f, 0.6f, 0.7f), leafLight, default, 0.2f);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.85f, -0.25f), new Vector3(0.75f, 0.6f, 0.7f), leafLight, default, 0.2f);
            // Лицо
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.02f, 0.27f), new Vector3(0.56f, 0.6f, 0.25f), face);
            Part(head, PrimitiveType.Capsule, new Vector3(0f, -0.12f, 0.46f), new Vector3(0.17f, 0.22f, 0.17f), skin, new Vector3(55f, 0f, 0f));
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.3f, 0.58f), new Vector3(0.2f, 0.17f, 0.17f), skin);
            Eye(head, new Vector3(-0.13f, 0.12f, 0.36f), 0.17f, GameAssets.Hex("#3B2412"));
            Eye(head, new Vector3(0.13f, 0.12f, 0.36f), 0.17f, GameAssets.Hex("#3B2412"));
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.25f, 0.38f), new Vector3(0.16f, 0.03f, 0.03f), barkDark);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.34f, 0.0f, 0.15f), new Vector3(0.18f, 0.22f, 0.08f), face, new Vector3(0f, -30f, 0f));
            Part(head, PrimitiveType.Sphere, new Vector3(0.34f, 0.0f, 0.15f), new Vector3(0.18f, 0.22f, 0.08f), face, new Vector3(0f, 30f, 0f));

            // Руки-ветки
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Pivot(trunk, "Arm", new Vector3(0.33f * side, 1.45f, 0f), new Vector3(0f, 0f, 35f * side));
                rig.arms.Add(arm);
                Part(arm, PrimitiveType.Cylinder, new Vector3(0f, -0.32f, 0f), new Vector3(0.08f, 0.32f, 0.08f), bark);
                Part(arm, PrimitiveType.Cylinder, new Vector3(0.08f * side, -0.55f, 0.05f), new Vector3(0.05f, 0.12f, 0.05f), bark, new Vector3(30f, 0f, -40f * side));
                Part(arm, PrimitiveType.Sphere, new Vector3(0f, -0.66f, 0f), new Vector3(0.22f, 0.12f, 0.2f), leafLight, default, 0.2f);
            }

            // Огромные ноги
            for (int side = -1; side <= 1; side += 2)
            {
                var leg = Pivot(trunk, "Leg", new Vector3(0.2f * side, 0.62f, 0f));
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.25f, 0f), new Vector3(0.12f, 0.25f, 0.12f), skin);
                Part(leg, PrimitiveType.Sphere, new Vector3(0f, -0.53f, 0.17f), new Vector3(0.36f, 0.17f, 0.62f), skin);
                for (int t = -1; t <= 1; t++)
                    Part(leg, PrimitiveType.Sphere, new Vector3(t * 0.1f, -0.55f, 0.47f), Vector3.one * 0.11f, skin);
                Part(leg, PrimitiveType.Sphere, new Vector3(0.17f * -side, -0.52f, 0.46f), Vector3.one * 0.13f, skin);
            }
            return rig;
        }

        // ---------- Шимпанзини Бананини: шимпанзе в банане ----------

        static BrainrotRig BuildChimpanzini(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Chimpanzini);
            Color banana = GameAssets.Hex("#FFD93B");
            Color bananaLight = GameAssets.Hex("#FFE98A");
            Color bananaTip = GameAssets.Hex("#6B4226");
            Color fur = GameAssets.Hex("#6D4C33");
            Color face = GameAssets.Hex("#E8C49A");

            var torso = Pivot(rig.body, "Torso", Vector3.zero);
            // Изогнутый банан
            Part(torso, PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0.08f), new Vector3(0.66f, 0.42f, 0.66f), banana, new Vector3(-12f, 0f, 0f), 0.6f);
            Part(torso, PrimitiveType.Capsule, new Vector3(0f, 1.25f, 0f), new Vector3(0.7f, 0.42f, 0.7f), banana, new Vector3(6f, 0f, 0f), 0.6f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 45f;
                var q = Quaternion.Euler(0f, a, 0f);
                Part(torso, PrimitiveType.Cube, q * new Vector3(0f, 0f, 0.33f) + new Vector3(0f, 1.05f, 0.03f), new Vector3(0.03f, 0.75f, 0.03f), GameAssets.Hex("#E0B92A"), new Vector3(0f, a, 0f));
            }
            Part(torso, PrimitiveType.Sphere, new Vector3(0f, 0.47f, 0.13f), new Vector3(0.2f, 0.15f, 0.2f), bananaTip);

            // Лепестки кожуры
            for (int i = 0; i < 4; i++)
            {
                var flap = Pivot(torso, "Peel", new Vector3(0f, 1.52f, 0f), new Vector3(0f, i * 90f, 0f));
                Part(flap, PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.5f), new Vector3(0.34f, 0.5f, 0.06f), banana, new Vector3(118f, 0f, 0f), 0.6f);
                Part(flap, PrimitiveType.Cube, new Vector3(0f, 0.01f, 0.48f), new Vector3(0.28f, 0.45f, 0.05f), bananaLight, new Vector3(118f, 0f, 0f), 0.4f);
            }

            var head = Pivot(torso, "Head", new Vector3(0f, 1.78f, 0f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.58f, 0.56f, 0.55f), fur);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.03f, 0.2f), new Vector3(0.44f, 0.38f, 0.22f), face);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.13f, 0.28f), new Vector3(0.32f, 0.2f, 0.2f), face);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.05f, -0.09f, 0.38f), Vector3.one * 0.04f, bananaTip);
            Part(head, PrimitiveType.Sphere, new Vector3(0.05f, -0.09f, 0.38f), Vector3.one * 0.04f, bananaTip);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.19f, 0.37f), new Vector3(0.16f, 0.025f, 0.03f), bananaTip, new Vector3(0f, 0f, 0f));
            Eye(head, new Vector3(-0.11f, 0.08f, 0.29f), 0.14f, Color.black);
            Eye(head, new Vector3(0.11f, 0.08f, 0.29f), 0.14f, Color.black);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.31f, 0.03f, 0f), new Vector3(0.1f, 0.2f, 0.18f), face);
            Part(head, PrimitiveType.Sphere, new Vector3(0.31f, 0.03f, 0f), new Vector3(0.1f, 0.2f, 0.18f), face);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.27f, -0.02f), new Vector3(0.15f, 0.08f, 0.15f), fur);

            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Pivot(torso, "Arm", new Vector3(0.36f * side, 1.3f, 0.05f), new Vector3(0f, 0f, 20f * side));
                rig.arms.Add(arm);
                Part(arm, PrimitiveType.Capsule, new Vector3(0f, -0.3f, 0f), new Vector3(0.12f, 0.3f, 0.12f), fur);
                Part(arm, PrimitiveType.Sphere, new Vector3(0f, -0.62f, 0f), new Vector3(0.16f, 0.14f, 0.16f), face);
                if (side == 1)
                    Part(arm, PrimitiveType.Capsule, new Vector3(0f, -0.66f, 0.12f), new Vector3(0.09f, 0.17f, 0.09f), banana, new Vector3(70f, 0f, 20f), 0.6f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var leg = Pivot(torso, "Leg", new Vector3(0.17f * side, 0.5f, 0.05f));
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Capsule, new Vector3(0f, -0.22f, 0f), new Vector3(0.14f, 0.22f, 0.14f), fur);
                Part(leg, PrimitiveType.Sphere, new Vector3(0f, -0.45f, 0.08f), new Vector3(0.2f, 0.1f, 0.3f), face);
            }
            return rig;
        }

        // ---------- Бомбардиро Крокодило: крокодил-бомбардировщик ----------

        static BrainrotRig BuildBombardiro(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Bombardiro);
            rig.flying = true;
            Color green = GameAssets.Hex("#5E8C31");
            Color greenLight = GameAssets.Hex("#86B04F");
            Color military = GameAssets.Hex("#556B2F");
            Color metal = GameAssets.Hex("#3B3B3B");

            var plane = Pivot(rig.body, "Plane", new Vector3(0f, 1.3f, 0f));
            // Фюзеляж-тело крокодила
            Part(plane, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.8f, 1.3f, 0.8f), green, new Vector3(90f, 0f, 0f), 0.4f);
            Part(plane, PrimitiveType.Capsule, new Vector3(0f, -0.15f, 0.05f), new Vector3(0.65f, 1.15f, 0.6f), greenLight, new Vector3(90f, 0f, 0f), 0.4f);
            // Гребень на спине
            for (int i = 0; i < 7; i++)
                Part(plane, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0.7f - i * 0.28f), new Vector3(0.12f, 0.12f, 0.12f), military, new Vector3(45f, 0f, 0f));
            // Кабина
            Part(plane, PrimitiveType.Sphere, new Vector3(0f, 0.38f, 0.45f), new Vector3(0.42f, 0.32f, 0.6f), GameAssets.Hex("#9FD8F0"), default, 0.95f);

            var head = Pivot(plane, "Head", new Vector3(0f, 0f, 1.1f));
            rig.head = head;
            Part(head, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.5f), new Vector3(0.6f, 0.22f, 1.0f), green, default, 0.4f);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.18f, 0.45f), new Vector3(0.55f, 0.14f, 0.9f), greenLight, new Vector3(10f, 0f, 0f), 0.4f);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.06f, 0.48f), new Vector3(0.5f, 0.08f, 0.85f), GameAssets.Hex("#8B1E2B"));
            for (int i = 0; i < 6; i++)
            {
                float z = 0.15f + i * 0.15f;
                Part(head, PrimitiveType.Cube, new Vector3(-0.27f, -0.08f, z), new Vector3(0.05f, 0.09f, 0.05f), Color.white, new Vector3(0f, 0f, 45f), 0.7f, false);
                Part(head, PrimitiveType.Cube, new Vector3(0.27f, -0.08f, z), new Vector3(0.05f, 0.09f, 0.05f), Color.white, new Vector3(0f, 0f, 45f), 0.7f, false);
            }
            Part(head, PrimitiveType.Sphere, new Vector3(-0.12f, 0.17f, 0.95f), Vector3.one * 0.07f, metal);
            Part(head, PrimitiveType.Sphere, new Vector3(0.12f, 0.17f, 0.95f), Vector3.one * 0.07f, metal);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.2f, 0.25f, 0.05f), Vector3.one * 0.3f, green);
            Part(head, PrimitiveType.Sphere, new Vector3(0.2f, 0.25f, 0.05f), Vector3.one * 0.3f, green);
            Eye(head, new Vector3(-0.2f, 0.33f, 0.12f), 0.2f, Color.black, new Vector3(-10f, -15f, 0f), true);
            Eye(head, new Vector3(0.2f, 0.33f, 0.12f), 0.2f, Color.black, new Vector3(-10f, 15f, 0f), true);

            // Крылья с двигателями
            for (int side = -1; side <= 1; side += 2)
            {
                var wing = Pivot(plane, "Wing", new Vector3(0.35f * side, 0f, 0.15f));
                rig.wings.Add(wing);
                Part(wing, PrimitiveType.Cube, new Vector3(1.4f * side, 0f, 0f), new Vector3(2.8f, 0.09f, 0.75f), military, default, 0.3f);
                Part(wing, PrimitiveType.Cylinder, new Vector3(1.6f * side, 0.05f, 0f), new Vector3(0.45f, 0.01f, 0.45f), Color.white, default, 0.3f, false);
                Part(wing, PrimitiveType.Cylinder, new Vector3(1.6f * side, 0.06f, 0f), new Vector3(0.28f, 0.01f, 0.28f), GameAssets.Hex("#D32F2F"), default, 0.3f, false);
                Part(wing, PrimitiveType.Cylinder, new Vector3(0.75f * side, -0.05f, 0.2f), new Vector3(0.32f, 0.35f, 0.32f), metal, new Vector3(90f, 0f, 0f), 0.6f);
                var prop = Pivot(wing, "Propeller", new Vector3(0.75f * side, -0.05f, 0.6f));
                rig.spinners.Add(prop);
                Part(prop, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.15f, GameAssets.Hex("#C62828"));
                Part(prop, PrimitiveType.Cube, Vector3.zero, new Vector3(0.08f, 0.8f, 0.03f), metal, default, 0.6f, false);
                Part(prop, PrimitiveType.Cube, Vector3.zero, new Vector3(0.8f, 0.08f, 0.03f), metal, default, 0.6f, false);
            }

            // Хвост
            var tail = Pivot(plane, "Tail", new Vector3(0f, 0f, -1.1f));
            rig.tail = tail;
            Part(tail, PrimitiveType.Capsule, new Vector3(0f, 0f, -0.3f), new Vector3(0.4f, 0.45f, 0.4f), green, new Vector3(90f, 0f, 0f), 0.4f);
            Part(tail, PrimitiveType.Cube, new Vector3(0f, 0.38f, -0.5f), new Vector3(0.08f, 0.62f, 0.45f), military, new Vector3(-15f, 0f, 0f));
            Part(tail, PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.55f), new Vector3(1.4f, 0.06f, 0.4f), military);

            // Бомбы
            for (int side = -1; side <= 1; side += 2)
            {
                Part(plane, PrimitiveType.Capsule, new Vector3(0.28f * side, -0.48f, 0f), new Vector3(0.22f, 0.3f, 0.22f), metal, new Vector3(90f, 0f, 0f), 0.6f);
                Part(plane, PrimitiveType.Sphere, new Vector3(0.28f * side, -0.48f, 0.3f), Vector3.one * 0.12f, GameAssets.Hex("#D32F2F"));
            }
            return rig;
        }

        // ---------- Капучино Ассассино: чашка-ниндзя с катанами ----------

        static BrainrotRig BuildCappuccino(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Cappuccino);
            Color cup = GameAssets.Hex("#F5F1E8");
            Color coffee = GameAssets.Hex("#8B5A2B");
            Color foam = GameAssets.Hex("#E8D2B0");
            Color black = GameAssets.Hex("#1A1A1F");
            Color steel = GameAssets.Hex("#D8DEE6");

            var torso = Pivot(rig.body, "Cup", Vector3.zero);
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 0.52f, 0f), new Vector3(1.05f, 0.03f, 1.05f), cup, default, 0.8f);
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 1.08f, 0f), new Vector3(0.78f, 0.55f, 0.78f), cup, default, 0.8f);
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 0.66f, 0f), new Vector3(0.66f, 0.12f, 0.66f), cup, default, 0.8f);
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 1.62f, 0f), new Vector3(0.7f, 0.02f, 0.7f), coffee, default, 0.6f);
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 1.635f, 0f), new Vector3(0.52f, 0.02f, 0.52f), foam, default, 0.3f);
            Part(torso, PrimitiveType.Sphere, new Vector3(-0.06f, 1.655f, 0.04f), new Vector3(0.14f, 0.02f, 0.14f), Color.white);
            Part(torso, PrimitiveType.Sphere, new Vector3(0.06f, 1.655f, 0.04f), new Vector3(0.14f, 0.02f, 0.14f), Color.white);
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 1.655f, -0.04f), new Vector3(0.12f, 0.02f, 0.12f), Color.white, new Vector3(0f, 45f, 0f));

            // Ручка чашки
            Part(torso, PrimitiveType.Cube, new Vector3(0.47f, 1.3f, 0f), new Vector3(0.2f, 0.08f, 0.09f), cup, default, 0.8f);
            Part(torso, PrimitiveType.Cube, new Vector3(0.47f, 0.85f, 0f), new Vector3(0.2f, 0.08f, 0.09f), cup, default, 0.8f);
            Part(torso, PrimitiveType.Cube, new Vector3(0.57f, 1.075f, 0f), new Vector3(0.08f, 0.53f, 0.09f), cup, default, 0.8f);

            var head = Pivot(torso, "Mask", new Vector3(0f, 1.32f, 0f));
            rig.head = head;
            Part(head, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.8f, 0.11f, 0.8f), black, default, 0.4f);
            Eye(head, new Vector3(-0.15f, 0.01f, 0.36f), 0.15f, Color.black, new Vector3(0f, -15f, 0f));
            Eye(head, new Vector3(0.15f, 0.01f, 0.36f), 0.15f, Color.black, new Vector3(0f, 15f, 0f));
            Part(head, PrimitiveType.Cube, new Vector3(-0.15f, 0.1f, 0.39f), new Vector3(0.18f, 0.04f, 0.04f), black, new Vector3(0f, -15f, -15f));
            Part(head, PrimitiveType.Cube, new Vector3(0.15f, 0.1f, 0.39f), new Vector3(0.18f, 0.04f, 0.04f), black, new Vector3(0f, 15f, 15f));
            var knot = Pivot(head, "Knot", new Vector3(0f, 0f, -0.4f));
            rig.tail = knot;
            Part(knot, PrimitiveType.Cube, new Vector3(-0.08f, -0.05f, -0.2f), new Vector3(0.07f, 0.04f, 0.42f), black, new Vector3(25f, -15f, 0f));
            Part(knot, PrimitiveType.Cube, new Vector3(0.08f, -0.05f, -0.2f), new Vector3(0.07f, 0.04f, 0.42f), black, new Vector3(25f, 15f, 0f));

            // Руки с катанами
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Pivot(torso, "Arm", new Vector3(0.36f * side, 1.1f, 0.2f), new Vector3(0f, 0f, 15f * side));
                rig.arms.Add(arm);
                Part(arm, PrimitiveType.Capsule, new Vector3(0f, -0.25f, 0f), new Vector3(0.09f, 0.25f, 0.09f), black);
                Part(arm, PrimitiveType.Sphere, new Vector3(0f, -0.5f, 0f), Vector3.one * 0.12f, black);
                var katana = Pivot(arm, "Katana", new Vector3(0f, -0.5f, 0f), new Vector3(75f, 0f, 0f));
                Part(katana, PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(0.05f, 0.25f, 0.06f), GameAssets.Hex("#7B1E1E"));
                Part(katana, PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f), new Vector3(0.16f, 0.03f, 0.1f), GameAssets.Hex("#E0B040"), default, 0.8f);
                Part(katana, PrimitiveType.Cube, new Vector3(0f, 0.52f, 0f), new Vector3(0.03f, 0.95f, 0.09f), steel, default, 0.95f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var leg = Pivot(torso, "Leg", new Vector3(0.16f * side, 0.5f, 0f));
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.22f, 0f), new Vector3(0.09f, 0.22f, 0.09f), black);
                Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.45f, 0.05f), new Vector3(0.15f, 0.08f, 0.24f), black);
            }
            return rig;
        }

        // ---------- Тунг Тунг Тунг Сахур: деревянный человечек с битой ----------

        static BrainrotRig BuildTungTung(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.TungTung);
            Color wood = GameAssets.Hex("#C8935A");
            Color woodDark = GameAssets.Hex("#A9744F");
            Color woodLight = GameAssets.Hex("#E2B88A");
            Color bat = GameAssets.Hex("#6B4226");

            var log = Pivot(rig.body, "Log", Vector3.zero);
            Part(log, PrimitiveType.Cylinder, new Vector3(0f, 1.25f, 0f), new Vector3(0.56f, 0.85f, 0.56f), wood, default, 0.2f);
            Part(log, PrimitiveType.Cylinder, new Vector3(0f, 2.105f, 0f), new Vector3(0.5f, 0.01f, 0.5f), woodLight, default, 0.2f);
            Part(log, PrimitiveType.Cylinder, new Vector3(0f, 2.11f, 0f), new Vector3(0.32f, 0.01f, 0.32f), woodDark, default, 0.2f);
            Part(log, PrimitiveType.Cylinder, new Vector3(0f, 2.115f, 0f), new Vector3(0.14f, 0.01f, 0.14f), woodLight, default, 0.2f);
            Part(log, PrimitiveType.Cylinder, new Vector3(0f, 0.75f, 0f), new Vector3(0.575f, 0.02f, 0.575f), woodDark, default, 0.2f);
            Part(log, PrimitiveType.Cylinder, new Vector3(0f, 1.05f, 0f), new Vector3(0.575f, 0.015f, 0.575f), woodDark, default, 0.2f);
            // Сучок
            Part(log, PrimitiveType.Cylinder, new Vector3(-0.27f, 1.0f, 0.05f), new Vector3(0.1f, 0.06f, 0.1f), woodDark, new Vector3(0f, 0f, 90f));

            var head = Pivot(log, "Face", new Vector3(0f, 1.6f, 0f));
            rig.head = head;
            Eye(head, new Vector3(-0.13f, 0.05f, 0.25f), 0.26f, Color.black);
            Eye(head, new Vector3(0.13f, 0.05f, 0.25f), 0.26f, Color.black);
            Part(head, PrimitiveType.Cube, new Vector3(-0.13f, 0.24f, 0.27f), new Vector3(0.22f, 0.05f, 0.05f), GameAssets.Hex("#3B2412"), new Vector3(0f, 0f, -14f));
            Part(head, PrimitiveType.Cube, new Vector3(0.13f, 0.24f, 0.27f), new Vector3(0.22f, 0.05f, 0.05f), GameAssets.Hex("#3B2412"), new Vector3(0f, 0f, 14f));
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.1f, 0.29f), new Vector3(0.08f, 0.1f, 0.08f), woodDark);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.25f, 0.28f), new Vector3(0.24f, 0.05f, 0.04f), GameAssets.Hex("#3B2412"));

            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Pivot(log, "Arm", new Vector3(0.31f * side, 1.55f, 0f), new Vector3(0f, 0f, 10f * side));
                rig.arms.Add(arm);
                Part(arm, PrimitiveType.Cylinder, new Vector3(0f, -0.38f, 0f), new Vector3(0.08f, 0.38f, 0.08f), woodDark, default, 0.2f);
                Part(arm, PrimitiveType.Sphere, new Vector3(0f, -0.78f, 0f), Vector3.one * 0.14f, woodDark);
                if (side == 1)
                {
                    var b = Pivot(arm, "Bat", new Vector3(0f, -0.78f, 0f), new Vector3(35f, 0f, -20f));
                    Part(b, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.06f, 0.22f, 0.06f), bat, default, 0.4f);
                    Part(b, PrimitiveType.Capsule, new Vector3(0f, 0.8f, 0f), new Vector3(0.2f, 0.55f, 0.2f), bat, default, 0.4f);
                    Part(b, PrimitiveType.Cylinder, new Vector3(0f, -0.07f, 0f), new Vector3(0.09f, 0.02f, 0.09f), bat, default, 0.4f);
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var leg = Pivot(log, "Leg", new Vector3(0.15f * side, 0.42f, 0f));
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.2f, 0f), new Vector3(0.1f, 0.2f, 0.1f), woodDark, default, 0.2f);
                Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.4f, 0.05f), new Vector3(0.16f, 0.08f, 0.26f), GameAssets.Hex("#5C3B1F"));
            }
            return rig;
        }
    }
}
