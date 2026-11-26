namespace SocialLeading.AllInOne
{
    /// <summary>One person in the room: a networked player or a simulated partner.</summary>
    public interface IParticipant
    {
        /// <summary>Client id, or 1000 + role for a simulated partner.</summary>
        int Id { get; }

        bool IsLocal { get; }
        bool IsBot { get; }

        /// <summary>False in the lobby.</summary>
        bool InRoom { get; }

        ParticipantKind Kind { get; }
        int AvatarId { get; }

        /// <summary>The role this person asked for. The role that counts is <see cref="Roster.RoleOf"/>.</summary>
        int WantedRole { get; }

        /// <summary>The last step this person finished, see <see cref="SessionState.Mark"/>.</summary>
        int DoneMark { get; }

        string UserKey { get; }
        int Connection { get; }
        bool Muted { get; }
        AvatarBody Body { get; }
    }
}
