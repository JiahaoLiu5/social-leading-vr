using System.Collections.Generic;
using UnityEngine;

namespace DemoKit.Sample
{
    /// <summary>
    /// Shows every component of the kit: a four-step start panel, a gallery, a side panel, captions, and toasts.
    /// Add it to an empty object and press Play, or use the menu Demo Kit > Create Sample Scene.
    /// </summary>
    public sealed class DemoKitSample : MonoBehaviour
    {
        [SerializeField] private bool setUpRig = true;

        public DemoPanel StartPanel { get; private set; }
        public DemoPanel Gallery { get; private set; }
        public DemoPanel Side { get; private set; }
        public DemoCaptionBar Captions { get; private set; }
        public DemoDesktopRig Rig { get; private set; }

        /// <summary>What the user chose so far. Tests read it.</summary>
        public string Log { get; private set; } = string.Empty;

        private DemoPages pages;
        private DemoProgressDots steps;
        private DemoButton back, next;
        private Texture2D[] portraits;

        private void Start()
        {
            if (setUpRig) SetUpRig();
            Build();
            Arrange(new Vector3(0f, 1.5f, 0f));
            DemoUi.AnyClicked += OnAnyClicked;
        }

        private void OnDestroy()
        {
            DemoUi.AnyClicked -= OnAnyClicked;
            if (portraits == null) return;
            foreach (Texture2D t in portraits)
            {
                if (Application.isPlaying) Destroy(t); else DestroyImmediate(t);
            }
        }

        private void OnAnyClicked(DemoElement element, DemoPointer pointer) { Log += element.Path + "\n"; }

        private void SetUpRig()
        {
            if (DemoXr.IsHeadsetActive())
            {
                // The scene's XR rig moves the camera. Aim poses for both hands live next to it.
                Transform space = Camera.main != null ? Camera.main.transform.parent : null;
                if (space == null) return;
                DemoInput.AddHands(DemoPoseDriver.Create(space, DemoHand.Left).transform, DemoPoseDriver.Create(space, DemoHand.Right).transform);
                DemoUi.Head = Camera.main.transform;
                return;
            }
            Rig = DemoDesktopRig.Create(Vector3.zero, 0f, Camera.main);
            Rig.MotionKeys = true;
            DemoUi.Head = Rig.Head;
            DemoInput.AddMouse(Rig.Camera);
        }

        /// <summary>Builds all panels. Also works in edit mode.</summary>
        public void Build()
        {
            portraits = Portraits();
            StartPanel = BuildStart();
            Gallery = BuildGallery();
            Side = BuildSide();
            Captions = DemoCaptionBar.Create(null);
        }

        /// <summary>Places the panels around a pair of eyes that look along Z.</summary>
        public void Arrange(Vector3 eyes)
        {
            StartPanel.ShowFacing(eyes + new Vector3(0f, -0.06f, 1.45f), eyes);
            Gallery.ShowFacing(eyes + new Vector3(1.28f, -0.06f, 1.02f), eyes);
            Side.ShowFacing(eyes + new Vector3(-1.18f, -0.06f, 1.10f), eyes);
            if (Application.isPlaying) Captions.Place(1.2f, -0.62f);
        }

        // Start panel -----------------------------------------------------------------------------------------

        private DemoPanel BuildStart()
        {
            DemoPanel panel = DemoUi.Panel("start", 1140f)
                .Steps("steps", 0, "Avatar", "Role", "Experience", "Join").Out(out steps)
                .Pages("pages", p => p
                    .Page("avatar", AvatarPage)
                    .Page("role", RolePage)
                    .Page("experience", ExperiencePage)
                    .Page("join", JoinPage)).Out(out pages)
                .Row(r => r
                    .Button("back", "Back", () => pages.Back(), DemoStyle.Secondary, DemoIcon.Left, 190f).Out(out back)
                    .Flex()
                    .Primary("next", "Next", () => pages.Next(), DemoIcon.Right, 240f).Out(out next));
            pages.Changed += OnPage;
            OnPage(0);
            back.Snap();
            return panel;
        }

