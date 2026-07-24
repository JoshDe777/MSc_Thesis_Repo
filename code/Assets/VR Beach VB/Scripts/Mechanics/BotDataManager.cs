using BotBehaviourTree;
using UnityEngine;
using Volleyball;

public class BotDataManager : MonoBehaviour
{
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private GameObject player;

    [Header("General Bot Settings.")]
    [SerializeField] private float botSpeed = 1.0f;
    [SerializeField] private Transform[] botSpawnPositions;

    [Header("Team-based presets: 0=T1, 1=T2.")]
    public Transform[] BallCrossingThresholds;
    [SerializeField] private bool[] IsTeamZGreaterThanTH;

    private GameObject[] team1 = new GameObject[2];
    private GameObject[] team2 = new GameObject[2];

    private void Start()
    {
        team1[0] = player;
        team1[1] = SpawnBot(botSpawnPositions[0], Teams.Team1);
        team2[0] = SpawnBot(botSpawnPositions[1], Teams.Team2);
        team2[1] = SpawnBot(botSpawnPositions[2], Teams.Team2);

        team1[1].GetComponent<BotBT>().Init(
            botSpawnPositions[0].position, 
            team1[0], 
            player
        );

        team2[0].GetComponent<BotBT>().Init(
            botSpawnPositions[1].position,
            team2[1],
            player
        );

        team2[1].GetComponent<BotBT>().Init(
            botSpawnPositions[2].position,
            team2[0],
            player
        );
    }

    public float GetZThreshold(Teams team) => BallCrossingThresholds[(int) team - 1].position.z;
    public bool GetZSideOfTH(Teams team) => IsTeamZGreaterThanTH[(int)team - 1];
    public float GetBotSpeed() => botSpeed;

    private GameObject SpawnBot(Transform spawnTransform, Teams team)
    {
        GameObject obj = Instantiate(botPrefab, spawnTransform.position, Quaternion.identity);
        obj.GetComponent<TeamTracker>().SetTeam(team);
        return obj;
    }
}
