using System.Collections.Generic;

namespace SocialLeading.AllInOne
{
    /// <summary>What one person in the room claims to be.</summary>
    public struct Claim
    {
        public int id;
        public int wanted;
        public string userKey;
        public int connection;
        public bool participant;
        public bool bot;
    }

    /// <summary>
    /// Decides who has which role. Pure and deterministic: the same claims give the same roles on every client, in any order.
    /// </summary>
    public static class RoleResolver
    {
        /// <summary>
        /// Leftovers of clients that crashed or slept: the same user is in the room again with a higher connection number.
        /// </summary>
        public static HashSet<int> Ghosts(IReadOnlyList<Claim> claims)
        {
            var ghosts = new HashSet<int>();
            for (int a = 0; a < claims.Count; a++)
            {
                if (string.IsNullOrEmpty(claims[a].userKey)) continue;
                for (int b = 0; b < claims.Count; b++)
                    if (claims[b].userKey == claims[a].userKey && claims[b].connection > claims[a].connection) ghosts.Add(claims[a].id);
            }
            return ghosts;
        }

        /// <summary>
        /// Role per id of every participant. Lower ids choose first. Who does not get the wanted role gets the next free one,
        /// or -1 when all roles are taken.
        /// </summary>
        public static Dictionary<int, int> Resolve(IReadOnlyList<Claim> claims, int roleCount)
        {
            HashSet<int> ghosts = Ghosts(claims);
            var people = new List<Claim>();
            foreach (Claim c in claims)
                if (c.participant && !ghosts.Contains(c.id)) people.Add(c);
            people.Sort((a, b) => a.id.CompareTo(b.id));

            var roles = new Dictionary<int, int>();
            var taken = new bool[roleCount];
            foreach (Claim c in people)
            {
                if (c.wanted < 0 || c.wanted >= roleCount || taken[c.wanted]) continue;
                taken[c.wanted] = true;
                roles[c.id] = c.wanted;
            }
            foreach (Claim c in people)
                if (!roles.ContainsKey(c.id)) roles[c.id] = NextFree(taken, c.wanted);
            return roles;
        }

        private static int NextFree(bool[] taken, int after)
        {
            for (int i = 1; i <= taken.Length; i++)
            {
                int role = ((after + i) % taken.Length + taken.Length) % taken.Length;
                if (taken[role]) continue;
                taken[role] = true;
                return role;
            }
            return -1;
        }
    }
}
