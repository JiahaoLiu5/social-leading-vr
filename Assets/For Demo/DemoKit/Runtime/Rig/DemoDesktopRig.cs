using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DemoKit
{
    /// <summary>
    /// Stands in for a headset and two controllers on a desktop. Head, LeftHand, and RightHand move like the transforms
    /// of an XR rig: the head at eye height with the camera in it, the hands as controllers held in front of the body.
    /// Right mouse button turns the view, WASD walks, C crouches, number keys move the hands.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class DemoDesktopRig : MonoBehaviour
    {
        public enum Motion { Idle, Wave, RaiseLeft, RaiseRight, RaiseBoth, Point, Talk }

        [Header("Body")]
        [SerializeField] private float eyeHeight = 1.6f;
        [SerializeField] private float crouchHeight = 1.05f;
        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private float runSpeed = 3.2f;
        [SerializeField] private float lookSpeed = 0.13f;
        [SerializeField] private float turnSpeed = 90f;

        [Header("Parts (created when empty)")]
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Camera viewCamera;

        [Header("Hands")]
        [SerializeField] private Motion motion = Motion.Idle;
        [Tooltip("Rotation from a controller to the wrist the avatar expects.")]
        [SerializeField] private Quaternion leftWristOffset = Quaternion.identity;
        [SerializeField] private Quaternion rightWristOffset = Quaternion.identity;

        [Header("Control")]
        [SerializeField] private bool userControl = true;
        [SerializeField] private bool motionKeys = true;
        [Tooltip("Q and E turn the body. The arrow keys always do.")]
        [SerializeField] private bool turnKeys = true;
        [SerializeField] private bool showHelp = true;
        [Tooltip("Replaces the built-in help line.")]
        [SerializeField] private string helpText;
        [Tooltip("Keeps the rig inside this area around its start (metres). 0 = no limit.")]
        [SerializeField] private float walkRadius;

        private float yaw, pitch, crouch, clock, leftUp, rightUp;
        private Vector3 left, right, start;
        private bool built, looking, borrowed;
        private Transform cameraParent;
        private Vector3 cameraPosition;
        private Quaternion cameraRotation;
        private float helpAlpha = 1f;
        private GUIStyle helpStyle;

        /// <summary>The floor point between the feet. Corresponds to the origin of an XR rig.</summary>
        public Transform Origin { get { return transform; } }
        public Transform Head { get { Build(); return head; } }
        public Transform LeftHand { get { Build(); return leftHand; } }
        public Transform RightHand { get { Build(); return rightHand; } }
        public Camera Camera { get { Build(); return viewCamera; } }

        public Motion CurrentMotion { get { return motion; } set { motion = value; MotionChanged?.Invoke(value); } }

        public event Action<Motion> MotionChanged;

        /// <summary>False ignores keyboard and mouse, for scripted bodies.</summary>
        public bool UserControl { get { return userControl; } set { userControl = value; if (!value) FreeCursor(); } }

        /// <summary>False keeps the number keys free for the application.</summary>
        public bool MotionKeys { get { return motionKeys; } set { motionKeys = value; } }

        /// <summary>False keeps Q and E free for the application. The arrow keys still turn the body.</summary>
        public bool TurnKeys { get { return turnKeys; } set { turnKeys = value; } }

        public bool ShowHelp { get { return showHelp; } set { showHelp = value; } }

        /// <summary>Replaces the built-in help line. Empty = the built-in one.</summary>
        public string HelpText { get { return helpText; } set { helpText = value; } }

        public float EyeHeight { get { return eyeHeight; } set { eyeHeight = value; } }
        public bool Crouching { get; set; }
        public float WalkRadius { get { return walkRadius; } set { walkRadius = value; } }

        /// <summary>Optional limit of the walking area: takes the next floor position and returns an allowed one.</summary>
        public Func<Vector3, Vector3> Limit { get; set; }

        public float Yaw { get { return yaw; } set { yaw = value; } }
        public float Pitch { get { return pitch; } set { pitch = Mathf.Clamp(value, -85f, 85f); } }
        public Quaternion LeftWristOffset { get { return leftWristOffset; } set { leftWristOffset = value; } }
        public Quaternion RightWristOffset { get { return rightWristOffset; } set { rightWristOffset = value; } }

        /// <summary>World point that the Point motion aims at. Null = along the view (or the mouse under user control).</summary>
        public Vector3? PointTarget { get; set; }

        /// <summary>Creates a rig. Pass an existing camera to use it, otherwise one is made.</summary>
        public static DemoDesktopRig Create(Vector3 position, float yaw = 0f, Camera camera = null)
        {
            var go = new GameObject("Desktop Rig");
            // Inactive until the camera is set, so that Awake does not make a second one.
            go.SetActive(false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var rig = go.AddComponent<DemoDesktopRig>();
            rig.viewCamera = camera;
            rig.borrowed = camera != null;
            if (camera != null)
            {
                Transform c = camera.transform;
                rig.cameraParent = c.parent;
                rig.cameraPosition = c.localPosition;
                rig.cameraRotation = c.localRotation;
            }
            go.SetActive(true);
            rig.yaw = yaw;
            rig.start = position;
            rig.Build();
            rig.Pose(0f, true);
            return rig;
        }

        /// <summary>Moves the rig at once.</summary>
        public void Teleport(Vector3 position, float newYaw)
        {
            transform.position = position;
            start = position;
            yaw = newYaw;
            Pose(0f, true);
        }

        private void Awake()
        {
            yaw = transform.eulerAngles.y;
            start = transform.position;
            Build();
            Pose(0f, true);
        }

        private void Build()
        {
            if (built) return;
            built = true;
            useGUILayout = false;
            if (head == null) head = Child("Head");
            if (leftHand == null) leftHand = Child("Left Hand");
            if (rightHand == null) rightHand = Child("Right Hand");
            if (viewCamera == null)
            {
                var go = new GameObject("Camera");
                viewCamera = go.AddComponent<Camera>();
                viewCamera.nearClipPlane = 0.05f;
                viewCamera.fieldOfView = 65f;
                go.tag = "MainCamera";
                if (FindListener() == null) go.AddComponent<AudioListener>();
            }
            if (viewCamera.transform != head)
            {
                viewCamera.transform.SetParent(head, false);
                viewCamera.transform.localPosition = Vector3.zero;
                viewCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private static AudioListener FindListener()
        {
#if UNITY_2022_2_OR_NEWER
            return FindAnyObjectByType<AudioListener>();
#else
            return FindObjectOfType<AudioListener>();
#endif
        }

        private Transform Child(string childName)
        {
            var t = new GameObject(childName).transform;
            t.SetParent(transform, false);
            return t;
        }

        private void Update()
        {
            float dt = DemoClock.DeltaTime;
            if (userControl) Control(dt);
            Pose(dt, false);
        }

        private void Control(float dt)
        {
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            if (m != null)
            {
                bool look = m.rightButton.isPressed;
                if (look != looking)
                {
                    looking = look;
                    Cursor.lockState = look ? CursorLockMode.Locked : CursorLockMode.None;
                    Cursor.visible = !look;
                }
                if (look)
                {
                    Vector2 d = m.delta.ReadValue() * lookSpeed;
                    yaw += d.x;
                    pitch = Mathf.Clamp(pitch - d.y, -85f, 85f);
                }
            }
            if (k == null) return;
            Vector3 move = Vector3.zero;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) move.z += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) move.z -= 1f;
            if (k.dKey.isPressed) move.x += 1f;
            if (k.aKey.isPressed) move.x -= 1f;
            if (k.rightArrowKey.isPressed || turnKeys && k.eKey.isPressed) yaw += turnSpeed * dt;
            if (k.leftArrowKey.isPressed || turnKeys && k.qKey.isPressed) yaw -= turnSpeed * dt;
            if (k.cKey.wasPressedThisFrame) Crouching = !Crouching;
            if (move.sqrMagnitude > 0f)
            {
                float speed = k.leftShiftKey.isPressed ? runSpeed : walkSpeed;
                Vector3 p = transform.position + Quaternion.Euler(0f, yaw, 0f) * move.normalized * speed * dt;
                if (walkRadius > 0f)
                {
                    Vector3 off = p - start;
                    off.y = 0f;
                    if (off.magnitude > walkRadius) p = start + off.normalized * walkRadius + Vector3.up * (p.y - start.y);
                }
                transform.position = Limit != null ? Limit(p) : p;
            }
            if (k.hKey.wasPressedThisFrame) showHelp = !showHelp;
            if (!motionKeys) return;
            if (Hit(k.digit1Key, k.numpad1Key)) CurrentMotion = Motion.Idle;
            if (Hit(k.digit2Key, k.numpad2Key)) CurrentMotion = Motion.Wave;
            if (Hit(k.digit3Key, k.numpad3Key)) CurrentMotion = Motion.RaiseLeft;
            if (Hit(k.digit4Key, k.numpad4Key)) CurrentMotion = Motion.RaiseRight;
            if (Hit(k.digit5Key, k.numpad5Key)) CurrentMotion = Motion.RaiseBoth;
            if (Hit(k.digit6Key, k.numpad6Key)) CurrentMotion = Motion.Point;
            if (Hit(k.digit7Key, k.numpad7Key)) CurrentMotion = Motion.Talk;
        }

        private static bool Hit(ButtonControl a, ButtonControl b) => a.wasPressedThisFrame || b.wasPressedThisFrame;

        private void Pose(float dt, bool instant)
        {
            clock += dt;
            crouch = instant ? (Crouching ? 1f : 0f) : DemoEase.Damp(crouch, Crouching ? 1f : 0f, 0.09f, dt);
            float drop = (eyeHeight - crouchHeight) * crouch;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            head.localPosition = new Vector3(0.004f * Mathf.Sin(clock * 0.8f), eyeHeight - drop + 0.003f * Mathf.Sin(clock * 1.6f), 0f);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            float t = clock, s = eyeHeight / 1.6f;
            var restL = new Vector3(-0.28f + 0.01f * Mathf.Sin(t * 0.9f), 0.92f + 0.012f * Mathf.Sin(t * 1.3f), 0.12f);
            var restR = new Vector3(0.28f + 0.01f * Mathf.Sin(t * 1.1f + 1f), 0.92f + 0.012f * Mathf.Sin(t * 1.2f + 2f), 0.12f);
            Vector3 goalL = restL, goalR = restR;
            float upL = 0f, upR = 0f;
            bool pointing = false;
            switch (motion)
            {
                case Motion.Wave:
                    goalR = new Vector3(0.36f + 0.13f * Mathf.Sin(t * 7.5f), 1.62f, 0.28f);
                    upR = 1f;
                    break;
                case Motion.RaiseLeft: goalL = new Vector3(-0.32f, 1.82f, 0.16f); upL = 1f; break;
                case Motion.RaiseRight: goalR = new Vector3(0.32f, 1.82f, 0.16f); upR = 1f; break;
                case Motion.RaiseBoth:
                    goalL = new Vector3(-0.32f, 1.82f, 0.16f);
                    goalR = new Vector3(0.32f, 1.82f, 0.16f);
                    upL = upR = 1f;
                    break;
                case Motion.Point: goalR = new Vector3(0.20f, 1.28f, 0.42f); pointing = true; break;
                case Motion.Talk:
                {
                    float a = 0.5f + 0.5f * Mathf.Sin(t * 1.7f), b = 0.5f + 0.5f * Mathf.Sin(t * 1.3f + 1.4f);
                    goalL = new Vector3(-0.24f - 0.10f * a, 1.10f + 0.16f * a, 0.34f + 0.06f * b);
                    goalR = new Vector3(0.24f + 0.12f * b, 1.10f + 0.20f * b, 0.34f + 0.06f * a);
                    upL = 0.35f * a;
                    upR = 0.4f * b;
                    break;
                }
            }
            var down = new Vector3(0f, drop, 0f);
            goalL = goalL * s - down;
            goalR = goalR * s - down;
            left = instant ? goalL : DemoEase.Damp(left, goalL, 0.08f, dt);
            right = instant ? goalR : DemoEase.Damp(right, goalR, 0.08f, dt);
            leftUp = instant ? upL : DemoEase.Damp(leftUp, upL, 0.08f, dt);
            rightUp = instant ? upR : DemoEase.Damp(rightUp, upR, 0.08f, dt);
            leftHand.localPosition = left;
            rightHand.localPosition = right;
            leftHand.localRotation = Controller(leftUp, -1f) * leftWristOffset;
            Quaternion r = transform.rotation * (Controller(rightUp, 1f) * rightWristOffset);
            if (pointing)
            {
                Vector3 to = Target() - rightHand.position;
                if (to.sqrMagnitude > 1e-4f) r = Quaternion.LookRotation(to, Vector3.up) * rightWristOffset;
            }
            rightHand.rotation = instant ? r : DemoEase.Damp(rightHand.rotation, r, 0.05f, dt);
        }

        private Vector3 Target()
        {
            if (PointTarget.HasValue) return PointTarget.Value;
            var ray = new Ray(head.position, head.forward);
            if (userControl && !looking && viewCamera != null && Mouse.current != null) ray = viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            return DemoInput.Raycast(ray, 10f, ~0, out DemoHit hit) ? hit.point : ray.GetPoint(3f);
        }

        /// <summary>Controller orientation: along the body at rest, up when the hand is raised.</summary>
        private static Quaternion Controller(float up, float side) =>
            Quaternion.Euler(Mathf.Lerp(62f, -78f, up), side * Mathf.Lerp(-6f, 8f, up), 0f);

        private void OnDisable() { FreeCursor(); }

        private void FreeCursor()
        {
            if (!looking) return;
            looking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Destroys the rig. A camera that was passed to Create goes back to its old parent and pose first. Destroying the
        /// object of the rig directly destroys that camera too.
        /// </summary>
        public void Remove()
        {
            FreeCursor();
            if (borrowed && viewCamera != null)
            {
                Transform c = viewCamera.transform;
                c.SetParent(cameraParent, false);
                c.localPosition = cameraPosition;
                c.localRotation = cameraRotation;
            }
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }

        private GUIContent help;
        private int helpKey = -1;

        /// <summary>The help line. It is put together only when it changes.</summary>
        private GUIContent Help()
        {
            int key = (turnKeys ? 1 : 0) + (motionKeys ? 2 : 0) + (string.IsNullOrEmpty(helpText) ? 0 : 4);
            if (help == null || key != helpKey || key >= 4 && help.text != helpText)
            {
                helpKey = key;
                help = new GUIContent(key >= 4 ? helpText
                    : "Right mouse: look    WASD: walk    Shift: faster    " + (turnKeys ? "Q E" : "Arrows") + ": turn    C: crouch    Left mouse: press"
                        + (motionKeys ? "    1 idle  2 wave  3 4 5 raise  6 point  7 talk" : "") + "    H: hide");
            }
            return help;
        }

        private void OnGUI()
        {
            if (!userControl) return;
            helpAlpha = Mathf.MoveTowards(helpAlpha, showHelp ? 1f : 0f, DemoClock.DeltaTime * 5f);
            if (helpAlpha <= 0f) return;
            if (helpStyle == null)
            {
                helpStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, alignment = TextAnchor.MiddleLeft };
                helpStyle.normal.textColor = Color.white;
            }
            GUIContent text = Help();
            Vector2 size = helpStyle.CalcSize(text);
            // A text with line breaks takes more lines.
            float height = Mathf.Max(30f, size.y + 8f);
            var r = new Rect(16f, Screen.height - 14f - height, Mathf.Min(size.x + 28f, Screen.width - 32f), height);
            Color before = GUI.color;
            GUI.color = new Color(0.10f, 0.11f, 0.13f, 0.72f * helpAlpha);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, GUI.color, 0f, 8f);
            GUI.color = new Color(1f, 1f, 1f, 0.92f * helpAlpha);
            GUI.Label(new Rect(r.x + 14f, r.y, r.width - 20f, r.height), text, helpStyle);
            GUI.color = before;
        }
    }
}
