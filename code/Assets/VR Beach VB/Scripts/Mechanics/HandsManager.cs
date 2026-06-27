using UnityEngine;

public class HandsManager : MonoBehaviour
{
    [Header("Collider Assignments")]
    [SerializeField] private BoxCollider m_HandCollider;
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

        Debug.Log("Enabled physics!");
    }

    void OnDrawGizmos()
    {
        if (m_HandCollider == null) return;

        Gizmos.color = new Color(0.5f, 1f, 0f, 1f); // Lime green

        // Match the object's transform so the gizmo rotates/scales with it
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(m_HandCollider.center, m_HandCollider.size);
    }
}
