using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CrowdStates
{
    EMPTY = 0,              // 0%
    SPARSE = 1,             // 33%
    NORMAL = 2,             // 66%
    DENSE = 3               // 100%
}

public class CrowdManager : MonoBehaviour
{
    private List<GrandstandCreator> crowdManagers = new();
    private List<GameObject> spectators = new();
    [SerializeField]
    private CrowdStates crowdState = CrowdStates.DENSE;

    private void Start()
    {
        // get all GrandstandCreators in scene and add them to the list of stands to manage.
        var list = FindObjectsByType<GrandstandCreator>();
        foreach(var item in list)
            crowdManagers.Add(item);


        // get all spectators in scene and add them to the list of spectator objects.
        var temp = GameObject.FindGameObjectsWithTag("Spectator");
        foreach (var item in temp)
            spectators.Add(item);

        if (spectators.Count == 0)
            Debug.LogError("Couldn't find any spectators in game!");

        // set to full density by default
        SetDensity(CrowdStates.DENSE);
    }

    public void SetDensityInput(TMP_Dropdown dropdown)
    {
        SetDensity((CrowdStates) dropdown.value);
    }

    private void SetDensity(CrowdStates newDensity)
    {
        crowdState = newDensity;
        int counter = 0;

        foreach(var spectator in spectators)
        {

            // disable all spectators if crowd set to EMPTY
            if (crowdState == CrowdStates.EMPTY){
                spectator.SetActive(false);
                continue;
            }

            // enable all spectators if crowd set to DENSE
            if (crowdState == CrowdStates.DENSE)
            {
                spectator.SetActive(true);
                continue;
            }

            // in density SPARSE, only activate every 3rd, in density NORMAL activate every 2 of 3 spectator.
            // counter mod 3 -> 0, 1, 2
            // crowdState SPARSE = 1 -> c%3 < 1 -> only 0
            // crowdState NORMAL = 2 -> c%3 < 2 -> 0 and 1
            spectator.SetActive((counter % 3) < (int) crowdState);
            counter++;
        }
    }

    public string GetCrowdState() => crowdState.ToString();
}
