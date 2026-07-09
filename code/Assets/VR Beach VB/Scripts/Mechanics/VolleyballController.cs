using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer;

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
        public UnityEvent OnBallDestroy { get; private set; }
        /// <summary>
        /// An event called when the volleyball hits the ground (when the ball is 'killed').
        /// </summary>
        public UnityEvent OnBallKilled { get; private set; }

        public Teams lastTouch { get; private set; } = Teams.Team1;
        public Vector3 killPos { get; private set; } = Vector3.zero;

        [Header("Lifetime Parameters")]
        [SerializeField] private float selfDestructTimeLeft = 10.0f;

        #if UNITY_EDITOR
        [SerializeField] private GameObject debugSpherePrefab;
        private GameObject activeDebugSphere = null;
        #endif

        [Header("General Hit Settings")]
        [SerializeField] private float serveThrowForce = 1.75f;
        [SerializeField] private float pokeToSpikeSpeedTH = 0.1f;
        [SerializeField] private float hitCooldownTime = 0.1f;
        [SerializeField] private bool testingHits = false;
        private float activeCooldown = 0.0f;

        [Header("One Hand Hit Smoothing")]
        [SerializeField][Tooltip("The force multiplicator applied to the weakest recorded hits.")] private float oneHandHitMaxModifier = 50f;
        [SerializeField][Tooltip("The force multiplicator applied to the strongest recorded hits.")] private float oneHandHitMinModifier = 13f;
        [SerializeField][Tooltip("The smoothness of the force decay factor.")] private float oneHandHitDecayFactor = 0.25f;
        [SerializeField][Tooltip("The hit speed determined to benefit from half the multiplier.")] private float oneHandHitMidwaySpd = 8f;

        [Header("Poke Smoothing")]
        [SerializeField][Tooltip("The force multiplicator applied to the weakest recorded hits.")] private float pokeMaxModifier = 1.5f;
        [SerializeField][Tooltip("The force multiplicator applied to the strongest recorded hits.")] private float pokeMinModifier = 0.5f;
        [SerializeField][Tooltip("The smoothness of the force decay factor.")] private float pokeDecayFactor = 0.8f;
        [SerializeField] private float pokeMidwaySpd = 8f;

        [Header("Audio")]
        [SerializeField] private float defaultAudioModifier = 1.0f;
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

        private PlayerHudNotification notification;
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

            notification = FindAnyObjectByType<PlayerHudNotification>();

            EnterStateAwaitingServe();
        }

        private void Update()
        {
            if(activeCooldown > 0)
                activeCooldown -= Time.deltaTime;

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

            Vector3 force = Vector3.up * serveThrowForce;
            body.AddForce(force, ForceMode.VelocityChange);
            activeCooldown = hitCooldownTime/2;
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
            // ignore if ball not in play, or in hit cooldown.
            if (lifetime != VolleyballLifetimeState.InPlay || activeCooldown > 0)
                return;

            if (other.CompareTag("Hand"))
            {
                var hands = other.GetComponent<HandsManager>();
                if (other.gameObject.name == "Hand_Left")
                {
                    inLeftHand = true;
                    leftHandData = new(
                        other.ClosestPoint(transform.position),
                        neckThreshold.position,
                        hands.stableVelocity

                    );
                }
                else
                {
                    inRightHand = true;
                    rightHandData = new(
                        other.ClosestPoint(transform.position),
                        neckThreshold.position,
                        hands.stableVelocity
                    );
                }

                // play spike if exiting any hand.
                processHitInNextFrame = true;
                var vel = other.GetComponent<HandsManager>().stableVelocity;
                Debug.Log($"Hand speed recorded in at {vel.magnitude:0.00} m/s (velocity: {vel})!");

                // set hit cooldown.
                activeCooldown = hitCooldownTime;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // ignore if ball dead.
            if(lifetime != VolleyballLifetimeState.InPlay)
                return;

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

                if(testingHits)
                    Process1HandTestHit();
                else
                {
                    // if hitting upwards (deltaY > TH) -> underhand, else spike.
                    if (selectedHitData.handSpeed > pokeToSpikeSpeedTH)
                        Process1HandHit(selectedHitData);
                    else    // otherwise it is a poke
                        ProcessPoke(selectedHitData);
                }
            }

            // cancel all hit data & tracking after processing.
            // if done in OnTriggerExit incurs risk of cancelling the bools before processing -> invalid stuff.
            inLeftHand = false;
            leftHandData = null;
            inRightHand = false;
            rightHandData = null;
        }

        private void ProcessSet(HitData combinedHitData)
        {
            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(setSound);
            notification.ShowText("Setting!");
        }

        private void ProcessDig(HitData combinedHitData)
        {
            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(digSound);
            notification.ShowText("Digging!");
        }

        private void ProcessPoke(HitData handHitData)
        {
            // underhand = send ball in hand direction, with force derived from hand speed.
            float forceModifier = CalculatePokeModifier(handHitData.handSpeed) * handHitData.handSpeed;
            body.AddForce(forceModifier * handHitData.handVelocity.normalized);

            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(grabSound);
            notification.ShowText("Poking!");
        }

        private static int recordedSpeed = 4;

        private void Process1HandTestHit()
        {
            float actualSpeed = (float) recordedSpeed++ / 10f;
            // underhand = send ball in hand direction, with force derived from hand speed.
            float forceModifier = actualSpeed < pokeToSpikeSpeedTH ? 
                CalculatePokeModifier(actualSpeed) * (float)actualSpeed : 
                CalculateUnderhandHitModifier(actualSpeed) * (float)actualSpeed;
            body.AddForce(forceModifier * new Vector3(0, 1, -1).normalized);

            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(spikeSound);
            notification.ShowText($"(hand: {actualSpeed:0.000} m/s)");
        }

        private void Process1HandHit(HitData handHitData)
        {
            // underhand = send ball in hand direction, with force derived from hand speed.
            float forceModifier = CalculateUnderhandHitModifier(handHitData.handSpeed) * handHitData.handSpeed;
            body.AddForce(forceModifier * handHitData.handVelocity.normalized);

            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(spikeSound);
            notification.ShowText("1 Hand Fast Hit!");
        }

        /// <summary>
        /// Logistic decay function based on the parameters passed in the inspector, with the variable representing the hand's speed.
        /// Visualised at https://www.geogebra.org/m/gmrpfb4x
        /// </summary>
        /// <param name="x">The hand's speed.</param>
        /// <returns></returns>
        private float CalculateUnderhandHitModifier(float x)
        {
            float numerator = oneHandHitMaxModifier - oneHandHitMinModifier;
            float denominator = 1+Mathf.Exp(oneHandHitDecayFactor * (x-oneHandHitMidwaySpd));
            return oneHandHitMinModifier + numerator / denominator;
        }

        private float CalculatePokeModifier(float x) => (pokeMaxModifier - pokeMinModifier) /
            (1 + Mathf.Exp(pokeDecayFactor * (x - pokeMidwaySpd))) +
            pokeMinModifier;
        #endregion
    }
}
