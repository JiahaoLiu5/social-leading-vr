using System;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>The avatars of a project in a fixed order. An avatar id is an index in this list.</summary>
    [CreateAssetMenu(menuName = "Social Leading/All-in-One/Avatar Catalog", fileName = "Avatar Catalog")]
    public sealed class AvatarCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public string label;
            [Tooltip("Path of the character prefab below a Resources folder. Loaded on demand.")]
            public string resourcePath;
            public Texture2D thumbnail;
        }

        public Entry[] avatars = Array.Empty<Entry>();

        public int Count => avatars.Length;

        /// <summary>The entry, or null for an index that is not an avatar.</summary>
        public Entry Get(int index) => index >= 0 && index < avatars.Length ? avatars[index] : null;

        public string Label(int index) => Get(index)?.label ?? "No avatar";
    }
}
