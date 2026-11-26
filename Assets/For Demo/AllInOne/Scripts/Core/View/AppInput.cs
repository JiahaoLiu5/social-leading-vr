using UnityEngine;
using UnityEngine.InputSystem;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// The buttons and keys of the app, whatever the device. Controllers as in the study: A confirms, X rejects, the right
    /// thumbstick moves sliders. Added: B for "I am done", Y or the menu button for the menu.
    /// </summary>
    [DefaultExecutionOrder(-260)]
    public sealed class AppInput : MonoBehaviour
    {
        private InputAction confirm, reject, done, menu, link, leave, next, repeat, pause;
        private InputAction rightStick, leftStick, sliderKeys;
        private InputAction leftTrigger, leftGrip, rightTrigger, rightGrip;
        private InputAction[] all;

        public bool ConfirmPressed => confirm.WasPressedThisFrame();
        public bool RejectPressed => reject.WasPressedThisFrame();
        public bool DonePressed => done.WasPressedThisFrame();
        public bool MenuPressed => menu.WasPressedThisFrame();
        public bool LinkPressed => link.WasPressedThisFrame();
        public bool LeavePressed => leave.WasPressedThisFrame();
        public bool OperatorNextPressed => next.WasPressedThisFrame();
        public bool OperatorRepeatPressed => repeat.WasPressedThisFrame();
        public bool OperatorPausePressed => pause.WasPressedThisFrame();

        public Vector2 LeftStick => leftStick.ReadValue<Vector2>();
        public Vector2 RightStick => rightStick.ReadValue<Vector2>();

        /// <summary>Slider direction from the right thumbstick or the minus and equals keys.</summary>
        public float SliderAxis
        {
            get
            {
                float keys = sliderKeys.ReadValue<float>();
                return Mathf.Abs(keys) > 0.1f ? keys : RightStick.x;
            }
        }

        /// <summary>Left pinch, left grab, right pinch, right grab.</summary>
        public Vector4 HandPose => new Vector4(leftTrigger.ReadValue<float>(), leftGrip.ReadValue<float>(),
                                               rightTrigger.ReadValue<float>(), rightGrip.ReadValue<float>());

        private void OnEnable()
        {
            const string left = "<XRController>{LeftHand}/", right = "<XRController>{RightHand}/";
            confirm = Button(right + "{PrimaryButton}", "<Keyboard>/enter", "<Keyboard>/numpadEnter");
            reject = Button(left + "{PrimaryButton}", "<Keyboard>/backspace");
            done = Button(right + "{SecondaryButton}", "<Keyboard>/enter", "<Keyboard>/numpadEnter");
            menu = Button(left + "{MenuButton}", left + "{SecondaryButton}", "<Keyboard>/tab");
            link = Button("<Keyboard>/l");
            leave = Button("<Keyboard>/escape");
            next = Button("<Keyboard>/space");
            repeat = Button("<Keyboard>/r");
            pause = Button("<Keyboard>/p");
            rightStick = Value(right + "{Primary2DAxis}", "Vector2");
            leftStick = Value(left + "{Primary2DAxis}", "Vector2");
            leftTrigger = Value(left + "{Trigger}", "Axis");
            leftGrip = Value(left + "{Grip}", "Axis");
            rightTrigger = Value(right + "{Trigger}", "Axis");
            rightGrip = Value(right + "{Grip}", "Axis");
            sliderKeys = new InputAction("Slider", InputActionType.Value, null, null, null, "Axis");
            sliderKeys.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/minus").With("Positive", "<Keyboard>/equals");
            all = new[]
            {
                confirm, reject, done, menu, link, leave, next, repeat, pause, rightStick, leftStick,
                leftTrigger, leftGrip, rightTrigger, rightGrip, sliderKeys
            };
            foreach (InputAction action in all) action.Enable();
        }

        private void OnDisable()
        {
            foreach (InputAction action in all) action.Dispose();
        }

        private static InputAction Button(params string[] paths)
        {
            var action = new InputAction(paths[0], InputActionType.Button);
            foreach (string path in paths) action.AddBinding(path);
            return action;
        }

        private static InputAction Value(string path, string type) =>
            new InputAction(path, InputActionType.Value, path, null, null, type);
    }
}
