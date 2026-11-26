using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// Shows a texture, sprite, or RenderTexture with rounded corners and smooth edges, without a mask.
    /// </summary>
    [AddComponentMenu("")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DemoPicture : MaskableGraphic
    {
        private const int CornerSteps = 10;
        private const float Fringe = 1.2f;

        [SerializeField] private Texture texture;
        [SerializeField] private Sprite sprite;
        [SerializeField] private float radius = 18f;
        [Tooltip("True: fill the frame and crop. False: show the whole picture.")]
        [SerializeField] private bool cover = true;

        public override Texture mainTexture
        {
            get
            {
                if (sprite != null) return sprite.texture;
                return texture != null ? texture : s_WhiteTexture;
            }
        }

        public bool HasPicture { get { return texture != null || sprite != null; } }

        public Texture Texture
        {
            get { return texture; }
            set { texture = value; sprite = null; SetAllDirty(); }
        }

        public Sprite Sprite
        {
            get { return sprite; }
            set { sprite = value; texture = null; SetAllDirty(); }
        }

        public float Radius { get { return radius; } set { radius = value; SetVerticesDirty(); } }

        public bool Cover { get { return cover; } set { cover = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect frame = GetPixelAdjustedRect();
            if (frame.width <= 1f || frame.height <= 1f) return;

            Rect uv = new Rect(0f, 0f, 1f, 1f);
            float aspect = 1f;
            if (sprite != null)
            {
                Rect r = sprite.textureRect;
                Texture t = sprite.texture;
                uv = new Rect(r.x / t.width, r.y / t.height, r.width / t.width, r.height / t.height);
                aspect = r.width / Mathf.Max(1f, r.height);
            }
            else if (texture != null) aspect = (float)texture.width / Mathf.Max(1, texture.height);

            float frameAspect = frame.width / frame.height;
            if (HasPicture && cover)
            {
                if (aspect > frameAspect) { float w = uv.width * frameAspect / aspect; uv.x += (uv.width - w) * 0.5f; uv.width = w; }
                else { float h = uv.height * aspect / frameAspect; uv.y += (uv.height - h) * 0.5f; uv.height = h; }
            }
            else if (HasPicture)
            {
                Vector2 center = frame.center;
                if (aspect > frameAspect) frame.height = frame.width / aspect; else frame.width = frame.height * aspect;
                frame.center = center;
            }

            float r0 = Mathf.Clamp(radius, 0f, Mathf.Min(frame.width, frame.height) * 0.5f);
            Color32 solid = color;
            Color clearColor = color; clearColor.a = 0f;
            Color32 clear = clearColor;
            Vector2 mid = frame.center;
            vh.AddVert(mid, solid, Map(mid, frame, uv));
            int count = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                float sx = corner == 0 || corner == 3 ? 1f : -1f, sy = corner < 2 ? 1f : -1f;
                var pivot = new Vector2(sx > 0f ? frame.xMax - r0 : frame.xMin + r0, sy > 0f ? frame.yMax - r0 : frame.yMin + r0);
                for (int i = 0; i <= CornerSteps; i++)
                {
                    float angle = (corner * 90f + 90f * i / CornerSteps) * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 inner = pivot + dir * Mathf.Max(0f, r0 - Fringe * 0.5f);
                    Vector2 outer = pivot + dir * (r0 + Fringe * 0.5f);
                    vh.AddVert(inner, solid, Map(inner, frame, uv));
                    vh.AddVert(outer, clear, Map(outer, frame, uv));
                    count++;
                }
            }
            for (int i = 0; i < count; i++)
            {
                int a = 1 + i * 2, b = 1 + (i + 1) % count * 2;
                vh.AddTriangle(0, b, a);
                vh.AddTriangle(a, b, b + 1);
                vh.AddTriangle(a, b + 1, a + 1);
            }
        }

        private static Vector2 Map(Vector2 p, Rect frame, Rect uv) =>
            new Vector2(uv.x + (p.x - frame.x) / frame.width * uv.width, uv.y + (p.y - frame.y) / frame.height * uv.height);
    }
}
