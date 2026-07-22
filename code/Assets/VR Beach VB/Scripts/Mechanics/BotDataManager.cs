using UnityEngine;
using Volleyball;

public class BotDataManager : MonoBehaviour
{
    [Header("General Bot Settings.")]
    [SerializeField] private float botSpeed = 1.0f;
    [SerializeField] private Transform[] botSpawnPositions;

    [Header("Team-based presets: 0=T1, 1=T2.")]
    public Transform[] BallCrossingThresholds;
    [SerializeField] private bool[] IsTeamZGreaterThanTH;

    public float GetZThreshold(Teams team) => BallCrossingThresholds[(int) team - 1].position.z;
    public bool GetZSideOfTH(Teams team) => IsTeamZGreaterThanTH[(int)team - 1];
    public float GetBotSpeed() => botSpeed;
}
