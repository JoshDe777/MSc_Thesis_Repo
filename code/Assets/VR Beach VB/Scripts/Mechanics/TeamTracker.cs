using UnityEngine;
using Volleyball;

public class TeamTracker : MonoBehaviour
{
    [SerializeField] private Teams team;

    public Teams GetTeam() => team;
}
