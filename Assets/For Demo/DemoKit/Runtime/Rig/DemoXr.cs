using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace DemoKit
{
    /// <summary>Small XR helpers that need no XR package.</summary>
    public static class DemoXr
    {
        private static readonly List<XRDisplaySubsystem> Displays = new List<XRDisplaySubsystem>();

        /// <summary>
        /// True when a headset is rendering (a build on a headset, or the Editor with a linked headset). False on a desktop
        /// without one. OpenXR starts to render some frames after the first scene was loaded, so wait a few seconds for it
        /// when an XRDisplaySubsystem exists, before choosing the desktop.
        /// </summary>
        public static bool IsHeadsetActive()
        {
            SubsystemManager.GetSubsystems(Displays);
            for (int i = 0; i < Displays.Count; i++)
                if (Displays[i].running) return true;
            return false;
        }

        /// <summary>Vibrates a controller. Does nothing when there is none.</summary>
        public static void Vibrate(DemoHand hand, float amplitude, float seconds)
        {
            if (hand == DemoHand.None || amplitude <= 0f) return;
            InputDevice device = InputDevices.GetDeviceAtXRNode(hand == DemoHand.Left ? XRNode.LeftHand : XRNode.RightHand);
            if (device.isValid) device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), seconds);
        }
    }
}
