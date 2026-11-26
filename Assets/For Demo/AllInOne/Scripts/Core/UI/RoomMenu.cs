using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// The menu inside a room and the place of its pages: on the left controller with a headset, beside the view on a desktop.
    /// </summary>
    public sealed class RoomMenu : MonoBehaviour
    {
        private const float Width = 460f;

        // In front of the left hand and turned to the head, so that the right hand can point at it from the side.
        private static readonly Vector3 HandOffset = new Vector3(0.02f, 0.14f, 0.22f);
        private static readonly Quaternion HandTilt = Quaternion.Euler(25f, -15f, 0f);

        // At the left of the picture of a desktop, parallel to it.
        private static readonly Vector3 DesktopOffset = new Vector3(-0.46f, 0.02f, 0.9f);

        [SerializeField] private AllInOneApp app;
        [SerializeField] private SessionState session;
        [SerializeField] private RoomConnector connector;
        [SerializeField] private Roster roster;
        [SerializeField] private RigMode mode;
        [SerializeField] private ViewRig view;
        [SerializeField] private Seating seating;
        [SerializeField] private AppInput input;
        [SerializeField] private RoomPanel roomPage;
        [SerializeField] private LinkPanel linkPage;

        private DemoPanel panel, leaveDialog;
        private bool active;

        /// <summary>True while the menu or one of its pages is shown.</summary>
        public bool IsOpen => IsShown(panel) || roomPage.IsShown || linkPage.IsShown;

        /// <summary>True while a page hangs on the left hand. The ray of that hand is off then, because it would cross the page.</summary>
        public bool OnLeftHand => panel.IsHeld() || roomPage.IsHeld || linkPage.IsHeld;

        public void Begin() => active = true;

        public void End()
        {
            active = false;
            CloseAll();
        }

        public void CloseAll()
        {
            if (panel != null) panel.Hide();
            if (leaveDialog != null) leaveDialog.Hide();
            roomPage.Hide();
            linkPage.Hide();
        }

        public void OpenMenu()
        {
            CloseAll();
            Build();
            Place(panel);
        }

        public void OpenRoom()
        {
            CloseAll();
            roomPage.Show(this);
        }

        public void OpenLink()
        {
            if (!session.MayChangeLink(roster.Local)) return;
            CloseAll();
            linkPage.Show(this);
        }

        /// <summary>Puts a page where the menu belongs.</summary>
        public void Place(DemoPanel page)
        {
            if (mode.Desktop)
            {
                page.Scale(0.62f);
                page.ShowAttached(DemoUi.Head, DesktopOffset, Quaternion.identity).OnTop(8);
                return;
            }
            page.Scale(0.5f);
            page.ShowAttached(view.LeftPointer, HandOffset, HandTilt, DemoHand.Left).OnTop(8);
        }

        private void Update()
        {
            if (!active) return;
            if (input.MenuPressed)
            {
                if (IsOpen) CloseAll();
                else OpenMenu();
            }
            else if (input.LinkPressed)
            {
                if (linkPage.IsShown) linkPage.Hide();
                else OpenLink();
            }
            else if (input.LeavePressed) AskToLeave();
            // The link only exists in free play.
            if (linkPage.IsShown && session.Experience != Experience.FreePlay) linkPage.Hide();
        }

        private void Build()
        {
            if (panel != null) panel.Close();
            LocalChoices choices = connector.Choices;
            DemoBuilder b = DemoUi.Panel("menu", Width, 0f, 36f)
                .Header("Menu", "close", CloseAll)
                .Button("room", "Room and session", OpenRoom, DemoStyle.Secondary, DemoIcon.People);
            if (session.MayChangeLink(roster.Local)) b.Button("link", "Movement link", OpenLink, DemoStyle.Secondary, DemoIcon.Link);
            b.Switch("mute", "Mute microphone", roster.Local != null && roster.Local.Muted, SetMuted)
                .Switch("captions", "Captions", choices.captions, on => choices.captions = on)
                .Button("recentre", "Recentre", seating.Recentre, DemoStyle.Secondary, DemoIcon.Home);
            if (AnswerStore.HasAnswers) b.Button("delete-answers", "Delete my answers", app.DeleteAnswers);
            b.Button("leave", "Leave the room", app.Leave, DemoStyle.Secondary, DemoIcon.Left);
            panel = b.Build();
            DemoUi.Get<DemoSwitch>("menu/mute").Interactable = !connector.Offline;
        }

        private void SetMuted(bool muted)
        {
            connector.Choices.muted = muted;
            if (roster.Local != null) roster.Local.SetMuted(muted);
        }

        private void AskToLeave()
        {
            CloseAll();
            if (leaveDialog == null)
            {
                leaveDialog = DemoUi.Panel("leave-dialog", 560f)
                    .Heading("Leave the room?")
                    .Row(r => r
                        .Button("stay", "Stay", () => leaveDialog.Hide())
                        .Flex()
                        .Primary("yes", "Leave", app.Leave));
            }
            leaveDialog.ShowInFrontOf(DemoUi.Head, 1.1f, -0.05f).OnTop(9);
        }

        private static bool IsShown(DemoPanel p) => p != null && p.IsShown;
    }
}
