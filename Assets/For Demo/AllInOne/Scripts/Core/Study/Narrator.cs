using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Plays the narration of the running step at the right position, with captions. Follows the session clock, so that a
    /// late joiner hears the clip from where it is.
    /// </summary>
    [DefaultExecutionOrder(-140)]
    [RequireComponent(typeof(AudioSource))]
    public sealed class Narrator : MonoBehaviour
    {
        private const double ShortestRest = 0.4;

        // Close to the user and small, so that a partner at arm's length does not stand in front of it. Heights are metres
        // from the eyes: above the session bar, or at the upper edge of the view while a panel with buttons is open.
        private const float CaptionWidth = 1240f;
        private const float CaptionDistance = 0.75f;
        private const float CaptionScale = 0.5f;
        private const float CaptionHeight = -0.30f;
        private const float CaptionHeightOverPanel = 0.35f;

        [SerializeField] private AppConfig config;
        [SerializeField] private SessionState session;
        [SerializeField] private SessionDirector director;
        [SerializeField] private SessionClock clock;
        [SerializeField] private RoomConnector connector;
        [SerializeField] private StudyStage stage;
        [SerializeField] private RoomMenu menu;
        [SerializeField] private OperatorPanel operatorPanel;

        private AudioSource source;
        private DemoCaptionBar captions;
        private StepPhase lastPhase;
        private int playingMark = -1;
        private bool loaded, captionsShown, overPanel;

        /// <summary>Switches the voice off, not the captions.</summary>
        public bool Muted { get; set; }

        public DemoCaptionBar Captions => captions;

        public bool IsPlaying => source.isPlaying;

        private void Awake() => source = GetComponent<AudioSource>();

        private void Start()
        {
            captions = DemoCaptionBar.Create(null, "captions", CaptionWidth, CaptionDistance, CaptionHeight);
            captions.Bar.Scale(CaptionScale);
            captions.Bar.FollowView();
            captions.Clock = () => (float)director.View.time;
        }

        private void Update()
        {
            LoadClips();
            StepView view = director.View;
            bool speaking = view.phase == StepPhase.Narration && clock.Now >= session.StepStartTime;
            int mark = speaking ? session.Mark(view.step) : -1;
            if (mark != playingMark)
            {
                playingMark = mark;
                if (speaking) Play(view);
                else if (view.phase != StepPhase.Response && view.phase != StepPhase.Gap) Stop();
            }
            // The session bar comes back in front of the user with every phase; the captions keep their place above it.
            if (view.phase != lastPhase) captions.Bar.Placement.Recentre();
            lastPhase = view.phase;
            source.mute = Muted || clock.TestSpeed > 1.01f;
            if (!speaking) return;
            Follow(view);
            ShowCaptions(view);
        }

        /// <summary>Clips are loaded together when a session starts, so that no step begins late.</summary>
        private void LoadClips()
        {
            bool wanted = session.Experience == Experience.FullSession;
            if (loaded == wanted) return;
            loaded = wanted;
            foreach (StudyStep step in config.script.steps)
            {
                if (step.clip == null) continue;
                if (wanted) step.clip.LoadAudioData();
                else step.clip.UnloadAudioData();
            }
        }

        private void Play(StepView view)
        {
            source.clip = director.Step.clip;
            captionsShown = false;
            if (source.clip == null || source.clip.length - view.time < ShortestRest) return;
            source.time = Mathf.Clamp((float)view.time, 0f, source.clip.length - 0.05f);
            source.Play();
        }

        private void Follow(StepView view)
        {
            if (source.clip == null) return;
            if (session.Paused)
            {
                if (source.isPlaying) source.Pause();
                return;
            }
            if (!source.isPlaying && source.clip.length - view.time > ShortestRest)
            {
                source.time = (float)view.time;
                source.Play();
            }
            // Offline the clock can run faster or be paused, so the position is corrected when it drifts.
            else if (source.isPlaying && Mathf.Abs(source.time - (float)view.time) > 0.35f && view.time < source.clip.length)
                source.time = (float)view.time;
        }

        private void ShowCaptions(StepView view)
        {
            bool panelOpen = stage.TaskOpen || menu.IsOpen || operatorPanel.OnLeftHand;
            if (overPanel != panelOpen)
            {
                overPanel = panelOpen;
                captions.Place(CaptionDistance, overPanel ? CaptionHeightOverPanel : CaptionHeight);
            }
            bool wanted = connector.Choices.captions;
            if (wanted && !captionsShown && director.Step.captions.Length > 0)
            {
                captions.Play(director.Step.captions, config.script.Length(view.step), "Narrator", (float)view.time);
                captionsShown = true;
            }
            else if (!wanted && captionsShown)
            {
                captions.Stop();
                captionsShown = false;
            }
        }

        private void Stop()
        {
            source.Stop();
            source.clip = null;
            captions.Stop();
            captionsShown = false;
        }
    }
}
