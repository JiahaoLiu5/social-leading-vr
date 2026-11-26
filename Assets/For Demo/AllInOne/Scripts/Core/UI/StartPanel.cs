using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>The panel of the menu: avatar, role, experience, and how to join, in four short steps.</summary>
    public sealed class StartPanel : MonoBehaviour
    {
        private const float Width = 1120f;
        private const float LabelWidth = 150f;

        [SerializeField] private AppConfig config;
        [SerializeField] private AllInOneApp app;
        [SerializeField] private RoomConnector connector;

        private readonly ChoiceGroup avatars = new ChoiceGroup();
        private readonly ChoiceGroup roles = new ChoiceGroup();
        private readonly ChoiceGroup experiences = new ChoiceGroup();
        private readonly ChoiceGroup directors = new ChoiceGroup();
        private readonly ChoiceGroup paces = new ChoiceGroup();
        private readonly ChoiceGroup rooms = new ChoiceGroup();

        private DemoPanel panel;
        private DemoPages pages, joinPages;
        private DemoProgressDots steps;
        private DemoButton back, next, online, operatorOnline, deleteAnswers;
        private DemoLabel status;

        private LocalChoices Choices => connector.Choices;

        /// <summary>Shows the panel with the choices the user made last.</summary>
        public void Show(Pose place)
        {
            if (panel == null) Build();
            joinPages.Go(0);
            ShowChoices();
            panel.ShowAt(place.position, place.rotation);
        }

        public void Hide()
        {
            if (panel != null) panel.Hide();
        }

        /// <summary>Opens the last page, for a user who wants to join again.</summary>
        public void GoToJoin(string message)
        {
            pages.Go(pages.Count - 1);
            SetStatus(message);
        }

        /// <summary>Opens the first page.</summary>
        public void GoToStart()
        {
            if (panel == null) return;
            pages.Go(0);
            SetStatus(string.Empty);
        }

        /// <summary>Switches the join buttons off while a connection is made.</summary>
        public void SetBusy(bool busy, string message = "")
        {
            if (panel == null) return;
            joinPages.gameObject.SetActive(!busy);
            back.Interactable = !busy && pages.Index > 0;
            SetStatus(message);
        }

        // Building --------------------------------------------------------------------------------------------

        private void Build()
        {
            panel = DemoUi.Panel("start", Width)
                .Steps("steps", 0, "Avatar", "Role", "Experience", "Join").Out(out steps)
                .Pages("pages", p => p
                    .Page("avatar", AvatarPage)
                    .Page("role", RolePage)
                    .Page("experience", ExperiencePage)
                    .Page("join", JoinPage)).Out(out pages)
                .Row(r => r
                    .Button("back", "Back", () => pages.Back(), DemoStyle.Quiet, DemoIcon.Left, 190f).Out(out back)
                    .Flex()
                    .Primary("next", "Next", () => pages.Next(), DemoIcon.Right, 240f).Out(out next));
            pages.Changed += OnPage;
            OnPage(0);

            avatars.Chosen += app.ChooseAvatar;
            roles.Chosen += i => Choices.role = i < config.roles.Count ? i : LocalChoices.RandomRole;
            experiences.Chosen += i =>
            {
                Choices.experience = i == 0 ? Experience.FreePlay : Experience.FullSession;
                ShowSessionOptions();
            };
            directors.Chosen += i => Choices.director = (Director)i;
            paces.Chosen += i => Choices.pace = (Pace)i;
            rooms.Chosen += i =>
            {
                Choices.room = i + 1;
                ShowOnline();
            };
        }

        private void AvatarPage(DemoBuilder b)
        {
            b.Title("Choose your avatar", "Everyone in the room sees you as this person.")
                .CardRow(avatars, 214f, PanelParts.AvatarCards(config.avatars), 18f);
        }

        private void RolePage(DemoBuilder b)
        {
            b.Title("Choose your role", "Your choice in this demo. Two people share one room.")
                .CardRow(roles, 0f, PanelParts.RoleCards(config.roles, b.Theme, true))
                .Button("role-random", "Assign at random", null, DemoStyle.Secondary, DemoIcon.Dice).Out(out DemoButton random)
                .Do(() => roles.Add(random))
                .Label("note", config.roleNote, DemoTextStyle.Caption);
        }

        private void ExperiencePage(DemoBuilder b)
        {
            int minutes = Mathf.RoundToInt(config.script.UsualDuration(Pace.Study) / 60f);
            int quick = Mathf.RoundToInt(config.script.UsualDuration(Pace.Quick) / 60f);
            var cards = new[]
            {
                new DemoCardData("exp-free", "Free play", "Meet, talk, gesture, and try the link settings.") { icon = DemoIcon.Chat },
                new DemoCardData("exp-full", "Full session", config.script.Count + " steps with narration, about " + minutes + " minutes.") { icon = DemoIcon.Clock }
            };
            b.Title("Choose the experience", "You can change it later in the room.")
                .CardRow(experiences, 0f, cards)
                .Box(x => x
                    .Options(directors, "Steps", LabelWidth, ("director-auto", "Automatic"), ("director-operator", "Operator"))
                    .Options(paces, "Pace", LabelWidth, ("pace-study", "Study timing"), ("pace-quick", "Quick preview, about " + quick + " min")), 24f, 14f)
                .Row(r => r
                    .Column(c => c.Caption("Operator: the steps wait for an experimenter who joins as operator. Answers to the questions of a full session stay on this device, and you can delete them."), 1f)
                    .Button("delete-answers", "Delete my answers", DeleteAnswers).Out(out deleteAnswers));
        }

        private void DeleteAnswers()
        {
            app.DeleteAnswers();
            ShowSessionOptions();
        }

        private void JoinPage(DemoBuilder b)
        {
            var numbers = new (string, string)[config.roomCount];
            for (int i = 0; i < numbers.Length; i++) numbers[i] = ("room-" + (i + 1), (i + 1).ToString());
            b.Title("Join", "People who choose the same room number meet in the same room.")
                .Box(x => x.Options(rooms, "Room", 110f, numbers), 24f)
                .Pages("join", p => p
                    .Page("participant", ParticipantButtons)
                    .Page("operator", OperatorButtons)).Out(out joinPages)
                .Label("status", "", DemoTextStyle.Strong).Out(out status);
        }

        private void ParticipantButtons(DemoBuilder b)
        {
            b.Primary("join-online", "Join online", () => app.Join(false, ParticipantKind.Participant)).Out(out online)
                .Row(r => r
                    .Button("join-solo", "Solo with a simulated partner", () => app.Join(true, ParticipantKind.Participant), DemoStyle.Secondary, DemoIcon.Person)
                    .Flex()
                    .Button("join-operator", "Join as operator", () => joinPages.Go(1), DemoStyle.Secondary, DemoIcon.People))
                .Caption(connector.HasKey
                    ? "Joining online turns on your microphone. Solo needs no network and no microphone."
                    : config.keyHint + " Solo needs no key, no network, and no microphone.");
        }

        private void OperatorButtons(DemoBuilder b)
        {
            b.Text("An operator has no body and advances the steps of the full session.")
                .Row(r => r
                    .Button("operator-online", "Operate this room", () => app.Join(false, ParticipantKind.Operator), DemoStyle.Primary).Out(out operatorOnline)
                    .Flex()
                    .Button("operator-solo", "Rehearse alone", () => app.Join(true, ParticipantKind.Operator)))
                .Button("operator-back", "Join as participant instead", () => joinPages.Go(0), DemoStyle.Quiet, DemoIcon.Left);
        }

        // State -----------------------------------------------------------------------------------------------

        private void OnPage(int index)
        {
            steps.Index = index;
            back.Interactable = index > 0;
            next.gameObject.SetActive(!pages.IsLast);
        }

        private void ShowChoices()
        {
            joinPages.gameObject.SetActive(true);
            avatars.Index = Choices.avatar;
            roles.Index = Choices.role == LocalChoices.RandomRole ? config.roles.Count : Choices.role;
            experiences.Index = Choices.experience == Experience.FullSession ? 1 : 0;
            directors.Index = (int)Choices.director;
            paces.Index = (int)Choices.pace;
            rooms.Index = Choices.room - 1;
            ShowSessionOptions();
            ShowOnline();
            OnPage(pages.Index);
        }

        /// <summary>Director and pace only matter for a full session.</summary>
        private void ShowSessionOptions()
        {
            bool full = Choices.experience == Experience.FullSession;
            directors.SetEnabled(full);
            paces.SetEnabled(full);
            deleteAnswers.gameObject.SetActive(AnswerStore.HasAnswers);
        }

        private void ShowOnline()
        {
            online.Interactable = connector.HasKey;
            operatorOnline.Interactable = connector.HasKey;
            online.Label = "Join room " + Choices.room + " online";
            operatorOnline.Label = "Operate room " + Choices.room;
        }

        private void SetStatus(string message)
        {
            if (status != null) status.Text = message;
        }
    }
}
