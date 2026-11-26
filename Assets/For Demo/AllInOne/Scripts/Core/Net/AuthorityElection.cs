using System.Collections.Generic;

namespace SocialLeading.AllInOne
{
    /// <summary>Chooses the client that advances the steps of an automatic session: the participant with the lowest id.</summary>
    public sealed class AuthorityElection
    {
        /// <summary>A client must be elected this long without interruption, so that two clients never act during a hand-over.</summary>
        public const double HoldSeconds = 1.0;

        private int lastElected = -1;
        private double since;

        /// <summary>Lowest id among the participants. Ghosts, operators, spectators, and simulated partners never count. -1 = nobody.</summary>
        public static int Elect(IReadOnlyList<Claim> claims)
        {
            HashSet<int> ghosts = RoleResolver.Ghosts(claims);
            int best = -1;
            foreach (Claim c in claims)
                if (c.participant && !c.bot && !ghosts.Contains(c.id) && (best < 0 || c.id < best)) best = c.id;
            return best;
        }

        /// <summary>True when the local client may act.</summary>
        public bool MayAct(int elected, int localId, double now)
        {
            if (elected != lastElected || now < since)
            {
                lastElected = elected;
                since = now;
            }
            return elected >= 0 && elected == localId && now - since >= HoldSeconds;
        }

        public void Reset() => lastElected = -1;
    }
}
