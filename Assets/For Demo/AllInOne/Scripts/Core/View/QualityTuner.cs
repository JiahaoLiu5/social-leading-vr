using UnityEngine;
using UnityEngine.XR;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Sets the quality of the integrated demo at run time, because the project's default level is made for the study
    /// builds. The old values come back when the scene ends.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class QualityTuner : MonoBehaviour
    {
        [SerializeField] private Light[] roomLights = System.Array.Empty<Light>();

        /// <summary>True on a standalone headset. Tests can set it on a PC, to see and measure the same there.</summary>
        public static bool Standalone { get; set; }

        private int pixelLights, mipmapLimit, antiAliasing, frameRate;
        private ShadowQuality shadows;
        private SkinWeights skinWeights;

        private void Awake()
        {
            pixelLights = QualitySettings.pixelLightCount;
            mipmapLimit = QualitySettings.globalTextureMipmapLimit;
            antiAliasing = QualitySettings.antiAliasing;
            shadows = QualitySettings.shadows;
            skinWeights = QualitySettings.skinWeights;
            frameRate = Application.targetFrameRate;

            QualitySettings.globalTextureMipmapLimit = 0;
            if (Standalone) UseHeadsetValues();
            else UseDesktopValues();
        }

        private void OnDestroy()
        {
            QualitySettings.pixelLightCount = pixelLights;
            QualitySettings.globalTextureMipmapLimit = mipmapLimit;
            QualitySettings.antiAliasing = antiAliasing;
            QualitySettings.shadows = shadows;
            QualitySettings.skinWeights = skinWeights;
            Application.targetFrameRate = frameRate;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Standalone = Application.isMobilePlatform;

        private void UseHeadsetValues()
        {
            QualitySettings.pixelLightCount = 1;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.skinWeights = SkinWeights.TwoBones;
            QualitySettings.antiAliasing = 4;
            XRSettings.eyeTextureResolutionScale = 1f;
            foreach (Light l in roomLights)
                if (l != null) l.shadows = LightShadows.None;
        }

        private static void UseDesktopValues()
        {
            QualitySettings.pixelLightCount = Mathf.Max(2, QualitySettings.pixelLightCount);
            QualitySettings.skinWeights = SkinWeights.FourBones;
            QualitySettings.antiAliasing = Mathf.Max(4, QualitySettings.antiAliasing);
            Application.targetFrameRate = 90;
        }
    }
}
