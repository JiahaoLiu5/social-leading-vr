using System.Collections.Generic;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Gives every body its source and its link settings, from the study profile in a full session and from the session
    /// model in free play. Nothing switches at once: a ratio is ramped, and a new source or mode goes through ratio 0.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public sealed class LinkWiring : MonoBehaviour
    {
        public const float RampSeconds = 0.7f;

        [SerializeField] private AppConfig config;
        [SerializeField] private Roster roster;
        [SerializeField] private SessionState session;

        private readonly Dictionary<AvatarBody, BodyLink> links = new Dictionary<AvatarBody, BodyLink>();
        private readonly List<AvatarBody> gone = new List<AvatarBody>();

        private void LateUpdate()
        {
            IReadOnlyList<IParticipant> people = roster.People;
            for (int i = 0; i < people.Count; i++)
            {
                IParticipant person = people[i];
                if (person.Body == null) continue;
                if (!links.TryGetValue(person.Body, out BodyLink state)) links[person.Body] = state = new BodyLink(person.Body);
                state.Step(Wanted(person), Time.deltaTime);
            }
            Forget();
        }

        private LinkSettings Wanted(IParticipant person)
        {
            var wanted = new LinkSettings();
            int role = roster.RoleOf(person);
            if (role < 0 || session.Experience == Experience.None) return wanted;

            bool study = session.Experience == Experience.FullSession;
            int topology = study ? 0 : session.Topology;
            IParticipant source = study || session.LinkOn ? roster.ByRole(config.topologies.SourceOf(topology, role)) : null;
            wanted.source = source != null && source.Body != null ? source.Body.transform : null;
            if (study)
            {
                ModulationProfile p = config.studyProfile;
                wanted.hands = p.hands;
                wanted.handRatio = p.handRatio;
                wanted.position = p.position;
                wanted.positionRatio = p.positionRatio;
                wanted.smoothing = p.partnerSmoothing;
                wanted.maxPositionOffset = p.maxPositionOffset;
            }
            else
            {
                wanted.hands = (ModulationProfile.HandMode)session.HandMode;
                wanted.handRatio = session.HandRatio;
                wanted.position = (ModulationProfile.HeadMode)session.PositionMode;
                wanted.positionRatio = session.PositionRatio;
                wanted.smoothing = session.Smoothing;
                wanted.maxPositionOffset = config.playMaxPositionOffset;
            }
            return wanted;
        }

        private void Forget()
        {
            if (links.Count <= roster.People.Count) return;
            gone.Clear();
            foreach (AvatarBody body in links.Keys)
                if (body == null) gone.Add(body);
            foreach (AvatarBody body in gone) links.Remove(body);
        }

        private struct LinkSettings
        {
            public Transform source;
            public ModulationProfile.HandMode hands;
            public float handRatio;
            public ModulationProfile.HeadMode position;
            public float positionRatio;
            public float smoothing;
            public float maxPositionOffset;
        }

        /// <summary>A value that moves to its goal in a fixed time, whatever the distance.</summary>
        private struct Ramp
        {
            private float from, goal, elapsed;

            public float Value { get; private set; }

            public void Step(float wanted, float deltaTime)
            {
                if (!Mathf.Approximately(wanted, goal))
                {
                    from = Value;
                    goal = wanted;
                    elapsed = 0f;
                }
                elapsed = Mathf.Min(RampSeconds, elapsed + deltaTime);
                Value = elapsed >= RampSeconds ? goal : Mathf.Lerp(from, goal, Mathf.SmoothStep(0f, 1f, elapsed / RampSeconds));
            }
        }

        /// <summary>The link of one body, with the two ramps of its channels.</summary>
        private sealed class BodyLink
        {
            private readonly AvatarBody body;
            private Ramp hands, position;

            public BodyLink(AvatarBody body) => this.body = body;

            public void Step(LinkSettings wanted, float deltaTime)
            {
                ModulationProfile live = body.LiveProfile;
                bool sameSource = body.link.explicitSource == wanted.source;
                bool sameHands = sameSource && live.hands == wanted.hands;
                bool samePosition = sameSource && live.position == wanted.position;

                hands.Step(sameHands && wanted.source != null && wanted.hands != ModulationProfile.HandMode.Off ? wanted.handRatio : 0f, deltaTime);
                position.Step(samePosition && wanted.source != null && wanted.position != ModulationProfile.HeadMode.Off ? wanted.positionRatio : 0f, deltaTime);

                if (!sameSource && hands.Value <= 0f && position.Value <= 0f)
                {
                    body.link.explicitSource = wanted.source;
                    body.link.linkEnabled = wanted.source != null;
                }
                if (live.hands != wanted.hands && hands.Value <= 0f) live.hands = wanted.hands;
                if (live.position != wanted.position && position.Value <= 0f) live.position = wanted.position;

                live.handRatio = hands.Value;
                live.positionRatio = position.Value;
                live.partnerSmoothing = wanted.smoothing;
                live.maxPositionOffset = wanted.maxPositionOffset;
            }
        }
    }
}
