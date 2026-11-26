using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Everybody in the room, networked or simulated, with the role that counts. Roles are only computed again when
    /// somebody's data changed.
    /// </summary>
    [DefaultExecutionOrder(-180)]
    public sealed class Roster : MonoBehaviour
    {
        [SerializeField] private AppConfig config;

        private readonly List<IParticipant> everybody = new List<IParticipant>();
        private readonly List<IParticipant> people = new List<IParticipant>();
        private readonly List<Claim> claims = new List<Claim>();
        private HashSet<int> ghosts = new HashSet<int>();
        private Dictionary<int, int> roles = new Dictionary<int, int>();
        private bool dirty = true;

        /// <summary>Raised after the roles or the people changed.</summary>
        public event Action Changed;

        /// <summary>Everybody except leftovers of dropped connections.</summary>
        public IReadOnlyList<IParticipant> People => people;

        /// <summary>This user's networked player, or null outside a room.</summary>
        public NetworkPlayer Local { get; private set; }

        /// <summary>Participants in the room that have a role, simulated partners included.</summary>
        public int SeatedCount { get; private set; }

        /// <summary>Client that advances automatic sessions, or -1.</summary>
        public int ElectedId { get; private set; } = -1;

        public bool OperatorPresent { get; private set; }

        public void Add(IParticipant person)
        {
            if (!everybody.Contains(person)) everybody.Add(person);
            MarkDirty();
        }

        /// <summary>Takes a person out at once, because nobody may touch a destroyed player until the roles are computed again.</summary>
        public void Remove(IParticipant person)
        {
            everybody.Remove(person);
            people.Remove(person);
            if (ReferenceEquals(Local, person)) Local = null;
            MarkDirty();
        }

        public void MarkDirty() => dirty = true;

        /// <summary>The role that counts, or -1.</summary>
        public int RoleOf(IParticipant person) => person != null && roles.TryGetValue(person.Id, out int role) ? role : -1;

        public bool IsGhost(IParticipant person) => ghosts.Contains(person.Id);

        /// <summary>Who has a role in the room, or null.</summary>
        public IParticipant ByRole(int role)
        {
            foreach (IParticipant p in people)
                if (RoleOf(p) == role) return p;
            return null;
        }

        /// <summary>True when another person has the role. Simulated partners give theirs up.</summary>
        public bool IsTaken(int role)
        {
            IParticipant holder = ByRole(role);
            return holder != null && !holder.IsLocal && !holder.IsBot;
        }

        /// <summary>True when another participant or an operator is in the room.</summary>
        public bool SomebodyElseInRoom
        {
            get
            {
                foreach (IParticipant p in people)
                    if (!p.IsLocal && !p.IsBot && p.InRoom && p.Kind != ParticipantKind.Spectator) return true;
                return false;
            }
        }

        /// <summary>True when every seated participant has finished the step with this mark.</summary>
        public bool AllDone(int mark)
        {
            if (SeatedCount == 0) return false;
            foreach (IParticipant p in people)
                if (RoleOf(p) >= 0 && p.DoneMark != mark) return false;
            return true;
        }

        /// <summary>Computes the roles now when something changed.</summary>
        public void Refresh()
        {
            if (!dirty) return;
            dirty = false;
            claims.Clear();
            Local = null;
            foreach (IParticipant p in everybody)
            {
                if (p.IsLocal && p is NetworkPlayer player) Local = player;
                claims.Add(new Claim
                {
                    id = p.Id,
                    wanted = p.WantedRole,
                    userKey = p.UserKey,
                    connection = p.Connection,
                    participant = p.Kind == ParticipantKind.Participant && p.InRoom,
                    bot = p.IsBot
                });
            }
            ghosts = RoleResolver.Ghosts(claims);
            roles = RoleResolver.Resolve(claims, config.roles.Count);
            ElectedId = AuthorityElection.Elect(claims);

            people.Clear();
            SeatedCount = 0;
            OperatorPresent = false;
            foreach (IParticipant p in everybody)
            {
                if (ghosts.Contains(p.Id)) continue;
                people.Add(p);
                if (RoleOf(p) >= 0) SeatedCount++;
                OperatorPresent |= p.Kind == ParticipantKind.Operator && p.InRoom;
            }
            people.Sort((a, b) => a.Id.CompareTo(b.Id));
            Changed?.Invoke();
        }

        private void Update() => Refresh();
    }
}
