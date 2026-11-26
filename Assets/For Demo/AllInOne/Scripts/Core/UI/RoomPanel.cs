using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Page of the menu: who is here, the own avatar and role, and what the room runs next.</summary>
    public sealed class RoomPanel : MonoBehaviour
    {
        private const float Width = 900f;
        private const float LabelWidth = 150f;

        [SerializeField] private AppConfig config;
        [SerializeField] private AllInOneApp app;
        [SerializeField] private SessionState session;
        [SerializeField] private RoomConnector connector;
        [SerializeField] private Roster roster;

        private readonly ChoiceGroup avatars = new ChoiceGroup();
        private readonly ChoiceGroup roles = new ChoiceGroup();
        private readonly ChoiceGroup experiences = new ChoiceGroup();
        private readonly ChoiceGroup directors = new ChoiceGroup();
        private readonly ChoiceGroup paces = new ChoiceGroup();

        private DemoPanel panel;
        private DemoLabel people;
        private DemoButton start;

        private LocalChoices Choices => connector.Choices;

        public bool IsShown => panel != null && panel.IsShown;

        public bool IsHeld => panel.IsHeld();

        public void Show(RoomMenu menu)
        {
            if (panel == null) Build(menu);
            if (session.Experience != Experience.None) Choices.experience = session.Experience;
            if (session.Experience == Experience.FullSession) TakeSettings();
            Refresh();
            menu.Place(panel);
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

        private void Build(RoomMenu menu)
        {
            panel = DemoUi.Panel("room", Width, 0f, 40f)
                .Header("Room and session", "close", menu.CloseAll)
                .Label("people", " ", DemoTextStyle.Caption).Out(out people)
                .CardRow(avatars, 120f, PanelParts.AvatarCards(config.avatars), 12f)
                .Options(roles, "Role", LabelWidth, PanelParts.RoleOptions(config.roles))
                .Divider()
                .Options(experiences, "Run", LabelWidth, ("exp-free", "Free play"), ("exp-full", "Full session"))
                .Options(directors, "Steps", LabelWidth, ("director-auto", "Automatic"), ("director-operator", "Operator"))
                .Options(paces, "Pace", LabelWidth, ("pace-study", "Study timing"), ("pace-quick", "Quick preview"))
                .Primary("start", "Start for everybody", Begin).Out(out start);

            avatars.Chosen += app.ChooseAvatar;
            roles.Chosen += app.ChangeRole;
            experiences.Chosen += i =>
            {
                Choices.experience = i == 0 ? Experience.FreePlay : Experience.FullSession;
                Refresh();
            };
            directors.Chosen += i =>
            {
                Choices.director = (Director)i;
                // Before the first step the choice counts at once; a running session only changes hands when nobody operates it.
                if ((session.Waiting || session.Running) && session.MayChangeDirector(roster.Local)) session.SetDirector(Choices.director);
            };
            paces.Chosen += i =>
            {
                Choices.pace = (Pace)i;
                if (session.Waiting && session.MayChangePace(roster.Local)) session.SetPace(Choices.pace);
            };
        }

        private void Begin()
        {
            app.StartForEverybody(Choices.experience, Choices.director, Choices.pace);
            Hide();
        }

        private void OnSession(SessionChange change, bool local)
        {
            if (session.Waiting || session.Running) TakeSettings();
            Refresh();
        }

        /// <summary>The page shows how the session runs. After its end it shows how the next one would run.</summary>
        private void TakeSettings()
        {
            Choices.director = session.Director;
            Choices.pace = session.Pace;
        }

        private void Refresh()
        {
            NetworkPlayer me = roster.Local;
            if (me == null) return;
            people.Text = LobbyPanel.PeopleText(roster, config);
            bool participant = me.Kind == ParticipantKind.Participant;
            avatars.Index = me.AvatarId;
            avatars.SetEnabled(participant && session.MayChangeAvatar(me));
            int mine = roster.RoleOf(me);
            roles.Index = mine;
            for (int role = 0; role < roles.Count; role++)
                roles.SetEnabled(role, participant && session.MayChangeRole(me) && !roster.IsTaken(role));
            bool full = Choices.experience == Experience.FullSession;
            experiences.Index = full ? 1 : 0;
            experiences.SetEnabled(session.MayChangeExperience(me));
            directors.Index = (int)Choices.director;
            directors.SetEnabled(full && !connector.Offline && session.MayChangeDirector(me));
            paces.Index = (int)Choices.pace;
            paces.SetEnabled(full && session.MayChangePace(me));
            start.Interactable = session.MayChangeExperience(me);
            start.Label = full ? "Start the session for everybody" : "Start free play for everybody";
        }
    }
}
