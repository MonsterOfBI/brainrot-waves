using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Модели 20 новых брейнротов (выживание и защита). Собраны из простых фигур
    /// в том же стиле, что и сюжетные герои: смотрят вперёд (+Z), стоят на y = 0, рост около 2 м.
    /// </summary>
    public static partial class BrainrotModels
    {
        static Color H(string hex) => GameAssets.Hex(hex);

        // ---------- Общие детали ----------

        /// <summary>Две ноги (человеческие или звериные) с обувью.</summary>
        static void TwoLegs(BrainrotRig rig, Transform parent, float hipY, float spread, float thick, Color skin, Color shoe, float shoeLength = 0.3f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var leg = Pivot(parent, "Leg", new Vector3(spread * side, hipY, 0f));
                rig.legs.Add(leg);
                float len = hipY - 0.06f;
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -len * 0.5f, 0f), new Vector3(thick, len * 0.5f, thick), skin);
                Part(leg, PrimitiveType.Cube, new Vector3(0f, -hipY + 0.05f, 0.06f), new Vector3(thick * 1.7f, 0.1f, shoeLength), shoe, default, 0.5f);
            }
        }

        /// <summary>Четыре ноги животного.</summary>
        static void FourLegs(BrainrotRig rig, Transform parent, float hipY, float halfWidth, float halfLength, float thick, Color skin, Color hoof)
        {
            Vector3[] spots = { new Vector3(-halfWidth, hipY, halfLength), new Vector3(halfWidth, hipY, halfLength), new Vector3(halfWidth, hipY, -halfLength), new Vector3(-halfWidth, hipY, -halfLength) };
            foreach (var s in spots)
            {
                var leg = Pivot(parent, "Leg", s);
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -hipY * 0.5f, 0f), new Vector3(thick, hipY * 0.5f, thick), skin);
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -hipY + 0.05f, 0f), new Vector3(thick * 1.25f, 0.05f, thick * 1.25f), hoof);
            }
        }

        static void TwoArms(BrainrotRig rig, Transform parent, float shoulderY, float spread, float length, float thick, Color skin, Color hand, float outward = 15f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Pivot(parent, "Arm", new Vector3(spread * side, shoulderY, 0f), new Vector3(0f, 0f, outward * side));
                rig.arms.Add(arm);
                Part(arm, PrimitiveType.Capsule, new Vector3(0f, -length * 0.5f, 0f), new Vector3(thick, length * 0.5f, thick), skin);
                Part(arm, PrimitiveType.Sphere, new Vector3(0f, -length, 0f), Vector3.one * thick * 1.4f, hand);
            }
        }

        static BrainrotRig BuildNew(BrainrotId id, Transform parent)
        {
            switch (id)
            {
                case BrainrotId.Ballerina: return Ballerina(parent);
                case BrainrotId.Lirili: return Lirili(parent);
                case BrainrotId.Boneca: return Boneca(parent);
                case BrainrotId.TrippiTroppi: return Trippi(parent);
                case BrainrotId.FrigoCamelo: return Frigo(parent);
                case BrainrotId.VacaSaturno: return Vaca(parent);
                case BrainrotId.Bobritto: return Bobritto(parent);
                case BrainrotId.Glorbo: return Glorbo(parent);
                case BrainrotId.Burbaloni: return Burbaloni(parent);
                case BrainrotId.Trulimero: return Trulimero(parent);
                case BrainrotId.ChefCrab: return Chef(parent);
                case BrainrotId.Svinino: return Svinino(parent);
                case BrainrotId.Orangutini: return Orangutini(parent);
                case BrainrotId.CactoHippo: return Cacto(parent);
                case BrainrotId.Bombombini: return Gusini(parent);
                case BrainrotId.RhinoToaster: return Rhino(parent);
                case BrainrotId.Blueberrini: return Blueberrini(parent);
                case BrainrotId.Cocofanto: return Cocofanto(parent);
                case BrainrotId.Girafa: return Girafa(parent);
                default: return TaTaTa(parent);
            }
        }

        // ---------- Балерина Капучина: балерина с чашкой капучино вместо головы ----------
        static BrainrotRig Ballerina(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Ballerina);
            Color pink = H("#F48FB1"), pinkLight = H("#F8BBD0"), skin = H("#F1C9A5"), cup = H("#FAFAFA"), coffee = H("#8D5B3B"), foam = H("#F3E2C7");
            var torso = Pivot(rig.body, "Torso", Vector3.zero);
            // Пачка
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 1.05f, 0f), new Vector3(1.25f, 0.06f, 1.25f), pinkLight, default, 0.4f);
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 1.11f, 0f), new Vector3(1.05f, 0.05f, 1.05f), pink, default, 0.4f);
            Part(torso, PrimitiveType.Capsule, new Vector3(0f, 1.38f, 0f), new Vector3(0.36f, 0.32f, 0.26f), pink, default, 0.5f);
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 1.72f, 0f), new Vector3(0.1f, 0.06f, 0.1f), skin);
            // Голова-чашка
            var head = Pivot(torso, "Head", new Vector3(0f, 1.78f, 0f));
            rig.head = head;
            Part(head, PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.25f, 0.6f), cup, default, 0.8f);
            Part(head, PrimitiveType.Cylinder, new Vector3(0f, 0.505f, 0f), new Vector3(0.54f, 0.01f, 0.54f), coffee, default, 0.6f);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.515f, 0f), new Vector3(0.3f, 0.02f, 0.26f), foam, default, 0.3f, false);
            Part(head, PrimitiveType.Cube, new Vector3(0.36f, 0.25f, 0f), new Vector3(0.1f, 0.24f, 0.06f), cup, default, 0.8f);
            Part(head, PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(0.7f, 0.02f, 0.7f), cup, default, 0.8f);
            Eye(head, new Vector3(-0.12f, 0.3f, 0.27f), 0.16f, H("#5D4037"));
            Eye(head, new Vector3(0.12f, 0.3f, 0.27f), 0.16f, H("#5D4037"));
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.13f, 0.29f), new Vector3(0.12f, 0.05f, 0.03f), H("#E57373"));
            // Руки над головой и ноги на пуантах
            TwoArms(rig, torso, 1.62f, 0.3f, 0.55f, 0.07f, skin, skin, 150f);
            TwoLegs(rig, torso, 1.0f, 0.12f, 0.07f, skin, pink, 0.14f);
            return rig;
        }

        // ---------- Лирили Ларила: слон-кактус в сандалиях с часами ----------
        static BrainrotRig Lirili(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Lirili);
            Color green = H("#7CB342"), greenDark = H("#558B2F"), spike = H("#F1F8E9"), sandal = H("#8D6E63");
            var body = Pivot(rig.body, "Torso", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.15f, -0.1f), new Vector3(1.3f, 1.05f, 1.6f), green, default, 0.25f);
            for (int i = 0; i < 18; i++)
            {
                var q = Quaternion.Euler(Random.Range(-60f, 40f), i * 20f, 0f);
                Part(body, PrimitiveType.Cube, new Vector3(0f, 1.15f, -0.1f) + q * new Vector3(0f, 0.2f, 0.62f), new Vector3(0.03f, 0.03f, 0.16f), spike, q.eulerAngles, 0.5f, false);
            }
            var head = Pivot(body, "Head", new Vector3(0f, 1.45f, 0.75f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.75f, 0.7f, 0.7f), green, default, 0.25f);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.42f, 0.05f, -0.1f), new Vector3(0.12f, 0.6f, 0.5f), greenDark, new Vector3(0f, -20f, 0f));
            Part(head, PrimitiveType.Sphere, new Vector3(0.42f, 0.05f, -0.1f), new Vector3(0.12f, 0.6f, 0.5f), greenDark, new Vector3(0f, 20f, 0f));
            var trunk = Pivot(head, "Trunk", new Vector3(0f, -0.1f, 0.32f), new Vector3(30f, 0f, 0f));
            rig.tail = trunk;
            Part(trunk, PrimitiveType.Capsule, new Vector3(0f, -0.3f, 0f), new Vector3(0.18f, 0.3f, 0.18f), green);
            Part(trunk, PrimitiveType.Capsule, new Vector3(0f, -0.62f, 0.08f), new Vector3(0.14f, 0.18f, 0.14f), green, new Vector3(-30f, 0f, 0f));
            Eye(head, new Vector3(-0.18f, 0.12f, 0.27f), 0.15f, Color.black);
            Eye(head, new Vector3(0.18f, 0.12f, 0.27f), 0.15f, Color.black);
            // Часы на боку
            var clock = Part(body, PrimitiveType.Cylinder, new Vector3(0.66f, 1.25f, 0.2f), new Vector3(0.38f, 0.04f, 0.38f), Color.white, new Vector3(0f, 0f, 90f), 0.6f);
            clock.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(H("#FFF59D"), 1.2f);
            var hand = Pivot(body, "ClockHand", new Vector3(0.69f, 1.25f, 0.2f), new Vector3(0f, 90f, 0f));
            rig.spinners.Add(hand);
            Part(hand, PrimitiveType.Cube, new Vector3(0f, 0.07f, 0f), new Vector3(0.03f, 0.14f, 0.02f), Color.black, default, 0.5f, false);
            FourLegs(rig, body, 0.62f, 0.38f, 0.42f, 0.16f, green, sandal);
            return rig;
        }

        // ---------- Бонека Амбалабу: лягушка в покрышке на человеческих ногах ----------
        static BrainrotRig Boneca(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Boneca);
            Color frog = H("#43A047"), frogLight = H("#9CCC65"), tyre = H("#212121"), rim = H("#9E9E9E"), skin = H("#E0AC69");
            var torso = Pivot(rig.body, "Torso", Vector3.zero);
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 1.15f, 0f), new Vector3(1.2f, 0.28f, 1.2f), tyre, new Vector3(90f, 0f, 0f), 0.3f);
            Part(torso, PrimitiveType.Cylinder, new Vector3(0f, 1.15f, 0.02f), new Vector3(0.7f, 0.29f, 0.7f), rim, new Vector3(90f, 0f, 0f), 0.7f);
            for (int i = 0; i < 12; i++)
            {
                var q = Quaternion.Euler(0f, 0f, i * 30f);
                Part(torso, PrimitiveType.Cube, new Vector3(0f, 1.15f, 0f) + q * new Vector3(0f, 0.6f, 0f), new Vector3(0.12f, 0.06f, 0.6f), H("#424242"), new Vector3(0f, 0f, i * 30f), 0.2f, false);
            }
            var head = Pivot(torso, "Head", new Vector3(0f, 1.15f, 0.1f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.05f), new Vector3(0.68f, 0.5f, 0.62f), frog, default, 0.6f);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.12f, 0.12f), new Vector3(0.55f, 0.25f, 0.5f), frogLight, default, 0.6f);
            Eye(head, new Vector3(-0.2f, 0.25f, 0.15f), 0.24f, Color.black);
            Eye(head, new Vector3(0.2f, 0.25f, 0.15f), 0.24f, Color.black);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.08f, 0.36f), new Vector3(0.36f, 0.025f, 0.02f), H("#1B5E20"));
            TwoLegs(rig, torso, 0.62f, 0.2f, 0.1f, skin, H("#ECEFF1"));
            TwoArms(rig, torso, 1.25f, 0.6f, 0.45f, 0.07f, skin, skin, 30f);
            return rig;
        }

        // ---------- Триппи Троппи: креветка с кошачьей головой ----------
        static BrainrotRig Trippi(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.TrippiTroppi);
            Color shrimp = H("#FF8A65"), shrimpLight = H("#FFCCBC"), cat = H("#BDBDBD"), catDark = H("#757575");
            var torso = Pivot(rig.body, "Torso", new Vector3(0f, 0.9f, 0f));
            for (int i = 0; i < 4; i++)
            {
                float s = 0.62f - i * 0.1f;
                Part(torso, PrimitiveType.Sphere, new Vector3(0f, 0.05f * i, -0.38f * i), new Vector3(s, s * 0.9f, s * 0.95f), i % 2 == 0 ? shrimp : shrimpLight, default, 0.6f);
            }
            var tail = Pivot(torso, "Tail", new Vector3(0f, 0.2f, -1.4f));
            rig.tail = tail;
            Part(tail, PrimitiveType.Cube, new Vector3(-0.12f, 0f, -0.1f), new Vector3(0.18f, 0.04f, 0.3f), shrimp, new Vector3(0f, -25f, 0f));
            Part(tail, PrimitiveType.Cube, new Vector3(0.12f, 0f, -0.1f), new Vector3(0.18f, 0.04f, 0.3f), shrimp, new Vector3(0f, 25f, 0f));
            var head = Pivot(torso, "Head", new Vector3(0f, 0.25f, 0.42f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.6f, 0.55f, 0.52f), cat, default, 0.4f);
            Part(head, PrimitiveType.Cube, new Vector3(-0.2f, 0.3f, 0f), new Vector3(0.16f, 0.16f, 0.06f), catDark, new Vector3(0f, 0f, 45f));
            Part(head, PrimitiveType.Cube, new Vector3(0.2f, 0.3f, 0f), new Vector3(0.16f, 0.16f, 0.06f), catDark, new Vector3(0f, 0f, 45f));
            Eye(head, new Vector3(-0.12f, 0.06f, 0.22f), 0.15f, H("#AED581"), default, true);
            Eye(head, new Vector3(0.12f, 0.06f, 0.22f), 0.15f, H("#AED581"), default, true);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0.27f), new Vector3(0.06f, 0.04f, 0.04f), H("#F48FB1"));
            for (int side = -1; side <= 1; side += 2)
                for (int w = 0; w < 2; w++)
                    Part(head, PrimitiveType.Cube, new Vector3(0.2f * side, -0.08f + w * 0.05f, 0.24f), new Vector3(0.22f, 0.01f, 0.01f), Color.white, new Vector3(0f, 0f, (w == 0 ? -8f : 8f) * side), 0.5f, false);
            // Много лапок
            for (int i = 0; i < 4; i++)
            {
                var leg = Pivot(torso, "Leg", new Vector3(i % 2 == 0 ? -0.2f : 0.2f, -0.2f, 0.1f - (i / 2) * 0.45f));
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.3f, 0f), new Vector3(0.05f, 0.3f, 0.05f), shrimp);
            }
            return rig;
        }

        // ---------- Фриго Камело: верблюд-холодильник в ботинках ----------
        static BrainrotRig Frigo(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.FrigoCamelo);
            Color fridge = H("#ECEFF1"), steel = H("#90A4AE"), camel = H("#C69C6D"), boot = H("#4E342E");
            var body = Pivot(rig.body, "Fridge", Vector3.zero);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.35f, -0.1f), new Vector3(0.9f, 1.3f, 0.8f), fridge, default, 0.8f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.62f, 0.31f), new Vector3(0.88f, 0.02f, 0.02f), steel, default, 0.8f, false);
            Part(body, PrimitiveType.Cube, new Vector3(0.36f, 1.35f, 0.33f), new Vector3(0.05f, 0.4f, 0.05f), steel, default, 0.9f, false);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 2.05f, -0.15f), new Vector3(0.55f, 0.35f, 0.5f), camel, default, 0.3f);
            var neck = Pivot(body, "Neck", new Vector3(0f, 1.75f, 0.35f), new Vector3(-35f, 0f, 0f));
            Part(neck, PrimitiveType.Capsule, new Vector3(0f, 0.32f, 0f), new Vector3(0.2f, 0.36f, 0.2f), camel);
            var head = Pivot(neck, "Head", new Vector3(0f, 0.68f, 0f), new Vector3(35f, 0f, 0f));
            rig.head = head;
            Part(head, PrimitiveType.Capsule, new Vector3(0f, 0f, 0.15f), new Vector3(0.26f, 0.32f, 0.26f), camel, new Vector3(90f, 0f, 0f));
            Eye(head, new Vector3(-0.11f, 0.1f, 0.12f), 0.12f, Color.black);
            Eye(head, new Vector3(0.11f, 0.1f, 0.12f), 0.12f, Color.black);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.05f, 0.42f), new Vector3(0.2f, 0.12f, 0.1f), H("#8D6E63"));
            // Иней
            var frost = Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.35f, 0.33f), new Vector3(0.3f, 0.3f, 0.02f), Color.white, default, 0.5f, false);
            frost.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(H("#B3E5FC"), 1.4f);
            FourLegs(rig, body, 0.72f, 0.3f, 0.25f, 0.09f, camel, boot);
            return rig;
        }

        // ---------- Ла Вака Сатурно Сатурнита: корова с кольцом Сатурна ----------
        static BrainrotRig Vaca(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.VacaSaturno);
            Color white = H("#FAFAFA"), black = H("#212121"), pinkNose = H("#F8BBD0"), ring = H("#FFE082"), horn = H("#FFF8E1");
            var body = Pivot(rig.body, "Torso", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.15f, 0f), new Vector3(1.05f, 0.95f, 1.5f), white, default, 0.4f);
            Vector3[] spots = { new Vector3(0.4f, 1.3f, 0.2f), new Vector3(-0.45f, 1.05f, -0.3f), new Vector3(0.2f, 1.5f, -0.4f), new Vector3(-0.2f, 1.45f, 0.45f) };
            foreach (var sp in spots) Part(body, PrimitiveType.Sphere, sp, new Vector3(0.35f, 0.3f, 0.35f), black, default, 0.4f, false);
            var head = Pivot(body, "Head", new Vector3(0f, 1.45f, 0.75f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.6f, 0.55f, 0.55f), white, default, 0.4f);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.12f, 0.25f), new Vector3(0.45f, 0.3f, 0.25f), pinkNose);
            Part(head, PrimitiveType.Capsule, new Vector3(-0.28f, 0.28f, 0f), new Vector3(0.06f, 0.14f, 0.06f), horn, new Vector3(0f, 0f, 40f));
            Part(head, PrimitiveType.Capsule, new Vector3(0.28f, 0.28f, 0f), new Vector3(0.06f, 0.14f, 0.06f), horn, new Vector3(0f, 0f, -40f));
            Eye(head, new Vector3(-0.15f, 0.1f, 0.22f), 0.14f, Color.black);
            Eye(head, new Vector3(0.15f, 0.1f, 0.22f), 0.14f, Color.black);
            // Кольцо Сатурна из светящихся камешков
            var ringPivot = Pivot(body, "Ring", new Vector3(0f, 1.2f, 0f), new Vector3(20f, 0f, 0f));
            rig.spinners.Add(ringPivot);
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2f / 20f;
                var r = Part(ringPivot, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 1.2f, Mathf.Sin(a) * 1.2f, 0f), Vector3.one * 0.14f, ring, default, 0.6f, false);
                r.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(ring, 1.3f);
            }
            ringPivot.localRotation = Quaternion.Euler(80f, 0f, 0f);
            FourLegs(rig, body, 0.62f, 0.3f, 0.45f, 0.12f, white, black);
            return rig;
        }

        // ---------- Бобритто Бандито: бобр-гангстер в шляпе с томми-ганом ----------
        static BrainrotRig Bobritto(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Bobritto);
            Color fur = H("#8D6E63"), furLight = H("#BCAAA4"), hat = H("#212121"), band = H("#B71C1C"), gun = H("#37474F"), wood = H("#6D4C41");
            var torso = Pivot(rig.body, "Torso", Vector3.zero);
            Part(torso, PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(0.75f, 0.6f, 0.65f), fur, default, 0.3f);
            Part(torso, PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0.18f), new Vector3(0.5f, 0.6f, 0.35f), furLight, default, 0.3f);
            var tail = Pivot(torso, "Tail", new Vector3(0f, 0.6f, -0.3f), new Vector3(-60f, 0f, 0f));
            rig.tail = tail;
            Part(tail, PrimitiveType.Cube, new Vector3(0f, 0f, -0.35f), new Vector3(0.4f, 0.08f, 0.6f), H("#4E342E"), default, 0.2f);
            var head = Pivot(torso, "Head", new Vector3(0f, 1.75f, 0.05f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.6f, 0.55f, 0.55f), fur, default, 0.3f);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.08f, 0.22f), new Vector3(0.35f, 0.25f, 0.25f), furLight);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.35f), new Vector3(0.12f, 0.08f, 0.08f), Color.black);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.2f, 0.32f), new Vector3(0.14f, 0.12f, 0.03f), Color.white, default, 0.7f);
            Eye(head, new Vector3(-0.15f, 0.1f, 0.2f), 0.12f, Color.black);
            Eye(head, new Vector3(0.15f, 0.1f, 0.2f), 0.12f, Color.black);
            Part(head, PrimitiveType.Cylinder, new Vector3(0f, 0.26f, 0f), new Vector3(0.75f, 0.02f, 0.75f), hat, default, 0.4f);
            Part(head, PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0f), new Vector3(0.45f, 0.12f, 0.45f), hat, default, 0.4f);
            Part(head, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.46f, 0.03f, 0.46f), band, default, 0.4f);
            TwoArms(rig, torso, 1.4f, 0.4f, 0.45f, 0.09f, fur, furLight, 10f);
            // Томми-ган в правой руке
            var gunPivot = Pivot(rig.arms[1], "Gun", new Vector3(0f, -0.45f, 0.1f), new Vector3(-80f, 0f, 0f));
            Part(gunPivot, PrimitiveType.Cube, new Vector3(0f, 0f, 0.2f), new Vector3(0.08f, 0.1f, 0.6f), gun, default, 0.6f);
            Part(gunPivot, PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0.15f), new Vector3(0.2f, 0.04f, 0.2f), gun, new Vector3(0f, 0f, 90f), 0.6f);
            Part(gunPivot, PrimitiveType.Cube, new Vector3(0f, -0.02f, -0.15f), new Vector3(0.07f, 0.1f, 0.25f), wood, default, 0.3f);
            TwoLegs(rig, torso, 0.55f, 0.2f, 0.11f, fur, H("#3E2723"));
            return rig;
        }

        // ---------- Глорбо Фруттодрилло: крокодил-арбуз ----------
        static BrainrotRig Glorbo(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Glorbo);
            Color melon = H("#2E7D32"), stripe = H("#1B5E20"), flesh = H("#E53935"), croc = H("#558B2F"), teeth = Color.white;
            var body = Pivot(rig.body, "Torso", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.05f, -0.1f), new Vector3(1.25f, 1.1f, 1.45f), melon, default, 0.5f);
            for (int i = 0; i < 6; i++)
                Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.05f, -0.1f), new Vector3(1.27f, 1.12f, 0.12f), stripe, new Vector3(0f, i * 30f, 0f), 0.5f, false);
            var head = Pivot(body, "Head", new Vector3(0f, 1.25f, 0.65f));
            rig.head = head;
            Part(head, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.35f), new Vector3(0.55f, 0.22f, 0.8f), croc, default, 0.3f);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.12f, 0.32f), new Vector3(0.5f, 0.14f, 0.72f), croc, default, 0.3f);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.32f), new Vector3(0.45f, 0.06f, 0.68f), flesh, default, 0.3f, false);
            for (int i = 0; i < 5; i++)
            {
                Part(head, PrimitiveType.Cube, new Vector3(-0.24f, 0f, 0.05f + i * 0.14f), new Vector3(0.04f, 0.08f, 0.04f), teeth, new Vector3(0f, 0f, 45f), 0.7f, false);
                Part(head, PrimitiveType.Cube, new Vector3(0.24f, 0f, 0.05f + i * 0.14f), new Vector3(0.04f, 0.08f, 0.04f), teeth, new Vector3(0f, 0f, 45f), 0.7f, false);
            }
            Eye(head, new Vector3(-0.18f, 0.28f, 0.05f), 0.18f, H("#FFEB3B"), default, true);
            Eye(head, new Vector3(0.18f, 0.28f, 0.05f), 0.18f, H("#FFEB3B"), default, true);
            var tail = Pivot(body, "Tail", new Vector3(0f, 0.8f, -0.8f));
            rig.tail = tail;
            Part(tail, PrimitiveType.Capsule, new Vector3(0f, 0f, -0.4f), new Vector3(0.25f, 0.45f, 0.2f), croc, new Vector3(90f, 0f, 0f));
            FourLegs(rig, body, 0.5f, 0.45f, 0.35f, 0.13f, croc, croc);
            return rig;
        }

        // ---------- Бурбалони Лулилоли: капибара в кокосе ----------
        static BrainrotRig Burbaloni(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Burbaloni);
            Color shell = H("#5D4037"), shellLight = H("#8D6E63"), capy = H("#A1887F"), capyDark = H("#6D4C41");
            var body = Pivot(rig.body, "Shell", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.0f, 0f), new Vector3(1.4f, 1.3f, 1.4f), shell, default, 0.2f);
            for (int i = 0; i < 10; i++)
            {
                var q = Quaternion.Euler(Random.Range(-50f, 50f), i * 36f, 0f);
                Part(body, PrimitiveType.Cube, new Vector3(0f, 1f, 0f) + q * new Vector3(0f, 0f, 0.68f), new Vector3(0.02f, 0.3f, 0.02f), shellLight, q.eulerAngles, 0.2f, false);
            }
            Part(body, PrimitiveType.Cylinder, new Vector3(0f, 1.62f, 0f), new Vector3(0.95f, 0.04f, 0.95f), H("#FFF8E1"), default, 0.3f);
            var head = Pivot(body, "Head", new Vector3(0f, 1.8f, 0.05f));
            rig.head = head;
            Part(head, PrimitiveType.Capsule, new Vector3(0f, 0.05f, 0.1f), new Vector3(0.55f, 0.36f, 0.6f), capy, new Vector3(90f, 0f, 0f), 0.3f);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0.42f), new Vector3(0.4f, 0.3f, 0.2f), capyDark, default, 0.3f);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.22f, 0.25f, -0.05f), Vector3.one * 0.12f, capyDark);
            Part(head, PrimitiveType.Sphere, new Vector3(0.22f, 0.25f, -0.05f), Vector3.one * 0.12f, capyDark);
            Eye(head, new Vector3(-0.17f, 0.16f, 0.22f), 0.1f, Color.black);
            Eye(head, new Vector3(0.17f, 0.16f, 0.22f), 0.1f, Color.black);
            for (int i = 0; i < 4; i++)
            {
                var leg = Pivot(body, "Leg", new Vector3(i % 2 == 0 ? -0.35f : 0.35f, 0.32f, i < 2 ? 0.25f : -0.25f));
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Capsule, new Vector3(0f, -0.15f, 0f), new Vector3(0.18f, 0.16f, 0.18f), capy);
            }
            return rig;
        }

        // ---------- Трулимеро Трулишина: рыба с головой кота на человеческих ногах ----------
        static BrainrotRig Trulimero(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Trulimero);
            Color fish = H("#4DD0E1"), fishDark = H("#00838F"), belly = H("#E0F7FA"), cat = H("#FFB74D"), skin = H("#F1C9A5");
            var torso = Pivot(rig.body, "Torso", new Vector3(0f, 1.25f, 0f));
            Part(torso, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.75f, 0.85f, 1.5f), fish, default, 0.8f);
            Part(torso, PrimitiveType.Sphere, new Vector3(0f, -0.15f, 0.05f), new Vector3(0.6f, 0.55f, 1.3f), belly, default, 0.7f);
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.45f, -0.1f), new Vector3(0.05f, 0.35f, 0.6f), fishDark, new Vector3(-20f, 0f, 0f));
            var tail = Pivot(torso, "Tail", new Vector3(0f, 0f, -0.75f));
            rig.tail = tail;
            Part(tail, PrimitiveType.Cube, new Vector3(0f, 0.15f, -0.2f), new Vector3(0.05f, 0.4f, 0.3f), fishDark, new Vector3(-30f, 0f, 0f));
            Part(tail, PrimitiveType.Cube, new Vector3(0f, -0.15f, -0.2f), new Vector3(0.05f, 0.4f, 0.3f), fishDark, new Vector3(30f, 0f, 0f));
            var head = Pivot(torso, "Head", new Vector3(0f, 0.15f, 0.75f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.55f, 0.5f, 0.45f), cat, default, 0.4f);
            Part(head, PrimitiveType.Cube, new Vector3(-0.18f, 0.27f, -0.02f), new Vector3(0.14f, 0.14f, 0.05f), cat, new Vector3(0f, 0f, 45f));
            Part(head, PrimitiveType.Cube, new Vector3(0.18f, 0.27f, -0.02f), new Vector3(0.14f, 0.14f, 0.05f), cat, new Vector3(0f, 0f, 45f));
            Eye(head, new Vector3(-0.11f, 0.06f, 0.19f), 0.14f, H("#66BB6A"), default, true);
            Eye(head, new Vector3(0.11f, 0.06f, 0.19f), 0.14f, H("#66BB6A"), default, true);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0.23f), new Vector3(0.05f, 0.035f, 0.035f), H("#E91E63"));
            for (int side = -1; side <= 1; side += 2)
            {
                var leg = Pivot(torso, "Leg", new Vector3(0.18f * side, -0.45f, 0f));
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.38f, 0f), new Vector3(0.1f, 0.38f, 0.1f), skin);
                Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.76f, 0.07f), new Vector3(0.17f, 0.1f, 0.3f), H("#1565C0"), default, 0.5f);
            }
            return rig;
        }

        // ---------- Шеф Крабракадабра: краб-повар и волшебник ----------
        static BrainrotRig Chef(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.ChefCrab);
            Color crab = H("#E64A19"), crabLight = H("#FF8A65"), hat = Color.white;
            var body = Pivot(rig.body, "Torso", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.95f, 0f), new Vector3(1.3f, 0.75f, 1.0f), crab, default, 0.6f);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.85f, 0.15f), new Vector3(1.0f, 0.5f, 0.75f), crabLight, default, 0.6f);
            var head = Pivot(body, "Head", new Vector3(0f, 1.3f, 0.2f));
            rig.head = head;
            for (int side = -1; side <= 1; side += 2)
            {
                Part(head, PrimitiveType.Cylinder, new Vector3(0.15f * side, 0.15f, 0f), new Vector3(0.05f, 0.15f, 0.05f), crab);
                Eye(head, new Vector3(0.15f * side, 0.33f, 0.02f), 0.16f, Color.black);
            }
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.05f, 0.25f), new Vector3(0.3f, 0.03f, 0.02f), H("#3E2723"));
            // Поварской колпак
            Part(body, PrimitiveType.Cylinder, new Vector3(0f, 1.45f, -0.15f), new Vector3(0.4f, 0.15f, 0.4f), hat, default, 0.5f);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.7f, -0.15f), new Vector3(0.55f, 0.4f, 0.55f), hat, default, 0.5f);
            // Клешни
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Pivot(body, "Arm", new Vector3(0.65f * side, 1.0f, 0.2f), new Vector3(0f, 0f, 60f * side));
                rig.arms.Add(arm);
                Part(arm, PrimitiveType.Capsule, new Vector3(0f, -0.3f, 0f), new Vector3(0.12f, 0.3f, 0.12f), crab);
                Part(arm, PrimitiveType.Sphere, new Vector3(0f, -0.7f, 0.05f), new Vector3(0.35f, 0.3f, 0.2f), crab, default, 0.6f);
                Part(arm, PrimitiveType.Cube, new Vector3(0.08f * side, -0.88f, 0.05f), new Vector3(0.08f, 0.22f, 0.12f), crab, new Vector3(0f, 0f, 20f * side), 0.6f);
            }
            // Волшебная палочка-лопатка
            var wand = Part(rig.arms[1], PrimitiveType.Cylinder, new Vector3(0f, -0.8f, 0.3f), new Vector3(0.04f, 0.25f, 0.04f), H("#FFD54F"), new Vector3(90f, 0f, 0f), 0.5f, false);
            wand.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(H("#FFD54F"), 1.6f);
            for (int i = 0; i < 6; i++)
            {
                var leg = Pivot(body, "Leg", new Vector3((i % 2 == 0 ? -1f : 1f) * 0.45f, 0.75f, 0.25f - (i / 2) * 0.25f), new Vector3(0f, 0f, (i % 2 == 0 ? 1f : -1f) * 40f));
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Capsule, new Vector3(0f, -0.4f, 0f), new Vector3(0.07f, 0.42f, 0.07f), crab);
            }
            return rig;
        }

        // ---------- Свинино Бомбондино: свинья-бомба ----------
        static BrainrotRig Svinino(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Svinino);
            Color bomb = H("#263238"), pink = H("#F48FB1"), pinkDark = H("#EC407A");
            var body = Pivot(rig.body, "Bomb", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.0f, 0f), Vector3.one * 1.35f, bomb, default, 0.7f);
            Part(body, PrimitiveType.Cylinder, new Vector3(0f, 1.72f, 0f), new Vector3(0.35f, 0.08f, 0.35f), H("#546E7A"), default, 0.7f);
            var fuse = Pivot(body, "Fuse", new Vector3(0f, 1.8f, 0f));
            rig.tail = fuse;
            Part(fuse, PrimitiveType.Capsule, new Vector3(0.08f, 0.18f, 0f), new Vector3(0.05f, 0.18f, 0.05f), H("#8D6E63"), new Vector3(0f, 0f, -25f), 0.3f, false);
            var spark = Part(fuse, PrimitiveType.Sphere, new Vector3(0.18f, 0.38f, 0f), Vector3.one * 0.16f, Color.white, default, 0.5f, false);
            spark.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(H("#FFAB00"), 4f);
            var head = Pivot(body, "Face", new Vector3(0f, 1.05f, 0.6f));
            rig.head = head;
            Part(head, PrimitiveType.Cylinder, new Vector3(0f, -0.05f, 0.08f), new Vector3(0.36f, 0.08f, 0.3f), pink, new Vector3(90f, 0f, 0f), 0.5f);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.07f, -0.05f, 0.17f), Vector3.one * 0.07f, pinkDark);
            Part(head, PrimitiveType.Sphere, new Vector3(0.07f, -0.05f, 0.17f), Vector3.one * 0.07f, pinkDark);
            Eye(head, new Vector3(-0.2f, 0.22f, 0.02f), 0.16f, Color.black);
            Eye(head, new Vector3(0.2f, 0.22f, 0.02f), 0.16f, Color.black);
            Part(body, PrimitiveType.Cube, new Vector3(-0.42f, 1.62f, 0.15f), new Vector3(0.22f, 0.22f, 0.05f), pink, new Vector3(0f, 0f, 30f));
            Part(body, PrimitiveType.Cube, new Vector3(0.42f, 1.62f, 0.15f), new Vector3(0.22f, 0.22f, 0.05f), pink, new Vector3(0f, 0f, -30f));
            FourLegs(rig, body, 0.4f, 0.32f, 0.3f, 0.1f, pink, pinkDark);
            return rig;
        }

        // ---------- Орангутини Ананасини: орангутан в ананасе ----------
        static BrainrotRig Orangutini(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Orangutini);
            Color pine = H("#FFB300"), pineDark = H("#E65100"), leaf = H("#43A047"), ape = H("#BF5B1E"), face = H("#E0A675");
            var body = Pivot(rig.body, "Pineapple", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), new Vector3(1.1f, 1.45f, 1.0f), pine, default, 0.4f);
            for (int r = 0; r < 4; r++)
                for (int i = 0; i < 8; i++)
                {
                    var q = Quaternion.Euler(-30f + r * 20f, i * 45f + r * 22f, 0f);
                    Part(body, PrimitiveType.Cube, new Vector3(0f, 1.1f, 0f) + q * new Vector3(0f, 0f, 0.5f) + Vector3.up * (r - 1.5f) * 0.25f, Vector3.one * 0.07f, pineDark, new Vector3(0f, i * 45f, 45f), 0.3f, false);
                }
            for (int i = 0; i < 6; i++)
                Part(body, PrimitiveType.Cube, new Vector3(0f, 1.95f, 0f), new Vector3(0.12f, 0.6f, 0.04f), leaf, new Vector3(-25f, i * 60f, 0f), 0.3f);
            var head = Pivot(body, "Head", new Vector3(0f, 1.3f, 0.45f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.6f, 0.55f, 0.3f), ape, default, 0.3f);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.05f, 0.1f), new Vector3(0.42f, 0.4f, 0.2f), face, default, 0.3f);
            Eye(head, new Vector3(-0.1f, 0.06f, 0.18f), 0.12f, H("#3E2723"));
            Eye(head, new Vector3(0.1f, 0.06f, 0.18f), 0.12f, H("#3E2723"));
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.14f, 0.2f), new Vector3(0.2f, 0.1f, 0.08f), H("#8D6E63"));
            TwoArms(rig, body, 1.35f, 0.55f, 0.85f, 0.11f, ape, face, 18f);
            TwoLegs(rig, body, 0.45f, 0.22f, 0.12f, ape, face, 0.22f);
            return rig;
        }

        // ---------- Иль Както Иппопотамо: бегемот-кактус ----------
        static BrainrotRig Cacto(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.CactoHippo);
            Color cactus = H("#9CCC65"), cactusDark = H("#689F38"), spike = H("#FFFDE7"), flower = H("#EC407A");
            var body = Pivot(rig.body, "Torso", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.0f, -0.1f), new Vector3(1.4f, 1.1f, 1.8f), cactus, default, 0.3f);
            for (int i = 0; i < 6; i++)
                Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.0f, -0.1f), new Vector3(1.42f, 1.12f, 0.1f), cactusDark, new Vector3(0f, i * 30f, 0f), 0.3f, false);
            for (int i = 0; i < 22; i++)
            {
                var q = Quaternion.Euler(Random.Range(-70f, 30f), i * 16.4f, 0f);
                Part(body, PrimitiveType.Cube, new Vector3(0f, 1f, -0.1f) + q * new Vector3(0f, 0f, 0.75f), new Vector3(0.025f, 0.025f, 0.18f), spike, q.eulerAngles, 0.5f, false);
            }
            var head = Pivot(body, "Head", new Vector3(0f, 1.15f, 0.85f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.85f, 0.7f, 0.75f), cactus, default, 0.3f);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.15f, 0.3f), new Vector3(0.75f, 0.45f, 0.4f), cactusDark, default, 0.3f);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.15f, -0.08f, 0.49f), Vector3.one * 0.07f, H("#33691E"));
            Part(head, PrimitiveType.Sphere, new Vector3(0.15f, -0.08f, 0.49f), Vector3.one * 0.07f, H("#33691E"));
            Eye(head, new Vector3(-0.22f, 0.25f, 0.2f), 0.15f, Color.black);
            Eye(head, new Vector3(0.22f, 0.25f, 0.2f), 0.15f, Color.black);
            var f = Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0.1f), new Vector3(0.3f, 0.12f, 0.3f), flower, default, 0.5f);
            Part(f, PrimitiveType.Sphere, new Vector3(0f, 0.4f, 0f), new Vector3(0.4f, 0.6f, 0.4f), H("#FFEB3B"), default, 0.5f, false);
            FourLegs(rig, body, 0.48f, 0.42f, 0.5f, 0.2f, cactus, cactusDark);
            return rig;
        }

        // ---------- Бомбомбини Гусини: гусь-истребитель ----------
        static BrainrotRig Gusini(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Bombombini);
            rig.flying = true;
            Color goose = H("#FAFAFA"), grey = H("#90A4AE"), beak = H("#FF9800"), engine = H("#455A64");
            var plane = Pivot(rig.body, "Plane", new Vector3(0f, 1.3f, 0f));
            Part(plane, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.65f, 1.0f, 0.65f), goose, new Vector3(90f, 0f, 0f), 0.6f);
            var neck = Pivot(plane, "Neck", new Vector3(0f, 0.2f, 0.75f), new Vector3(-40f, 0f, 0f));
            Part(neck, PrimitiveType.Capsule, new Vector3(0f, 0.35f, 0f), new Vector3(0.22f, 0.4f, 0.22f), goose, default, 0.6f);
            var head = Pivot(neck, "Head", new Vector3(0f, 0.75f, 0f), new Vector3(40f, 0f, 0f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.36f, 0.34f, 0.42f), goose, default, 0.6f);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.3f), new Vector3(0.14f, 0.08f, 0.3f), beak, default, 0.5f);
            Eye(head, new Vector3(-0.12f, 0.06f, 0.1f), 0.1f, Color.black);
            Eye(head, new Vector3(0.12f, 0.06f, 0.1f), 0.1f, Color.black);
            // Крылья-самолёт с двигателями и пропеллерами
            for (int side = -1; side <= 1; side += 2)
            {
                var wing = Pivot(plane, "Wing", new Vector3(0.3f * side, 0f, 0.05f));
                rig.wings.Add(wing);
                Part(wing, PrimitiveType.Cube, new Vector3(0.95f * side, 0f, 0f), new Vector3(1.9f, 0.06f, 0.55f), grey, new Vector3(0f, -12f * side, 0f), 0.6f);
                Part(wing, PrimitiveType.Cylinder, new Vector3(0.75f * side, -0.12f, 0.15f), new Vector3(0.2f, 0.25f, 0.2f), engine, new Vector3(90f, 0f, 0f), 0.6f);
                var prop = Pivot(wing, "Prop", new Vector3(0.75f * side, -0.12f, 0.42f));
                rig.spinners.Add(prop);
                Part(prop, PrimitiveType.Cube, Vector3.zero, new Vector3(0.6f, 0.06f, 0.02f), H("#212121"), default, 0.5f, false);
            }
            Part(plane, PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.85f), new Vector3(0.06f, 0.5f, 0.35f), grey, new Vector3(-20f, 0f, 0f), 0.6f);
            Part(plane, PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.9f), new Vector3(0.9f, 0.05f, 0.3f), grey, default, 0.6f);
            // Лапки-шасси
            for (int side = -1; side <= 1; side += 2)
                Part(plane, PrimitiveType.Cube, new Vector3(0.18f * side, -0.4f, 0.1f), new Vector3(0.2f, 0.04f, 0.25f), beak, default, 0.5f);
            return rig;
        }

        // ---------- Рино Тостерино: носорог-тостер ----------
        static BrainrotRig Rhino(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.RhinoToaster);
            Color steel = H("#CFD8DC"), steelDark = H("#90A4AE"), toast = H("#D7A35E"), crust = H("#8D5524"), rhino = H("#9E9E9E");
            var body = Pivot(rig.body, "Toaster", Vector3.zero);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.1f), new Vector3(1.1f, 0.85f, 1.4f), steel, default, 0.85f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.1f), new Vector3(1.12f, 0.12f, 1.42f), steelDark, default, 0.85f, false);
            for (int i = -1; i <= 1; i += 2)
            {
                Part(body, PrimitiveType.Cube, new Vector3(0.22f * i, 1.62f, -0.1f), new Vector3(0.12f, 0.4f, 0.75f), toast, default, 0.2f);
                Part(body, PrimitiveType.Cube, new Vector3(0.22f * i, 1.83f, -0.1f), new Vector3(0.13f, 0.06f, 0.77f), crust, default, 0.2f);
            }
            Part(body, PrimitiveType.Cube, new Vector3(0.57f, 1.2f, 0.2f), new Vector3(0.08f, 0.12f, 0.06f), H("#212121"), default, 0.5f);
            var head = Pivot(body, "Head", new Vector3(0f, 1.15f, 0.65f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.15f), new Vector3(0.7f, 0.6f, 0.75f), rhino, default, 0.4f);
            Part(head, PrimitiveType.Capsule, new Vector3(0f, 0.25f, 0.5f), new Vector3(0.14f, 0.25f, 0.14f), H("#F5F5F5"), new Vector3(-30f, 0f, 0f), 0.6f);
            Part(head, PrimitiveType.Cube, new Vector3(-0.25f, 0.3f, -0.05f), new Vector3(0.12f, 0.2f, 0.06f), rhino, new Vector3(0f, 0f, 25f));
            Part(head, PrimitiveType.Cube, new Vector3(0.25f, 0.3f, -0.05f), new Vector3(0.12f, 0.2f, 0.06f), rhino, new Vector3(0f, 0f, -25f));
            Eye(head, new Vector3(-0.2f, 0.1f, 0.38f), 0.12f, Color.black);
            Eye(head, new Vector3(0.2f, 0.1f, 0.38f), 0.12f, Color.black);
            FourLegs(rig, body, 0.68f, 0.38f, 0.45f, 0.14f, rhino, steelDark);
            return rig;
        }

        // ---------- Блюберрини Октопуссини: осьминог-черника ----------
        static BrainrotRig Blueberrini(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Blueberrini);
            Color berry = H("#3949AB"), berryLight = H("#7986CB"), calyx = H("#1A237E"), leaf = H("#43A047");
            var body = Pivot(rig.body, "Berry", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.35f, 0f), Vector3.one * 1.25f, berry, default, 0.75f);
            Part(body, PrimitiveType.Sphere, new Vector3(-0.25f, 1.6f, 0.4f), Vector3.one * 0.25f, berryLight, default, 0.9f, false);
            for (int i = 0; i < 5; i++)
                Part(body, PrimitiveType.Cube, new Vector3(0f, 1.98f, 0f), new Vector3(0.12f, 0.06f, 0.3f), calyx, new Vector3(0f, i * 72f, 0f), 0.4f);
            Part(body, PrimitiveType.Cube, new Vector3(0.1f, 2.08f, 0f), new Vector3(0.25f, 0.04f, 0.12f), leaf, new Vector3(0f, 0f, 20f));
            var head = Pivot(body, "Face", new Vector3(0f, 1.35f, 0.55f));
            rig.head = head;
            Eye(head, new Vector3(-0.2f, 0.12f, 0.02f), 0.26f, Color.black);
            Eye(head, new Vector3(0.2f, 0.12f, 0.02f), 0.26f, Color.black);
            Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.2f, 0.06f), new Vector3(0.16f, 0.1f, 0.06f), calyx);
            // Щупальца — работают как ноги
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                var q = Quaternion.Euler(0f, a, 0f);
                var leg = Pivot(body, "Tentacle", new Vector3(0f, 0.85f, 0f) + q * new Vector3(0f, 0f, 0.42f), new Vector3(0f, a, 0f));
                rig.legs.Add(leg);
                Part(leg, PrimitiveType.Capsule, new Vector3(0f, -0.38f, 0.08f), new Vector3(0.14f, 0.42f, 0.14f), berry, new Vector3(15f, 0f, 0f), 0.7f);
            }
            return rig;
        }

        // ---------- Кокофанто Элефанто: слон с головой-кокосом ----------
        static BrainrotRig Cocofanto(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Cocofanto);
            Color grey = H("#9E9E9E"), greyDark = H("#757575"), coco = H("#795548"), cocoDark = H("#4E342E"), tusk = H("#FFF8E1");
            var body = Pivot(rig.body, "Torso", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.25f, -0.15f), new Vector3(1.35f, 1.15f, 1.75f), grey, default, 0.3f);
            var head = Pivot(body, "Head", new Vector3(0f, 1.55f, 0.85f));
            rig.head = head;
            Part(head, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.85f, coco, default, 0.2f);
            for (int i = 0; i < 6; i++)
                Part(head, PrimitiveType.Cube, Vector3.zero, new Vector3(0.87f, 0.02f, 0.02f), cocoDark, new Vector3(i * 30f, 0f, 30f), 0.2f, false);
            Part(head, PrimitiveType.Sphere, new Vector3(-0.52f, 0f, -0.1f), new Vector3(0.12f, 0.65f, 0.55f), greyDark, new Vector3(0f, -15f, 0f));
            Part(head, PrimitiveType.Sphere, new Vector3(0.52f, 0f, -0.1f), new Vector3(0.12f, 0.65f, 0.55f), greyDark, new Vector3(0f, 15f, 0f));
            var trunk = Pivot(head, "Trunk", new Vector3(0f, -0.15f, 0.38f), new Vector3(20f, 0f, 0f));
            rig.tail = trunk;
            Part(trunk, PrimitiveType.Capsule, new Vector3(0f, -0.35f, 0f), new Vector3(0.2f, 0.38f, 0.2f), grey);
            Part(trunk, PrimitiveType.Capsule, new Vector3(0f, -0.75f, 0.1f), new Vector3(0.15f, 0.2f, 0.15f), grey, new Vector3(-35f, 0f, 0f));
            Part(head, PrimitiveType.Capsule, new Vector3(-0.2f, -0.3f, 0.35f), new Vector3(0.06f, 0.2f, 0.06f), tusk, new Vector3(60f, 0f, 0f), 0.6f);
            Part(head, PrimitiveType.Capsule, new Vector3(0.2f, -0.3f, 0.35f), new Vector3(0.06f, 0.2f, 0.06f), tusk, new Vector3(60f, 0f, 0f), 0.6f);
            Eye(head, new Vector3(-0.2f, 0.12f, 0.35f), 0.14f, Color.black);
            Eye(head, new Vector3(0.2f, 0.12f, 0.35f), 0.14f, Color.black);
            FourLegs(rig, body, 0.72f, 0.42f, 0.5f, 0.2f, grey, greyDark);
            return rig;
        }

        // ---------- Жирафа Селестре: жираф-космонавт ----------
        static BrainrotRig Girafa(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.Girafa);
            Color yellow = H("#FFCA28"), spot = H("#8D6E63"), suit = H("#ECEFF1"), visor = H("#90CAF9");
            var body = Pivot(rig.body, "Torso", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.1f, -0.1f), new Vector3(0.8f, 0.7f, 1.2f), suit, default, 0.6f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.3f, -0.45f), new Vector3(0.5f, 0.45f, 0.25f), H("#B0BEC5"), default, 0.6f);
            var neck = Pivot(body, "Neck", new Vector3(0f, 1.35f, 0.35f), new Vector3(-15f, 0f, 0f));
            Part(neck, PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.22f, 0.55f, 0.22f), yellow);
            for (int i = 0; i < 4; i++)
                Part(neck, PrimitiveType.Sphere, new Vector3(i % 2 == 0 ? 0.09f : -0.09f, 0.25f + i * 0.22f, 0.06f), new Vector3(0.1f, 0.1f, 0.06f), spot, default, 0.3f, false);
            var head = Pivot(neck, "Head", new Vector3(0f, 1.15f, 0f), new Vector3(15f, 0f, 0f));
            rig.head = head;
            Part(head, PrimitiveType.Capsule, new Vector3(0f, 0f, 0.15f), new Vector3(0.3f, 0.3f, 0.3f), yellow, new Vector3(90f, 0f, 0f));
            Part(head, PrimitiveType.Cylinder, new Vector3(-0.1f, 0.25f, 0f), new Vector3(0.04f, 0.1f, 0.04f), yellow);
            Part(head, PrimitiveType.Cylinder, new Vector3(0.1f, 0.25f, 0f), new Vector3(0.04f, 0.1f, 0.04f), yellow);
            Eye(head, new Vector3(-0.12f, 0.08f, 0.15f), 0.11f, Color.black);
            Eye(head, new Vector3(0.12f, 0.08f, 0.15f), 0.11f, Color.black);
            var helmet = Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0.12f), Vector3.one * 0.8f, visor, default, 0.95f, false);
            helmet.GetComponent<Renderer>().sharedMaterial = GameAssets.Transparent(new Color(0.6f, 0.85f, 1f, 0.28f));
            FourLegs(rig, body, 0.85f, 0.25f, 0.35f, 0.08f, suit, H("#78909C"));
            return rig;
        }

        // ---------- Та Та Та Сахур: чайник с деревянными руками ----------
        static BrainrotRig TaTaTa(Transform parent)
        {
            var rig = NewRig(parent, BrainrotId.TaTaSahur);
            Color metal = H("#B0BEC5"), metalDark = H("#78909C"), wood = H("#A9744F"), stick = H("#6B4226");
            var body = Pivot(rig.body, "Kettle", Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), new Vector3(1.0f, 0.95f, 1.0f), metal, default, 0.85f);
            Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.8f, 0f), new Vector3(0.85f, 0.06f, 0.85f), metalDark, default, 0.85f);
            Part(body, PrimitiveType.Cylinder, new Vector3(0f, 1.67f, 0f), new Vector3(0.5f, 0.04f, 0.5f), metalDark, default, 0.85f);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.75f, 0f), Vector3.one * 0.15f, H("#212121"), default, 0.6f);
            Part(body, PrimitiveType.Capsule, new Vector3(0f, 1.25f, 0.55f), new Vector3(0.12f, 0.3f, 0.12f), metal, new Vector3(55f, 0f, 0f), 0.85f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.55f, -0.38f), new Vector3(0.08f, 0.4f, 0.1f), H("#212121"), new Vector3(-20f, 0f, 0f), 0.4f);
            var steam = Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.52f, 0.85f), Vector3.one * 0.18f, Color.white, default, 0.5f, false);
            steam.GetComponent<Renderer>().sharedMaterial = GameAssets.Transparent(new Color(1f, 1f, 1f, 0.5f));
            var head = Pivot(body, "Face", new Vector3(0f, 1.25f, 0.42f));
            rig.head = head;
            Eye(head, new Vector3(-0.17f, 0.08f, 0.02f), 0.2f, Color.black);
            Eye(head, new Vector3(0.17f, 0.08f, 0.02f), 0.2f, Color.black);
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.16f, 0.04f), new Vector3(0.22f, 0.04f, 0.03f), H("#3B2412"));
            TwoArms(rig, body, 1.3f, 0.5f, 0.6f, 0.07f, wood, wood, 15f);
            var s = Pivot(rig.arms[1], "Stick", new Vector3(0f, -0.6f, 0f), new Vector3(35f, 0f, -20f));
            Part(s, PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(0.07f, 0.45f, 0.07f), stick, default, 0.4f);
            TwoLegs(rig, body, 0.62f, 0.17f, 0.08f, wood, H("#5C3B1F"), 0.24f);
            return rig;
        }
    }
}
