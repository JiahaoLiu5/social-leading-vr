using System;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Who moves whom. A topology names the source role of every role, or -1 for none.</summary>
    [CreateAssetMenu(menuName = "Social Leading/All-in-One/Topology Catalog", fileName = "Topology Catalog")]
    public sealed class TopologyCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public string label;
            public int[] sourceOfRole = Array.Empty<int>();
        }

        public Entry[] topologies = Array.Empty<Entry>();

        public int Count => topologies.Length;

        /// <summary>The role that moves the given role, or -1.</summary>
        public int SourceOf(int topology, int role)
        {
            if (topology < 0 || topology >= topologies.Length) return -1;
            int[] sources = topologies[topology].sourceOfRole;
            return role >= 0 && role < sources.Length ? sources[role] : -1;
        }

        public string[] Labels => Array.ConvertAll(topologies, t => t.label);
    }
}
