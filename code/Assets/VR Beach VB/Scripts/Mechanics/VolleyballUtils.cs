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
        public HitData(Vector3 _hitPos, Vector3 _ttp, float _handSpeed)
        {
            hitPos = _hitPos;
            torsoThresholdPos = _ttp;
            handSpeed = _handSpeed;
        }

        public Vector3 hitPos;
        public Vector3 torsoThresholdPos;
        public float handSpeed;

        public static HitData CombineData(HitData data1, HitData data2)
        {
            // average all values.
            return new(
                Vector3.Lerp(data1.hitPos, data2.hitPos, 0.5f),
                Vector3.Lerp(data1.torsoThresholdPos, data2.torsoThresholdPos, 0.5f),
                Mathf.Lerp(data1.handSpeed, data2.handSpeed, 0.5f)
            );
        }

        public HitData CombineWith(HitData other) => CombineData(this, other);
    }
}
