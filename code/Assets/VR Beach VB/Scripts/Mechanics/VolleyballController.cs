using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System;
using XRMultiplayer;

namespace Volleyball {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(AudioSource))]
    public class VolleyballController : MonoBehaviour
    {
        #region variable declaration
        /// <summary> The Rigidbody attached to the Volleyball; Required to function. </summary>
        private Rigidbody body;
        /// <summary> The XR Grab Interactable component attached to the Volleyball; Required to function. </summary>
        private XRGrabInteractable interactable;
        /// <summary> The audio source from which ball clips are played. </summary>
        private AudioSource audioSource;

        /// <summary> A display of the ball's lifetime, from pre-match, serving, in play, to dead. </summary>
        public VolleyballLifetimeState lifetime { get; private set; } = VolleyballLifetimeState.DeadBall;
        /// <summary> An event called when the volleyball object is destroyed in the scene. </summary>
        public UnityEvent OnBallDestroy { get; private set; }
        /// <summary> An event called when the volleyball hits the ground (when the ball is 'killed'). </summary>
        public UnityEvent OnBallKilled { get; private set; }

        /// <summary> The team that last touched the ball.</summary>
        public Teams lastTouch { get; private set; } = Teams.Team1;

        /// <summary> The amount of touches made by a team since first getting possession.</summary>
        public int TeamTouches { get; private set; } = 0;

        private bool changePossession = false;

        /// <summary> The coordinates in world space where the ball was considered killed.</summary>
        public Vector3 killPos { get; private set; } = Vector3.zero;

        [Header("Lifetime Parameters")]
        [SerializeField][Tooltip("The amount of time given to the ball after a kill to cleanly delete itself from the scene.")] private float selfDestructTimeLeft = 5f;

        #if UNITY_EDITOR
        [SerializeField] private GameObject debugSpherePrefab;
        private GameObject activeDebugSphere = null;
        #endif

        [Header("General Hit Settings")]
        /// <summary>The force at which the ball is sent upwards when serving, instead of relying on throw speed.</summary>
        [SerializeField] private float serveThrowForce = 7.5f;
        /// <summary>The hand velocity value beneath which a 1-handed hit is considered a poke.</summary>
        [SerializeField] private float pokeToSpikeSpeedTH = 3f;
        /// <summary>The amount of time in seconds between two hits, to avoid double hitting.</summary>
        [SerializeField] private float hitCooldownTime = 0.1f;
        /// <summary>The time in seconds left before another hit can be registered.</summary>
        private float activeCooldown = 0.0f;

        [Header("Hit Testing Parameters")]
        /// <summary> The switch determining whether to enable debug messages and features or not.</summary>
        [SerializeField] private bool enableDebugFeatures = true;
        /// <summary> The switch to determinte whether to fake velocity values for hits or not.</summary>
        [SerializeField] private bool testingHits = false;
        /// <summary> The fake velocity used for the first hit when testing.</summary>
        [SerializeField] private float startSpeed = 1f;
        /// <summary> The velocity increment applied after every hit.</summary>
        [SerializeField] private float incrementStep = 1f;
        /// <summary> The current fake velocity applied to tested hits.</summary>
        private static float recordedSpeed = 0;
        /// <summary> A switch marking whether it is the first test hit or not. Used to not reset the recordedSpeed value with every new ball.</summary>
        private static bool firstBall = true;

        [Header("One Hand Hit Params")]
        [SerializeField][Tooltip("[1-Hand Fast Hits] The force multiplicator applied to the weakest recorded hits.")] private float oneHandHitMaxModifier = 55f;
        [SerializeField][Tooltip("[1-Hand Fast Hits] The force multiplicator applied to the strongest recorded hits.")] private float oneHandHitMinModifier = 13f;
        [SerializeField][Tooltip("[1-Hand Fast Hits] The smoothness of the force decay factor.")] private float oneHandHitDecayFactor = 0.33f;
        [SerializeField][Tooltip("[1-Hand Fast Hits] The hit speed determined to benefit from half the multiplier.")] private float oneHandHitMidwaySpd = 5.5f;

        [Header("Set Params")]
        [SerializeField][Tooltip("[Set] The force multiplicator applied to the weakest recorded hits.")] private float setMaxModifier = 55f;
        [SerializeField][Tooltip("[Set] The force multiplicator applied to the strongest recorded hits.")] private float setMinModifier = 27.5f;
        [SerializeField][Tooltip("[Set] The smoothness of the force decay factor.")] private float setEpsilon = 0.01f;
        [SerializeField][Tooltip("[Set] The hit speed determined to benefit from half the multiplier.")] private float setMaxSpd = 7f;
        private float setRiseFactor = 0f;

        [Header("Dig Params")]
        [SerializeField][Tooltip("The proportion of ball speed carried over from the ball's velocity on impact.")] private float digBallVelModifier = 0.8f;
        [SerializeField][Tooltip("The multiplier for combined hand speed applied to the ball when digging.")] private float digHandVelModifier = 1.0f;

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
        /// <summary> The value determining whether the ball was hit with the right hand this last frame.</summary>
        private bool inRightHand = false;
        /// <summary> The value determining whether the ball was hit with the left hand this last frame..</summary>
        private bool inLeftHand = false;
        /// <summary> The switch to indicate a registered hit is to be processed in the next frame in Update().</summary>
        private bool processHitInNextFrame = false;

        /// <summary> The y-value segregating sets from digs.</summary>
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
            interactable = GetComponent<XRGrabInteractable>();
            audioSource = GetComponent<AudioSource>();
            audioSource.volume *= defaultAudioModifier;

            var temp = GameObject.FindGameObjectWithTag("NeckThreshold");
            if (!temp)
                Debug.LogError("No Neck Threshold object found in scene!");
            else
                neckThreshold = temp.transform;

            if (testingHits && firstBall){
                recordedSpeed = startSpeed;
                firstBall = false;
            }

            if(setMaxSpd != 0)
                setRiseFactor = (setMaxModifier - setMinModifier) / setMaxSpd;
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

            if(enableDebugFeatures)
            // instantiate debug sphere on contact point for feedback
                activeDebugSphere = Instantiate(debugSpherePrefab, killPos, Quaternion.identity);
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
            if(enableDebugFeatures && activeDebugSphere)
                Destroy(activeDebugSphere);

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
                if (enableDebugFeatures)
                    Debug.Log("Ball killed within bounds.");
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
                // data doesn't change based on handedness
                HitData data = new(
                        other.ClosestPoint(transform.position),
                        neckThreshold.position,
                        hands.StableVelocity,
                        hands.GetPalmOrientation(),
                        body.linearVelocity,
                        hands.GetDigNormal(),
                        hands.GetHandedness()
                    );

                if (hands.GetHandedness() == Handedness.LEFT)
                {
                    inLeftHand = true;
                    leftHandData = data;
                    if (enableDebugFeatures)
                        Debug.Log("Hit by left hand!");
                }
                else if(hands.GetHandedness() == Handedness.RIGHT)
                {
                    inRightHand = true;
                    rightHandData = data;
                    if (enableDebugFeatures)
                        Debug.Log("Hit by right hand!");
                }

                // play spike if exiting any hand.
                processHitInNextFrame = true;
                var touchingTeam = other.GetComponent<TeamTracker>().GetTeam();
                if (lastTouch != touchingTeam)
                    changePossession = true;
                lastTouch = touchingTeam;
                var vel = other.GetComponent<HandsManager>().StableVelocity;
                if (enableDebugFeatures)
                    notification.ShowText($"Hit velocity: {vel} ({vel.magnitude} m/s).");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // ignore if ball dead.
            if(lifetime != VolleyballLifetimeState.InPlay)
                return;

            if (other.CompareTag("BallBoundsCollider")){
                OnExitBounds();

                if (enableDebugFeatures)
                    Debug.Log("Ball out of bounds.");
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
                if (combinedHitData.HitPos.y >= combinedHitData.SetThresholdPos.y)
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
                    if (selectedHitData.HandSpeed > pokeToSpikeSpeedTH)
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

            // set hit cooldown.
            activeCooldown = hitCooldownTime;
            // reset team touches if changing possession.
            if (changePossession)
                TeamTouches = 0;

            // increment team touches.
            TeamTouches++;
        }

        private void ProcessSet(HitData combinedHitData)
        {
            float forceModifier = CalculateSetModifier(combinedHitData.HandSpeed) * combinedHitData.HandSpeed;
            var force = forceModifier * combinedHitData.PalmOrientation.normalized;
            body.AddForce(force, ForceMode.Force);

            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(setSound);

            if (enableDebugFeatures){
                notification.ShowText($"Setting! (force of magnitude {forceModifier:0.00}, hands @ {combinedHitData.HandSpeed:0.00}m/s)");
                Debug.Log($"Set @ hand spd = {combinedHitData.HandSpeed:0.00}, force of magnitude {forceModifier:0.00}.");
            }
        }

        private void ProcessDig(HitData combinedHitData)
        {
            Vector3 reflectDirection = Vector3.Reflect(combinedHitData.BallVelocity.normalized, combinedHitData.HandVector.normalized);
            float ballVelocityModifier = CalculateDigBallVelocityModifier(combinedHitData.BallVelocity.magnitude) * combinedHitData.BallVelocity.magnitude;
            float handVelocityModifier = CalculateDigHandVelocityModifier(combinedHitData.HandVelocity.magnitude) * combinedHitData.HandVelocity.magnitude;
            Vector3 force = (ballVelocityModifier + handVelocityModifier) * reflectDirection;
            body.AddForce(force, ForceMode.Force);

            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(digSound);

            if (enableDebugFeatures)
            {
                notification.ShowText($"Digging! ball @ {ballVelocityModifier}, hand @ {handVelocityModifier}.");
                Debug.Log($"Registered Dig with modifiers: ball @ {ballVelocityModifier}, hand @ {handVelocityModifier}, for a total force of magnitude {force.magnitude}.");
                Debug.Log($"Incoming ball direction: {combinedHitData.BallVelocity.normalized}\nHand Normal: {combinedHitData.HandVector}\nReflected direction: {reflectDirection}.");
            }
        }

        private void ProcessPoke(HitData handHitData)
        {
            // underhand = send ball in hand direction, with force derived from hand speed.
            float forceModifier = CalculatePokeModifier(handHitData.HandSpeed) * handHitData.HandSpeed;
            body.AddForce(forceModifier * handHitData.HandVelocity.normalized, ForceMode.Force);

            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(grabSound);

            if (enableDebugFeatures)
                notification.ShowText("Poking!");
        }

        private void Process1HandTestHit()
        {
            float actualSpeed = recordedSpeed;
            // underhand = send ball in hand direction, with force derived from hand speed.
            float forceModifier = actualSpeed < pokeToSpikeSpeedTH ? 
                CalculatePokeModifier(actualSpeed) * (float)actualSpeed : 
                CalculateUnderhandHitModifier(actualSpeed) * (float)actualSpeed;
            body.AddForce(forceModifier * new Vector3(0, 1, -1).normalized, ForceMode.Force);

            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(spikeSound);
            notification.ShowText($"(hand: {actualSpeed:0.000} m/s)");

            recordedSpeed += incrementStep;
        }

        private void Process1HandHit(HitData handHitData)
        {
            // underhand = send ball in hand direction, with force derived from hand speed.
            float forceModifier = CalculateUnderhandHitModifier(handHitData.HandSpeed) * handHitData.HandSpeed;
            body.AddForce(forceModifier * handHitData.HandVelocity.normalized, ForceMode.Force);

            // audio queue + debug statement for classification recognition.
            audioSource.PlayOneShot(spikeSound);

            if (enableDebugFeatures)
                // notification.ShowText("1 Hand Fast Hit!");
                notification.ShowText($"1-handed fast hit!");
        }

        /// <summary>
        /// Logistic decay function based on the parameters passed in the inspector, with the variable representing the hand's speed.
        /// Visualised at https://www.geogebra.org/m/gmrpfb4x
        /// </summary>
        /// <param name="x">The hand's linear velocity magnitude on impact.</param>
        /// <returns></returns>
        private float CalculateUnderhandHitModifier(float x)
        {
            float numerator = oneHandHitMaxModifier - oneHandHitMinModifier;
            float denominator = 1+Mathf.Exp(oneHandHitDecayFactor * (x-oneHandHitMidwaySpd));
            return oneHandHitMinModifier + numerator / denominator;
        }

        private float CalculatePokeModifier(float x) => CalculateUnderhandHitModifier(x);

        /// <summary>
        /// Negative e function based on the parameters passed in the inspector, with the variable representing the hand's speed.
        /// Visualised at https://www.geogebra.org/m/brpustrz
        /// </summary>
        /// <param name="x">The hand's linear velocity magnitude on impact.</param>
        /// <returns></returns>
        private float CalculateSetModifier(float x)
        {
            float a = setMaxModifier - setMinModifier;
            float ep = setEpsilon * a;
            float b = -((float) Math.Log(ep) - (float) Math.Log(a)) / setMaxSpd;
            return a * Mathf.Exp(-b * x) + setMinModifier;
        }

        private float CalculateDigBallVelocityModifier(float x) => digBallVelModifier;

        private float CalculateDigHandVelocityModifier(float x) => digHandVelModifier;
        #endregion

        #region Audio Handling
        public void PlayKillInBounds() => audioSource.PlayOneShot(killSound);
        public void PlayKillOOB() => audioSource.PlayOneShot(oobSound);
        #endregion
    }
}
