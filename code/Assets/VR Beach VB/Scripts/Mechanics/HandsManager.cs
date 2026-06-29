using UnityEngine;

public class HandsManager : MonoBehaviour
{
    [Header("Collider Assignments")]
    [SerializeField] private Collider m_HandCollider;
    //[SerializeField] private BoxCollider m_ThumbCollider;
    private Collider m_ballCollider = null;

    [Header("Parameters")]
    [SerializeField] private float handActivationDelay = 0.1f;
    private bool awaitingHandActivation = false;
    private float activationTimer = 0.1f;

    private void Update()
    {
        if (!awaitingHandActivation)
            return;

        activationTimer -= Time.deltaTime;
        if (activationTimer <= 0)
        {
            EnableHandPhysics();
        }
    }

    // instant disable ok
    public void DisableHandPhysics(Collider ball)
    {
        m_ballCollider = ball;
        Physics.IgnoreCollision(m_HandCollider, ball, true);
        //Physics.IgnoreCollision(m_ThumbCollider, ball, true);
        Debug.Log("Disabled Hand Physics!");
    }

    // delayed activation for serve
    public void RequestEnableHandPhysics() => awaitingHandActivation = true;

    private void EnableHandPhysics()
    {
        if (m_ballCollider == null)
            return;

        // enable hands
        Physics.IgnoreCollision(m_HandCollider, m_ballCollider, false);
        //Physics.IgnoreCollision(m_ThumbCollider, m_ballCollider, false);

        // reset timer & quit timer consideration
        awaitingHandActivation = false;
        activationTimer = handActivationDelay;

        Debug.Log("Enabled Hand Physics!");
    }
}
