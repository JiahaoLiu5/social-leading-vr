using System.Text;
using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Shown after connecting and before entering. Only now is it known which roles other people have and what the room runs.
    /// </summary>
    public sealed class LobbyPanel : MonoBehaviour
    {
        private const float Width = 1120f;

        [SerializeField] private AppConfig config;
        [SerializeField] private AllInOneApp app;
        [SerializeField] private RoomConnector connector;
        [SerializeField] private SessionState session;
        [SerializeField] private Roster roster;

        private readonly ChoiceGroup avatars = new ChoiceGroup();
        private readonly ChoiceGroup roles = new ChoiceGroup();

        private DemoPanel panel;
        private DemoLabel heading, people, status;
        private DemoButton again, free;

        private LocalChoices Choices => connector.Choices;

        public void Show(Pose place)
        {
            if (panel == null) Build();
            heading.Text = "Room " + Choices.room;
            avatars.Index = Choices.avatar;
            Refresh();
            panel.ShowAt(place.position, place.rotation);
            roster.Changed -= Refresh;
            roster.Changed += Refresh;
            session.Changed -= OnSession;
            session.Changed += OnSession;
        }

        public void Hide()
        {
            roster.Changed -= Refresh;
            session.Changed -= OnSession;
            if (panel != null) panel.Hide();
        }

        private void OnDestroy()
        {
            roster.Changed -= Refresh;
            session.Changed -= OnSession;
        }

        private void Build()
        {
            panel = DemoUi.Panel("lobby", Width)
                .Label("heading", "Room", DemoTextStyle.Title).Out(out heading)
                .Label("status", "The room is empty.").Out(out status)
                .Label("people", "Nobody else is here.", DemoTextStyle.Caption).Out(out people)
                .Divider()
                .CardRow(roles, 0f, PanelParts.RoleCards(config.roles, DemoUi.Theme, false))
                .Button("role-random", "Assign at random", null, DemoStyle.Secondary, DemoIcon.Dice).Out(out DemoButton random)
                .Do(() => roles.Add(random))
                .CardRow(avatars, 130f, PanelParts.AvatarCards(config.avatars), 18f)
                .Label("note", config.roleNote, DemoTextStyle.Caption)
                .Row(r => r
                    .Button("again", "Run the session again", () => app.ChooseRestart(Experience.FullSession)).Out(out again)
                    .Button("free", "Start free play", () => app.ChooseRestart(Experience.FreePlay)).Out(out free))
                .Row(r => r
                    .Button("leave", "Leave", app.Leave, DemoStyle.Quiet, DemoIcon.Left, 200f)
                    .Flex()
                    .Primary("enter", "Enter", app.Enter, DemoIcon.Right, 260f));
            avatars.Chosen += app.ChooseAvatar;
            roles.Chosen += i => Choices.role = i < config.roles.Count ? i : LocalChoices.RandomRole;
        }

        private void OnSession(SessionChange change, bool local) => Refresh();

        private void Refresh()
        {
            for (int role = 0; role < config.roles.Count; role++)
            {
                bool taken = roster.IsTaken(role);
                roles.SetEnabled(role, !taken);
                roles.SetTag(role, taken ? "Taken" : string.Empty);
                if (taken && Choices.role == role) Choices.role = FirstFree();
            }
            roles.Index = Choices.role == LocalChoices.RandomRole ? config.roles.Count : Choices.role;
            bool full = FirstFree() == LocalChoices.RandomRole;
            roles.SetEnabled(config.roles.Count, !full);
            people.Text = PeopleText(roster, config);
            status.Text = full ? StatusText() + " All roles are taken, so you would watch." : StatusText();
            bool finished = session.Finished && roster.SomebodyElseInRoom;
            again.gameObject.SetActive(finished);
            free.gameObject.SetActive(finished);
            if (panel.IsShown) panel.Layout();
        }

        private int FirstFree()
        {
            for (int role = 0; role < config.roles.Count; role++)
                if (!roster.IsTaken(role)) return role;
            return LocalChoices.RandomRole;
        }

        private string StatusText()
        {
            if (session.Idle)
            {
                return Choices.experience == Experience.FreePlay
                    ? "You start free play here."
                    : "You start a full session here.";
            }
            if (session.Experience == Experience.FreePlay) return "This room runs free play.";
            if (session.Finished) return "This room has finished its session.";
            string steps = session.Waiting ? "waiting to start" : "step " + (session.Step + 1) + " of " + session.StepCount;
            return "This room runs a full session, " + (session.Director == Director.Automatic ? "automatic" : "with an operator") + ", " + steps + ".";
        }

        /// <summary>One line per person in the room.</summary>
        public static string PeopleText(Roster roster, AppConfig config)
        {
            var text = new StringBuilder();
            foreach (IParticipant p in roster.People)
            {
                if (!p.InRoom) continue;
                if (text.Length > 0) text.Append('\n');
                text.Append(p.IsLocal ? "You" : p.IsBot ? "Simulated partner" : "Person " + (p.Id + 1));
                text.Append(": ").Append(RoleText(p, roster, config));
                if (p.Kind == ParticipantKind.Participant && p.AvatarId >= 0) text.Append(", ").Append(config.avatars.Label(p.AvatarId));
            }
            return text.Length > 0 ? text.ToString() : "Nobody else is here.";
        }

        private static string RoleText(IParticipant p, Roster roster, AppConfig config)
        {
            switch (p.Kind)
            {
                case ParticipantKind.Operator: return "operator";
                case ParticipantKind.Spectator: return "watching";
                default: return config.roles.Label(roster.RoleOf(p));
            }
        }
    }
}
