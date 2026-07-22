using UnityEngine;

namespace BotBT
{
    public class DoGoToBotQuarter : Node
    {
        private Vector3 restingSpot;
        private readonly Transform transform;
        public DoGoToBotQuarter(Transform _transform, Vector3 _restingSpot) { 
            transform = _transform;
            restingSpot = _restingSpot;
        }

        public override NodeState Evaluate()
        {
            if(Vector3.Distance(transform.position, restingSpot) < 0.01f)
            {
                transform.position = restingSpot;
                // transform.LookAt();                                // look at the net.
            }
            else
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    restingSpot,
                    BotBT.BotSpeed * Time.deltaTime
                );

                transform.LookAt(restingSpot);
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}
