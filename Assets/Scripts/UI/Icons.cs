using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    public enum IconKind
    {
        Heart, Sword, Bullet, Shield, Boot, Crosshair, Plus, Coin, Magnet, Chest, Bolt, Reroll, Star, Crystal,
        Ammo, Reload, Fang, Bomb, Fire, Skull, Gear, Speaker, Globe, Lock, Trophy, Clock, Flask, Brain, Back,
        Keyboard, Monitor, Play, Note, Exit, Pause, Wave, Check,
        Leaf, Spiral, Snow, Tower, Cloud, Home, Infinity, Book, Users
    }

    /// <summary>
    /// Иконки интерфейса, нарисованные кодом (никаких картинок не нужно).
    /// Белая фигура с тёмной обводкой — цвет задаётся через Image.color.
    /// </summary>
    public static class Icons
    {
        const int Size = 128;
        static readonly Dictionary<IconKind, Sprite> cache = new Dictionary<IconKind, Sprite>();

        public static Sprite Get(IconKind kind)
        {
            if (cache.TryGetValue(kind, out var s) && s != null) return s;
            s = Render(Shape(kind));
            s.name = "Icon_" + kind;
            cache[kind] = s;
            return s;
        }

        static Sprite Render(Func<Vector2, float> sdf)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Trilinear;
            var pixels = new Color32[Size * Size];
            float px = 2f / Size;
            const float outline = 0.075f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f) * 1.08f;
                    float d = sdf(p);
                    float fill = Mathf.Clamp01(0.5f - d / px);
                    float outer = Mathf.Clamp01(0.5f - (d - outline) / px);
                    float a = Mathf.Max(fill, outer * 0.85f);
                    float c = outer > 0f ? fill / Mathf.Max(outer, 0.0001f) : 1f;
                    byte v = (byte)(Mathf.Clamp01(c) * 255f);
                    pixels[y * Size + x] = new Color32(v, v, v, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(true);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------- Примитивы (расстояние со знаком: <0 внутри) ----------

        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float Circle(Vector2 p, float x, float y, float r) => Circle(p, new Vector2(x, y), r);

        static float Box(Vector2 p, float cx, float cy, float hx, float hy, float round = 0f)
        {
            var q = new Vector2(Mathf.Abs(p.x - cx) - hx + round, Mathf.Abs(p.y - cy) - hy + round);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - round;
        }

        static float Seg(Vector2 p, float ax, float ay, float bx, float by, float r)
        {
            var a = new Vector2(ax, ay);
            var b = new Vector2(bx, by);
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        static float Ring(Vector2 p, float cx, float cy, float r, float t) => Mathf.Abs(Circle(p, cx, cy, r)) - t;

        static float Poly(Vector2 p, params float[] xy)
        {
            int n = xy.Length / 2;
            var v0 = new Vector2(xy[0], xy[1]);
            float d = Vector2.Dot(p - v0, p - v0);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                var vi = new Vector2(xy[i * 2], xy[i * 2 + 1]);
                var vj = new Vector2(xy[j * 2], xy[j * 2 + 1]);
                var e = vj - vi;
                var w = p - vi;
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= vi.y, c2 = p.y < vj.y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        static float Star(Vector2 p, float cx, float cy, float outer, float inner, int points = 5)
        {
            var xy = new float[points * 4];
            for (int i = 0; i < points * 2; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / points;
                float r = i % 2 == 0 ? outer : inner;
                xy[i * 2] = cx + Mathf.Cos(a) * r;
                xy[i * 2 + 1] = cy + Mathf.Sin(a) * r;
            }
            return Poly(p, xy);
        }

        static Vector2 Rot(Vector2 p, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        static float U(params float[] d)
        {
            float m = d[0];
            for (int i = 1; i < d.Length; i++) m = Mathf.Min(m, d[i]);
            return m;
        }

        static float Sub(float a, float b) => Mathf.Max(a, -b);

        /// <summary>Дуга кольца от угла a0 до a1 (в градусах, против часовой).</summary>
        static float Arc(Vector2 p, float r, float t, float a0, float a1)
        {
            float ang = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            bool inside = a0 <= a1 ? (ang >= a0 && ang <= a1) : (ang >= a0 || ang <= a1);
            if (inside) return Mathf.Abs(p.magnitude - r) - t;
            var e0 = new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), Mathf.Sin(a0 * Mathf.Deg2Rad)) * r;
            var e1 = new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad)) * r;
            return Mathf.Min((p - e0).magnitude, (p - e1).magnitude) - t;
        }

        static float Bullet(Vector2 p, float cx, float cy, float s)
        {
            var q = (p - new Vector2(cx, cy)) / s;
            float d = U(Box(q, 0f, -0.25f, 0.28f, 0.5f, 0.04f),
                Poly(q, -0.28f, 0.2f, 0.28f, 0.2f, 0.2f, 0.55f, 0f, 0.8f, -0.2f, 0.55f));
            d = Sub(d, Box(q, 0f, 0.2f, 0.4f, 0.03f));
            return d * s;
        }

        // ---------- Фигуры ----------

        static Func<Vector2, float> Shape(IconKind kind)
        {
            switch (kind)
            {
                case IconKind.Heart:
                    return p => U(Circle(p, -0.36f, 0.28f, 0.42f), Circle(p, 0.36f, 0.28f, 0.42f), Poly(p, -0.76f, 0.12f, 0.76f, 0.12f, 0f, -0.8f));
                case IconKind.Sword:
                    return p =>
                    {
                        var q = Rot(p, 45f);
                        return U(Poly(q, -0.13f, -0.15f, 0.13f, -0.15f, 0.13f, 0.62f, 0f, 0.85f, -0.13f, 0.62f),
                            Box(q, 0f, -0.2f, 0.38f, 0.07f, 0.03f),
                            Seg(q, 0f, -0.25f, 0f, -0.6f, 0.08f),
                            Circle(q, 0f, -0.68f, 0.12f));
                    };
                case IconKind.Bullet:
                    return p => Bullet(Rot(p, -30f), 0f, 0f, 1.05f);
                case IconKind.Ammo:
                    return p => U(Bullet(p, -0.45f, -0.05f, 0.75f), Bullet(p, 0f, 0.05f, 0.75f), Bullet(p, 0.45f, -0.05f, 0.75f));
                case IconKind.Shield:
                    return p => Sub(Poly(p, -0.66f, 0.72f, 0.66f, 0.72f, 0.66f, 0.05f, 0f, -0.82f, -0.66f, 0.05f),
                        Poly(p, 0.06f, 0.55f, 0.48f, 0.55f, 0.48f, 0.08f, 0.06f, -0.52f));
                case IconKind.Boot:
                    return p => U(Box(p, -0.18f, 0.22f, 0.3f, 0.55f, 0.08f), Box(p, 0.12f, -0.52f, 0.62f, 0.24f, 0.18f), Box(p, -0.18f, 0.7f, 0.36f, 0.08f, 0.04f));
                case IconKind.Crosshair:
                    return p => U(Ring(p, 0f, 0f, 0.55f, 0.08f), Seg(p, 0f, 0.3f, 0f, 0.9f, 0.08f), Seg(p, 0f, -0.3f, 0f, -0.9f, 0.08f),
                        Seg(p, 0.3f, 0f, 0.9f, 0f, 0.08f), Seg(p, -0.3f, 0f, -0.9f, 0f, 0.08f), Circle(p, 0f, 0f, 0.1f));
                case IconKind.Plus:
                    return p => U(Box(p, 0f, 0f, 0.22f, 0.75f, 0.06f), Box(p, 0f, 0f, 0.75f, 0.22f, 0.06f));
                case IconKind.Coin:
                    return p => Sub(Sub(Circle(p, 0f, 0f, 0.82f), Ring(p, 0f, 0f, 0.6f, 0.05f)), Box(p, 0f, 0f, 0.09f, 0.32f, 0.05f));
                case IconKind.Magnet:
                    return p => U(Arc(p - new Vector2(0f, -0.05f), 0.45f, 0.18f, 180f, 360f),
                        Box(p, -0.45f, 0.35f, 0.18f, 0.4f), Box(p, 0.45f, 0.35f, 0.18f, 0.4f));
                case IconKind.Chest:
                    return p => Sub(U(Box(p, 0f, -0.25f, 0.8f, 0.45f, 0.06f), Box(p, 0f, 0.38f, 0.8f, 0.24f, 0.2f)),
                        U(Box(p, 0f, 0.12f, 0.9f, 0.035f), Box(p, 0f, 0.02f, 0.1f, 0.12f, 0.03f)));
                case IconKind.Bolt:
                    return p => Poly(p, 0.25f, 0.92f, -0.52f, -0.06f, -0.06f, -0.06f, -0.28f, -0.92f, 0.52f, 0.12f, 0.06f, 0.12f);
                case IconKind.Reroll:
                    return p => Sub(Box(p, 0f, 0f, 0.72f, 0.72f, 0.2f),
                        U(Circle(p, -0.36f, 0.36f, 0.12f), Circle(p, 0.36f, -0.36f, 0.12f), Circle(p, 0f, 0f, 0.12f), Circle(p, 0.36f, 0.36f, 0.12f), Circle(p, -0.36f, -0.36f, 0.12f)));
                case IconKind.Reload:
                    return p => U(Arc(p, 0.58f, 0.11f, 60f, 330f), Poly(p, 0.2f, 0.5f, 0.75f, 0.85f, 0.7f, 0.22f));
                case IconKind.Star:
                    return p => Star(p, 0f, -0.04f, 0.9f, 0.4f);
                case IconKind.Crystal:
                    return p => Sub(Poly(p, -0.65f, 0.3f, -0.32f, 0.72f, 0.32f, 0.72f, 0.65f, 0.3f, 0f, -0.85f),
                        U(Seg(p, -0.6f, 0.3f, 0.6f, 0.3f, 0.025f), Seg(p, -0.15f, 0.3f, 0f, -0.8f, 0.025f), Seg(p, 0.15f, 0.3f, 0f, -0.8f, 0.025f)));
                case IconKind.Fang:
                    return p => U(Box(p, 0f, 0.55f, 0.82f, 0.18f, 0.1f), Poly(p, -0.62f, 0.45f, -0.18f, 0.45f, -0.4f, -0.75f), Poly(p, 0.18f, 0.45f, 0.62f, 0.45f, 0.4f, -0.75f));
                case IconKind.Bomb:
                    return p => U(Circle(p, -0.1f, -0.15f, 0.62f), Box(p, 0.28f, 0.42f, 0.16f, 0.12f, 0.03f), Seg(p, 0.35f, 0.55f, 0.55f, 0.75f, 0.05f), Star(p, 0.65f, 0.8f, 0.2f, 0.08f));
                case IconKind.Fire:
                    return p => U(Circle(p, 0f, -0.32f, 0.52f), Poly(p, -0.5f, -0.25f, 0.5f, -0.25f, 0.15f, 0.92f, 0.05f, 0.4f, -0.2f, 0.65f));
                case IconKind.Skull:
                    return p => Sub(U(Circle(p, 0f, 0.15f, 0.65f), Box(p, 0f, -0.52f, 0.35f, 0.25f, 0.08f)),
                        U(Circle(p, -0.26f, 0.08f, 0.17f), Circle(p, 0.26f, 0.08f, 0.17f), Poly(p, -0.08f, -0.25f, 0.08f, -0.25f, 0f, -0.08f), Box(p, 0f, -0.62f, 0.03f, 0.15f)));
                case IconKind.Gear:
                    return p =>
                    {
                        float d = Circle(p, 0f, 0f, 0.58f);
                        for (int i = 0; i < 8; i++)
                        {
                            var q = Rot(p, i * 45f);
                            d = Mathf.Min(d, Box(q, 0f, 0.7f, 0.14f, 0.18f, 0.04f));
                        }
                        return Sub(d, Circle(p, 0f, 0f, 0.24f));
                    };
                case IconKind.Speaker:
                    return p => U(Box(p, -0.55f, 0f, 0.18f, 0.24f, 0.04f), Poly(p, -0.45f, 0.24f, 0.05f, 0.62f, 0.05f, -0.62f, -0.45f, -0.24f),
                        Arc(p - new Vector2(0.05f, 0f), 0.4f, 0.07f, 300f, 60f), Arc(p - new Vector2(0.05f, 0f), 0.7f, 0.07f, 305f, 55f));
                case IconKind.Globe:
                    return p =>
                    {
                        var e = new Vector2(p.x / 0.42f, p.y / 0.82f);
                        float ellipse = Mathf.Abs(e.magnitude - 1f) * 0.42f - 0.06f;
                        return U(Ring(p, 0f, 0f, 0.82f, 0.07f), ellipse, Seg(p, -0.8f, 0f, 0.8f, 0f, 0.06f), Seg(p, 0f, -0.8f, 0f, 0.8f, 0.06f));
                    };
                case IconKind.Lock:
                    return p => U(Sub(Box(p, 0f, -0.3f, 0.62f, 0.48f, 0.1f), U(Circle(p, 0f, -0.2f, 0.13f), Box(p, 0f, -0.42f, 0.05f, 0.18f))),
                        Arc(p - new Vector2(0f, 0.2f), 0.38f, 0.1f, 0f, 180f), Box(p, -0.38f, 0.2f, 0.1f, 0.1f), Box(p, 0.38f, 0.2f, 0.1f, 0.1f));
                case IconKind.Trophy:
                    return p => U(Poly(p, -0.55f, 0.75f, 0.55f, 0.75f, 0.4f, 0.05f, 0.1f, -0.15f, -0.1f, -0.15f, -0.4f, 0.05f),
                        Arc(p - new Vector2(-0.55f, 0.42f), 0.22f, 0.07f, 90f, 290f), Arc(p - new Vector2(0.55f, 0.42f), 0.22f, 0.07f, 250f, 90f),
                        Box(p, 0f, -0.38f, 0.08f, 0.25f), Box(p, 0f, -0.72f, 0.45f, 0.12f, 0.04f));
                case IconKind.Clock:
                    return p => U(Ring(p, 0f, 0f, 0.75f, 0.09f), Seg(p, 0f, 0f, 0f, 0.5f, 0.07f), Seg(p, 0f, 0f, 0.35f, -0.15f, 0.07f));
                case IconKind.Flask:
                    return p => Sub(Poly(p, -0.2f, 0.82f, 0.2f, 0.82f, 0.2f, 0.28f, 0.7f, -0.62f, 0.58f, -0.82f, -0.58f, -0.82f, -0.7f, -0.62f, -0.2f, 0.28f),
                        Poly(p, -0.45f, -0.25f, 0.45f, -0.25f, 0.42f, -0.3f, -0.42f, -0.3f));
                case IconKind.Brain:
                    return p => Sub(U(Circle(p, -0.35f, 0.25f, 0.38f), Circle(p, 0.35f, 0.25f, 0.38f), Circle(p, -0.45f, -0.2f, 0.36f), Circle(p, 0.45f, -0.2f, 0.36f),
                            Circle(p, 0f, 0.35f, 0.38f), Circle(p, 0f, -0.25f, 0.4f)),
                        U(Seg(p, 0f, 0.7f, 0f, -0.6f, 0.035f), Arc(p - new Vector2(-0.35f, 0.05f), 0.2f, 0.03f, 200f, 340f), Arc(p - new Vector2(0.35f, 0.05f), 0.2f, 0.03f, 200f, 340f)));
                case IconKind.Back:
                    return p => U(Poly(p, -0.8f, 0f, -0.2f, 0.55f, -0.2f, -0.55f), Box(p, 0.25f, 0f, 0.5f, 0.17f, 0.05f));
                case IconKind.Keyboard:
                    return p =>
                    {
                        float d = Box(p, 0f, 0f, 0.9f, 0.55f, 0.12f);
                        for (int r = 0; r < 2; r++)
                            for (int c = 0; c < 5; c++)
                                d = Sub(d, Box(p, -0.6f + c * 0.3f, 0.25f - r * 0.28f, 0.09f, 0.09f, 0.02f));
                        return Sub(d, Box(p, 0f, -0.3f, 0.45f, 0.08f, 0.02f));
                    };
                case IconKind.Monitor:
                    return p => U(Sub(Box(p, 0f, 0.2f, 0.85f, 0.55f, 0.08f), Box(p, 0f, 0.22f, 0.7f, 0.4f, 0.02f)), Box(p, 0f, -0.5f, 0.08f, 0.2f), Box(p, 0f, -0.72f, 0.4f, 0.07f, 0.03f));
                case IconKind.Play:
                    return p => Poly(p, -0.5f, 0.75f, 0.75f, 0f, -0.5f, -0.75f);
                case IconKind.Note:
                    return p => U(Circle(p, -0.3f, -0.5f, 0.28f), Box(p, -0.06f, 0.15f, 0.07f, 0.65f), Poly(p, -0.06f, 0.8f, 0.55f, 0.55f, 0.55f, 0.3f, -0.06f, 0.55f));
                case IconKind.Exit:
                    return p => U(Sub(Box(p, -0.3f, 0f, 0.45f, 0.75f, 0.06f), Box(p, -0.3f, 0f, 0.3f, 0.6f)), Poly(p, 0.85f, 0f, 0.4f, 0.4f, 0.4f, -0.4f), Box(p, 0.15f, 0f, 0.3f, 0.12f));
                case IconKind.Pause:
                    return p => U(Box(p, -0.3f, 0f, 0.17f, 0.7f, 0.05f), Box(p, 0.3f, 0f, 0.17f, 0.7f, 0.05f));
                case IconKind.Wave:
                    return p => U(Seg(p, -0.8f, 0.4f, 0.8f, 0.4f, 0.12f), Seg(p, -0.8f, 0f, 0.8f, 0f, 0.12f), Seg(p, -0.8f, -0.4f, 0.8f, -0.4f, 0.12f));
                case IconKind.Check:
                    return p => U(Seg(p, -0.6f, 0f, -0.15f, -0.5f, 0.13f), Seg(p, -0.15f, -0.5f, 0.7f, 0.55f, 0.13f));
                case IconKind.Leaf:
                    return p =>
                    {
                        var q = Rot(p, -35f);
                        var e = new Vector2(q.x / 0.45f, q.y / 0.85f);
                        return Sub(U((e.magnitude - 1f) * 0.45f, Seg(q, 0f, -0.85f, 0f, -1f, 0.06f)), Seg(q, 0f, -0.75f, 0f, 0.7f, 0.035f));
                    };
                case IconKind.Spiral:
                    return p => U(Arc(p, 0.72f, 0.09f, 20f, 300f), Arc(p, 0.45f, 0.09f, 200f, 120f), Circle(p, 0f, 0f, 0.14f));
                case IconKind.Snow:
                    return p =>
                    {
                        float d = 9f;
                        for (int i = 0; i < 3; i++)
                        {
                            var q = Rot(p, i * 60f);
                            d = U(d, Seg(q, 0f, -0.85f, 0f, 0.85f, 0.07f), Seg(q, 0f, 0.5f, 0.22f, 0.72f, 0.05f), Seg(q, 0f, 0.5f, -0.22f, 0.72f, 0.05f),
                                Seg(q, 0f, -0.5f, 0.22f, -0.72f, 0.05f), Seg(q, 0f, -0.5f, -0.22f, -0.72f, 0.05f));
                        }
                        return d;
                    };
                case IconKind.Tower:
                    return p => U(Box(p, 0f, -0.55f, 0.6f, 0.2f, 0.05f), Box(p, 0f, -0.05f, 0.35f, 0.35f, 0.08f), Circle(p, 0f, 0.35f, 0.32f), Seg(p, 0f, 0.35f, 0.75f, 0.6f, 0.1f));
                case IconKind.Cloud:
                    return p => U(Circle(p, -0.35f, -0.1f, 0.35f), Circle(p, 0.05f, 0.12f, 0.45f), Circle(p, 0.42f, -0.12f, 0.33f), Box(p, 0f, -0.25f, 0.65f, 0.2f, 0.15f));
                case IconKind.Home:
                    return p => U(Sub(Box(p, 0f, -0.3f, 0.6f, 0.45f, 0.05f), Box(p, 0f, -0.48f, 0.16f, 0.28f, 0.04f)), Poly(p, -0.85f, 0.1f, 0f, 0.85f, 0.85f, 0.1f));
                case IconKind.Infinity:
                    return p => U(Ring(p, -0.4f, 0f, 0.3f, 0.1f), Ring(p, 0.4f, 0f, 0.3f, 0.1f));
                case IconKind.Book:
                    return p => U(Sub(Box(p, -0.38f, 0f, 0.36f, 0.62f, 0.05f), Box(p, -0.38f, 0.1f, 0.22f, 0.04f)), Sub(Box(p, 0.38f, 0f, 0.36f, 0.62f, 0.05f), Box(p, 0.38f, 0.1f, 0.22f, 0.04f)));
                case IconKind.Users:
                    return p => U(Circle(p, -0.3f, 0.3f, 0.25f), Circle(p, 0.35f, 0.35f, 0.22f), Box(p, -0.3f, -0.4f, 0.4f, 0.28f, 0.2f), Box(p, 0.4f, -0.35f, 0.32f, 0.24f, 0.18f));
                default:
                    return p => Circle(p, 0f, 0f, 0.7f);
            }
        }
    }
}
