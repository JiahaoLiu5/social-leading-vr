using Normal.Realtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Networked data of one player. Only the owner writes it. The defaults are sentinels, so that a client that sees the
    /// player before the values arrive shows nothing instead of a wrong avatar.
    /// </summary>
    [Preserve]
    [RealtimeModel]
    public partial class PlayerModel
    {
        [RealtimeProperty(1, true, true)] private int _avatarId = -1;
        [RealtimeProperty(2, true, true)] private int _role = -1;
        [RealtimeProperty(3, true, true)] private int _kind;
        [RealtimeProperty(4, true, true)] private int _phase;
        [RealtimeProperty(5, true, true)] private int _doneMark;
        [RealtimeProperty(6, true, true)] private string _userKey = "";
        [RealtimeProperty(7, false, false)] private Vector4 _handPose;
        [RealtimeProperty(8, true, true)] private bool _muted;
        [RealtimeProperty(9, true, true)] private int _connection;
    }
}
