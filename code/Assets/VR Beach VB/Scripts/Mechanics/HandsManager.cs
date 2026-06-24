using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class HandsManager : MonoBehaviour
{
    BoxCollider m_BoxCollider;

    [SerializeField] private float handActivationDelay = 0.1f;
    private bool awaitingHandActivation = false;
    private float activationTimer = 0.1f;

    private void Awake()
    {
        m_BoxCollider = GetComponent<BoxCollider>();
    }

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
    public void DisableHandPhysics() => m_BoxCollider.enabled = false;

    // delayed activation for serve
    public void RequestEnableHandPhysics() => awaitingHandActivation = true;

    private void EnableHandPhysics()
    {
        // enable hands
        m_BoxCollider.enabled = true;

        // reset timer & quit timer consideration
        awaitingHandActivation = false;
        activationTimer = handActivationDelay;

        Debug.Log("Enabled physics!");
    }
}