        private void OnPage(int index)
        {
            steps.Index = index;
            back.Interactable = index > 0;
            next.Label = pages.IsLast ? "Join" : "Next";
        }

        private void AvatarPage(DemoBuilder b)
        {
            string[] names = { "Avatar A", "Avatar B", "Avatar C", "Avatar D", "Avatar E", "Avatar F", "Avatar G", "Avatar H", "Avatar I", "Avatar J" };
            var cards = new List<DemoCardData>();
            for (int i = 0; i < names.Length; i++) cards.Add(new DemoCardData(Letter(i), names[i], null, portraits[i % portraits.Length]));
            b.Title("Choose your avatar", "Everyone in the room will see you as this person.")
                .Cards("avatar", cards, null, 4, 2, 136f, 0);
        }

        private static string Letter(int i) => ((char)('a' + i)).ToString();

        private void RolePage(DemoBuilder b)
        {
            DemoTheme t = b.Theme;
            b.Title("Choose your role", "Roles that someone else already has cannot be chosen.")
                .Cards("role", new[]
                {
                    new DemoCardData("leader", "Leader", "Your movement is shared with the follower.") { color = t.leader, icon = DemoIcon.Person },
                    new DemoCardData("follower", "Follower", "Your avatar carries a share of the leader's movement.") { color = t.follower, icon = DemoIcon.Link },
                    new DemoCardData("observer", "Observer", "You take part, and your movement stays your own.") { color = t.observer, icon = DemoIcon.People, tag = "Taken", enabled = false }
                }, null, 3, 1, 0f, 0)
                .Button("role.random", "Assign at random", () => DemoUi.Get<DemoScrollGrid>("role").Select("follower", true), DemoStyle.Secondary, DemoIcon.Dice)
                .Note("In the actual study, participants did not know that there were different roles or that movement was modulated. Roles were assigned at random.");
        }

        private void ExperiencePage(DemoBuilder b)
        {
            b.Title("Choose the experience", "You can come back to this menu at any time.")
                .Cards("experience", new[]
                {
                    new DemoCardData("free", "Free play", "Meet, talk, and gesture. Change the movement link live.") { icon = DemoIcon.Chat },
                    new DemoCardData("session", "Full session", "All steps with narration, from the greeting to the last task.") { icon = DemoIcon.Clock }
                }, null, 2, 1, 0f, 1)
                .Box(x => x
                    .Toggle("director", "Steps", new[] { "Automatic", "Operator" }, 0, null, false, 150f)
                    .Toggle("pace", "Pace", new[] { "Study timing", "Quick preview" }, 0, null, false, 150f), 24f, 14f);
        }

        private void JoinPage(DemoBuilder b)
        {
            b.Title("Join a room", "People who choose the same number meet in the same room.")
                .Cards("join", new[]
                {
                    new DemoCardData("online", "Online", "Meet other people in the room with this number.") { icon = DemoIcon.People },
                    new DemoCardData("solo", "Solo", "No network needed. Simulated partners take the other roles.") { icon = DemoIcon.Person },
                    new DemoCardData("operator", "Operator", "No body. Start the session and go through its steps.") { icon = DemoIcon.Play }
                }, id => DemoUi.Get<DemoStepper>("room").Interactable = id != "solo", 3, 1, 0f, 0)
                .Box(x => x.Stepper("room", "Room number", 1f, 9f, 1f));
        }

        // Gallery ---------------------------------------------------------------------------------------------

        private DemoPanel BuildGallery()
        {
            return DemoUi.Panel("gallery", 940f)
                .Title("Components")
                .Row(r => r
                    .Primary("g.primary", "Primary")
                    .Button("g.secondary", "Secondary")
                    .Button("g.quiet", "Quiet", null, DemoStyle.Quiet)
                    .Button("g.disabled", "Disabled"), 16f)
                .Row(r => r
                    .Button("g.mic", "Speak", null, DemoStyle.Secondary, DemoIcon.Mic)
                    .Button("g.play", "Play", null, DemoStyle.Secondary, DemoIcon.Play)
                    .Button("g.pause", "Pause", null, DemoStyle.Secondary, DemoIcon.Pause)
                    .Button("g.home", null, null, DemoStyle.Secondary, DemoIcon.Home)
                    .Button("g.close", null, null, DemoStyle.Quiet, DemoIcon.Close), 16f)
                .Divider()
                .Stepper("g.ratio", "Ratio", 0f, 1f, 0.5f, null, 0.1f, "0.0")
                .Slider("g.smooth", "Smoothing", 0f, 1f, 0.3f, null, 0.05f, "0.00' s'", 380f)
                .Toggle("g.mode", "Hands", new[] { "Blend", "Superimpose" }, 0)
                .Switch("g.position", "Position channel", false)
                .Progress("g.time", "Time left", 0.64f, "3:12")
                .Dots("g.dots", 5, 1)
                .Do(() => DemoUi.Find("g.disabled").Interactable = false);
        }

