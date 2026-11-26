using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Puts a character on a body. Each project has its own kind of character.</summary>
    public abstract class AvatarDresser : MonoBehaviour
    {
        /// <summary>The character on the body, or null.</summary>
        public abstract GameObject Current { get; }

        /// <summary>Animator of the character, or null. Drives the fingers.</summary>
        public abstract Animator CurrentAnimator { get; }

        /// <summary>True while a character is being loaded.</summary>
        public abstract bool Loading { get; }

        /// <summary>Loads a character and replaces the old one. Null removes it.</summary>
        public abstract void Dress(AvatarCatalog.Entry entry);

        /// <summary>Shows or hides the character without stopping it.</summary>
        public abstract void SetVisible(bool visible);

        /// <summary>Puts the character below the head and its feet on the floor at once.</summary>
        public abstract void Snap();
    }
}
