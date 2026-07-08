using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using Volleyball;

public class HandsManager : MonoBehaviour
{
    /// <summary>
    /// A list of local velocities by frames, used to calculate a more stable velocity value.
    /// </summary>
    private readonly Queue<Vector3> lastNFrames = new();
    
    /// <summary>
    /// The amount of frames stored to aggregate velocity/momentum
    /// </summary>
    public const uint nFrames = 3;

    /// <summary>
    /// A vector storing the hand's position in the last frame, to calculate local velocity.
    /// </summary>
    private Vector3 lastPos = Vector3.zero;

    /// <summary>
    /// Position change per second, calculated as an aggregate of the last [5] frames (see nFrames).
    /// </summary>
    public Vector3 stableVelocity { get; private set; }

    private void Update()
    {
        UpdateVelocity();
    }

    private void UpdateVelocity()
    {
        // if reached maximum frame buffer capacity, flush out oldest record.
        if(lastNFrames.Count == nFrames)
            lastNFrames.Dequeue();

        // calculate local velocity and add to queue
        var tempVelocity = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;
        lastNFrames.Enqueue(tempVelocity);

        // average over the queued velocities to get 'local' velocity/momentum.
        var tempVector = Vector3.zero;
        Queue<Vector3> copy = new(lastNFrames);
        for (int i = 0; i < lastNFrames.Count; i++)
            tempVector += copy.Dequeue() / lastNFrames.Count;

        stableVelocity = tempVector;
    }
}
