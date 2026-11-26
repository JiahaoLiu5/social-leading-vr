using System.Collections.Generic;
using UnityEngine;

namespace DemoKit
{
    /// <summary>The line and the reticle of a pointer. Works in the built-in pipeline and in URP, and in single-pass stereo.</summary>
    internal sealed class DemoPointerVisual
    {
        private const int Points = 24;

        private readonly GameObject root;
        private readonly LineRenderer line;
        private readonly Transform reticle;
        private readonly MeshRenderer reticleRenderer;
        private readonly Material lineMaterial, reticleMaterial;
        private readonly Vector3[] points = new Vector3[Points];
        private readonly Gradient gradient = new Gradient();
        private readonly GradientColorKey[] colorKeys = { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
        private readonly GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
        private float alpha, ring, press;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int RingId = Shader.PropertyToID("_Ring");
        private static readonly int PressId = Shader.PropertyToID("_Press");

        /// <summary>The visual is a child of its pointer and goes with it. It is drawn in world space.</summary>
        public DemoPointerVisual(Transform owner, int layer)
        {
            root = new GameObject("Visual") { layer = layer };
            root.transform.SetParent(owner, false);
            Vector3 s = owner.lossyScale;
            root.transform.localScale = new Vector3(1f / NotZero(s.x), 1f / NotZero(s.y), 1f / NotZero(s.z));

            var lineObject = new GameObject("Line") { layer = layer };
            lineObject.transform.SetParent(root.transform, false);
            line = lineObject.AddComponent<LineRenderer>();
            lineMaterial = DemoMaterials.Unlit("DemoKit Line");
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.positionCount = Points;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            line.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            line.sortingOrder = 90;

            var dot = new GameObject("Reticle") { layer = layer };
            dot.transform.SetParent(root.transform, false);
            dot.AddComponent<MeshFilter>().sharedMesh = DemoMaterials.Quad;
            reticleRenderer = dot.AddComponent<MeshRenderer>();
            reticleMaterial = DemoMaterials.Reticle("DemoKit Reticle");
            reticleRenderer.sharedMaterial = reticleMaterial;
            reticleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            reticleRenderer.receiveShadows = false;
            reticleRenderer.sortingOrder = 100;
            reticle = dot.transform;
            Hide();
        }

        private static float NotZero(float v) => Mathf.Abs(v) < 1e-5f ? 1f : v;

        public void Hide()
        {
            alpha = 0f;
            if (line == null) return;
            line.enabled = false;
            reticleRenderer.enabled = false;
        }

        public void Destroy()
        {
            DestroyObject(root);
            DestroyObject(lineMaterial);
            DestroyObject(reticleMaterial);
        }

        /// <summary>A soft curve from the origin to the end point.</summary>
        public void DrawLine(Vector3 origin, Vector3 direction, Vector3 end, float width, Color color, float targetAlpha, float startGap, float dt)
        {
            alpha = DemoEase.Damp(alpha, targetAlpha, 0.05f, dt);
            if (alpha < 0.01f) { line.enabled = false; return; }
            float length = Vector3.Distance(origin, end);
            Vector3 start = origin + direction * Mathf.Min(startGap, length * 0.4f);
            Vector3 control = origin + direction * (length * 0.55f);
            for (int i = 0; i < Points; i++)
            {
                float t = i / (Points - 1f), u = 1f - t;
                points[i] = u * u * start + 2f * u * t * control + t * t * end;
            }
            line.SetPositions(points);
            line.startWidth = width * 0.55f;
            line.endWidth = width;
            // It fades toward the panel, so that it does not cover the text it points at.
            alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            alphaKeys[1] = new GradientAlphaKey(alpha * 0.85f, 0.15f);
            alphaKeys[2] = new GradientAlphaKey(alpha * 0.85f, 0.55f);
            alphaKeys[3] = new GradientAlphaKey(alpha * 0.10f, 1f);
            gradient.SetKeys(colorKeys, alphaKeys);
            line.colorGradient = gradient;
            lineMaterial.SetColor(ColorId, color);
            line.enabled = true;
        }

        /// <summary>A straight stub that fades out, for a ray that is not on a panel.</summary>
        public void DrawStub(Vector3 origin, Vector3 direction, float length, float width, Color color, float targetAlpha, float startGap, float dt)
        {
            alpha = DemoEase.Damp(alpha, targetAlpha, 0.08f, dt);
            if (alpha < 0.01f) { line.enabled = false; return; }
            for (int i = 0; i < Points; i++) points[i] = origin + direction * (startGap + (length - startGap) * i / (Points - 1f));
            line.SetPositions(points);
            line.startWidth = width * 0.55f;
            line.endWidth = width * 0.8f;
            alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            alphaKeys[1] = new GradientAlphaKey(alpha * 0.6f, 0.25f);
            alphaKeys[2] = new GradientAlphaKey(alpha * 0.5f, 0.6f);
            alphaKeys[3] = new GradientAlphaKey(0f, 1f);
            gradient.SetKeys(colorKeys, alphaKeys);
            line.colorGradient = gradient;
            lineMaterial.SetColor(ColorId, color);
            line.enabled = true;
        }

        public void HideLine() { line.enabled = false; alpha = 0f; }

        /// <summary>Ring goes from 0 (dot) to 1 (open ring). Press goes from 0 to 1.</summary>
        public void DrawReticle(Vector3 point, Vector3 normal, float size, Color color, float targetRing, float targetPress, float dt)
        {
            ring = DemoEase.Damp(ring, targetRing, 0.04f, dt);
            press = DemoEase.Damp(press, targetPress, 0.03f, dt);
            reticle.SetPositionAndRotation(point + normal * 0.002f, Quaternion.LookRotation(-normal));
            reticle.localScale = Vector3.one * size;
            reticleMaterial.SetColor(ColorId, color);
            reticleMaterial.SetFloat(RingId, ring);
            reticleMaterial.SetFloat(PressId, press);
            reticleRenderer.enabled = true;
        }

        public void HideReticle() { reticleRenderer.enabled = false; }

        private static void DestroyObject(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }

    /// <summary>Materials and meshes that are made in code.</summary>
    internal static class DemoMaterials
    {
        private static Mesh quad;

        public static Material Unlit(string name) => Make(name, "DemoKit/DemoKitUnlit", "DemoKit/Unlit");

        public static Material Reticle(string name) => Make(name, "DemoKit/DemoKitReticle", "DemoKit/Reticle");

        public static Material Fade(string name) => Make(name, "DemoKit/DemoKitFade", "DemoKit/Fade");

        /// <summary>A unit quad whose front looks along -Z, like a canvas.</summary>
        public static Mesh Quad
        {
            get
            {
                if (quad != null) return quad;
                quad = new Mesh { name = "DemoKit Quad", hideFlags = HideFlags.HideAndDontSave };
                quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
                quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
                quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                quad.RecalculateBounds();
                return quad;
            }
        }

        private static Material Make(string name, string resource, string shaderName)
        {
            Shader shader = Resources.Load<Shader>(resource);
            if (shader == null) shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return new Material(shader) { name = name };
        }

        // Drawing over the scene ------------------------------------------------------------------------------

        private static readonly Dictionary<Material, Material> OnTop = new Dictionary<Material, Material>();

        /// <summary>A copy of an interface material that is drawn over everything that was drawn before, whatever its depth.</summary>
        public static Material OverScene(Material original)
        {
            if (original == null) return null;
            if (OnTop.TryGetValue(original, out Material copy) && copy != null) return copy;
            copy = new Material(original) { name = original.name + " (over the scene)", hideFlags = HideFlags.HideAndDontSave };
            copy.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
            OnTop[original] = copy;
            return copy;
        }
    }
}
