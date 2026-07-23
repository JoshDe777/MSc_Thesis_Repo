using UnityEngine;
using Volleyball;

public class TeamTracker : MonoBehaviour
{
    [SerializeField] private Teams team;
    public bool LastTouch = false;
    [SerializeField] private TeamTracker otherHand = null;

    private void Start()
    {
        // synchronise teams in case of linked per-hand tracker.
        if (otherHand)
            otherHand.team = this.team;
    }

    public Teams GetTeam() => team;

    public void Touch(){ 
        LastTouch = true; 
        if(otherHand)
            otherHand.LastTouch = true;
    }
    public void Dispossess()
    {
        LastTouch = false;
        if (otherHand)
            otherHand.LastTouch = false;
    }
}
