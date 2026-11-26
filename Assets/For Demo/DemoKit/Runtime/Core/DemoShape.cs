using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// An Image that draws a rounded fill, an outline, a soft shadow, or an icon.
    /// It fetches its sprite when it is enabled, so it also works in scenes and prefabs saved from the editor.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class DemoShape : Image
    {
        public enum Form { Rounded, Outline, Shadow, Icon }

        [SerializeField] private Form form;
        [SerializeField] private float radius = 20f;
        [SerializeField] private float amount = 2f;
        [SerializeField] private DemoIcon icon;

        public float Radius { get { return radius; } }
        public DemoIcon IconName { get { return icon; } }

        public void SetRounded(float cornerRadius) { form = Form.Rounded; radius = cornerRadius; Refresh(); }

        public void SetOutline(float cornerRadius, float thickness) { form = Form.Outline; radius = cornerRadius; amount = thickness; Refresh(); }

        public void SetShadow(float cornerRadius, float blur) { form = Form.Shadow; radius = cornerRadius; amount = blur; Refresh(); }

        public void SetIcon(DemoIcon name) { form = Form.Icon; icon = name; Refresh(); }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        private void Refresh()
        {
            raycastTarget = false;
            DemoTheme theme = DemoTheme.Current;
            switch (form)
            {
                case Form.Rounded: Apply(theme.roundedSprite, DemoSprites.Rounded(radius), true); break;
                case Form.Outline: Apply(theme.outlineSprite, DemoSprites.Outline(radius, amount), false); break;
                case Form.Shadow: Apply(theme.shadowSprite, DemoSprites.Shadow(radius, amount), true); break;
                case Form.Icon:
                    type = Type.Simple;
                    preserveAspect = true;
                    overrideSprite = icon == DemoIcon.None ? null : DemoSprites.Icon(icon);
                    enabled = icon != DemoIcon.None;
                    break;
            }
        }

        private void Apply(Sprite custom, Sprite generated, bool fill)
        {
            type = Type.Sliced;
            fillCenter = fill;
            overrideSprite = custom != null ? custom : generated;
            float corner = custom != null ? custom.border.x * 100f / custom.pixelsPerUnit : 0f;
            pixelsPerUnitMultiplier = corner > 0f ? corner / Mathf.Max(1f, radius) : 1f;
        }
    }
}
