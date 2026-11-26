using System.Collections.Generic;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Loads one of the humanoid IK characters and wires it to the displayed poses of the body. Characters are loaded on
    /// demand, so that the app starts quickly and keeps at most two per person in memory.
    /// </summary>
    [RequireComponent(typeof(AvatarBody))]
    public sealed class HumanoidDresser : AvatarDresser
    {
        private readonly List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();
        private AvatarBody body;
        private IKFootSolver[] feet = System.Array.Empty<IKFootSolver>();
        private GameObject current;
        private IKTargetFollowVRRig rig;
        private Animator animator;
        private int request;
        private bool loading, visible = true;

        public override GameObject Current => current;
        public override Animator CurrentAnimator => animator;
        public override bool Loading => loading;

        private void Awake() => body = GetComponent<AvatarBody>();

        public override void Dress(AvatarCatalog.Entry entry)
        {
            int mine = ++request;
            if (entry == null)
            {
                loading = false;
                Clear();
                return;
            }
            loading = true;
            ResourceRequest load = Resources.LoadAsync<GameObject>(entry.resourcePath);
            load.completed += _ =>
            {
                // A newer request won, or the body is gone.
                if (this == null || mine != request) return;
                loading = false;
                var prefab = load.asset as GameObject;
                if (prefab == null) Debug.LogError("All-in-One: character not found: " + entry.resourcePath);
                else Wear(prefab, entry.id);
            };
        }

        public override void SetVisible(bool on)
        {
            visible = on;
            foreach (SkinnedMeshRenderer r in renderers)
                if (r != null) r.enabled = on;
        }

        public override void Snap()
        {
            if (rig == null) return;
            PlaceUnderHead();
            foreach (IKFootSolver foot in feet)
                if (foot != null && foot.isActiveAndEnabled) foot.Snap();
        }

        private void Wear(GameObject prefab, string id)
        {
            // The rig builds its graph when it is enabled, so the parent must be active and the pose roughly right.
            GameObject next = Instantiate(prefab, transform);
            next.name = id;
            next.transform.localPosition = Vector3.zero;
            next.transform.localRotation = Quaternion.identity;
            Clear();
            current = next;
            rig = next.GetComponentInChildren<IKTargetFollowVRRig>(true);
            rig.head.vrTarget = body.headOut;
            rig.leftHand.vrTarget = body.leftOut;
            rig.rightHand.vrTarget = body.rightOut;
            PlaceUnderHead();
            animator = rig.GetComponent<Animator>();
            feet = next.GetComponentsInChildren<IKFootSolver>(true);
            next.GetComponentsInChildren(true, renderers);
            SetVisible(visible);
        }

        private void PlaceUnderHead() => rig.transform.SetPositionAndRotation(
            body.headOut.position + rig.headBodyPositionOffset, Quaternion.Euler(0f, body.headOut.eulerAngles.y, 0f));

        private void Clear()
        {
            if (current != null) Destroy(current);
            current = null;
            rig = null;
            animator = null;
            feet = System.Array.Empty<IKFootSolver>();
            renderers.Clear();
        }
    }
}
