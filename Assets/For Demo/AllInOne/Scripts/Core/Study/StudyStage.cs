using System.Collections.Generic;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// The boards, panels, and tasks of a project. The state follows from the step alone, never from events, so that late
    /// joiners see the same.
    /// </summary>
    public abstract class StudyStage : MonoBehaviour
    {
        /// <summary>Shows what belongs to the step. Called every frame.</summary>
        public abstract void Apply(StepView view);

        /// <summary>Hides everything and forgets the local answers.</summary>
        public abstract void Clear();

        /// <summary>True while the right thumbstick belongs to a task.</summary>
        public abstract bool UsesThumbstick { get; }

        /// <summary>True while a task panel is open.</summary>
        public abstract bool TaskOpen { get; }

        /// <summary>Floor position of a wall board that a simulated partner may take, away from the user.</summary>
        public abstract Vector3 BoardFor(Vector3 userHead, out float yaw);

        /// <summary>Names of the visible boards and panels, for tests.</summary>
        public abstract void CollectVisible(List<string> names);
    }
}
