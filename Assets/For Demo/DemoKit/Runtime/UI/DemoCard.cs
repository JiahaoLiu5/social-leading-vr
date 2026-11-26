using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>Content of one card.</summary>
    [Serializable]
    public sealed class DemoCardData
    {
        public string id;
        public string title;
        public string caption;
        [Tooltip("Small label on the card, for example Default or Taken.")]
        public string tag;
        [Tooltip("Picture or RenderTexture.")]
        public Texture texture;
        public Sprite sprite;
        [Tooltip("Icon of a card without a picture.")]
        public DemoIcon icon;
        [Tooltip("Colour of the icon and of the picture background. Clear = from the theme.")]
        public Color color = Color.clear;
        public bool enabled = true;

        public DemoCardData() { }

        public DemoCardData(string id, string title, string caption = null, Texture texture = null)
        {
            this.id = id;
            this.title = title;
            this.caption = caption;
            this.texture = texture;
        }
    }

    /// <summary>
    /// A card with a picture (texture, sprite, or RenderTexture) or an icon, a title, a caption, and a tag.
    /// </summary>
    public sealed class DemoCard : DemoElement
    {
        [SerializeField] private DemoShape fill;
        [SerializeField] private DemoShape line;
        [SerializeField] private DemoShape ring;
        [SerializeField] private DemoShape frame;
        [SerializeField] private DemoPicture picture;
        [SerializeField] private DemoShape icon;
        [SerializeField] private RectTransform check;
        [SerializeField] private CanvasGroup checkGroup;
        [SerializeField] private RectTransform tagBox;
        [SerializeField] private DemoShape tagFill;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI caption;
        [SerializeField] private TextMeshProUGUI tagText;
        [SerializeField] private Color tint;
        [SerializeField] private bool pictured;

        public DemoPicture Picture { get { return picture; } }

        public string Title { get { return title.text; } set { title.text = value ?? string.Empty; } }

        public string Caption
        {
            get { return caption.text; }
            set { caption.text = value ?? string.Empty; caption.gameObject.SetActive(!string.IsNullOrEmpty(value)); }
        }

        /// <summary>Small label on the card. Empty hides it.</summary>
        public string Tag
        {
            get { return tagText.text; }
            set { tagText.text = value ?? string.Empty; tagBox.gameObject.SetActive(!string.IsNullOrEmpty(value)); }
        }

        /// <summary>Shows a texture or a RenderTexture.</summary>
        public void SetTexture(Texture texture) { if (picture != null) picture.Texture = texture; Snap(); }

        public void SetSprite(Sprite sprite) { if (picture != null) picture.Sprite = sprite; Snap(); }

        internal static DemoCard Create(Transform parent, string id, DemoCardData data, DemoTheme theme, float pictureHeight)
        {
            RectTransform root = DemoMake.Rect("Card " + id, parent);
            VerticalLayoutGroup rootLayout = DemoMake.Vertical(root, 0f);
            rootLayout.childForceExpandHeight = true;
            var c = root.gameObject.AddComponent<DemoCard>();
            c.pictured = pictureHeight > 0f;
            c.tint = data.color.a > 0f ? data.color : theme.accent;
            DemoMake.Size(root, 0f, -1f, 1f);

            c.body = DemoMake.Rect("Body", root);
            c.fill = DemoMake.Fill(c.body, theme.cardRadius, Color.white);
            c.line = DemoMake.Outline(c.body, theme.cardRadius, 2.5f, theme.line);
            c.ring = DemoMake.Outline(c.body, theme.cardRadius, 5f, theme.accent);

            if (c.pictured)
            {
                DemoMake.Vertical(c.body, 10f, DemoMake.Pad(12f, 12f, 12f, 16f));
                RectTransform view = DemoMake.Rect("Picture", c.body);
                DemoMake.Size(view, -1f, pictureHeight, -1f, 1f);
                float inner = Mathf.Max(6f, theme.cardRadius - 8f);
                c.frame = DemoMake.Fill(view, inner, data.color.a > 0f ? data.color : theme.surface);
                RectTransform image = DemoMake.Stretch(DemoMake.Rect("Image", view));
                image.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                c.picture = image.gameObject.AddComponent<DemoPicture>();
                c.picture.raycastTarget = false;
                c.picture.Radius = inner;
                if (data.sprite != null) c.picture.Sprite = data.sprite; else c.picture.Texture = data.texture;
                if (data.icon != DemoIcon.None && !c.picture.HasPicture)
                {
                    c.icon = DemoMake.Icon(view, data.icon, pictureHeight * 0.5f, theme.inkFaint);
                    DemoMake.Anchor(c.icon.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.one * pictureHeight * 0.5f);
                }
                c.title = DemoMake.Line(c.body, data.title, theme, theme.captionSize + 2f, theme.ink, true, TextAlignmentOptions.Top);
                DemoMake.Size(c.title, -1f, (theme.captionSize + 2f) * 1.3f);
                c.caption = DemoMake.Text(c.body, data.caption, theme, theme.captionSize - 2f, theme.inkSoft, false, TextAlignmentOptions.Top);
            }
            else
            {
                DemoMake.Vertical(c.body, 14f, DemoMake.Pad(30f, 30f, 28f, 30f));
                RectTransform head = DemoMake.Rect("Head", c.body);
                DemoMake.Horizontal(head, 18f, null, TextAnchor.MiddleLeft);
                if (data.icon != DemoIcon.None)
                {
                    RectTransform badge = DemoMake.Rect("Badge", head);
                    DemoMake.Size(badge, 60f, 60f);
                    c.frame = DemoMake.Fill(badge, 18f, Color.Lerp(Color.white, c.tint, 0.14f));
                    c.icon = DemoMake.Icon(badge, data.icon, 34f, c.tint);
                    DemoMake.Anchor(c.icon.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.one * 34f);
                }
                c.title = DemoMake.Text(head, data.title, theme, theme.headingSize - 1f, theme.ink, true, TextAlignmentOptions.Left);
                DemoMake.Size(c.title, -1f, -1f, 1f);
                c.caption = DemoMake.Text(c.body, data.caption, theme, theme.captionSize + 1f, theme.inkSoft);
            }
            c.caption.gameObject.SetActive(!string.IsNullOrEmpty(data.caption));

            // The check mark and the tag lie over the corners and take no part in the layout.
            c.check = DemoMake.Rect("Check", c.body);
            c.check.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            DemoMake.Anchor(c.check, Vector2.one, Vector2.one * 0.5f, new Vector2(-16f, -16f), Vector2.one * 46f);
            DemoMake.Fill(c.check, 23f, Color.white);
            DemoShape disc = DemoMake.Backdrop("Disc", c.check, 4f);
            disc.SetRounded(19f);
            disc.color = theme.accent;
            DemoShape mark = DemoMake.Icon(c.check, DemoIcon.Check, 26f, Color.white);
            DemoMake.Anchor(mark.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.one * 26f);
            c.checkGroup = c.check.gameObject.AddComponent<CanvasGroup>();

            c.tagBox = DemoMake.Rect("Tag", c.body);
            c.tagBox.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            DemoMake.Anchor(c.tagBox, new Vector2(0f, 1f), new Vector2(0f, 1f), c.pictured ? new Vector2(22f, -22f) : new Vector2(28f, 13f), Vector2.zero);
            var fit = c.tagBox.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            DemoMake.Horizontal(c.tagBox, 0f, DemoMake.Pad(13f, 13f, 3f, 4f), TextAnchor.MiddleCenter);
            c.tagFill = DemoMake.Fill(c.tagBox, 15f, theme.ink);
            c.tagText = DemoMake.Text(c.tagBox, data.tag, theme, theme.captionSize - 5f, Color.white, true, TextAlignmentOptions.Center, false);
            c.tagText.overflowMode = TextOverflowModes.Overflow;
            c.tagBox.gameObject.SetActive(!string.IsNullOrEmpty(data.tag));

            c.Interactable = data.enabled;
            c.Bind(id, theme);
            c.Finish();
            return c;
        }

        protected override void Draw(float hover, float press, float select, float disable)
        {
            if (fill == null || title == null) return;
            DemoTheme t = Theme;
            float on = 1f - disable;
            Color back = Color.Lerp(Color.white, t.accentSoft, select * 0.85f);
            back = Color.Lerp(back, t.surface, Mathf.Max(Mathf.Max(press * 0.8f, disable * 0.7f), hover * (1f - select) * on * 0.45f));
            fill.color = back;
            line.color = new Color(t.line.r, t.line.g, t.line.b, 1f - 0.4f * disable);
            ring.color = new Color(t.accent.r, t.accent.g, t.accent.b, on * Mathf.Max(select, hover * 0.55f));
            title.color = Color.Lerp(t.ink, t.inkFaint, disable);
            caption.color = Color.Lerp(t.inkSoft, t.inkFaint, disable);
            if (picture != null)
            {
                Color p = picture.HasPicture ? Color.white : Color.clear;
                p.a *= 1f - 0.6f * disable;
                picture.color = p;
            }
            if (icon != null) icon.color = Color.Lerp(pictured ? t.inkFaint : tint, t.inkFaint, disable);
            if (!pictured && frame != null) frame.color = Color.Lerp(Color.Lerp(Color.Lerp(Color.white, tint, 0.16f), Color.white, select), t.surfaceDeep, disable * 0.6f);
            if (tagFill != null)
            {
                tagFill.color = Color.Lerp(t.ink, t.inkSoft, disable);
                tagText.color = Color.white;
            }
            float show = select * on;
            checkGroup.alpha = show;
            check.localScale = Vector3.one * (show <= 0.01f ? 0.6f : 0.6f + 0.4f * DemoEase.OutBack(show));
            body.localScale = Vector3.one * (1f + 0.02f * hover * on - 0.03f * press);
            Vector3 z = body.localPosition;
            z.z = -7f * (hover * on - press);
            body.localPosition = z;
        }
    }
}
