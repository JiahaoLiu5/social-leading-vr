using System;
using DemoKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// The two private tasks of the dyad session, page by page: the two ratings, and the two rounds of splitting with a
    /// scripted partner. Runs on the session clock. Controller buttons, rays, and keys work at the same time.
    /// </summary>
    public sealed class DyadTaskFlow : MonoBehaviour
    {
        public enum Task { None, Ratings, Split }

        public enum Page
        {
            Hidden, Question1, Rating1, Question2, Rating2, Split1, WaitOffer1, Offer1, WaitReply, Reply, Split2, WaitOffer2, Offer2, Final
        }

        public const string ConfirmId = "task-confirm", AcceptId = "task-accept", RejectId = "task-reject", SliderId = "task-slider";

        [Serializable]
        public sealed class Panel
        {
            public GameObject root;
            public Slider slider;
            public RatingUI rating;
            public SplitUI split;
            [NonSerialized] public float startValue;

            public int Value => rating != null ? rating.playerValue : split != null ? split.playerValue : 0;

            public void Change(int amount)
            {
                if (rating != null) rating.ChangeSliderValue(amount);
                else if (split != null) split.ChangeSliderValue(amount);
            }
        }

        [SerializeField] private DyadTaskConfig config;
        [SerializeField] private SessionClock clock;
        [SerializeField] private AppInput input;
        [SerializeField] private Transform root;

        [Header("Pages")]
        [SerializeField] private GameObject question1;
        [SerializeField] private Panel rating1;
        [SerializeField] private GameObject question2;
        [SerializeField] private Panel rating2;
        [SerializeField] private Panel split1;
        [SerializeField] private GameObject offer1;
        [SerializeField] private GameObject waiting;
        [SerializeField] private GameObject reply;
        [SerializeField] private TMP_Text replyText;
        [SerializeField] private Panel split2;
        [SerializeField] private GameObject offer2;
        [SerializeField] private GameObject final;

        private GameObject[] pages;
        private Panel[] sliders;
        private float pageSeconds, stickHeld, stickWait;

        /// <summary>Raised when the last answer of a task was given.</summary>
        public event Action<Task> Completed;

        /// <summary>Raised with a name and a value for every answer.</summary>
        public event Action<string, string> Answered;

        public Task Current { get; private set; }

        public Page Shown { get; private set; }

        /// <summary>True when the current task was finished.</summary>
        public bool Done { get; private set; }

        /// <summary>The own first split, which decides the scripted reply.</summary>
        public int FirstSplit { get; private set; }

        /// <summary>The slider of the shown page, or null.</summary>
        public Panel ActivePanel
        {
            get
            {
                switch (Shown)
                {
                    case Page.Rating1: return rating1;
                    case Page.Rating2: return rating2;
                    case Page.Split1: return split1;
                    case Page.Split2: return split2;
                    default: return null;
                }
            }
        }

        public bool IsOpen => Shown != Page.Hidden;

        /// <summary>True while the session is paused: no page turns and no answer counts.</summary>
        public bool Halted { get; set; }

        /// <summary>Opens a task from its first page.</summary>
        public void Open(Task task, float yaw)
        {
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            ResetFlow();
            Current = task;
            Go(task == Task.Ratings ? Page.Question1 : Page.Split1);
        }

        /// <summary>Closes the panels. The answers given so far are kept.</summary>
        public void Close()
        {
            Current = Task.None;
            Go(Page.Hidden);
        }

        /// <summary>The last page of the session.</summary>
        public void ShowFinal(float yaw)
        {
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (Shown != Page.Final) Go(Page.Final);
        }

        /// <summary>Forgets everything and puts the sliders back.</summary>
        public void ResetFlow()
        {
            Done = false;
            FirstSplit = 0;
            foreach (Panel panel in sliders) panel.slider.value = panel.startValue;
        }

        public void Confirm()
        {
            switch (Shown)
            {
                case Page.Rating1:
                    Answer("Rating 1", rating1.Value);
                    Go(Page.Question2);
                    break;
                case Page.Rating2:
                    Answer("Rating 2", rating2.Value);
                    Finish(Page.Hidden);
                    break;
                case Page.Split1:
                    FirstSplit = split1.Value;
                    Answer("Split 1", FirstSplit);
                    Go(Page.WaitOffer1);
                    break;
                case Page.Split2:
                    Answer("Split 2", split2.Value);
                    Go(Page.WaitOffer2);
                    break;
                case Page.Offer1:
                case Page.Offer2:
                    Decide(true);
                    break;
            }
        }

        public void Reject()
        {
            if (Shown == Page.Offer1 || Shown == Page.Offer2) Decide(false);
        }

        private void Decide(bool accept)
        {
            bool first = Shown == Page.Offer1;
            Answered?.Invoke(first ? "Decision 1" : "Decision 2", accept ? "accept" : "reject");
            if (first) Go(Page.WaitReply);
            else Finish(Page.Final);
        }

        private void Answer(string answer, int value) =>
            Answered?.Invoke(answer, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        private void Finish(Page next)
        {
            Done = true;
            Go(next);
            Completed?.Invoke(Current);
        }

        private void Awake()
        {
            pages = new[]
            {
                null, question1, rating1.root, question2, rating2.root, split1.root, waiting, offer1, waiting, reply, split2.root, waiting,
                offer2, final
            };
            sliders = new[] { rating1, rating2, split1, split2 };
            foreach (Panel panel in sliders)
            {
                panel.startValue = panel.slider.value;
                MakeDraggable(panel.slider);
            }
            Show();
        }

        private void OnEnable() => DemoUi.AnyClicked += OnClicked;

        private void OnDisable() => DemoUi.AnyClicked -= OnClicked;

        private void OnClicked(DemoElement element, DemoPointer pointer)
        {
            if (!IsOpen || Halted || !(element is DemoHotspot)) return;
            switch (element.Id)
            {
                case ConfirmId:
                case AcceptId:
                    Confirm();
                    break;
                case RejectId:
                    Reject();
                    break;
            }
        }

        private void Update()
        {
            if (!IsOpen || Halted || Shown == Page.Final) return;
            pageSeconds += clock.DeltaTime;
            Turn();
            if (input.ConfirmPressed) Confirm();
            else if (input.RejectPressed) Reject();
            MoveSlider();
        }

        /// <summary>Pages that end by themselves.</summary>
        private void Turn()
        {
            switch (Shown)
            {
                case Page.Question1 when pageSeconds >= config.questionSeconds:
                    Go(Page.Rating1);
                    break;
                case Page.Question2 when pageSeconds >= config.questionSeconds:
                    Go(Page.Rating2);
                    break;
                case Page.WaitOffer1 when pageSeconds >= config.waitSeconds:
                    Go(Page.Offer1);
                    break;
                case Page.WaitReply when pageSeconds >= config.waitSeconds:
                    replyText.text = FirstSplit >= config.rejectThreshold ? config.rejectText : config.acceptText;
                    Go(Page.Reply);
                    break;
                case Page.Reply when pageSeconds >= config.replySeconds:
                    Go(Page.Split2);
                    break;
                case Page.WaitOffer2 when pageSeconds >= config.waitSeconds:
                    Go(Page.Offer2);
                    break;
            }
        }

        private void MoveSlider()
        {
            Panel panel = ActivePanel;
            if (panel == null) return;
            float axis = input.SliderAxis;
            if (Mathf.Abs(axis) <= config.stickDeadZone)
            {
                stickHeld = stickWait = 0f;
                return;
            }
            stickHeld += Time.deltaTime;
            stickWait -= Time.deltaTime;
            if (stickWait > 0f) return;
            bool rating = panel.rating != null;
            stickWait = rating ? config.ratingInterval : config.splitInterval;
            // The step grows the longer the stick is held, as in the study. The rating scale has only seven positions.
            int amount = rating ? 1 : Mathf.Min(Mathf.FloorToInt(1f + 2f * stickHeld), config.maxIncrement);
            panel.Change(axis > 0f ? amount : -amount);
        }

        /// <summary>A pointer that holds the slider moves it.</summary>
        private static void MakeDraggable(Slider slider)
        {
            if (slider.TryGetComponent(out DemoHotspot track))
                track.Dragged += (_, at) => slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, at.x);
        }

        private void Go(Page page)
        {
            Shown = page;
            pageSeconds = 0f;
            stickHeld = stickWait = 0f;
            Show();
        }

        private void Show()
        {
            GameObject wanted = pages[(int)Shown];
            foreach (GameObject page in pages)
                if (page != null && page != wanted) page.SetActive(false);
            if (wanted != null) wanted.SetActive(true);
        }
    }
}
