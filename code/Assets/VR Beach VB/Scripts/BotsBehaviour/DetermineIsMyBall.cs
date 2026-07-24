using NUnit.Framework;
using UnityEngine;

namespace BotBehaviourTree
{
    public class DetermineIsMyBall : Node
    {
        // z threshold related
        private readonly float thresholdZValue;
        private readonly bool zGreaterThanTH;

        private readonly float hitEstHeight;

        private readonly string BallObjKey = "ballObject";
        private readonly string TargetPosKey = "targetPos";

        readonly Transform transform;
        readonly GameObject teammate;

        public DetermineIsMyBall(float _zTHVal, bool _zGreaterTH, float _hitHeight, Transform _transform, GameObject _tm)
        {
            thresholdZValue = _zTHVal;
            zGreaterThanTH = _zGreaterTH;
            hitEstHeight = _hitHeight;
            transform = _transform;
            teammate = _tm;
        }

        public override NodeState Evaluate()
        {
            // fetch the ball's current position.
            var ball = GetBall();
            if (!ball)
            {
                ClearData(BallObjKey);

                state = NodeState.FAILURE;
                return state;
            }

            // if ball is not crossing the net return false
            SetData(BallObjKey, ball);
            var pos = ball.transform.position;

            // determine whether ball and bot are on the same side
            bool onSameSide = (zGreaterThanTH && pos.z >= thresholdZValue) || (!zGreaterThanTH && pos.z <= thresholdZValue);
            if (onSameSide)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            // if tm touched the ball last return true
            if (teammate.GetComponent<TeamTracker>().LastTouch)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            // if estimated hit position is closer to bot than teammate return success
            var hitPos = EstimateHitPosition(ball);
            if (Vector3.Distance(hitPos, teammate.transform.position) > Vector3.Distance(hitPos, transform.position))
            {
                SetData(TargetPosKey, hitPos);
                state = NodeState.SUCCESS;
                return state;
            }
            else
                ClearData(TargetPosKey);

            state = NodeState.FAILURE;
            return state;
        }

        private GameObject GetBall()
        {
            return GameObject.FindWithTag("Ball");
        }

        /// <summary>
        /// Prompt: " how do I estimate, given the ball's current position and velocity, where it will be when it reaches a certain y value,
        /// should its flight path continue unchecked? Is there a built-in method for that in Unity's physics system, or do I need to solve this manually?".
        /// Function ideated with and partially implemented by Claude AI.
        /// 
        /// with 
        /// - p the ball's position
        /// - v the ball's velocity
        /// - g the gravity's downward pull
        /// solves for t in the equation: p.y + v.y*t + 0.5*g.y*t^2 = targetYVal
        /// 0.5*g*t^2 comes from integrating the acceleration twice to get position apparently.
        /// 
        /// Using this t value then, generalise the equation to vec3 to find the estimate position.
        /// => return p + v*t + 0.5*g*t^2
        /// </summary>
        /// <param name="ball"></param>
        /// <returns></returns>
        private Vector3 EstimateHitPosition(GameObject ball)
        {
            var vel = ball.GetComponent<Rigidbody>().linearVelocity;
            var g = Physics.gravity;

            // solve for point in time t where ball reaches y height of hitEstHeight.
            float a = 0.5f * g.y;
            float b = vel.y;
            float c = ball.transform.position.y - hitEstHeight;

            // calculate for negative discriminant solver of the quadratic equation, to account for the case y is unreachable.
            float disc = b * b - 4 * a * c;
            if (disc < 0)
                return Vector3.zero;

            float sqrtD = Mathf.Sqrt(disc);
            float t1 = (-b + sqrtD) / (2*a);
            float t2 = (-b - sqrtD) / (2*a);

            float t = -1f;
            if (t1 > 0 && t2 > 0) t = Mathf.Min(t1, t2);
            else t = Mathf.Max(t1, t2);

            // y unreachable because t negative (in the past)
            if (t < 0) return Vector3.zero;

            return ball.transform.position + vel * t + 0.5f * g * t * t;
        }
    }
}
