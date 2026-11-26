using System;
using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>When a step of an automatic session ends.</summary>
    public enum AdvanceRule
    {
        /// <summary>When its time is over.</summary>
        Time,
        /// <summary>When its time is over, or earlier when everybody is done.</summary>
        TimeOrAllDone,
        /// <summary>When everybody is done. The time is only a limit.</summary>
        AllDone
    }

    /// <summary>What a simulated partner does in a part of a step.</summary>
    [Serializable]
    public sealed class BotCue
    {
        public enum Clock
        {
            /// <summary>Seconds since the start of the step.</summary>
            Step,
            /// <summary>Seconds since the end of the clip.</summary>
            Response
        }

        public enum Place { Stay, Seat, NearUser, MirrorSide, OwnBoard }

        public Clock clock;
        [Min(0f)] public float at;
        public Place place = Place.Stay;
        [Tooltip("Distance to the user for Near User (metres).")]
        public float distance = 0.85f;
        public SimulatedRig.MotionKind motion = SimulatedRig.MotionKind.Idle;
        [Tooltip("Second motion that alternates with the first. Off when the interval is 0.")]
        public SimulatedRig.MotionKind alternate = SimulatedRig.MotionKind.Idle;
        [Min(0f)] public float interval;
    }

    /// <summary>One step of a session: narration, what the user does, and when it ends.</summary>
    [Serializable]
    public sealed class StudyStep
    {
        public string id;
        public string title;
        public AudioClip clip;
        [Tooltip("What the voice says, with the second at which each piece is spoken.")]
        public DemoCaption[] captions = Array.Empty<DemoCaption>();
        [Tooltip("One short line while the users act.")]
        public string hint;
        [Tooltip("Seconds after the clip in study timing.")]
        public float responseSeconds = 10f;
        [Tooltip("Seconds after the clip in the quick preview. -1 = a fifth of the study time, 3 to 10 seconds.")]
        public float quickSeconds = -1f;
        public float minResponseSeconds = 2f;
        public AdvanceRule rule = AdvanceRule.TimeOrAllDone;
        [Tooltip("Share of the response time that people usually need. Only for the estimate on the start panel.")]
        [Range(0f, 1f)] public float usualShare = 1f;
        [Tooltip("Shows a countdown, because the narration names the time.")]
        public bool showTimer;
        [Tooltip("What the stage of the project shows in this step.")]
        public string[] cues = Array.Empty<string>();
        public BotCue[] bots = Array.Empty<BotCue>();
        [Tooltip("Simulated partners finish a task this many seconds after it opened (study timing).")]
        public float botDoneSeconds = 8f;

        public bool HasCue(string cue) => Array.IndexOf(cues, cue) >= 0;
    }

    /// <summary>All steps of a session in order, with the timing rules.</summary>
    [CreateAssetMenu(menuName = "Social Leading/All-in-One/Study Script", fileName = "Study Script")]
    public sealed class StudyScript : ScriptableObject
    {
        public StudyStep[] steps = Array.Empty<StudyStep>();
        public float countdownStudy = 5f;
        public float countdownQuick = 3f;
        public float gapStudy = 1f;
        public float gapQuick = 0.5f;

        public int Count => steps.Length;

        /// <summary>The step, or null for an index outside the session.</summary>
        public StudyStep Get(int index) => index >= 0 && index < steps.Length ? steps[index] : null;

        /// <summary>Length of the narration (seconds).</summary>
        public float Length(int index)
        {
            AudioClip clip = Get(index)?.clip;
            return clip != null ? clip.length : 0f;
        }

        /// <summary>Time for the users after the narration (seconds).</summary>
        public float Window(int index, Pace pace)
        {
            StudyStep step = Get(index);
            if (step == null) return 0f;
            if (pace == Pace.Study) return step.responseSeconds;
            return step.quickSeconds >= 0f ? step.quickSeconds : Mathf.Clamp(0.2f * step.responseSeconds, 3f, 10f);
        }

        public float Countdown(Pace pace) => pace == Pace.Study ? countdownStudy : countdownQuick;

        /// <summary>Pause after a step. There is none after the last one.</summary>
        public float Gap(int index, Pace pace) => index >= steps.Length - 1 ? 0f : pace == Pace.Study ? gapStudy : gapQuick;

        /// <summary>Length of the whole session when every step takes its full time (seconds).</summary>
        public float LongestDuration(Pace pace) => Duration(pace, false);

        /// <summary>Length of the whole session when people finish the tasks at their usual pace (seconds).</summary>
        public float UsualDuration(Pace pace) => Duration(pace, true);

        private float Duration(Pace pace, bool usual)
        {
            float sum = Countdown(pace);
            for (int i = 0; i < steps.Length; i++)
                sum += Length(i) + Window(i, pace) * (usual ? steps[i].usualShare : 1f) + Gap(i, pace);
            return sum;
        }
    }
}
