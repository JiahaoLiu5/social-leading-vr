using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// A world-space panel: canvas, paper background, soft shadow, and a collider that stops pointers.
    /// Build one with DemoUi.Panel(...).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DemoPanel : DemoWidget
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform content;
        [SerializeField] private BoxCollider box;
        [SerializeField] private bool ownsBox;
        [SerializeField] private float unitScale = 0.001f;
        [SerializeField] private float fadeSeconds = 0.22f;
        [SerializeField] private DemoPlacement placement;
        [SerializeField] private bool blocksPointers = true;

        private float shown = 1f, target = 1f;
        private bool destroyWhenHidden, introDone, announced, asleep;

        /// <summary>Raised when the panel starts to appear.</summary>
        public event Action<DemoPanel> Shown;

        /// <summary>Raised when the panel has disappeared. A handler may show it again.</summary>
        public event Action<DemoPanel> Hidden;

        /// <summary>The rectangle that holds the widgets.</summary>
        public RectTransform Content { get { return content; } }

        public Canvas Canvas { get { return canvas; } }

        public BoxCollider Box { get { return box; } }

        public DemoPlacement Placement
        {
            get
            {
                if (placement == null) placement = GetComponent<DemoPlacement>();
                if (placement == null) placement = gameObject.AddComponent<DemoPlacement>();
                return placement;
            }
        }

        /// <summary>True from Show until Hide, including the fade in.</summary>
        public bool IsShown { get { return target > 0.5f && gameObject.activeInHierarchy; } }

        /// <summary>True when pointers may use the panel, which is shortly after it starts to appear. Clicks from code only need IsShown.</summary>
        public bool AcceptsPointers { get { return IsShown && blocksPointers && (shown > 0.35f || !Application.isPlaying); } }

        /// <summary>False for captions and toasts, which let rays pass.</summary>
        public bool BlocksPointers
        {
            get { return blocksPointers; }
            set { blocksPointers = value; if (box != null && ownsBox) box.enabled = value && Theme.physicsColliders; }
        }

        /// <summary>
        /// The hand that carries the panel. Pointers of this hand pass through it, so that a menu on the left hand is used
        /// with the right one.
        /// </summary>
        public DemoHand HeldBy { get; set; }

        /// <summary>The box that rays are tested against, in local units.</summary>
        internal Vector3 HitCenter { get; private set; }

        internal Vector3 HitSize { get; private set; }

        internal bool AcceptsFrom(DemoHand pointerHand) => AcceptsPointers && (HeldBy == DemoHand.None || pointerHand != HeldBy);

        public float FadeSeconds { get { return fadeSeconds; } set { fadeSeconds = value; } }

        /// <summary>Size of the panel in metres.</summary>
        public Vector2 SizeMetres { get { return RectTransform.rect.size * unitScale; } }

        // Construction ----------------------------------------------------------------------------------------

        internal static DemoPanel Create(string id, DemoTheme theme, float width, float height, float padding, bool paper)
        {
            RectTransform rt = DemoMake.Rect("DemoPanel " + id, null, theme.layer);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, Mathf.Max(height, 10f));
            var p = rt.gameObject.AddComponent<DemoPanel>();
            p.unitScale = 1f / Mathf.Max(1f, theme.unitsPerMetre);
            rt.localScale = Vector3.one * p.unitScale;

            p.canvas = rt.gameObject.AddComponent<Canvas>();
            p.canvas.renderMode = RenderMode.WorldSpace;
            p.canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2
                | AdditionalCanvasShaderChannels.TexCoord3 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            p.group = rt.gameObject.AddComponent<CanvasGroup>();
            p.content = rt;

            DemoMake.Vertical(rt, theme.spacing, DemoMake.Pad(padding));
            if (height <= 0f)
            {
                var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
                fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            if (paper)
            {
                DemoShape shadow = DemoMake.Backdrop("Shadow", rt, -theme.shadowBlur);
                shadow.SetShadow(theme.panelRadius, theme.shadowBlur);
                shadow.color = theme.shadow;
                shadow.rectTransform.anchoredPosition3D = new Vector3(0f, -theme.shadowBlur * 0.22f, 6f);
                DemoMake.Fill(rt, theme.panelRadius, theme.paper);
                DemoMake.Outline(rt, theme.panelRadius, 2f, new Color(theme.line.r, theme.line.g, theme.line.b, 0.8f));
            }
            p.ownsBox = true;
            p.box = rt.gameObject.AddComponent<BoxCollider>();
            p.box.isTrigger = true;
            p.box.enabled = theme.physicsColliders;
            p.Bind(id, theme);
            p.FitCollider();
            if (Application.isPlaying)
            {
                // A new panel fades in from nothing, also in the frame in which it was made.
                p.shown = 0f;
                p.introDone = true;
                p.Apply();
            }
            return p;
        }

        /// <summary>
        /// Makes an existing world-space canvas a panel: it can be shown and hidden, stops pointers, and its DemoTargets can
        /// be pressed. The content and the size of the canvas stay as they are. The scale of the canvas must be uniform.
        /// </summary>
        public static DemoPanel Adopt(GameObject root, string id)
        {
            var p = root.GetComponent<DemoPanel>();
            if (p == null) p = root.AddComponent<DemoPanel>();
            p.canvas = root.GetComponent<Canvas>();
            p.group = root.GetComponent<CanvasGroup>();
            if (p.group == null) p.group = root.AddComponent<CanvasGroup>();
            p.content = (RectTransform)root.transform;
            p.unitScale = Mathf.Max(1e-6f, root.transform.localScale.x);
            p.box = root.GetComponent<BoxCollider>();
            p.ownsBox = p.box == null;
            if (p.ownsBox)
            {
                p.box = root.AddComponent<BoxCollider>();
                p.box.isTrigger = true;
                p.box.enabled = DemoTheme.Current.physicsColliders;
            }
            p.introDone = true;
            p.Bind(id, null);
            p.FitCollider();
            return p;
        }

        /// <summary>Removes all widgets and returns a builder for new content. A hidden panel stays hidden.</summary>
        public DemoBuilder Rebuild()
        {
            Wake();
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform child = content.GetChild(i);
                var layout = child.GetComponent<LayoutElement>();
                if (layout != null && layout.ignoreLayout && child.GetComponent<DemoShape>() != null) continue;
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject);
            }
            return new DemoBuilder(this, content, Theme);
        }

        /// <summary>Finds a widget of this panel by id.</summary>
        public T Get<T>(string widgetId) where T : DemoWidget => string.IsNullOrEmpty(widgetId) ? null : DemoUi.Search<T>(widgetId, null, this);

        /// <summary>Clicks an element of this panel. Returns false when it is missing, hidden, or disabled.</summary>
        public bool Click(string widgetId)
        {
            DemoElement e = Get<DemoElement>(widgetId);
            return e != null && e.Click();
        }

        /// <summary>Computes the layout now instead of at the end of the frame. Also works while the panel is hidden.</summary>
        public void Layout()
        {
            Wake();
            Canvas.ForceUpdateCanvases();
            foreach (DemoPages p in GetComponentsInChildren<DemoPages>(true)) p.Fit();
            // Two passes: text needs the final width before its height is known.
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            FitCollider();
            foreach (DemoElement e in GetComponentsInChildren<DemoElement>(true)) e.FitCollider();
            Sleep();
        }

        /// <summary>Layout and text need active objects. A hidden panel is made active for that, but stays transparent.</summary>
        private void Wake()
        {
            if (gameObject.activeSelf || target > 0.5f) return;
            asleep = true;
            Apply();
            gameObject.SetActive(true);
        }

        private void Sleep()
        {
            if (asleep && target < 0.5f) gameObject.SetActive(false);
            asleep = false;
        }

        // Show and hide ---------------------------------------------------------------------------------------

        public DemoPanel Show(bool instant = false)
        {
            bool raise = target < 0.5f || !announced;
            destroyWhenHidden = asleep = false;
            target = 1f;
            if (!gameObject.activeSelf) { shown = 0f; Apply(); gameObject.SetActive(true); }
            if (instant || !Application.isPlaying) { shown = 1f; introDone = true; }
            Apply();
            announced = true;
            if (raise) Shown?.Invoke(this);
            return this;
        }

        public DemoPanel Hide(bool instant = false, bool destroy = false)
        {
            bool hidden = target < 0.5f && !gameObject.activeSelf;
            target = 0f;
            destroyWhenHidden |= destroy;
            if (hidden && !destroyWhenHidden) return this;
            if (instant || !Application.isPlaying || !gameObject.activeInHierarchy) { shown = 0f; Gone(!hidden); }
            return this;
        }

        public void Close() { Hide(false, true); }

        /// <summary>
        /// Shows the panel in front of a head (default: DemoUi.Head). Height is relative to the eyes, in metres.
        /// </summary>
        public DemoPanel ShowInFrontOf(Transform head = null, float distance = 1.4f, float height = -0.08f,
            DemoFollow follow = DemoFollow.None, DemoBillboard billboard = DemoBillboard.Yaw, float angle = 0f)
        {
            HeldBy = DemoHand.None;
            Placement.angle = angle;
            Placement.InFrontOf(head != null ? head : DemoUi.Head, distance, height, follow, billboard);
            return Show();
        }

        /// <summary>Shows the panel at a fixed place. The readable side faces against the rotation's forward axis.</summary>
        public DemoPanel ShowAt(Vector3 position, Quaternion rotation)
        {
            HeldBy = DemoHand.None;
            if (placement != null) placement.Release();
            transform.SetPositionAndRotation(position, rotation);
            return Show();
        }

        /// <summary>Shows the panel at a fixed place, turned toward a point.</summary>
        public DemoPanel ShowFacing(Vector3 position, Vector3 viewer, bool upright = true)
        {
            Vector3 away = position - viewer;
            if (upright) away.y = 0f;
            return ShowAt(position, away.sqrMagnitude < 1e-6f ? Quaternion.identity : Quaternion.LookRotation(away));
        }

        /// <summary>
        /// Keeps the panel at an offset from a transform, for example above a wrist. Name the hand that carries it, so that
        /// its own ray does not get in the way.
        /// </summary>
        public DemoPanel ShowAttached(Transform anchor, Vector3 offset, Quaternion rotation, DemoHand heldBy = DemoHand.None)
        {
            HeldBy = heldBy;
            Placement.AttachTo(anchor, offset, rotation);
            return Show();
        }

        /// <summary>Draws the panel over others with a lower order, whatever their distance.</summary>
        public DemoPanel OnTop(int order = 10)
        {
            canvas.sortingOrder = order;
            return this;
        }

        /// <summary>Scales the whole panel. 1 is the size given by the theme.</summary>
        public DemoPanel Scale(float factor)
        {
            unitScale = factor / Mathf.Max(1f, Theme.unitsPerMetre);
            Apply();
            return this;
        }

        /// <summary>
        /// Draws the panel over everything in the scene, also over things that are nearer. For captions and toasts, which
        /// must stay readable when somebody stands close. Call it when the content is complete.
        /// </summary>
        public DemoPanel OverScene(bool on = true)
        {
            foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            {
                if (g is TMP_Text t) t.fontSharedMaterial = on ? DemoMaterials.OverScene(t.font.material) : t.font.material;
                else g.material = on ? DemoMaterials.OverScene(g.defaultMaterial) : null;
            }
            return this;
        }

        private void Start()
        {
            // A panel that was saved with a scene fades in.
            if (!introDone && target > 0.5f && shown >= 1f) { shown = 0f; Apply(); }
            introDone = true;
            if (announced || target < 0.5f) return;
            announced = true;
            Shown?.Invoke(this);
        }

        private void Update()
        {
            if (asleep) { Sleep(); return; }
            if (Mathf.Approximately(shown, target)) return;
            float dt = DemoClock.DeltaTime;
            shown = Mathf.MoveTowards(shown, target, dt / Mathf.Max(0.01f, fadeSeconds * (target > 0.5f ? 1f : 0.7f)));
            Apply();
            if (shown <= 0f && target < 0.5f) Gone(true);
        }

        private void Gone(bool raise)
        {
            Apply();
            asleep = false;
            gameObject.SetActive(false);
            if (raise) Hidden?.Invoke(this);
            if (!destroyWhenHidden || this == null) return;
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }

        private void Apply()
        {
            if (group == null) return;
            float e = DemoEase.OutCubic(shown);
            group.alpha = e;
            transform.localScale = Vector3.one * (unitScale * Mathf.Lerp(0.94f, 1f, e));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            FitCollider();
        }

        private void OnRectTransformDimensionsChange() { FitCollider(); }

        private void FitCollider()
        {
            Rect r = RectTransform.rect;
            // Six millimetres deep, behind the surface, whatever the units of the canvas are.
            float depth = 6f * DemoElement.Millimetre(transform);
            HitCenter = new Vector3(r.center.x, r.center.y, depth * 0.5f);
            HitSize = new Vector3(Mathf.Max(1e-4f, r.width), Mathf.Max(1e-4f, r.height), depth);
            if (!ownsBox || box == null) return;
            box.center = HitCenter;
            box.size = HitSize;
        }
    }
}
