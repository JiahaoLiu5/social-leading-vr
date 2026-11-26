using System;
using TMPro;
using UnityEngine;

namespace DemoKit
{
    /// <summary>
    /// Colours, sizes, fonts, and sprites shared by every panel.
    /// All lengths are in panel units. With the default scale one unit is one millimetre.
    /// </summary>
    [CreateAssetMenu(fileName = "DemoTheme", menuName = "Demo Kit/Theme")]
    public sealed class DemoTheme : ScriptableObject
    {
        [Header("Surfaces")]
        public Color paper = Rgb(0xFDFCFA);
        public Color surface = Rgb(0xF5F1EB);
        public Color surfaceDeep = Rgb(0xEAE4DB);
        public Color line = Rgb(0xDFD8CE);
        public Color shadow = new Color(0.09f, 0.08f, 0.07f, 0.34f);

        [Header("Text")]
        public Color ink = Rgb(0x1E2329);
        public Color inkSoft = Rgb(0x57616B);
        public Color inkFaint = Rgb(0xA8A198);
        public Color onAccent = Color.white;

        [Header("Accent")]
        public Color accent = Rgb(0xE2653B);
        public Color accentHover = Rgb(0xEC7750);
        public Color accentPressed = Rgb(0xC24F28);
        public Color accentSoft = Rgb(0xFDEDE6);

        [Header("Roles")]
        public Color leader = Rgb(0xE2653B);
        public Color follower = Rgb(0x3D4F60);
        public Color observer = Rgb(0xA89B8A);

        [Header("Feedback")]
        public Color good = Rgb(0x3B8F6E);
        public Color warning = Rgb(0xC98714);
        public Color danger = Rgb(0xBF3A2B);

        [Header("Shape (units)")]
        public float panelRadius = 40f;
        public float cardRadius = 26f;
        public float buttonRadius = 22f;
        public float boxRadius = 20f;
        public float panelPadding = 52f;
        public float spacing = 24f;
        public float shadowBlur = 70f;

        [Header("Type (units)")]
        public float titleSize = 50f;
        public float headingSize = 35f;
        public float bodySize = 30f;
        public float buttonSize = 30f;
        public float captionSize = 25f;

        [Header("Fonts (empty = the kit font, then the TextMeshPro default)")]
        public TMP_FontAsset font;
        public TMP_FontAsset fontBold;

        [Header("Sprites (empty = generated at run time)")]
        [Tooltip("9-slice sprite for filled rounded shapes. The left border is taken as the corner radius.")]
        public Sprite roundedSprite;
        [Tooltip("9-slice sprite for rounded outlines. The left border is taken as the corner radius.")]
        public Sprite outlineSprite;
        [Tooltip("9-slice sprite for the soft shadow behind panels.")]
        public Sprite shadowSprite;

        [Header("World")]
        [Tooltip("Panel units in one metre.")]
        public float unitsPerMetre = 1000f;
        [Tooltip("Layer of panels and their colliders. 5 is Unity's UI layer.")]
        public int layer = 5;
        [Tooltip("Off: the box colliders only hold the sensitive areas for the kit's own hit tests and are invisible to physics. On: they are triggers in the physics scene as well.")]
        public bool physicsColliders;

        private static DemoTheme current;
        private static TMP_FontAsset kitRegular, kitBold;
        private static bool fontsLoaded;

        /// <summary>
        /// The theme used by new panels: the one that was assigned, else an asset at Resources/DemoKit/DemoTheme,
        /// else a default created in code.
        /// </summary>
        public static DemoTheme Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<DemoTheme>("DemoKit/DemoTheme");
                if (current == null)
                {
                    current = CreateInstance<DemoTheme>();
                    current.name = "DemoTheme (default)";
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
            set { current = value; }
        }

        public TMP_FontAsset Regular
        {
            get
            {
                if (font != null) return font;
                LoadFonts();
                return kitRegular != null ? kitRegular : TMP_Settings.defaultFontAsset;
            }
        }

        /// <summary>The semibold face, or null when only a regular face exists (the bold style is used then).</summary>
        public TMP_FontAsset Bold
        {
            get
            {
                if (fontBold != null) return fontBold;
                if (font != null) return null;
                LoadFonts();
                return kitBold;
            }
        }

        /// <summary>The role colour for "leader", "follower", or "observer" (any case). Accent for anything else.</summary>
        public Color Role(string role)
        {
            if (string.Equals(role, "leader", StringComparison.OrdinalIgnoreCase)) return leader;
            if (string.Equals(role, "follower", StringComparison.OrdinalIgnoreCase)) return follower;
            if (string.Equals(role, "observer", StringComparison.OrdinalIgnoreCase)) return observer;
            return accent;
        }

        public Color Tone(DemoTone tone)
        {
            switch (tone)
            {
                case DemoTone.Good: return good;
                case DemoTone.Warning: return warning;
                case DemoTone.Danger: return danger;
                case DemoTone.Accent: return accent;
                default: return inkSoft;
            }
        }

        /// <summary>Colour from a hexadecimal value such as 0xE2653B.</summary>
        public static Color Rgb(int hex, float alpha = 1f) =>
            new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, alpha);

        private static void LoadFonts()
        {
            if (fontsLoaded && (kitRegular != null || kitBold != null)) return;
            fontsLoaded = true;
            kitRegular = Resources.Load<TMP_FontAsset>("DemoKit/DemoKit Text SDF");
            kitBold = Resources.Load<TMP_FontAsset>("DemoKit/DemoKit SemiBold SDF");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = null;
            kitRegular = kitBold = null;
            fontsLoaded = false;
        }
    }

    /// <summary>The meaning of a message or a status value.</summary>
    public enum DemoTone { Neutral, Accent, Good, Warning, Danger }
}
