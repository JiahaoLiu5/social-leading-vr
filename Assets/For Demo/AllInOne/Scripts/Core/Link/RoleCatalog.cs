using System;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>The roles of a project in a fixed order. A role is its index in this list.</summary>
    [CreateAssetMenu(menuName = "Social Leading/All-in-One/Role Catalog", fileName = "Role Catalog")]
    public sealed class RoleCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public string label;
            [TextArea(2, 3)] public string sentence;
            [Tooltip("Tag of the avatar root, for tools that search by tag.")]
            public string tag;
            public Vector3 seatPosition;
            public float seatYaw;
        }

        public Entry[] roles = Array.Empty<Entry>();

        public int Count => roles.Length;

        /// <summary>The entry, or null for an index that is not a role.</summary>
        public Entry Get(int index) => index >= 0 && index < roles.Length ? roles[index] : null;

        public string Label(int index) => Get(index)?.label ?? "No role";
    }
}
