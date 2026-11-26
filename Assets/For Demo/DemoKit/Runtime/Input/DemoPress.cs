using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemoKit
{
    public enum DemoHand { None, Left, Right }

    /// <summary>Answers whether a pointer is pressed in this frame.</summary>
    public interface IDemoPress
    {
        bool IsPressed { get; }
    }

    /// <summary>A press that is set from code, for tests and tutorials.</summary>
    public sealed class DemoManualPress : IDemoPress
    {
        public bool IsPressed { get; set; }
    }

    /// <summary>Creates press providers.</summary>
    public static class DemoPress
    {
        /// <summary>Trigger of an XR controller.</summary>
        public static IDemoPress Controller(DemoHand hand) =>
            new ActionPress("<XRController>{" + Usage(hand) + "}/triggerPressed", "<XRController>{" + Usage(hand) + "}/{TriggerButton}");

        /// <summary>
        /// Index pinch of a tracked hand (Meta hand tracking aim). Bound by name only, so XR Hands need not be installed.
        /// Presses during a system gesture are ignored.
        /// </summary>
        public static IDemoPress Pinch(DemoHand hand) => new PinchPress(hand);

        public static IDemoPress ControllerOrPinch(DemoHand hand) => Any(Controller(hand), Pinch(hand));

        /// <summary>
        /// Opens a menu: the menu or Y button of the left controller, the menu gesture of a tracked left hand (palm toward
        /// the face and pinch, Meta hand tracking aim), and Tab on a keyboard. Compare with the last frame to toggle.
        /// </summary>
        public static IDemoPress Menu() => Any(
            new ActionPress("<XRController>{LeftHand}/{MenuButton}", "<XRController>{LeftHand}/{SecondaryButton}", "<Keyboard>/tab"),
            new FlagPress(DemoHand.Left, MenuGesture));

        /// <summary>Left mouse button, but not while the right button turns the view.</summary>
        public static IDemoPress Mouse() => new FuncPress(() =>
        {
            UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
            return m != null && m.leftButton.isPressed && !m.rightButton.isPressed;
        });

        /// <summary>An existing input action. It is not enabled or disabled here.</summary>
        public static IDemoPress Action(InputAction action) => new FuncPress(() => action != null && action.IsPressed());

        /// <summary>Any Input System binding paths.</summary>
        public static IDemoPress Bindings(params string[] paths) => new ActionPress(paths);

        public static IDemoPress Func(Func<bool> pressed) => new FuncPress(pressed);

        public static DemoManualPress Manual() => new DemoManualPress();

        public static IDemoPress Any(params IDemoPress[] providers) => new AnyPress(providers);

        internal static string Usage(DemoHand hand) => hand == DemoHand.Left ? "LeftHand" : "RightHand";

        private sealed class FuncPress : IDemoPress
        {
            private readonly Func<bool> pressed;
            public FuncPress(Func<bool> pressed) { this.pressed = pressed; }
            public bool IsPressed { get { return pressed != null && pressed(); } }
        }

        private sealed class AnyPress : IDemoPress, IDisposable
        {
            private readonly IDemoPress[] providers;
            public AnyPress(IDemoPress[] providers) { this.providers = providers; }

            public bool IsPressed
            {
                get
                {
                    foreach (IDemoPress p in providers) if (p != null && p.IsPressed) return true;
                    return false;
                }
            }

            public void Dispose() { foreach (IDemoPress p in providers) (p as IDisposable)?.Dispose(); }
        }

        private sealed class ActionPress : IDemoPress, IDisposable
        {
            private readonly InputAction action;

            public ActionPress(params string[] paths)
            {
                action = new InputAction("DemoKit press", InputActionType.Button);
                foreach (string path in paths) action.AddBinding(path);
                action.Enable();
            }

            public bool IsPressed { get { return action.IsPressed(); } }

            public void Dispose() { action.Disable(); action.Dispose(); }
        }

        // Bits of MetaAimHand.aimFlags.
        private const int SystemGesture = 1 << 6;
        private const int MenuGesture = 1 << 8;

        private sealed class FlagPress : IDemoPress, IDisposable
        {
            private readonly InputAction flags;
            private readonly int mask;

            public FlagPress(DemoHand hand, int mask)
            {
                this.mask = mask;
                flags = new InputAction("DemoKit aim flags", InputActionType.Value, "<MetaAimHand>{" + Usage(hand) + "}/aimFlags");
                flags.Enable();
            }

            public bool IsPressed { get { return flags.activeControl != null && (flags.ReadValue<int>() & mask) != 0; } }

            public void Dispose() { flags.Dispose(); }
        }

        private sealed class PinchPress : IDemoPress, IDisposable
        {
            private readonly InputAction pinch, flags;

            public PinchPress(DemoHand hand)
            {
                string device = "<MetaAimHand>{" + Usage(hand) + "}";
                pinch = new InputAction("DemoKit pinch", InputActionType.Button, device + "/indexPressed");
                flags = new InputAction("DemoKit aim flags", InputActionType.Value, device + "/aimFlags");
                pinch.Enable();
                flags.Enable();
            }

            public bool IsPressed
            {
                get
                {
                    if (!pinch.IsPressed()) return false;
                    return flags.activeControl == null || (flags.ReadValue<int>() & SystemGesture) == 0;
                }
            }

            public void Dispose() { pinch.Dispose(); flags.Dispose(); }
        }
    }
}
