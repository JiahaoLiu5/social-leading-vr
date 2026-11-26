using System;
using System.Collections;
using UnityEngine;

namespace DemoKit
{
    /// <summary>
    /// Fades the whole view of one camera, for teleports and scene changes. Draws in both eyes, in the built-in pipeline
    /// and in URP. Other cameras are not affected.
    /// </summary>
    public sealed class DemoFade : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");

        [SerializeField] private Color color = new Color(0.06f, 0.06f, 0.07f, 1f);
        [Tooltip("Pointers are switched off while the view is covered.")]
        [SerializeField] private bool blockPointers = true;

        private static Mesh sphere;
        private MeshRenderer view;
        private Material material;
        private float alpha, from, target, seconds, elapsed;
        private bool running;
        private Action done, pending;

        public Color Color { get { return color; } set { color = value; Apply(); } }

        /// <summary>0 = clear, 1 = covered.</summary>
        public float Alpha { get { return alpha; } }

        public bool IsRunning { get { return running; } }

        public bool BlockPointers { get { return blockPointers; } set { blockPointers = value; Block(); } }

        /// <summary>The fade of a camera (default: the main camera). Created on first use.</summary>
        public static DemoFade For(Camera camera = null)
        {
            if (camera == null) camera = Camera.main;
            if (camera == null) return null;
            DemoFade fade = camera.GetComponentInChildren<DemoFade>(true);
            return fade != null ? fade : Create(camera);
        }

        private static DemoFade Create(Camera camera)
        {
            var go = new GameObject("DemoFade") { layer = LayerFor(camera) };
            go.transform.SetParent(camera.transform, false);
            var f = go.AddComponent<DemoFade>();
            // The near plane must lie inside the sphere, also in its corners.
            float size = Mathf.Max(0.2f, camera.nearClipPlane * 2.5f);
            f.Build(size);
            return f;
        }

        /// <summary>The first layer the camera renders.</summary>
        private static int LayerFor(Camera camera)
        {
            for (int i = 0; i < 32; i++)
                if ((camera.cullingMask & (1 << i)) != 0) return i;
            return 0;
        }

        private void Build(float size)
        {
            if (sphere == null) sphere = MakeSphere();
            gameObject.AddComponent<MeshFilter>().sharedMesh = sphere;
            view = gameObject.AddComponent<MeshRenderer>();
            material = DemoMaterials.Fade("DemoKit Fade");
            material.SetFloat(RadiusId, size * 0.9f);
            view.sharedMaterial = material;
            view.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view.receiveShadows = false;
            view.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            view.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            view.sortingOrder = 32000;
            Vector3 s = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(size / Mathf.Max(1e-4f, s.x), size / Mathf.Max(1e-4f, s.y), size / Mathf.Max(1e-4f, s.z));
            Apply();
        }

        /// <summary>Covers the view. A fade that starts before the last one has ended replaces it, and the last one's onDone is not called.</summary>
        public void Out(float duration = 0.25f, Action onDone = null) => To(1f, duration, onDone);

        /// <summary>Uncovers the view.</summary>
        public void In(float duration = 0.4f, Action onDone = null) => To(0f, duration, onDone);

        public void To(float newAlpha, float duration, Action onDone = null)
        {
            from = alpha;
            target = Mathf.Clamp01(newAlpha);
            seconds = duration;
            elapsed = 0f;
            done = onDone;
            running = true;
            Block();
            if (duration <= 0f) Finish();
        }

        /// <summary>
        /// Covers the view, runs the action, and uncovers it again. The actions of several calls in a row are all run. When
        /// the camera is switched off or destroyed before the view is covered, they are run at that moment.
        /// </summary>
        public void Blink(Action whileCovered, float outSeconds = 0.22f, float holdSeconds = 0.08f, float inSeconds = 0.38f)
        {
            if (!isActiveAndEnabled) { whileCovered?.Invoke(); return; }
            pending += whileCovered;
            StopAllCoroutines();
            StartCoroutine(BlinkRoutine(RunPending, outSeconds, holdSeconds, inSeconds));
        }

        private void RunPending()
        {
            Action p = pending;
            pending = null;
            p?.Invoke();
        }

        /// <summary>
        /// The same as a coroutine: yield return fade.BlinkRoutine(...). It also ends, after running the action, when the
        /// camera is switched off or destroyed meanwhile.
        /// </summary>
        public IEnumerator BlinkRoutine(Action whileCovered, float outSeconds = 0.22f, float holdSeconds = 0.08f, float inSeconds = 0.38f)
        {
            Out(outSeconds);
            while (Alive && running) yield return null;
            whileCovered?.Invoke();
            for (float t = 0f; Alive && t < holdSeconds; t += DemoClock.DeltaTime) yield return null;
            if (!Alive) yield break;
            In(inSeconds);
            while (Alive && running) yield return null;
        }

        private bool Alive { get { return this != null && isActiveAndEnabled; } }

        /// <summary>Pointers are off while the view is covered or about to be.</summary>
        private void Block() { DemoInput.Block(this, blockPointers && target > 0.5f && isActiveAndEnabled); }

        private void OnEnable() { Block(); }

        private void OnDisable()
        {
            DemoInput.Block(this, false);
            if (gameObject.scene.isLoaded && !quitting) RunPending();
        }

        private static bool quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            quitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting() { quitting = true; }

        private void Update()
        {
            if (!running) return;
            elapsed += DemoClock.DeltaTime;
            float t = seconds <= 0f ? 1f : Mathf.Clamp01(elapsed / seconds);
            alpha = Mathf.Lerp(from, target, DemoEase.InOutCubic(t));
            Apply();
            if (t >= 1f) Finish();
        }

        private void Finish()
        {
            alpha = target;
            running = false;
            Apply();
            Action d = done;
            done = null;
            d?.Invoke();
        }

        private void Apply()
        {
            if (view == null) return;
            view.enabled = alpha > 0.001f;
            material.SetColor(ColorId, new Color(color.r, color.g, color.b, color.a * alpha));
        }

        private void OnDestroy()
        {
            DemoInput.Block(this, false);
            if (material != null)
            {
                if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
            }
        }

        private static Mesh MakeSphere()
        {
            const int rings = 8, segments = 16;
            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var triangles = new int[rings * segments * 6];
            int v = 0, t = 0;
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int s = 0; s <= segments; s++)
                {
                    float theta = 2f * Mathf.PI * s / segments;
                    vertices[v++] = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)) * 1.1f;
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s, b = a + segments + 1;
                    triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = a + 1; triangles[t++] = b + 1;
                }
            return new Mesh { name = "DemoKit Fade", hideFlags = HideFlags.HideAndDontSave, vertices = vertices, triangles = triangles };
        }
    }
}
