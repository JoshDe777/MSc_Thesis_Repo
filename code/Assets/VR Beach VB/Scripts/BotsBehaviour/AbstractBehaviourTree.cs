using UnityEngine;

namespace BotBT
{
    public abstract class AbstractBehaviourTree : MonoBehaviour
    {
        private Node _root = null;

        protected virtual void Start()
        {
            _root = SetupTree();
        }

        private void Update()
        {
            _root?.Evaluate();
        }

        protected abstract Node SetupTree();
    }
}