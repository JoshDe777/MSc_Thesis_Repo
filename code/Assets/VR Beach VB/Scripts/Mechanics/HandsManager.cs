using UnityEngine;

public class HandsManager : MonoBehaviour
{
    private Vector3 lastPos = Vector3.zero;
    /// <summary>
    /// Position change per second, calculated using the difference between the current and last frame.
    /// </summary>
    public Vector3 velocity { get; private set; }

    private void Update()
    {
        UpdateVelocity();
    }

    private void UpdateVelocity()
    {
        velocity = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;
    }
}
