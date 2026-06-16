using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class VBMatchManager : MonoBehaviour
{
    [SerializeField] uint[] score = new uint[2]{0, 0};
    [SerializeField] uint maxScore = 11;
    [SerializeField] uint crowdChangeInterval = 5;

    [SerializeField] List<TMP_Text> scoreFields;
    [SerializeField] List<Transform> ballSpawnPositions;
    private CrowdOptionsMenu crowdOptions;

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
    }

    public void TeamScored(bool team1)
    {
        // increment the amount of points for the scoring team
        uint newScore = team1 ? ++score[0] : ++score[1];

        // verify if the game is won.
        if(newScore == maxScore)
            EndMatch(team1);

        // place the ball at the back where the winners serve
        ResetBall(team1);
        UpdateScoreUI();

        // every n points, pause the game & prompt the player to update the crowd.
        if ((score[0] + score[1]) % crowdChangeInterval == 0)
            PromptCrowdUpdate();
    }

    private void EndMatch(bool team1) {
        string winner = team1 ? "Team 1" : "Team 2";
        Debug.Log("Game Over! " + winner + " won!");
        score = new uint[2] {0, 0};
    }

    private void UpdateScoreUI()
    {
        // no update if empty or not initialised
        if (scoreFields == null || scoreFields.Count == 0)
            return;

        // ... fairly self-explanatory isn't it?
        foreach (var ui in scoreFields)
            ui.text = score[0].ToString() + ":" + score[1].ToString();
    }

    private void ResetBall(bool team1)
    {
        Debug.Log("Resetting Ball (not implemented yet).");
    }

    private void PromptCrowdUpdate()
    {
        crowdOptions.OpenMenu();
    }
}
