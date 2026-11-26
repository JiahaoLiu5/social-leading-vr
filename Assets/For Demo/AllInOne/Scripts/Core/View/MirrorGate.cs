using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Renders the mirror only while the user can see it, and with a smaller image every second frame on a headset.
    /// Panels are private to each user and stay out of the mirror.
    /// </summary>
    public sealed class MirrorGate : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Camera mirrorCamera;
        [SerializeField] private Renderer surface;
        [Tooltip("Farther than any place in the room.")]
        [SerializeField] private float maxDistance = 9f;
        [SerializeField] private Vector2Int headsetSize = new Vector2Int(1024, 640);

        private readonly Plane[] planes = new Plane[6];
        private RenderTexture image;

        /// <summary>True while the mirror is worth rendering.</summary>
        public bool InView { get; private set; }

        /// <summary>Renders the mirror once, for pictures.</summary>
        public void RenderNow() => mirrorCamera.Render();

        private void Start()
        {
            mirrorCamera.allowHDR = false;
            mirrorCamera.depthTextureMode = DepthTextureMode.None;
            mirrorCamera.cullingMask &= ~LayerMask.GetMask("UI");
            if (!QualityTuner.Standalone) return;
            image = new RenderTexture(headsetSize.x, headsetSize.y, 24) { name = "Mirror (headset)", antiAliasing = 2 };
            mirrorCamera.targetTexture = image;
            surface.material.mainTexture = image;
        }

        private void OnDestroy()
        {
            if (image == null) return;
            mirrorCamera.targetTexture = null;
            image.Release();
            Destroy(image);
        }

        private void LateUpdate()
        {
            Vector3 toMirror = surface.bounds.center - viewCamera.transform.position;
            InView = toMirror.sqrMagnitude < maxDistance * maxDistance
                     && Vector3.Dot(viewCamera.transform.forward, toMirror) > 0f
                     && InFrustum();
            mirrorCamera.enabled = InView && (image == null || (Time.frameCount & 1) == 0);
        }

        private bool InFrustum()
        {
            GeometryUtility.CalculateFrustumPlanes(viewCamera, planes);
            return GeometryUtility.TestPlanesAABB(planes, surface.bounds);
        }
    }
}
