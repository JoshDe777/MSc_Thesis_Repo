using System.Data.Common;
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

    public class HitData
    {
        public HitData(Vector3 _hitPos, Vector3 _ttp, Vector3 _handVelocity) {
            hitPos = _hitPos;
            torsoThresholdPos = _ttp;
            handVelocity = _handVelocity;
            handSpeed = _handVelocity.magnitude;
        }

        public Vector3 hitPos { get; private set; }
        public Vector3 torsoThresholdPos { get; private set; }
        public Vector3 handVelocity { get; private set; }
        public float handSpeed { get; private set; }

        public static HitData CombineData(HitData data1, HitData data2)
        {
            // average all values.
            return new(
                Vector3.Lerp(data1.hitPos, data2.hitPos, 0.5f),
                Vector3.Lerp(data1.torsoThresholdPos, data2.torsoThresholdPos, 0.5f),
                Vector3.Lerp(data1.handVelocity, data2.handVelocity, 0.5f)
            );
        }

        public HitData CombineWith(HitData other) => CombineData(this, other);
    }
}
