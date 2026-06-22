using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Volleyball {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class VolleyballController : MonoBehaviour
    {
        /// <summary>
        /// The Rigidbody attached to the Volleyball; Required to function.
        /// </summary>
        private Rigidbody body;
        /// <summary>
        /// The SphereCollider attached to the Volleyball; Required to function.
        /// </summary>
        private SphereCollider _collider;
        /// <summary>
        /// The XR Grab Interactable component attached to the Volleyball; Required to function.
        /// </summary>
        private XRGrabInteractable interactable;

        /// <summary>
        /// A display of the ball's lifetime, from pre-match, serving, in play, to dead.
        /// </summary>
        public VolleyballLifetimeState lifetime { get; private set; } = VolleyballLifetimeState.DeadBall;
        /// <summary>
        /// An event called when the volleyball object is destroyed in the scene.
        /// </summary>
        public UnityEvent OnBallDestroy;
        /// <summary>
        /// An event called when the volleyball hits the ground (when the ball is 'killed').
        /// </summary>
        public UnityEvent OnBallKilled;

        public Teams lastTouch { get; private set; } = Teams.Team1;
        public Vector3 killPos { get; private set; } = Vector3.zero;

        [SerializeField] private float selfDestructTimeLeft = 10.0f;

        private void Awake()
        {
            OnBallDestroy = new();
            OnBallKilled = new();

            body = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();
            interactable = GetComponent<XRGrabInteractable>();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

            interactable.selectEntered.AddListener(EnterStateServing);
            interactable.selectExited.AddListener(EnterStateInPlay);
            OnBallKilled.AddListener(EnterStateDeadBall);

            EnterStateAwaitingServe();
        }

        private void Update()
        {
            // exit update if not in deadball state.
            if (lifetime != VolleyballLifetimeState.DeadBall)
                return;

            // decrement timer until equal or lower than 0.
            if (selfDestructTimeLeft > 0)
                selfDestructTimeLeft -= Time.deltaTime;
            else
                SelfDestruct();
        }

        /// <summary>
        /// Called when instantiated. Disables physics movement & gravity, and sets the ball to a dormant state until grabbed.
        /// </summary>
        private void EnterStateAwaitingServe()
        {
            body.constraints = RigidbodyConstraints.FreezePosition;
            body.useGravity = false;
            lifetime = VolleyballLifetimeState.AwaitingServe;
            Debug.Log("Entering Start State.");
        }

        /// <summary>
        /// Called when grabbed. Enables gravity & physics movement.
        /// </summary>
        private void EnterStateServing(SelectEnterEventArgs _)
        {
            body.constraints = RigidbodyConstraints.None;
            lifetime = VolleyballLifetimeState.Serving;
            Debug.Log("Entering Serve.");
        }

        /// <summary>
        /// Called when releasing the ball while serving. Disables grabbing.
        /// </summary>
        private void EnterStateInPlay(SelectExitEventArgs _)
        {
            interactable.enabled = false;
            body.useGravity = true;
            lifetime = VolleyballLifetimeState.InPlay;
            lastTouch = Teams.Team1;                        // stop-gap for now; Assuming only 1 team so far.
            Debug.Log("Entering Play.");
        }

        /// <summary>
        /// Called when colliding with the ground. Ignores all interactions and starts a countdown for self-destruction.
        /// </summary>
        private void EnterStateDeadBall()
        {
            // ignore interactions optionally.
            // start self-destruct timer.
            lifetime = VolleyballLifetimeState.DeadBall;
            Debug.Log("Ball Killed.");
        }

        /// <summary>
        /// Called when the lifetime timer at the end of a point runs out. Destroys this gameobject cleanly.
        /// </summary>
        private void SelfDestruct()
        {
            // call any function to execute.
            OnBallDestroy?.Invoke();

            // remove any listeners tied to the ball.
            interactable.selectEntered.RemoveListener(EnterStateServing);
            interactable.selectExited.RemoveListener(EnterStateInPlay);
            OnBallKilled.RemoveAllListeners();
            OnBallDestroy.RemoveAllListeners();

            Debug.Log("Self-destructing.");

            // destroy the prefab.
            Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // ignore collision if not with ground
            if (!collision.gameObject.CompareTag("Ground"))
                return;

            OnBallKilled.Invoke();
            killPos = collision.transform.position;
        }
    }
}
