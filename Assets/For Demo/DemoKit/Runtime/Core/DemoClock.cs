using UnityEngine;

namespace DemoKit
{
    /// <summary>
    /// The clock of the interface. It ignores Time.timeScale, so panels work while the application is paused or
    /// accelerated. While frames are captured (Time.captureFramerate) it advances by the capture step, so that recordings
    /// and automated tests are exact.
    /// </summary>
    public static class DemoClock
    {
        private static float now, read;
        private static int frame = -1;

        /// <summary>Seconds since the last frame, at most 0.1.</summary>
        public static float DeltaTime
        {
            get { return Mathf.Min(FullDeltaTime, 0.1f); }
        }

        /// <summary>Seconds since the last frame, also when that was a long one.</summary>
        public static float FullDeltaTime
        {
            get { return Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime; }
        }

        /// <summary>Seconds on this clock. Only differences are meaningful. Frames in which nobody read it count too.</summary>
        public static float Now
        {
            get
            {
                int f = Time.frameCount;
                if (f == frame) return now;
                float t = Time.unscaledTime;
                if (frame >= 0 && f > frame) now += Time.captureDeltaTime > 0f ? Time.captureDeltaTime * (f - frame) : Mathf.Max(0f, t - read);
                frame = f;
                read = t;
                return now;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            now = read = 0f;
            frame = -1;
        }
    }
}
