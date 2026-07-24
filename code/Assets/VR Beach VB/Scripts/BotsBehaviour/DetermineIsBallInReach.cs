namespace BotBehaviourTree
{
    public class DetermineIsBallInReach : Node
    {
        readonly BotBT bot;

        public DetermineIsBallInReach(BotBT _bot) 
        {
            bot = _bot;
        }

        public override NodeState Evaluate()
        {
            return bot.BallInRange ? NodeState.SUCCESS : NodeState.FAILURE;
        }
    }
}
