using System.Collections.Generic;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Fills the roles that nobody has in an offline room with simulated partners.</summary>
    public sealed class SoloPartners : MonoBehaviour
    {
        [SerializeField] private AppConfig config;
        [SerializeField] private SessionState session;
        [SerializeField] private SessionDirector director;
        [SerializeField] private Roster roster;
        [SerializeField] private StudyStage stage;
        [SerializeField] private BotPartner prefab;

        private readonly List<BotPartner> bots = new List<BotPartner>();

        public IReadOnlyList<BotPartner> Bots => bots;

        /// <summary>Creates one partner for every role except the user's.</summary>
        public void Fill(int userRole, int userAvatar)
        {
            Clear();
            for (int role = 0; role < config.roles.Count; role++)
            {
                if (role == userRole) continue;
                BotPartner bot = Instantiate(prefab);
                bot.Begin(config, session, director, roster, stage, role, AvatarFor(role, userAvatar));
                bots.Add(bot);
            }
        }

        public void Clear()
        {
            foreach (BotPartner bot in bots)
                if (bot != null) Destroy(bot.gameObject);
            bots.Clear();
        }

        /// <summary>An avatar that neither the user nor an earlier partner wears.</summary>
        private int AvatarFor(int role, int userAvatar)
        {
            int count = config.avatars.Count;
            var free = new List<int>();
            for (int i = 0; i < count; i++)
                if (i != userAvatar && !bots.Exists(b => b.AvatarId == i)) free.Add(i);
            return free.Count > 0 ? free[(session.Seed + role) % free.Count] : userAvatar;
        }
    }
}
