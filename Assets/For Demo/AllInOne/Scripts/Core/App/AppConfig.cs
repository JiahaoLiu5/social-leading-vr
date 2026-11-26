using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Everything about one project that the shared code must not know: texts, rooms, places, and catalogs.</summary>
    [CreateAssetMenu(menuName = "Social Leading/All-in-One/App Config", fileName = "App Config")]
    public sealed class AppConfig : ScriptableObject
    {
        [Header("Texts")]
        [TextArea(2, 4)] public string roleNote;
        [TextArea(2, 4)] public string keyHint = "Online rooms need a Normcore app key. See docs/VR_SETUP.md.";
        [TextArea(2, 4)] public string comfortNote;
        [TextArea(2, 4)] public string finishNote;

        [Header("Rooms")]
        [Tooltip("The number in the prefix is the protocol version. Raise it whenever a model changes.")]
        public string roomPrefix = "sl-dyad-aio1-r";
        public string soloRoom = "solo";
        [Range(1, 9)] public int roomCount = 9;

        [Header("Places")]
        public float eyeHeight = 1.6f;
        public Vector3 lobbyPosition = new Vector3(0.35f, 0f, 0f);
        public float lobbyYaw = 90f;
        [Tooltip("First view direction on a desktop, so that the panel and the mirror are both visible.")]
        public float lobbyDesktopYaw = 62f;
        [Tooltip("Downward tilt of the view on a desktop (degrees).")]
        public float desktopPitch = 4f;
        public Vector3 startPanelPosition = new Vector3(1.55f, 1.38f, 0.95f);
        public Vector3 watchPosition = new Vector3(-2.6f, 0f, 0f);
        public Vector3 operatorDesktopPosition = new Vector3(-3.05f, 0f, 0f);
        public float operatorDesktopEyeHeight = 1.14f;
        [Tooltip("First view direction of an operator on a desktop. The panel stands at the left, the users at the right of it.")]
        public float operatorDesktopYaw = 77f;
        public Vector3 operatorDesktopPanelPosition = new Vector3(-2.30f, 1.18f, 0.87f);
        public float watchYaw = 90f;
        public Vector3 operatorPanelPosition = new Vector3(-1.83f, 1.35f, 0.92f);
        [Tooltip("Area in which the thumbstick and the desktop keys may move the user.")]
        public Vector3 walkMin = new Vector3(-2.7f, 0f, -4.3f);
        public Vector3 walkMax = new Vector3(2.7f, 0f, 3.4f);

        [Header("Catalogs")]
        public RoleCatalog roles;
        public AvatarCatalog avatars;
        public TopologyCatalog topologies;
        public StudyScript script;

        [Header("Movement link")]
        [Tooltip("Link of the full session.")]
        public ModulationProfile studyProfile;
        [Tooltip("Start values of free play.")]
        public ModulationProfile playProfile;
        [Tooltip("Largest superimposed position offset in free play (metres).")]
        public float playMaxPositionOffset = 0.6f;

        /// <summary>Name of an online room.</summary>
        public string RoomName(int number) => roomPrefix + number;

        /// <summary>Keeps a floor point inside the walking area.</summary>
        public Vector3 ClampToRoom(Vector3 position) => new Vector3(
            Mathf.Clamp(position.x, walkMin.x, walkMax.x), position.y, Mathf.Clamp(position.z, walkMin.z, walkMax.z));
    }
}
