using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Page of the menu: the movement link of free play, live for everybody in the room.</summary>
    public sealed class LinkPanel : MonoBehaviour
    {
        private const float Width = 900f;
        private const float LabelWidth = 150f;

        [SerializeField] private AppConfig config;
        [SerializeField] private SessionState session;

        private readonly ChoiceGroup hands = new ChoiceGroup();
        private readonly ChoiceGroup position = new ChoiceGroup();

        private DemoPanel panel;
        private DemoSwitch on;
        private DemoStepper ratio, smooth, positionRatio, topology;
        private DemoButton pin;

        public bool IsShown => panel != null && panel.IsShown;

        public bool IsHeld => panel.IsHeld();

        public void Show(RoomMenu menu)
        {
            if (panel == null) Build();
            pin.Interactable = true;
            Refresh();
            menu.Place(panel);
            session.Changed -= OnSession;
            session.Changed += OnSession;
        }

        public void Hide()
        {
            session.Changed -= OnSession;
            if (panel != null) panel.Hide();
        }

        private void OnDestroy() => session.Changed -= OnSession;

        private void Build()
        {
            panel = DemoUi.Panel("link", Width, 0f, 40f)
                .Header("Movement link", "close", Hide)
                .Switch("on", "Link on", true, session.SetLinkOn).Out(out on)
                .Stepper("topology", "Who moves whom", config.topologies.Labels, 0, session.SetTopology).Out(out topology)
                .Divider()
                .Options(hands, "Hands", LabelWidth, ("hand-off", "Off"), ("hand-blend", "Blend"), ("hand-super", "Superimpose"))
                .Stepper("ratio", "Share of hands", 0f, 1f, 0.3f, session.SetHandRatio, 0.1f, "0.0").Out(out ratio)
                .Stepper("smooth", "Smoothing (seconds)", 0f, 1f, 0.2f, session.SetSmoothing, 0.1f, "0.0").Out(out smooth)
                .Divider()
                .Options(position, "Position", LabelWidth, ("pos-off", "Off"), ("pos-super", "Superimpose"), ("pos-blend", "Blend"))
                .Stepper("pos-ratio", "Share of steps", 0f, 1f, 0.3f, session.SetPositionRatio, 0.1f, "0.0").Out(out positionRatio)
                .Row(r => r
                    .Button("reset", "Reset", session.ResetLink, DemoStyle.Quiet)
                    .Flex()
                    .Button("pin", "Put down here", Pin).Out(out pin));
            hands.Chosen += session.SetHandMode;
            position.Chosen += session.SetPositionMode;
        }

        private void Pin()
        {
            pin.Interactable = false;
            panel.HeldBy = DemoHand.None;
            panel.Placement.Release();
        }

        private void OnSession(SessionChange change, bool local)
        {
            if (change == SessionChange.Link || change == SessionChange.Started) Refresh();
        }

        private void Refresh()
        {
            on.SetSilently(session.LinkOn);
            topology.SetSilently(session.Topology);
            hands.Index = session.HandMode;
            ratio.SetSilently(session.HandRatio);
            smooth.SetSilently(session.Smoothing);
            position.Index = session.PositionMode;
            positionRatio.SetSilently(session.PositionRatio);
        }
    }
}
