using Normal.Realtime;
using UnityEngine.Scripting;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Networked data of the session in a room. Nobody owns it: every client may write, the last write wins.
    /// Room time needs a double, because a float would lose about two minutes of precision.
    /// </summary>
    [Preserve]
    [RealtimeModel]
    public partial class SessionModel
    {
        [RealtimeProperty(1, true, true)] private int _experience;
        [RealtimeProperty(2, true, true)] private int _director;
        [RealtimeProperty(3, true, true)] private int _pace;
        [RealtimeProperty(4, true, true)] private int _step = -1;
        [RealtimeProperty(5, true, true)] private double _stepStartTime;
        [RealtimeProperty(6, true, true)] private bool _paused;
        [RealtimeProperty(7, true, true)] private double _pauseTime;
        [RealtimeProperty(8, true, true)] private int _seed;
        [RealtimeProperty(9, true, true)] private int _sessionId;
        [RealtimeProperty(10, true, true)] private int _repeatCount;
        [RealtimeProperty(11, true, true)] private bool _linkOn = true;
        [RealtimeProperty(12, true, true)] private int _handMode = 1;
        [RealtimeProperty(13, true, true)] private float _handRatio = 0.3f;
        [RealtimeProperty(14, true, true)] private float _smoothing = 0.2f;
        [RealtimeProperty(15, true, true)] private int _positionMode;
        [RealtimeProperty(16, true, true)] private float _positionRatio = 0.3f;
        [RealtimeProperty(17, true, true)] private int _topology;
    }
}
