using UnityEngine;
using System.Collections.Generic;

namespace BotBehaviourTree {
    [RequireComponent(typeof(TeamTracker))]
    [RequireComponent(typeof(Collider))]
    public class BotBT : AbstractBehaviourTree
    {
        // script references
        private BotDataManager mgr;
        private TeamTracker team;
        private GameObject teammate;
        private GameObject player;

        [Header("General Parameters")]
        public static float BotSpeed;
        [SerializeField] private float hitYVal = 29.5f;

        // private parameters
        private Vector3 restingSpot = Vector3.zero;
        public bool BallInRange { get; private set; } = false;

        public void Init(Vector3 _restingSpot, GameObject _teammate, GameObject _player)
        {
            restingSpot = _restingSpot;
            teammate = _teammate;
            player = _player;

            SetupTree();
        }

        protected override void Start() { 
            // override Start to not setup the tree immediately, but waiting for manual Init instead.
        }

        protected override void Update()
        {
            base.Update();

            var pos = transform.position;
            pos.y = hitYVal;
            transform.position = pos;
        }

        protected override Node SetupTree()
        {
            mgr = FindAnyObjectByType<BotDataManager>();
            team = GetComponent<TeamTracker>();
            var teamVal = team.GetTeam();

            // data collection.
            BotSpeed = mgr.GetBotSpeed();

            // selector -> runs all children until one succeeds -> priority list;
            // first item = highest priority and exits if successful.
            Node node = new Selector(
                new List<Node>
                {
                    // sequence -> runs all children until one fails -> check and exit if check fails.
                    // first item = discriminator (should the actual function be called?)
                    new Sequence(
                        new List<Node>
                        {
                            new DetermineIsBallInReach(this),
                            new DoPlayBall(teamVal, transform, teammate.transform, player.transform, mgr.GetZThreshold(teamVal))
                        }
                    ),
                    new Sequence(
                        new List<Node>{
                            new DetermineIsMyBall(mgr.GetZThreshold(teamVal), mgr.GetZSideOfTH(teamVal), hitYVal, transform, teammate),
                            new DoGoToTargetPos(transform)
                        }
                    ),
                    new DoGoToBotQuarter(transform, restingSpot)
                }
            );
            return node;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Ball"))
                BallInRange = true;
        }

        private void OnTriggerExit(Collider other) 
        { 
            if(other.gameObject.CompareTag("Ball"))
                BallInRange = false;
        }
    }
}
