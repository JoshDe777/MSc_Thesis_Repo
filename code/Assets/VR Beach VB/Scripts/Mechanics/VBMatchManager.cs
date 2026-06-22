using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Volleyball;

namespace Volleyball
{
    public class VBMatchManager : MonoBehaviour
    {
        [SerializeField] uint[] score = new uint[2] { 0, 0 };
        [SerializeField] uint maxScore = 11;
        [SerializeField] uint crowdChangeInterval = 5;
        [SerializeField] private Transform[] bounds;

        [SerializeField] List<TMP_Text> scoreFields;
        [SerializeField] List<Transform> ballSpawnPositions;
        [SerializeField] GameObject ballPrefab;
        private CrowdOptionsMenu crowdOptions;
        private GameObject activeBall = null;
        private Vector3 killPos = Vector3.zero;
        private Teams lastTouch = Teams.None;


        #region Unity Functions
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // initialise a new list if none were assigned in editor.
            scoreFields ??= new();

            // throw an error if less than 2 ball spawn positions are provided (more is ok they'll just be ignored)
            if (ballSpawnPositions == null || ballSpawnPositions.Count < 2)
                Debug.LogError("Not enough VB spawn positions provided!");
            else if (ballSpawnPositions.Count > 2)
                Debug.LogWarning("Too many VB spawn positions provided! Positions with index 2 and above will be ignored.");

            crowdOptions = FindAnyObjectByType<CrowdOptionsMenu>();
            if (!crowdOptions)
                Debug.LogError("Couldn't find a CrowdOptionsMenu object in scene!");

            ResetBall(true);
        }
        #endregion

        #region UI updates

        private void UpdateScoreUI()
        {
            // no update if empty or not initialised
            if (scoreFields == null || scoreFields.Count == 0)
                return;

            // ... fairly self-explanatory isn't it?
            foreach (var ui in scoreFields)
                ui.text = score[0].ToString() + ":" + score[1].ToString();
        }
        #endregion

        #region Match Management
        public void TeamScored(bool team1)
        {
            // increment the amount of points for the scoring team
            uint newScore = team1 ? ++score[0] : ++score[1];

            // verify if the game is won.
            if (newScore == maxScore)
                EndMatch(team1);

            // place the ball at the back where the winners serve
            UpdateScoreUI();

            // every n points, pause the game & prompt the player to update the crowd.
            if ((score[0] + score[1]) % crowdChangeInterval == 0)
                PromptCrowdUpdate();
        }

        private void EndMatch(bool team1)
        {
            string winner = team1 ? "Team 1" : "Team 2";
            Debug.Log("Game Over! " + winner + " won!");
            score = new uint[2] { 0, 0 };
        }
        #endregion

        #region ball management
        private void ResetBall(bool team1)
        {
            Debug.Log("Resetting Ball (not implemented yet).");
            activeBall = Instantiate(ballPrefab, ballSpawnPositions[team1 ? 0 : 1]);
            var ball = activeBall.GetComponent<VolleyballController>();
            ball.OnBallKilled.AddListener(GetKillInfo);
            ball.OnBallDestroy.AddListener(ProcessPoint);
        }

        private void ProcessPoint()
        {
            Teams pointWinner = Teams.None;
            if (IsInBounds(killPos))
            {
                // courtside = ball closer to team 1 z pos/baseline or team 2 baseline?
                var distToTeam1Baseline = killPos.z - bounds[0].position.z;
                var distToTeam2Baseline = bounds[1].position.z - killPos.z;

                // if ball landed on team 1's court side, point goes to team 2, else team 1
                pointWinner = distToTeam1Baseline < distToTeam2Baseline ? Teams.Team2 : Teams.Team1;
            }
            // if ball out of bounds, last team to touch lost the point.
            else
                pointWinner = lastTouch == Teams.Team2 ? Teams.Team1 : Teams.Team2;

            // match updates & ball reset
            TeamScored(pointWinner == Teams.Team1);
            ResetBall(pointWinner == Teams.Team1);
        }

        private void GetKillInfo()
        {
            var ball = activeBall.GetComponent<VolleyballController>();
            killPos = ball.killPos;
            lastTouch = ball.lastTouch;
        }

        private bool IsInBounds(Vector3 contact)
        {
            // minmax bounding box; inclusive search since on the line counts as in.
            float maxX = Mathf.Max(bounds[0].position.x, bounds[1].position.x);
            float minX = Mathf.Min(bounds[0].position.x, bounds[1].position.x);
            float maxZ = Mathf.Max(bounds[0].position.z, bounds[1].position.z);
            float minZ = Mathf.Min(bounds[0].position.z, bounds[1].position.z);
            return contact.x <= maxX && contact.x >= minX && contact.z <= maxZ && contact.z >= minZ;
        }
        #endregion

        #region crowd update mechanic
        private void PromptCrowdUpdate()
        {
            crowdOptions.OpenMenu();
        }
        #endregion
    }
}
