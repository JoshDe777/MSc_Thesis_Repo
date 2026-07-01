using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class BackgroundAudioManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool shuffle = false;
    [SerializeField] private bool paused = false;

    [Header("Song Inputs")]
    [SerializeField] private List<AudioClip> playlist;
    [SerializeField][Tooltip("Titles in order of playlist above.")] private List<string> titles;
    [SerializeField][Tooltip("Authors in order of playlist above.")] private List<string> authors;

    private AudioSource source;
    private Queue<AudioClip> playQueue = new();
    private AudioClip nowPlaying = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();
        NextSong();
    }

    private Queue<AudioClip> ShufflePlay()
    {
        List<AudioClip> copy = new(playlist);
        for (int i = copy.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }

        return new Queue<AudioClip>(copy);
    }

    private void ReloadQueue()
    {
        playQueue = shuffle ? ShufflePlay() : new(playlist);
    }

    private void NextSong()
    {
        if (playQueue.Count == 0)
            ReloadQueue();

        nowPlaying = playQueue.Dequeue();
        source.clip = nowPlaying;
        source.Play();
        // propagate UI changes etc.
    }

    public void Pause()
    {
        paused = true;
        source.Pause();
    }

    public void Resume()
    {
        paused = false;
        source.Play();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Volume is at: " + source.volume);

        if (nowPlaying == null || paused)
            return;

        if (source.time < nowPlaying.length)
            return;

        NextSong();
    }
}
