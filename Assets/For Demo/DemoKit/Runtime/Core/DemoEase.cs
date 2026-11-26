using UnityEngine;

namespace DemoKit
{
    /// <summary>Small easing helpers. All of them are frame-rate independent.</summary>
    public static class DemoEase
    {
        public static float OutCubic(float t) { t = 1f - Mathf.Clamp01(t); return 1f - t * t * t; }

        public static float InOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        /// <summary>Ends with a slight overshoot.</summary>
        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            const float c = 1.4f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }

        /// <summary>Moves toward the target so that half of the distance is covered every halfLife seconds.</summary>
        public static float Damp(float value, float target, float halfLife, float dt) => Mathf.Lerp(value, target, Share(halfLife, dt));

        public static Vector3 Damp(Vector3 value, Vector3 target, float halfLife, float dt) => Vector3.Lerp(value, target, Share(halfLife, dt));

        public static Quaternion Damp(Quaternion value, Quaternion target, float halfLife, float dt) => Quaternion.Slerp(value, target, Share(halfLife, dt));

        private static float Share(float halfLife, float dt) => halfLife <= 0f ? 1f : 1f - Mathf.Exp(-0.6931472f * dt / halfLife);
    }
}
