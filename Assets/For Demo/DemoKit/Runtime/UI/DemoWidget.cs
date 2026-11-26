using UnityEngine;

namespace DemoKit
{
    /// <summary>Anything on a panel that has an id.</summary>
    public abstract class DemoWidget : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private DemoTheme theme;
        private DemoPanel panel;

        /// <summary>Place in the list of enabled widgets, kept by DemoUi.</summary>
        internal int liveIndex = -1;

        public string Id { get { return id; } set { id = value; } }

        public DemoTheme Theme { get { return theme != null ? theme : DemoTheme.Current; } }

        /// <summary>The panel this widget belongs to, or null.</summary>
        public DemoPanel Panel
        {
            get
            {
                if (panel == null) panel = GetComponentInParent<DemoPanel>(true);
                return panel;
            }
        }

        /// <summary>"panelId/widgetId", or just the id outside a panel.</summary>
        public string Path { get { return Panel != null && Panel != this ? Panel.Id + "/" + id : id; } }

        public RectTransform RectTransform { get { return (RectTransform)transform; } }

        /// <summary>True when the widget is active and its panel is shown.</summary>
        public bool IsVisible { get { return this != null && isActiveAndEnabled && (Panel == null || Panel.IsShown); } }

        internal void Bind(string widgetId, DemoTheme widgetTheme)
        {
            id = widgetId;
            theme = widgetTheme;
            DemoUi.Register(this);
            // Components made in edit mode get no OnEnable.
            if (!Application.isPlaying) DemoUi.Enter(this);
        }

        protected virtual void OnEnable()
        {
            DemoUi.Register(this);
            DemoUi.Enter(this);
        }

        protected virtual void OnDisable() { DemoUi.Leave(this); }

        protected virtual void OnDestroy()
        {
            DemoUi.Leave(this);
            DemoUi.Unregister(this);
        }
    }
}
