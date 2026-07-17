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
        public HitData(Vector3 _hitPos, Vector3 _ttp, Vector3 _handVelocity, Vector3 _palmOrientation) {
            HitPos = _hitPos;
            SetThresholdPos = _ttp;
            HandVelocity = _handVelocity;
            HandSpeed = _handVelocity.magnitude;
            PalmOrientation = _palmOrientation;
        }

        public Vector3 HitPos { get; private set; }
        public Vector3 SetThresholdPos { get; private set; }
        public Vector3 HandVelocity { get; private set; }
        public float HandSpeed { get; private set; }
        public Vector3 PalmOrientation { get; private set; }

        public static HitData CombineData(HitData data1, HitData data2)
        {
            // average position and threshold values, sum up velocities & palm positions (normalized for direction only).
            return new(
                Vector3.Lerp(data1.HitPos, data2.HitPos, 0.5f),
                Vector3.Lerp(data1.SetThresholdPos, data2.SetThresholdPos, 0.5f),
                data1.HandVelocity + data2.HandVelocity,
                (data1.PalmOrientation + data2.PalmOrientation).normalized
            );
        }

        public HitData CombineWith(HitData other) => CombineData(this, other);
    }
}
