using UnityEngine;
using UnityEngine.InputSystem;

namespace DemoKit
{
    /// <summary>The mouse on a desktop: a ray through the cursor. Inactive while the right button turns the view.</summary>
    public sealed class DemoPointerMouse : DemoPointer
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float maxDistance = 20f;

        private Ray ray;

        /// <summary>Empty = the main camera.</summary>
        public Camera Camera { get { return viewCamera; } set { viewCamera = value; } }

        public float MaxDistance { get { return maxDistance; } set { maxDistance = value; } }

        /// <summary>Default: the left mouse button.</summary>
        public IDemoPress Press { get; set; }

        /// <summary>Screen position to use instead of the mouse, for tests. Null = the mouse.</summary>
        public Vector2? ScreenPosition { get; set; }

        protected override void Awake()
        {
            base.Awake();
            HoverGrace = 0f;
            Press = DemoPress.Mouse();
        }

        protected override bool TryPlane(Transform plane, out Vector3 point) => RayOnPlane(ray, plane, out point);

        private void LateUpdate()
        {
            Camera cam = viewCamera != null ? viewCamera : UnityEngine.Camera.main;
            Mouse mouse = Mouse.current;
            bool pressed = Press != null && Press.IsPressed;
            bool active = cam != null && cam.isActiveAndEnabled && (ScreenPosition.HasValue || mouse != null && !mouse.rightButton.isPressed);
            if (!active)
            {
                Drive(false, false, default, pressed);
                return;
            }
            Vector2 at = ScreenPosition ?? (Cursor.lockState == CursorLockMode.Locked
                ? new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f)
                : mouse.position.ReadValue());
            ray = cam.ScreenPointToRay(at);
            bool on = DemoInput.Raycast(ray, maxDistance, Layers, out DemoHit hit, Hand);
            Drive(true, on, hit, pressed);
        }
    }
}
