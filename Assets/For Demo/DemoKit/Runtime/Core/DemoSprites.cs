using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemoKit
{
    public enum DemoIcon
    {
        None, Check, Plus, Minus, Left, Right, Up, Down, Close, Info, Warning, Person, People,
        Lock, Mic, Play, Pause, Dot, Dice, Home, Link, Clock, Chat
    }

    /// <summary>
    /// Rounded 9-slice sprites, soft shadows, and icons, drawn once at run time into a small atlas with mipmaps.
    /// Nothing has to be imported, and shapes keep their exact corner radius at any size.
    /// </summary>
    public static class DemoSprites
    {
        // Large enough for everything the kit draws, so that panels need few draw calls.
        private const int PageSize = 1024;
        private const int Padding = 8;
        private const float Density = 2f;

        private static readonly Dictionary<long, Sprite> Cache = new Dictionary<long, Sprite>();
        private static Texture2D page;
        private static int cursorX, cursorY, shelf;
        private static bool dirty, hooked;

        /// <summary>Filled rounded rectangle.</summary>
        public static Sprite Rounded(float radius)
        {
            radius = Mathf.Max(1f, Mathf.Round(radius));
            long key = Key(1, radius, 0f);
            if (TryGet(key, out Sprite s)) return s;
            float r = radius * Density;
            int size = Mathf.CeilToInt(r * 2f + 6f);
            float half = size * 0.5f;
            return Add(key, size, size, Vector4.one * Mathf.Ceil(r + 2f), 100f * Density,
                (x, y) => Cover(RoundRect(x - half, y - half, half - 1f, half - 1f, r)));
        }

        /// <summary>Rounded outline. The line lies inside the rectangle.</summary>
        public static Sprite Outline(float radius, float thickness)
        {
            radius = Mathf.Max(1f, Mathf.Round(radius));
            long key = Key(2, radius, thickness);
            if (TryGet(key, out Sprite s)) return s;
            float r = radius * Density, w = thickness * Density;
            int size = Mathf.CeilToInt(Mathf.Max(r, w) * 2f + 6f);
            float half = size * 0.5f;
            return Add(key, size, size, Vector4.one * Mathf.Ceil(Mathf.Max(r, w) + 2f), 100f * Density, (x, y) =>
            {
                float d = RoundRect(x - half, y - half, half - 1f, half - 1f, r);
                return Mathf.Min(Cover(d), Cover(-d - w));
            });
        }

        /// <summary>Soft shadow. The sprite is larger than the shape by the blur on every side.</summary>
        public static Sprite Shadow(float radius, float blur)
        {
            radius = Mathf.Max(1f, Mathf.Round(radius));
            blur = Mathf.Max(4f, Mathf.Round(blur));
            long key = Key(3, radius, blur);
            if (TryGet(key, out Sprite s)) return s;
            const float density = 0.5f;
            float r = radius * density, b = blur * density;
            int size = Mathf.CeilToInt((r + b) * 2f + 6f);
            float half = size * 0.5f, inner = half - b;
            return Add(key, size, size, Vector4.one * Mathf.Ceil(r + b + 2f), 100f * density, (x, y) =>
            {
                float d = RoundRect(x - half, y - half, inner, inner, r);
                float t = Mathf.Clamp01((d + b * 0.5f) / (b * 1.5f));
                t = 1f - t;
                return t * t * t;
            });
        }

        public static Sprite Icon(DemoIcon icon)
        {
            long key = Key(4, (int)icon, 0f);
            if (TryGet(key, out Sprite s)) return s;
            const int size = 96;
            const float half = size * 0.5f;
            // Icons are drawn in a square from -1 to 1 with y up.
            return Add(key, size, size, Vector4.zero, 200f,
                (x, y) => Cover(IconDistance(icon, (x - half) / (half - 3f), (y - half) / (half - 3f)) * (half - 3f)));
        }

        // Drawing ---------------------------------------------------------------------------------------------

        private static float IconDistance(DemoIcon icon, float x, float y)
        {
            const float w = 0.10f;
            switch (icon)
            {
                case DemoIcon.Check: return Min(Line(x, y, -0.58f, -0.02f, -0.16f, -0.42f), Line(x, y, -0.16f, -0.42f, 0.60f, 0.42f)) - 0.12f;
                case DemoIcon.Plus: return Min(Line(x, y, -0.55f, 0f, 0.55f, 0f), Line(x, y, 0f, -0.55f, 0f, 0.55f)) - w;
                case DemoIcon.Minus: return Line(x, y, -0.55f, 0f, 0.55f, 0f) - w;
                case DemoIcon.Left: return Min(Line(x, y, 0.24f, 0.56f, -0.30f, 0f), Line(x, y, -0.30f, 0f, 0.24f, -0.56f)) - w;
                case DemoIcon.Right: return Min(Line(x, y, -0.24f, 0.56f, 0.30f, 0f), Line(x, y, 0.30f, 0f, -0.24f, -0.56f)) - w;
                case DemoIcon.Up: return Min(Line(x, y, -0.56f, -0.24f, 0f, 0.30f), Line(x, y, 0f, 0.30f, 0.56f, -0.24f)) - w;
                case DemoIcon.Down: return Min(Line(x, y, -0.56f, 0.24f, 0f, -0.30f), Line(x, y, 0f, -0.30f, 0.56f, 0.24f)) - w;
                case DemoIcon.Close: return Min(Line(x, y, -0.45f, -0.45f, 0.45f, 0.45f), Line(x, y, -0.45f, 0.45f, 0.45f, -0.45f)) - w;
                case DemoIcon.Info:
                    return Min(Mathf.Abs(Disc(x, y, 0f, 0f, 0.80f)) - 0.07f, Min(Disc(x, y, 0f, 0.36f, 0.115f), Line(x, y, 0f, 0.02f, 0f, -0.40f) - 0.09f));
                case DemoIcon.Warning:
                {
                    float tri = Min(Line(x, y, 0f, 0.66f, -0.72f, -0.60f), Min(Line(x, y, -0.72f, -0.60f, 0.72f, -0.60f), Line(x, y, 0.72f, -0.60f, 0f, 0.66f))) - 0.08f;
                    return Min(tri, Min(Disc(x, y, 0f, -0.36f, 0.10f), Line(x, y, 0f, 0.20f, 0f, -0.10f) - 0.08f));
                }
                case DemoIcon.Person: return Min(Disc(x, y, 0f, 0.36f, 0.34f), Shoulders(x, y, 0f, -0.72f, 0.66f, 0.62f));
                case DemoIcon.People:
                {
                    float front = Min(Disc(x, y, -0.22f, 0.22f, 0.28f), Shoulders(x, y, -0.22f, -0.70f, 0.56f, 0.52f));
                    float back = Min(Disc(x, y, 0.46f, 0.34f, 0.22f), Shoulders(x, y, 0.46f, -0.56f, 0.42f, 0.50f));
                    float gap = 0.09f - Min(front, 10f);
                    return Min(front, Mathf.Max(back, gap));
                }
                case DemoIcon.Lock:
                {
                    float shackle = Mathf.Abs(Disc(x, y, 0f, 0.22f, 0.32f)) - 0.085f;
                    shackle = y > 0.22f ? shackle : Mathf.Max(Mathf.Abs(Mathf.Abs(x) - 0.32f) - 0.085f, -y - 0.2f);
                    return Min(RoundRect(x, y + 0.34f, 0.54f, 0.42f, 0.12f), shackle);
                }
                case DemoIcon.Mic:
                {
                    float capsule = Line(x, y, 0f, 0.46f, 0f, 0.06f) - 0.26f;
                    float cup = y < 0.06f ? Mathf.Abs(Disc(x, y, 0f, 0.06f, 0.46f)) - 0.07f : Line(x, y, -0.46f, 0.06f, -0.46f, 0.06f) - 0.07f;
                    if (y >= 0.06f) cup = Min(Disc(x, y, -0.46f, 0.06f, 0.07f), Disc(x, y, 0.46f, 0.06f, 0.07f));
                    return Min(capsule, Min(cup, Min(Line(x, y, 0f, -0.42f, 0f, -0.72f), Line(x, y, -0.28f, -0.74f, 0.28f, -0.74f)) - 0.07f));
                }
                case DemoIcon.Play: return Mathf.Max(Mathf.Max(-x - 0.36f, (x - 0.58f) * 0.60f + y * 0.80f), (x - 0.58f) * 0.60f - y * 0.80f) - 0.08f;
                case DemoIcon.Pause: return Min(RoundRect(x + 0.27f, y, 0.15f, 0.56f, 0.07f), RoundRect(x - 0.27f, y, 0.15f, 0.56f, 0.07f));
                case DemoIcon.Dot: return Disc(x, y, 0f, 0f, 0.92f);
                case DemoIcon.Dice:
                {
                    float box = Mathf.Abs(RoundRect(x, y, 0.62f, 0.62f, 0.2f)) - 0.075f;
                    float pips = Min(Disc(x, y, -0.28f, 0.28f, 0.12f), Min(Disc(x, y, 0f, 0f, 0.12f), Disc(x, y, 0.28f, -0.28f, 0.12f)));
                    return Min(box, pips);
                }
                case DemoIcon.Home:
                {
                    float roof = Min(Line(x, y, -0.68f, -0.02f, 0f, 0.58f), Line(x, y, 0f, 0.58f, 0.68f, -0.02f)) - 0.085f;
                    float walls = Min(Line(x, y, -0.46f, 0.04f, -0.46f, -0.58f), Min(Line(x, y, -0.46f, -0.58f, 0.46f, -0.58f), Line(x, y, 0.46f, -0.58f, 0.46f, 0.04f))) - 0.085f;
                    return Min(roof, walls);
                }
                case DemoIcon.Link:
                {
                    // Two interlocking rings, turned by 45 degrees.
                    float u = (x + y) * 0.7071f, v = (y - x) * 0.7071f;
                    float a = Mathf.Abs(RoundRect(u + 0.34f, v, 0.44f, 0.27f, 0.27f)) - 0.075f;
                    float b = Mathf.Abs(RoundRect(u - 0.34f, v, 0.44f, 0.27f, 0.27f)) - 0.075f;
                    return Min(a, b);
                }
                case DemoIcon.Clock:
                    return Min(Mathf.Abs(Disc(x, y, 0f, 0f, 0.72f)) - 0.08f, Min(Line(x, y, 0f, 0f, 0f, 0.40f), Line(x, y, 0f, 0f, 0.28f, -0.16f)) - 0.08f);
                case DemoIcon.Chat:
                {
                    float bubble = RoundRect(x, y - 0.10f, 0.74f, 0.52f, 0.26f);
                    float tail = Mathf.Max(Mathf.Max(-x - 0.36f, y + 0.36f), x - y - 0.50f);
                    float dots = Min(Disc(x, y, -0.32f, 0.10f, 0.095f), Min(Disc(x, y, 0f, 0.10f, 0.095f), Disc(x, y, 0.32f, 0.10f, 0.095f)));
                    return Mathf.Max(Min(bubble, tail), -dots);
                }
                default: return 1f;
            }
        }

        private static float Shoulders(float x, float y, float cx, float bottom, float halfWidth, float height)
        {
            // A half ellipse that stands on its base line.
            float px = (x - cx) / halfWidth, py = (y - bottom) / height;
            float d = (Mathf.Sqrt(px * px + py * py) - 1f) * Mathf.Min(halfWidth, height);
            return Mathf.Max(d, bottom - y);
        }

        private static float Min(float a, float b) => a < b ? a : b;

        private static float Disc(float x, float y, float cx, float cy, float r) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        private static float Line(float x, float y, float ax, float ay, float bx, float by)
        {
            float px = x - ax, py = y - ay, dx = bx - ax, dy = by - ay;
            float len = dx * dx + dy * dy;
            float t = len < 1e-6f ? 0f : Mathf.Clamp01((px * dx + py * dy) / len);
            px -= dx * t; py -= dy * t;
            return Mathf.Sqrt(px * px + py * py);
        }

        private static float RoundRect(float x, float y, float halfWidth, float halfHeight, float r)
        {
            r = Mathf.Min(r, Mathf.Min(halfWidth, halfHeight));
            float qx = Mathf.Abs(x) - halfWidth + r, qy = Mathf.Abs(y) - halfHeight + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float Cover(float distance) => Mathf.Clamp01(0.5f - distance);

        // Atlas -----------------------------------------------------------------------------------------------

        private static long Key(int kind, float a, float b) => ((long)kind << 40) | ((long)Mathf.RoundToInt(a * 10f) << 20) | (long)Mathf.RoundToInt(b * 10f);

        private static bool TryGet(long key, out Sprite sprite) => Cache.TryGetValue(key, out sprite) && sprite != null;

        private static Sprite Add(long key, int width, int height, Vector4 border, float pixelsPerUnit, Func<float, float, float> alpha)
        {
            Place(width, height, out int x0, out int y0);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f))));
            page.SetPixels32(x0, y0, width, height, pixels);
            MarkDirty();
            Sprite sprite = Sprite.Create(page, new Rect(x0, y0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = "DemoKit " + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }

        private static void Place(int width, int height, out int x, out int y)
        {
            if (page != null && cursorX + width + Padding > PageSize) { cursorX = Padding; cursorY += shelf + Padding; shelf = 0; }
            if (page == null || cursorY + height + Padding > PageSize)
            {
                // Sprites on the old page may still wait for their upload.
                Flush();
                // Three mip levels are enough at reading distance and cannot bleed across the padding.
                page = new Texture2D(PageSize, PageSize, TextureFormat.RGBA32, 4, false)
                {
                    name = "DemoKit Atlas",
                    filterMode = FilterMode.Trilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 4,
                    hideFlags = HideFlags.HideAndDontSave
                };
                var clear = new Color32[PageSize * PageSize];
                for (int i = 0; i < clear.Length; i++) clear[i] = new Color32(255, 255, 255, 0);
                page.SetPixels32(clear);
                cursorX = cursorY = Padding;
                shelf = 0;
            }
            x = cursorX;
            y = cursorY;
            cursorX += width + Padding;
            shelf = Mathf.Max(shelf, height);
        }

        /// <summary>A new panel draws a few dozen shapes. The texture is sent to the graphics card once for all of them.</summary>
        private static void MarkDirty()
        {
            dirty = true;
            if (hooked) return;
            hooked = true;
            Canvas.willRenderCanvases += Flush;
        }

        private static void Flush()
        {
            if (!dirty) return;
            dirty = false;
            if (page != null) page.Apply(true, false);
        }

        // The atlas survives changes between edit and play mode. It goes with the code that knows it.
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void FreeBeforeReload() { UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Free; }

        private static void Free()
        {
            var pages = new HashSet<Texture2D>();
            foreach (Sprite s in Cache.Values)
            {
                if (s == null) continue;
                pages.Add(s.texture);
                UnityEngine.Object.DestroyImmediate(s);
            }
            foreach (Texture2D t in pages)
                if (t != null) UnityEngine.Object.DestroyImmediate(t);
            Cache.Clear();
            page = null;
            cursorX = cursorY = shelf = 0;
        }
#endif
    }
}
