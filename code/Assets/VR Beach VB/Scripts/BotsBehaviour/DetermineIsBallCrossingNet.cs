using UnityEngine;

namespace BotBT
{
    public class DetermineIsBallCrossingNet : Node
    {
        // z threshold related
        private readonly float thresholdZValue;
        private readonly bool zGreaterThanTH;

        private readonly string BallObjKey = "ballObject";

        public DetermineIsBallCrossingNet(float _thresholdZValue, bool _zGreaterThanTH) 
        { 
            thresholdZValue = _thresholdZValue;
            zGreaterThanTH = _zGreaterThanTH;
        }

        public override NodeState Evaluate()
        {
            // get ball position
            var ball = GetBall();

            // if no ball, return FAILURE.
            if(ball == null)
            {
                ClearData(BallObjKey);

                state = NodeState.FAILURE;
                return state;
            }

            SetData(BallObjKey, ball);
            var pos = ball.transform.position;

            // determine whether ball and bot are on the same side
            bool onSameSide = (zGreaterThanTH && pos.z >= thresholdZValue) || (!zGreaterThanTH && pos.z <= thresholdZValue);

            state = onSameSide ? NodeState.SUCCESS : NodeState.FAILURE;

            // return SUCCESS if ball.z on bot's side of the TH else FAILURE.
            return state;
        }

        private GameObject GetBall()
        {
            return GameObject.FindWithTag("Ball");
        }
    }
}
