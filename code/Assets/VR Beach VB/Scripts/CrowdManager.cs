using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Volleyball;


public enum CrowdStates
{
    EMPTY = 0,              // 0%
    SPARSE = 1,             // 33%
    NORMAL = 2,             // 66%
    DENSE = 3               // 100%
}

public class CrowdManager : MonoBehaviour
{
    // --------------script references--------------
    private VBMatchManager matchManager;
    private readonly List<GrandstandCreator> crowdManagers = new();
    private readonly List<GameObject> spectators = new();

    [Header("Settings & Paramters")]
    [SerializeField] private CrowdStates crowdState = CrowdStates.EMPTY;
    [SerializeField] private CrowdStates defaultCrowdState = CrowdStates.EMPTY;

    [Header("Audio")]
    [SerializeField] private AudioSource[] sources;
    [SerializeField] private List<AudioClip> lv1Ambiant;
    [SerializeField] private List<AudioClip> lv2Ambiant;
    [SerializeField] private List<AudioClip> lv3Ambiant;
    [SerializeField][Tooltip("Applause clips in ascending order of level.")] private AudioClip[] applause;

    [Header("Storage Settings")]
    [SerializeField] private string foldername = "research-data";
    [SerializeField] private string csv_filename = "crowd_transition_data";
    [SerializeField] private string json_filename = "playerdata";
    private int playerID = -1;
    private readonly List<int> transitionHistory = new();

    // --------------private variables--------------
    /// <summary>
    /// The currently playing audio track.
    /// </summary>
    private readonly AudioClip[] nowPlaying = new AudioClip[2] { null, null };

    #region Unity Functions
    private void Start()
    {
        // assign player ID
        playerID = GetPlayerID();
        transitionHistory.Add(playerID);

        // get all GrandstandCreators in scene and add them to the list of stands to manage.
        var list = FindObjectsByType<GrandstandCreator>();
        foreach (var item in list)
            crowdManagers.Add(item);


        // get all spectators in scene and add them to the list of spectator objects.
        var temp = GameObject.FindGameObjectsWithTag("Spectator");
        foreach (var item in temp)
            spectators.Add(item);

        if (spectators.Count == 0)
            Debug.LogError("Couldn't find any spectators in game!");

        if (applause.Length != 3)
            Debug.LogError("Incorrect amount of applause clips assigned! (expecting 3)");

        // set to full density by default
        SetDensity(defaultCrowdState, true);

        matchManager = FindAnyObjectByType<VBMatchManager>();
        if (matchManager == null)
            Debug.LogError("Couldn't find a VBMatchManager in scene!");

        matchManager.OnPointScored.AddListener(PlayApplauseSounds);
    }

    private void Update() => UpdateAudio();

    private void OnApplicationQuit()
    {
        SaveTransitions();
    }
    #endregion

