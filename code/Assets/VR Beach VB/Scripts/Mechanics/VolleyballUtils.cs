using System.Data.Common;
using Unity.Netcode;
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
        public HitData(
            Vector3 _hitPos, 
            Vector3 _ttp, 
            Vector3 _handVelocity, 
            Vector3 _palmOrientation, 
            Vector3 _ballVelocity, 
            Vector3 _handVector,
            Handedness _handedness
        ) {
            HitPos = _hitPos;
            SetThresholdPos = _ttp;
            HandVelocity = _handVelocity;
            HandSpeed = _handVelocity.magnitude;
            PalmOrientation = _palmOrientation;
            BallVelocity = _ballVelocity;
            HandVector = _handVector;
            Handedness = _handedness;
        }

        public Vector3 HitPos { get; private set; }
        public Vector3 SetThresholdPos { get; private set; }
        public Vector3 HandVelocity { get; private set; }
        public float HandSpeed { get; private set; }
        public Vector3 PalmOrientation { get; private set; }
        public Vector3 BallVelocity { get; private set; }
        public Vector3 HandVector { get; private set; }
        public Handedness Handedness { get; private set; }

        public static HitData CombineData(HitData leftData, HitData rightData)
        {

            // average position and threshold values, sum up velocities & palm positions (normalized for direction only).
            return new(
                Vector3.Lerp(leftData.HitPos, rightData.HitPos, 0.5f),                      // average hit position
                Vector3.Lerp(leftData.SetThresholdPos, rightData.SetThresholdPos, 0.5f),    // average threshold position
                leftData.HandVelocity + rightData.HandVelocity,                             // summed velocity
                (leftData.PalmOrientation + rightData.PalmOrientation).normalized,          // summed palm orientation
                Vector3.Lerp(leftData.BallVelocity, rightData.BallVelocity, 0.5f),          // average ball velocity
                Vector3.Lerp(rightData.HandVector, leftData.HandVector, 0.5f),              // average hand normal.
                Handedness.COMBINED                                                         // Combined handedness
            );  
        }

        public HitData CombineWith(HitData other) => CombineData(this, other);
    }
}
