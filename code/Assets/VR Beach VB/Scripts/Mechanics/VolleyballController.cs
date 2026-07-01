using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Volleyball {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(AudioSource))]
    public class VolleyballController : MonoBehaviour
    {
        #region variable declaration
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
        /// The audio source from which ball clips are played.
        /// </summary>
        private AudioSource audioSource;

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

        #if UNITY_EDITOR
        [SerializeField] private GameObject debugSpherePrefab;
        private GameObject activeDebugSphere = null;
#endif

        [Header("Parameters")]
        [SerializeField] private float selfDestructTimeLeft = 10.0f;
        [SerializeField] private float defaultAudioModifier = 1.0f;

        [Header("Audio")]
        [SerializeField] private AudioClip spawnSound;
        [SerializeField] private AudioClip bounceSound;
        [SerializeField] private AudioClip digSound;
        [SerializeField] private AudioClip setSound;
        [SerializeField] private AudioClip spikeSound;
        [SerializeField] private AudioClip grabSound;
        [SerializeField] private AudioClip oobSound;
        [SerializeField] private AudioClip killSound;
        #endregion

        #region Unity Functions
        private void Awake()
        {
            OnBallDestroy = new();
            OnBallKilled = new();

            body = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();
            interactable = GetComponent<XRGrabInteractable>();
            audioSource = GetComponent<AudioSource>();
            audioSource.volume *= defaultAudioModifier;
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
        #endregion

        #region game state transition functions
        /// <summary>
        /// Called when instantiated. Disables physics movement & gravity, and sets the ball to a dormant state until grabbed.
        /// </summary>
        private void EnterStateAwaitingServe()
        {
            body.constraints = RigidbodyConstraints.FreezePosition;
            body.useGravity = false;
            lifetime = VolleyballLifetimeState.AwaitingServe;

            // ignore hands physics
            foreach(var mgr in FindObjectsByType<HandsManager>())
                mgr.DisableHandPhysics(GetComponent<SphereCollider>());

            audioSource.PlayOneShot(spawnSound);
        }

        /// <summary>
        /// Called when grabbed. Enables gravity & physics movement.
        /// </summary>
        private void EnterStateServing(SelectEnterEventArgs _)
        {
            body.constraints = RigidbodyConstraints.None;
            lifetime = VolleyballLifetimeState.Serving;

            audioSource.PlayOneShot(grabSound);
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

            // reenable hand physics
            foreach (var mgr in FindObjectsByType<HandsManager>())
                mgr.RequestEnableHandPhysics();

            audioSource.PlayOneShot(setSound);
        }

        /// <summary>
        /// Called when colliding with the ground. Ignores all interactions and starts a countdown for self-destruction.
        /// </summary>
        private void EnterStateDeadBall()
        {
            // ignore interactions optionally.
            // start self-destruct timer.
            lifetime = VolleyballLifetimeState.DeadBall;

            #if UNITY_EDITOR
            // instantiate debug sphere on contact point for feedback
            activeDebugSphere = Instantiate(debugSpherePrefab, killPos, Quaternion.identity);
            #endif
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

            // destroy any debug spheres attached to the ball.
            #if UNITY_EDITOR
            if(activeDebugSphere)
                Destroy(activeDebugSphere);
            #endif

            // destroy the prefab.
            Destroy(gameObject);
        }
        #endregion

        #region collision & trigger handling
        private void OnCollisionEnter(Collision collision)
        {
            // ignore collision if not with ground
            if (!(lifetime == VolleyballLifetimeState.InPlay))
                return;

            if(collision.gameObject.CompareTag("Ground")){
                var contactpoint = collision.GetContact(0);
                killPos = contactpoint.point;

                OnBallKilled.Invoke();
                audioSource.PlayOneShot(killSound);
            }
            else{
                audioSource.PlayOneShot(spikeSound);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // ignore oob if ball dead.
            if(lifetime == VolleyballLifetimeState.DeadBall || !other.gameObject.CompareTag("BallBoundsCollider"))
                return;

            if (other.CompareTag("BallBoundsCollider")){
                OnExitBounds();
            }
        }
        #endregion

        #region OOB handling
        private void OnExitBounds()
        {
            killPos = transform.position;

            OnBallKilled.Invoke();
            audioSource.PlayOneShot(oobSound);
        }
        #endregion
    }
}
