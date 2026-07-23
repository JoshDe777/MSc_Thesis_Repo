using System;
using UnityEngine;
using Volleyball;

namespace BotBT
{
    public class DoPlayBall : Node
    {
        readonly Teams team;
        readonly Transform transform;
        readonly Transform teammatePos;
        readonly Transform playerPos;
        readonly float safeZPos;

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

        private void PlayToTeammate(GameObject ball) { 
            
        }

        private void PlayToPlayer(GameObject ball)
        {

        }

        private void PlayOverNet(GameObject ball)
        {

        }

        private void PlayBallToTarget(Vector3 targetPos, GameObject ball)
        {

        }
    }
}
