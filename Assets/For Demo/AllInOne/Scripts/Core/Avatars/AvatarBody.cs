using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// The body of one person: raw tracked poses, the partner copies, and the displayed poses that the character follows.
    /// Used by networked players, simulated partners, and the preview in the menu.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class AvatarBody : MonoBehaviour
    {
        [SerializeField] private AppConfig config;

        [Header("Raw poses")]
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;

        [Header("Partner copies")]
        public Transform headPartner;
        public Transform leftPartner;
        public Transform rightPartner;
        public Transform leftPartnerSlow;
        public Transform rightPartnerSlow;

        [Header("Displayed poses")]
        public Transform headOut;
        public Transform leftOut;
        public Transform rightOut;

        [Header("Parts")]
        public GetPartnerTransform link;
        public ModulationProfileApplier applier;
        public AvatarDresser dresser;

        private int snapFrames;

        /// <summary>The link settings of this body. <see cref="LinkWiring"/> writes them.</summary>
        public ModulationProfile LiveProfile { get; private set; }

        /// <summary>Index in the avatar catalog, or -1.</summary>
        public int AvatarId { get; private set; } = -1;

        public bool Visible { get; private set; } = true;

        /// <summary>Finger poses: left pinch, left grab, right pinch, right grab.</summary>
        public Vector4 HandPose { get; set; }

        /// <summary>How far the displayed head is from the tracked head, without height.</summary>
        public Vector3 ViewOffset
        {
            get
            {
                Vector3 offset = headOut.position - head.position;
                offset.y = 0f;
                return offset;
            }
        }

        private void Awake()
        {
            LiveProfile = ScriptableObject.CreateInstance<ModulationProfile>();
            LiveProfile.name = "Live Profile";
            LiveProfile.handRatio = 0f;
            LiveProfile.positionRatio = 0f;
            applier.profile = LiveProfile;
            link.useExplicitSource = true;
            link.explicitSource = null;
            link.linkEnabled = false;
        }

        private void OnDestroy() => Destroy(LiveProfile);

        /// <summary>Dresses the body. The old character stays until the new one is loaded.</summary>
        public void SetAvatar(int avatar)
        {
            if (avatar == AvatarId) return;
            AvatarId = avatar;
            dresser.Dress(config.avatars.Get(avatar));
        }

        public void SetVisible(bool visible)
        {
            if (Visible == visible) return;
            Visible = visible;
            dresser.SetVisible(visible);
            if (visible) Snap();
        }

        /// <summary>Call after the body was moved at once. The character follows without turning or stepping.</summary>
        public void Snap() => snapFrames = 2;

        private void LateUpdate()
        {
            if (snapFrames <= 0) return;
            snapFrames--;
            dresser.Snap();
        }
    }
}
