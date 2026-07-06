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


        // -------------------- Hit Handling --------------------
        private bool inRightHand = false;
        private bool inLeftHand = false;
        private bool processHitInNextFrame = false;

        private Transform neckThreshold;

        private HitData leftHandData = null;
        private HitData rightHandData = null;

        [Header("Force Settings")]
        [SerializeField] private float serveThrowForce = 1.75f;
        [SerializeField] private float pokeToSpikeSpeedTH = 1.0f;
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

            var temp = GameObject.FindGameObjectWithTag("NeckThreshold");
            if (!temp)
                Debug.LogError("No Neck Threshold object found in scene!");
            else
                neckThreshold = temp.transform;
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
            // exit early if ball not in play
            if (lifetime < VolleyballLifetimeState.InPlay)
                return;

            if (processHitInNextFrame){
                processHitInNextFrame = false;
                ProcessHit();
            }

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
            lastTouch = Teams.Team2;                        // stop-gap for now; Assuming only 1 team so far.

            // reenable hand physics
            foreach (var mgr in FindObjectsByType<HandsManager>())
                mgr.RequestEnableHandPhysics();

            Vector3 force = Vector3.up * serveThrowForce;
            body.AddForce(force, ForceMode.Impulse);
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
        }

        private void OnTriggerEnter(Collider other)
        {
            // ignore if ball not in play.
            if (lifetime != VolleyballLifetimeState.InPlay)
                return;

            if (other.CompareTag("Hand"))
            {
                if (other.gameObject.name == "Hand_Left")
                {
                    inLeftHand = true;
                    leftHandData = new(
                        other.ClosestPoint(transform.position),
                        neckThreshold.position,
                        other.GetComponent<Rigidbody>().linearVelocity.magnitude
                    );
                    Debug.Log("Enter left hand");
                }
                else
                {
                    inRightHand = true;
                    rightHandData = new(
                        other.ClosestPoint(transform.position),
                        neckThreshold.position,
                        other.GetComponent<Rigidbody>().linearVelocity.magnitude
                    );
                    Debug.Log("Enter right hand");
                }

                // play spike if exiting any hand.
                processHitInNextFrame = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // ignore if ball dead.
            if(lifetime == VolleyballLifetimeState.DeadBall)
                return;

            // only do hands exit if triggered by hand x is marked as in any hand (so don't trigger on serve throw e.g.)
            if (other.CompareTag("Hand") && (inLeftHand || inRightHand))
            {
                if (other.gameObject.name == "Hand_Left"){
                    inLeftHand = false;
                    Debug.Log("Exit left hand");
                }
                else
                {
                    inRightHand = false;
                    Debug.Log("Exit right hand");
                }

                // play spike if exiting any hand.
                if (!inLeftHand && !inRightHand)
                    audioSource.PlayOneShot(spikeSound);
            }

            if (other.CompareTag("BallBoundsCollider"))
                OnExitBounds();
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

        #region Ball Handling
        private void ProcessHit()
        {
            // if in both hands, check for set or dig
            if(inLeftHand && inRightHand)
            {
                if (leftHandData == null || rightHandData == null)
                {
                    Debug.Log("Invalid selection!");
                    return;
                }

                var combinedHitData = leftHandData.CombineWith(rightHandData);
                if (combinedHitData.hitPos.y >= combinedHitData.torsoThresholdPos.y)
                    ProcessSet(combinedHitData);
                else
                    ProcessDig(combinedHitData);
            }
            else    // check for spike, 1-hand dig, or poke.
            {
                var selectedHitData = inLeftHand ? leftHandData : rightHandData;
                if(selectedHitData == null)
                {
                    Debug.Log("Invalid selection!");
                    return;
                }

                // if speed exceeds threshold it's either an underhand hit or a spike
                if (selectedHitData.handSpeed > pokeToSpikeSpeedTH)
                {
                    // if hit at head level -> spike
                    if(selectedHitData.hitPos.y > selectedHitData.torsoThresholdPos.y)
                        ProcessSpike(selectedHitData);
                    else    // else underhand
                        ProcessUnderhandHit(selectedHitData);
                }
                else    // otherwise it is a poke
                    ProcessPoke(selectedHitData);
            }

            leftHandData = null;
            rightHandData = null;
        }

        private void ProcessSet(HitData combinedHitData) {
            audioSource.PlayOneShot(setSound);
            Debug.Log("Setting!");
        }

        private void ProcessDig(HitData combinedHitData)
        {
            audioSource.PlayOneShot(digSound);
            Debug.Log("Digging!");
        }

        private void ProcessPoke(HitData handHitData)
        {
            audioSource.PlayOneShot(grabSound);
            Debug.Log("Poking!");
        }

        private void ProcessUnderhandHit(HitData handHitData) {
            audioSource.PlayOneShot(digSound);
            Debug.Log("Hitting underhand!");
        }

        private void ProcessSpike(HitData handHitData)
        {
            audioSource.PlayOneShot(spikeSound);
            Debug.Log("Spiking!");
        }
        #endregion
    }
}
