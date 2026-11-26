using System;
using DemoKit;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Management;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Decides once whether a headset or the desktop drives the rig. On a desktop all tracking components are switched off,
    /// because they would overwrite the simulated poses.
    /// </summary>
    [DefaultExecutionOrder(-250)]
    public sealed class RigMode : MonoBehaviour
    {
        private const float HeadsetWaitSeconds = 2f;

        [SerializeField] private XROrigin origin;
        [SerializeField] private TrackedPoseDriver trackingDriver;
        [SerializeField] private ActionBasedController leftController;
        [SerializeField] private ActionBasedController rightController;

        private float started;

        /// <summary>Raised once, when the decision is made.</summary>
        public event Action Decided;

        public bool IsDecided { get; private set; }
        public bool Desktop { get; private set; }

        /// <summary>Set before the first frame to ignore a headset.</summary>
        public bool ForceDesktop { get; set; }

        /// <summary>For tests: behaves as with a headset, whose devices are simulated.</summary>
        public static bool SimulateHeadset { get; set; }

        /// <summary>True while a headset delivers poses.</summary>
        public static bool HeadsetRuns => SimulateHeadset || DemoXr.IsHeadsetActive();

        private void Awake() => started = Time.realtimeSinceStartup;

        private void Update()
        {
            if (IsDecided) return;
            if (!ForceDesktop && HeadsetRuns) Decide(false);
            else if (ForceDesktop || Application.isBatchMode || !HeadsetMayCome()) Decide(true);
        }

        private bool HeadsetMayCome()
        {
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            bool loaded = settings != null && settings.Manager != null && settings.Manager.activeLoader != null;
            // A standalone headset has no desktop to fall back to. It may sleep, or show a system dialog, for much longer.
            return loaded && (Application.isMobilePlatform || Time.realtimeSinceStartup - started < HeadsetWaitSeconds);
        }

        private void Decide(bool desktop)
        {
            IsDecided = true;
            Desktop = desktop;
            if (desktop) SwitchTrackingOff();
            Decided?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => SimulateHeadset = false;

        private void SwitchTrackingOff()
        {
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            if (settings != null && settings.Manager != null && settings.Manager.activeLoader != null)
            {
                settings.Manager.StopSubsystems();
                settings.Manager.DeinitializeLoader();
            }
            origin.enabled = false;
            trackingDriver.enabled = false;
            leftController.enableInputTracking = false;
            rightController.enableInputTracking = false;
        }
    }
}
