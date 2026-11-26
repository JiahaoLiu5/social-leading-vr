using System;

namespace SocialLeading.AllInOne
{
    public enum Experience { None, FreePlay, FullSession }

    public enum Director { Automatic, Operator }

    public enum Pace { Study, Quick }

    public enum ParticipantKind { Participant, Operator, Spectator }

    /// <summary>What this user chose on the panels. Lives as long as the app and survives leaving a room.</summary>
    public sealed class LocalChoices
    {
        /// <summary>The role value that means "assign at random".</summary>
        public const int RandomRole = -1;

        public int avatar;
        public int role;
        public ParticipantKind kind = ParticipantKind.Participant;
        public Experience experience = Experience.FreePlay;
        public Director director = Director.Automatic;
        public Pace pace = Pace.Study;
        public int room = 1;
        public bool solo;
        public bool muted;
        public bool captions = true;

        /// <summary>False keeps the microphone closed, for tests and for people who only want to watch.</summary>
        public bool microphone = true;

        /// <summary>Random per app start. Tells a returning user from a leftover of the same user.</summary>
        public readonly string userKey = Guid.NewGuid().ToString("N").Substring(0, 8);

        /// <summary>Counts the connects of this app start.</summary>
        public int connection;
    }
}
