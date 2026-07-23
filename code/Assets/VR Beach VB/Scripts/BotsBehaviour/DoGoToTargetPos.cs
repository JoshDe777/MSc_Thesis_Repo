using UnityEngine;

namespace BotBT
{
    public class DoGoToTargetPos : Node
    {
        private readonly string TargetPosKey = "targetPos";

        private readonly Transform transform;

        public DoGoToTargetPos(Transform _transform)
        {
            transform = _transform;
        }

        public override NodeState Evaluate()
        {
            var target = (Vector3) GetData(TargetPosKey);
            target.y = transform.position.y;

            if (Vector3.Distance(transform.position, target) < 0.01f)
            {
                transform.position = target;
                // transform.LookAt();                                // look at the ball.
            }
            else
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    BotBT.BotSpeed * Time.deltaTime
                );

                transform.LookAt(target);
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}
