using UnityEngine;

namespace BotBehaviourTree
{
    public abstract class AbstractBehaviourTree : MonoBehaviour
    {
        private Node _root = null;

        protected virtual void Start()
        {
            _root = SetupTree();
        }

        protected virtual void Update()
        {
            _root?.Evaluate();
        }

        protected abstract Node SetupTree();
    }
}