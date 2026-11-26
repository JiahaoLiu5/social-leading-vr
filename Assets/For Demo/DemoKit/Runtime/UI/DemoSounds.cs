using UnityEngine;

namespace DemoKit
{
    /// <summary>A soft tick for clicks, made at run time. Assign ClickClip to use another sound, or set Volume to 0.</summary>
    public static class DemoSounds
    {
        public static float Volume = 0.35f;
        public static AudioClip ClickClip;

        private static AudioSource source;

        internal static void Click(DemoElement element)
        {
            if (Volume <= 0f || !Application.isPlaying || element == null) return;
            if (ClickClip == null) ClickClip = Tick();
            if (source == null)
            {
                var go = new GameObject("DemoKit Sounds") { hideFlags = HideFlags.HideInHierarchy };
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.minDistance = 0.5f;
                source.dopplerLevel = 0f;
            }
            source.transform.position = element.transform.position;
            source.PlayOneShot(ClickClip, Volume);
        }

        private static AudioClip Tick()
        {
            const int rate = 44100;
            var data = new float[rate / 18];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t * 95f);
                data[i] = envelope * (0.7f * Mathf.Sin(2f * Mathf.PI * 880f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 1760f * t));
            }
            AudioClip clip = AudioClip.Create("DemoKit Tick", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            source = null;
            ClickClip = null;
            Volume = 0.35f;
        }
    }
}