    #region GameObject density
    /// <summary>
    /// Updates the crowd's appearance and audio to a new density value.
    /// </summary>
    /// <param name="newDensity">The new desired density.</param>
    public void SetDensity(CrowdStates newDensity, bool overrideState = false)
    {
        RegisterState();

        // spare effort if state unchanged.
        if (crowdState == newDensity && !overrideState)
            return;

        crowdState = newDensity;
        int counter = 0;

        foreach (var spectator in spectators)
        {

            // disable all spectators if crowd set to EMPTY
            if (crowdState == CrowdStates.EMPTY)
            {
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
            spectator.SetActive((counter % 3) < (int)crowdState);
            counter++;
        }

        SetAudioDensity();
    }

    public string GetCrowdState() => crowdState.ToString();
    #endregion

    #region Audio Management
    private void PlayApplauseSounds()
    {
        // no applause to play if empty crowds.
        if (crowdState == CrowdStates.EMPTY)
            return;


        foreach (var source in sources)
            source.PlayOneShot(applause[(int)crowdState - 1]);
    }

    private void UpdateAudio()
    {
        // no updates needed if no crowd or no audio queued.
        if (crowdState == CrowdStates.EMPTY)

            for (int i = 0; i < sources.Length; i++)
            {
                if (nowPlaying[i] == null)
                    continue;

                var source = sources[i];

                if (!source.isPlaying || source.time < nowPlaying[i].length)
                    continue;

                Debug.Log(source.time);
                NextSoundtrack(i);
            }
    }

    private void NextSoundtrack(int i)
    {
        var source = sources[i];
        nowPlaying[i] = crowdState == CrowdStates.SPARSE ?
            lv1Ambiant[UnityEngine.Random.Range(0, lv1Ambiant.Count - 1)] : crowdState == CrowdStates.NORMAL ?
            lv2Ambiant[UnityEngine.Random.Range(0, lv2Ambiant.Count - 1)] : lv3Ambiant[UnityEngine.Random.Range(0, lv3Ambiant.Count - 1)];
        source.clip = nowPlaying[i];
        source.Play();
    }

    private void SetAudioDensity()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            var source = sources[i];

            // if no crowds, disable audio
            if (crowdState == CrowdStates.EMPTY)
                source.Pause();
            else    // switch on a new audio track from the given level.
                NextSoundtrack(i);
        }
    }
    #endregion

    #region file saving
    private int GetPlayerID()
    {
        // create storage folder if doesn't exist
        var cwd = UnityEngine.Application.dataPath;
        var folderpath = Path.Combine(cwd, foldername);
        if (!Directory.Exists(folderpath))
            Directory.CreateDirectory(folderpath);

        var filepath = Path.Combine(folderpath, $"{json_filename}.json");
        // init with value 0 if doesn't exist
        if (!File.Exists(filepath))
        {
            File.WriteAllText(filepath, "0");
            return 0;
        }


        // Read current ID
        int currentId = 0;
        string contents = File.ReadAllText(filepath);

        if (!int.TryParse(contents, out currentId))
        {
            // assign illegal value if parsing fails
            Debug.LogError("Error parsing playerID!");
            return -1;
        }

        // Save incremented value
        File.WriteAllText(filepath, (currentId + 1).ToString());

        return currentId;

    }

    private void RegisterState()
    {
        transitionHistory.Add((int) crowdState);
        Debug.Log($"Registered transition to state {crowdState}");
    }

    private void SaveTransitions()
    {
        // create storage folder if doesn't exist
        var cwd = UnityEngine.Application.dataPath;
        var folderpath = Path.Combine(cwd, foldername);
        if (!Directory.Exists(folderpath))
            Directory.CreateDirectory(folderpath);

        var filepath = Path.Combine(folderpath, $"{csv_filename}.csv");
        List<string[]> rows = new();
        if (File.Exists(filepath))
        {
            rows = File.ReadAllLines(filepath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(','))
                .ToList();
        }


        int currentMaxColumns = rows.Count > 0
                    ? rows.Max(r => r.Length)
                    : 0;

        int requiredColumns = System.Math.Max(
            currentMaxColumns,
            transitionHistory.Count);


        List<string[]> adjustedRows = new();
        foreach (var row in rows)
        {
            if (row.Length < requiredColumns)
            {
                string[] row_ = row.ToArray();
                System.Array.Resize(ref row_, requiredColumns);

                for (int i = 0; i < requiredColumns; i++)
                {
                    if (string.IsNullOrEmpty(row_[i]))
                        row_[i] = "-1";
                }

                adjustedRows.Add(row_);
            }
            else
                adjustedRows.Add(row);
        }


        string[] newRow = new string[requiredColumns];

        for (int i = 0; i < requiredColumns; i++)
        {
            newRow[i] = i < transitionHistory.Count
                ? transitionHistory[i].ToString()
                : "-1";
        }

        adjustedRows.Add(newRow);


        // Write CSV back
        File.WriteAllLines(
            filepath,
            adjustedRows.Select(r => string.Join(",", r)));

        Debug.Log($"Successfully saved transition data to {filepath}.");
    }
    #endregion
}
