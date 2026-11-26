using System;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    public enum StepPhase { Waiting, Countdown, Narration, Response, Gap, Finished }

    /// <summary>Where the session is right now.</summary>
    public struct StepView
    {
        public int step;
        public StepPhase phase;

        /// <summary>Seconds since the step started.</summary>
        public double time;

        /// <summary>Seconds until this phase ends by itself.</summary>
        public float remaining;

        /// <summary>Length of the response time.</summary>
        public float window;
    }

    /// <summary>
    /// Derives everything about the running step from the step number and its start time, so that clients that join late
    /// are in the right place at once.
    /// </summary>
    public static class StepTimeline
    {
        public static StepView Evaluate(StudyScript script, Pace pace, int step, double start, double now, bool paused, double pauseTime)
        {
            var view = new StepView { step = step };
            if (step < 0)
            {
                view.phase = start > 0.0 ? StepPhase.Countdown : StepPhase.Waiting;
                view.remaining = start > 0.0 ? Mathf.Max(0f, (float)(start - now)) : 0f;
                return view;
            }
            if (step >= script.Count)
            {
                view.phase = StepPhase.Finished;
                return view;
            }

            float length = script.Length(step);
            view.window = script.Window(step, pace);
            view.time = Math.Max(0.0, (paused ? pauseTime : now) - start);
            if (view.time < length)
            {
                view.phase = StepPhase.Narration;
                view.remaining = (float)(length - view.time);
            }
            else if (view.time < length + view.window)
            {
                view.phase = StepPhase.Response;
                view.remaining = (float)(length + view.window - view.time);
            }
            else view.phase = StepPhase.Gap;
            return view;
        }

        /// <summary>True when an automatic session goes on to the next step now.</summary>
        public static bool ShouldAdvance(StepView view, StudyScript script, Pace pace, bool allDone)
        {
            switch (view.phase)
            {
                case StepPhase.Countdown:
                    return view.remaining <= 0f;
                case StepPhase.Response:
                    return EndsEarly(view, script, allDone);
                case StepPhase.Gap:
                    double since = view.time - script.Length(view.step) - view.window;
                    return since >= script.Gap(view.step, pace) || EndsEarly(view, script, allDone);
                default:
                    return false;
            }
        }

        private static bool EndsEarly(StepView view, StudyScript script, bool allDone)
        {
            StudyStep step = script.Get(view.step);
            return allDone && step.rule != AdvanceRule.Time && view.time - script.Length(view.step) >= step.minResponseSeconds;
        }
    }
}
