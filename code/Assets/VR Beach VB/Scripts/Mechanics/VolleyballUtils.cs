using UnityEngine;

namespace Volleyball
{
    public enum VolleyballLifetimeState
    {
        AwaitingServe = 0,
        Serving = 1,
        InPlay = 2,
        DeadBall = 3
    }

    public enum Teams
    {
        None = 0,
        Team1 = 1,
        Team2 = 2
    }
}
