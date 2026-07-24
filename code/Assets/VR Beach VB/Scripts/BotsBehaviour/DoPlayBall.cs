using System;
using System.Data.SqlTypes;
using UnityEngine;
using Volleyball;

namespace BotBehaviourTree
{
    public class DoPlayBall : Node
    {
        readonly Teams team;
        readonly Transform transform;
        readonly Transform teammatePos;
        readonly Transform playerPos;
        readonly float safeZPos;
        readonly float targetVelMagnitude;

        private readonly string BallObjKey = "ballObject";

        public DoPlayBall(Teams _team, Transform _transform, Transform _teammate, Transform _player, float _safeZPos)
        {
            team = _team;
            transform = _transform;
            teammatePos = _teammate;
            playerPos = _player;
            safeZPos = _safeZPos;
        }

        public override NodeState Evaluate()
        {
            // get n touches from ball
            object temp = GetData(BallObjKey);
            if (temp == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            GameObject ball = null;
            // try casting the data as a GameObject
            try
            {
                ball = (GameObject)temp;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                state = NodeState.FAILURE;
                return state;
            }

            // ensure the ball exists if the cast succeeded.
            Debug.Assert(ball != null);

            VolleyballController controller = ball.GetComponent<VolleyballController>();

            // if n touches < 3 play to teammate,
            if (controller?.TeamTouches < 3)
                PlayToTeammate(ball);
                                    // else if teammate is the player, just send it over,
            else if (team == Teams.Team1)
                PlayOverNet(ball);
            else                    // else just aim for the player.
                PlayToPlayer(ball);

                return base.Evaluate();
        }

        private void PlayToTeammate(GameObject ball)
        {
            Debug.Log("Playing to Teammate.");
            PlayBallToTarget(teammatePos.position, ball);
        }

        private void PlayToPlayer(GameObject ball)
        {
            Debug.Log("Playing to Player.");
            PlayBallToTarget(playerPos.position, ball);
        }

        private void PlayOverNet(GameObject ball)
        {
            Debug.Log("Playing over net.");
            // play over net: keep own x position, y irrelevant, and use the safe z position to aim.
            var target = transform.position;
            target.z = safeZPos;
            PlayBallToTarget(target, ball);
        }

        /// <summary>
        /// prompt: "[H]ow do I estimate the force I need for a given object to reach a certain position following a certain vector 
        /// (under the assumption the directional vector is derived from the bird's flightpath, and assuming it might have existing velocity when hit)?
        /// e.g.I want to aim the ball to land comfortably at the player's head or just above it. I will do the vector as playerHead - ballPos, normalise, 
        /// then hard-write the y value to a certain proportion and re-normalise. To apply the force I want to infer a k for k*direction that will reliably 
        /// send the ball at a given total velocity post-force application."
        /// 
        /// attempts to solve for k in the equation
        /// |finalVelocity| = |currentVelocity + k*direction| = desiredSpeed; squared to avoid resorting to square roots, so
        /// |finalVelocity|^2 = |currentVelocity + k*direction|^2 = desiredSpeed^2
        /// </summary>
        /// <param name="targetPos"></param>
        /// <param name="ball"></param>
        private void PlayBallToTarget(Vector3 targetPos, GameObject ball)
        {
            var targetDir = targetPos - ball.transform.position;
            targetDir.y = 0;
            targetDir.Normalize();

            var y = (2.5f * (targetDir.x + targetDir.z));
            targetDir.y = y;
            targetDir.Normalize();

            float k = -1;

            var vel = ball.GetComponent<Rigidbody>().linearVelocity;
            float a = 1f;
            float b = 2f * Vector3.Dot(vel, targetDir);
            float c = vel.sqrMagnitude - targetVelMagnitude * targetVelMagnitude;

            float disc = b * b - 4 * a * c;

            if (disc >= 0)
            {
                float sqrtD = Mathf.Sqrt(disc);
                float k1 = (-b + sqrtD) / 2f;
                float k2 = (-b - sqrtD) / 2f;

                if (k1 > 0 && k2 > 0) 
                    k = Mathf.Min(k1, k2);
                else 
                    k = Mathf.Max(k1, k2);
            }

            if(k > 0)
                ball.GetComponent<Rigidbody>().AddForce(k * targetDir, ForceMode.VelocityChange);
        }
    }
}