        // Side panel ------------------------------------------------------------------------------------------

        private DemoPanel BuildSide()
        {
            return DemoUi.Panel("side", 640f)
                .Header("Movement link", "s.close", () => Side.Hide())
                .Status("s.connection", "Connection", "Connected", DemoTone.Good)
                .Switch("s.link", "Link on", true)
                .Toggle("s.topology", "Topology", new[] { "One", "Chain", "Ring" }, 0, null, true)
                .Stepper("s.ratio", "Ratio", 0f, 1f, 0.4f, null, 0.1f, "0.0")
                .Divider()
                .Button("s.menu", "Back to the menu", () => DemoUi.Toast("The start panel would open here."), DemoStyle.Secondary, DemoIcon.Home);
        }

        // Stand-in pictures -----------------------------------------------------------------------------------

        private static Texture2D[] Portraits()
        {
            Color[] skin = { DemoTheme.Rgb(0xE8B796), DemoTheme.Rgb(0xC98E69), DemoTheme.Rgb(0x8A5A3C), DemoTheme.Rgb(0xF1CDB2), DemoTheme.Rgb(0xB57E58) };
            Color[] hair = { DemoTheme.Rgb(0x2E2621), DemoTheme.Rgb(0x6B3F22), DemoTheme.Rgb(0x1A1A1E), DemoTheme.Rgb(0xB98645), DemoTheme.Rgb(0x4A4038) };
            Color[] shirt = { DemoTheme.Rgb(0x3D4F60), DemoTheme.Rgb(0xE2653B), DemoTheme.Rgb(0x6E8B74), DemoTheme.Rgb(0xA89B8A), DemoTheme.Rgb(0x7A5C8E) };
            Color[] back = { DemoTheme.Rgb(0xE9E1D6), DemoTheme.Rgb(0xDDE5E4), DemoTheme.Rgb(0xEFE0D6), DemoTheme.Rgb(0xE2DFE9), DemoTheme.Rgb(0xE5E8DA) };
            var list = new Texture2D[5];
            for (int i = 0; i < list.Length; i++) list[i] = Portrait(skin[i], hair[i], shirt[i], back[i]);
            return list;
        }

        private static Texture2D Portrait(Color skin, Color hair, Color shirt, Color back)
        {
            const int w = 320, h = 220;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear, hideFlags = HideFlags.HideAndDontSave, name = "Portrait" };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = Color.Lerp(back, Color.Lerp(back, Color.white, 0.5f), y / (float)h);
                    c = Color.Lerp(c, hair, Disc(x, y, 160f, 128f, 58f, 64f));
                    c = Color.Lerp(c, shirt, Disc(x, y, 160f, -34f, 112f, 92f));
                    c = Color.Lerp(c, Color.Lerp(skin, Color.black, 0.10f), Disc(x, y, 160f, 62f, 22f, 34f));
                    c = Color.Lerp(c, skin, Disc(x, y, 160f, 114f, 44f, 54f));
                    c = Color.Lerp(c, hair, Disc(x, y, 160f, 172f, 50f, 26f));
                    pixels[y * w + x] = c;
                }
            t.SetPixels(pixels);
            t.Apply(true, false);
            return t;
        }

        private static float Disc(int x, int y, float cx, float cy, float rx, float ry)
        {
            float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
            return Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * Mathf.Min(rx, ry) + 0.5f);
        }
    }
}
