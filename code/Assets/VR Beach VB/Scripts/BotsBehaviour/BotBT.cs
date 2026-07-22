using UnityEngine;
using System.Collections.Generic;

namespace BotBT {
    [RequireComponent(typeof(TeamTracker))]
    public class BotBT : AbstractBehaviourTree
    {
        private BotDataManager mgr;
        private TeamTracker team;

        private Vector3 restingSpot = Vector3.zero;
        public static float BotSpeed {  get; private set; }

        public void Init(Vector3 _restingSpot)
        {
            restingSpot = _restingSpot;

            SetupTree();
        }

        protected override void Start() { 
            
        }

        protected override Node SetupTree()
        {
            mgr = FindAnyObjectByType<BotDataManager>();
            team = GetComponent<TeamTracker>();
            var teamVal = team.GetTeam();

            // data collection.
            BotSpeed = mgr.GetBotSpeed();

            Node node = new Sequence(
                new List<Node>{
                    new DetermineIsBallCrossingNet(
                        mgr.GetZThreshold(teamVal),
                        mgr.GetZSideOfTH(teamVal)
                        ),
                    new DoGoToBotQuarter(transform, restingSpot)
                }
            );
            return node;
        }
    }
}
