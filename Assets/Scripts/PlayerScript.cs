using System.Collections;
using System.Numerics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class PlayerScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 5f;
    private Rigidbody rb;

    [Header("UI Buttons")]
    public Button drinkButton;

    [SerializeField]
    private float smoothRotation = 10f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [Header("Animation Rigging")]
    public Rig walkRig;
    public Rig armRig;
    public Rig glideRig;
    public Rig armPluckRig;

    [SerializeField]
    private float rigTransitionSpeed = 5f;

    [Header("Drinking Settings")]
    private CanvasTrigger canvasTrigger;
    private int currentWaterAmount = 0;

    private float horizontalInput;
    private bool isMoving;
    private bool isDrinking;

    public bool isCasting;
    public bool isGliding;
    private bool isBreathing;

    private Vector3 moveDirection;

    public float targetRigWeight;

    public GameObject crossReferrence;
    private BoxCollider crossCol;
    public GameObject cupGO;

    // Water Fountain interaction
    private bool isNearWaterFountain = false;
    private GameObject currentWaterFountain;

    // Rock Obstacle interaction
    private bool isBlockedByRock = false;
    private Vector3 rockObstaclePosition;
    private Vector3 lastValidPosition;

    // FastStop interaction
    private bool isBlockedByFastStop = false;
    private Vector3 fastStopPosition;
    private bool fastStopBlockLeft = false;
    private bool fastStopBlockRight = false;
    public RigBuilder rigBuilder;

    // Animation parameter names
    private const string MOVE_ANIMATION = "Walk";
    private const string IS_MOVING = "IsWalking";
    private const string IS_DRINKING = "IsDrinking";
    private const string IS_CASTING = "IsCasting";
    private const string IS_GLIDING = "IsGlide";

    [Header("Initial Cross Setup")]
    public Vector3 initialTransformCrossOffset;
    public Vector3 initialRotationCrossOffset;

    [Header("Final Cross Setup")]
    public Transform hitAnchor;
    public Transform handTransform;
    public Vector3 hitOffset;
    public Vector3 hitRotationOffset;
    public Vector3 crossOffset;
    public Vector3 crossRotationOffset;

    [Header("Glider Settings")]
    public GameObject crossGliderGO;

    [Header("Player Health Settings")]
    [HideInInspector]
    public int maxHealth = 100;
    public int currentHealth;
    public Image splashDamageImage;
    public float flashDuration = 0.5f;
    public Color flashColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("Player Attack Settings")]
    public SphereCollider crossHitCol;
    public LayerMask enemyLayer;
    public float damageAmount = 25f;

    [Header("Mobile Controls")]
    [SerializeField]
    private bool useMobileControls = false;
    private float mobileHorizontalInput;
    private bool mobileCastPressed = false;

    [Header("Pull Settings")]
    private const string PULL_TRIGGER = "Pull";
    private bool isPulling;
    private bool canPull;
    private bool pullAnimationComplete = false;

    public bool IsPulling
    {
        get { return isPulling; }
    }
    public bool IsPullAnimationComplete
    {
        get { return pullAnimationComplete; }
    }

    [Header("Sleep Settings")]
    public Vector3 sleepOffset;
    private bool isSleeping;
    private const string IS_SLEEPING = "isSleeping";
    public bool IsSleeping
    {
        get { return isSleeping; }
    }

    [Header("Rock Settings")]
    public GameObject crossRockGO;
    public Vector3 rockPushInitialAngle;
    public Vector3 rockPushAngle;
    public float rockRotationSpeed = 2f;
    private bool isRotatingRock = false;
    private float rockRotationProgress = 0f;

    [Header("Pluck Settings")]
    public PluckData pluck = new PluckData();

    [Header("Sail Settings")]
    public SailData sailData = new SailData();

    [Header("Storm Settings")]
    public StormData stormData = new StormData();

    private void Awake()
    {
        glideRig.weight = 0;
        currentHealth = maxHealth;
        isSleeping = false;
        rb = GetComponent<Rigidbody>();

        if (splashDamageImage != null)
            splashDamageImage.color = Color.clear;

        lastValidPosition = transform.position;

        if (crossRockGO != null)
            crossRockGO.transform.localRotation = UnityEngine.Quaternion.Euler(
                rockPushInitialAngle
            );

        if (drinkButton != null)
            drinkButton.interactable = false;
        else
            Debug.LogWarning("Drink button reference is missing!");
    }

    bool toBuildRig = false;

    void Start()
    {
        if (crossReferrence == null)
        {
            Debug.LogError("Cross reference GameObject is not assigned in the inspector!");
            return;
        }
        else
        {
            stormData.stormCross = crossReferrence;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                Debug.LogWarning("No Animator component found on player!");
        }

        if (walkRig == null)
        {
            RigBuilder rigBuilder = GetComponent<RigBuilder>();
            if (rigBuilder != null && rigBuilder.layers.Count > 0)
                walkRig = rigBuilder.layers[0].rig;
            else
                Debug.LogWarning("No Rig component assigned or found!");
        }

        if (armRig == null)
        {
            RigBuilder rigBuilder = GetComponent<RigBuilder>();
            if (rigBuilder != null && rigBuilder.layers.Count > 0)
                armRig = rigBuilder.layers[1].rig;
            else
                Debug.LogWarning("No Rig component assigned or found!");
        }

        crossCol = crossReferrence.GetComponent<BoxCollider>();
        crossCol.enabled = false;
    }

    void Update()
    {
        GetInput();

        if (isNearWaterFountain)
        {
            isMoving = false;
            horizontalInput = 0f;
        }

        if (!isBreathing && !isDrinking && !isCasting && !isGliding && !isPulling)
        {
            HandleMovement();
            HandleRotation();
        }

        HandleAnimation();
        HandleRigWeight();
        HandleCastingInput();
        HandleDrinkingInput();
        HandlePullingInput();
        HandleRockRotation();
        HandleStorm();

        if (isGliding == true)
        {
            this.transform.localRotation = UnityEngine.Quaternion.Euler(0, -90f, 0);
            DisableAllMovements();
        }

        if (sailData.isSailing == true)
        {
            this.transform.localRotation = UnityEngine.Quaternion.Euler(0, -90f, 0);
            DisableAllMovements();
        }

        if (isRotatingRock == false && crossRockGO != null)
        {
            crossRockGO.transform.localRotation = UnityEngine.Quaternion.Euler(
                rockPushInitialAngle
            );
        }

        HandlePluckRigDrop();
    }

    void HandlePullingInput()
    {
        if (Input.GetKeyDown(KeyCode.P) && canPull)
            OnPullButtonDown();

        if (Input.GetKeyUp(KeyCode.P))
            OnPullButtonUp();
    }

    void HandleRockRotation()
    {
        if (!isRotatingRock || crossRockGO == null)
            return;

        rockRotationProgress += Time.deltaTime * rockRotationSpeed;
        rockRotationProgress = Mathf.Clamp01(rockRotationProgress);

        Quaternion initialRot = UnityEngine.Quaternion.Euler(rockPushInitialAngle);
        Quaternion targetRot = UnityEngine.Quaternion.Euler(rockPushAngle);

        crossRockGO.transform.localRotation = UnityEngine.Quaternion.Lerp(
            initialRot,
            targetRot,
            rockRotationProgress
        );
    }

    // MOBILE UI CALLBACKS

    public void OnMoveLeftDown()
    {
        mobileHorizontalInput = -1f;
    }

    public void OnMoveRightDown()
    {
        mobileHorizontalInput = 1f;
    }

    public void OnMoveButtonUp()
    {
        mobileHorizontalInput = 0f;
    }

    void DisableAllMovements()
    {
        isMoving = false;
        isCasting = false;
        isDrinking = false;
        isPulling = false;
        isSleeping = false;
        pluck.Reset(); // was: isPlucking = false;
        pullAnimationComplete = false;
    }

    //=============
    // SLEEPING
    //=============

    #region SLEEPING
    public void StartSleeping(float duration)
    {
        if (!isSleeping && !isDrinking && !isCasting && !isGliding && !isPulling)
            StartCoroutine(SleepRoutine(duration));
    }

    private IEnumerator SleepRoutine(float duration)
    {
        isSleeping = true;
        isMoving = false;

        if (animator != null)
            animator.SetBool(IS_SLEEPING, true);

        transform.position += sleepOffset;
        rb.isKinematic = true;

        if (crossReferrence != null)
            crossReferrence.SetActive(false);

        Debug.Log($"Player is sleeping for {duration} seconds...");
        yield return new WaitForSeconds(duration);

        StopSleeping();
    }

    public void StopSleeping()
    {
        isSleeping = false;

        if (animator != null)
            animator.SetBool(IS_SLEEPING, false);

        transform.position -= sleepOffset;
        rb.isKinematic = false;

        if (crossReferrence != null)
            crossReferrence.SetActive(true);

        Debug.Log("Player woke up!");
    }

    public void AnimationEvent_EndSleeping()
    {
        Debug.Log("Sleep animation ended via Animation Event");
    }

    #endregion

    public void OnCastButtonDown()
    {
        mobileCastPressed = true;
    }

    public void OnCastButtonUp()
    {
        mobileCastPressed = false;
    }

    void GetInput()
    {
        if (useMobileControls)
            horizontalInput = mobileHorizontalInput;
        else
            horizontalInput = Input.GetAxisRaw("Horizontal");

        isMoving =
            !isBreathing
            && !isDrinking
            && !isCasting
            && !isGliding
            && !isSleeping
            && !pluck.isPlucking
            && !pluck.isEating
            && Mathf.Abs(horizontalInput) > 0.01f;
    }

    void HandleCastingInput()
    {
        bool castRequested = false;

        if (useMobileControls)
            castRequested = mobileCastPressed;
        else
            castRequested = Input.GetKeyDown(KeyCode.G);

        if (castRequested && !isCasting && !isBreathing && !isDrinking && !isGliding)
            StartCasting();

        mobileCastPressed = false;
    }

    void HandleDrinkingInput()
    {
        if (
            Input.GetKeyDown(KeyCode.K)
            && isNearWaterFountain
            && !isDrinking
            && !isCasting
            && !isBreathing
            && !isGliding
        )
            StartDrinking();
    }

    public void OnDrinkButtonDown()
    {
        if (
            isNearWaterFountain
            && !isDrinking
            && !isCasting
            && !isBreathing
            && !isGliding
            && !isPulling
        )
        {
            StartDrinking();
            Debug.Log("Drink button pressed - Starting drinking");
        }
    }

    void StartCasting()
    {
        isCasting = true;
        animator.SetBool(IS_CASTING, true);
        isMoving = false;
    }

    public void StopCasting()
    {
        isCasting = false;
        animator.SetBool(IS_CASTING, false);
    }

    void HandleMovement()
    {
        if (isMoving)
        {
            moveDirection = new Vector3(0, 0, horizontalInput);

            // Apply storm speed penalty
            float currentSpeed = stormData.isInStorm
                ? moveSpeed * stormData.playerSpeedModifier
                : moveSpeed;

            Vector3 intendedPosition =
                transform.position + (moveDirection * currentSpeed * Time.deltaTime);

            if (isBlockedByRock && IsMovingTowardRock(intendedPosition))
                return;

            if (isBlockedByFastStop && IsBlockedByFastStopDirection(horizontalInput))
                return;

            lastValidPosition = transform.position;
            transform.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.World);
        }
    }

    private bool IsMovingTowardRock(Vector3 intendedPosition)
    {
        Vector3 currentToRock = rockObstaclePosition - transform.position;
        Vector3 intendedToRock = rockObstaclePosition - intendedPosition;
        return intendedToRock.sqrMagnitude < currentToRock.sqrMagnitude;
    }

    private bool IsBlockedByFastStopDirection(float input)
    {
        if (input < 0 && fastStopBlockLeft)
        {
            Debug.Log("Movement blocked: FastStop blocking LEFT direction");
            return true;
        }

        if (input > 0 && fastStopBlockRight)
        {
            Debug.Log("Movement blocked: FastStop blocking RIGHT direction");
            return true;
        }

        return false;
    }

    public void OnFastStopTriggerEnter(Vector3 stopPosition, bool blockLeft, bool blockRight)
    {
        isBlockedByFastStop = true;
        fastStopPosition = stopPosition;
        fastStopBlockLeft = blockLeft;
        fastStopBlockRight = blockRight;

        string blockedDirections = "";
        if (blockLeft && blockRight)
            blockedDirections = "BOTH directions";
        else if (blockLeft)
            blockedDirections = "LEFT direction";
        else if (blockRight)
            blockedDirections = "RIGHT direction";
        else
            blockedDirections = "NO directions (FastStop inactive)";

        Debug.Log($"Entered FastStop zone - Blocking: {blockedDirections}");
    }

    public void OnFastStopTriggerExit()
    {
        isBlockedByFastStop = false;
        fastStopBlockLeft = false;
        fastStopBlockRight = false;
        Debug.Log("Exited FastStop zone - can move freely in all directions");
    }

    void HandleRotation()
    {
        if (isMoving)
        {
            Quaternion targetRotation;

            if (horizontalInput < 0)
                targetRotation = UnityEngine.Quaternion.Euler(0, 180, 0);
            else
                targetRotation = UnityEngine.Quaternion.Euler(0, 0, 0);

            transform.rotation = UnityEngine.Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                smoothRotation * Time.deltaTime
            );
        }
    }

    void HandleAnimation()
    {
        if (animator != null)
            animator.SetBool(IS_MOVING, isMoving);
    }

    void HandleRigWeight()
    {
        if (walkRig == null || armRig == null || glideRig == null)
            return;

        // When plucking, walkRig and armRig are suppressed; armPluckRig is handled by PluckZone coroutine
        if (
            isBreathing
            || isDrinking
            || isCasting
            || isGliding
            || isPulling
            || isSleeping
            || pluck.isPlucking
        )
            targetRigWeight = 0f;
        else
            targetRigWeight = 1f;

        walkRig.weight = Mathf.Lerp(
            walkRig.weight,
            targetRigWeight,
            Time.deltaTime * rigTransitionSpeed
        );
        armRig.weight = Mathf.Lerp(
            armRig.weight,
            targetRigWeight,
            Time.deltaTime * rigTransitionSpeed
        );
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("ActionTrigger"))
            return;

        if (other.name == "Water Fountain")
        {
            isNearWaterFountain = true;
            currentWaterFountain = other.gameObject;
            Debug.Log("Near water fountain - Press K to drink");
            canvasTrigger = other.GetComponent<CanvasTrigger>();
            canvasTrigger?.ActivateCanvas();

            if (drinkButton != null)
                drinkButton.interactable = true;
        }
        // Tree Zone is now fully handled by PluckZone.cs
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ActionTrigger") && other.name == "Water Fountain")
        {
            isNearWaterFountain = false;
            currentWaterFountain = null;
            Debug.Log("Left water fountain area");

            if (drinkButton != null)
                drinkButton.interactable = false;
        }
    }

    // =====================
    // EATING
    // =====================

    #region EATING

    public void OnEatButtonDown()
    {
        if (
            !pluck.isEating
            && !isDrinking
            && !isCasting
            && !isBreathing
            && !isGliding
            && !isPulling
        )
        {
            StartEating();
            Debug.Log("Eat button pressed - Starting eating");
        }
    }

    void StartEating()
    {
        pluck.isEating = true;
        animator.SetBool(IS_DRINKING, true); // reuse drink anim, or swap for a dedicated one
        isMoving = false;
        crossReferrence.SetActive(false);
        if (pluck.apple != null)
            pluck.apple.SetActive(true);
        Debug.Log("Started eating");
    }

    public void StopEating()
    {
        pluck.isEating = false;
        animator.SetBool(IS_DRINKING, false);
        crossReferrence.SetActive(true);
        if (pluck.apple != null)
            pluck.apple.SetActive(false);
    }

    public void AnimationEvent_EndEating()
    {
        if (pluck.hasTakenFruit == true)
        {
            Debug.Log("Eating animation ended");
            OnEatComplete();
        }
        else
        {
            return;
        }
    }

    private void OnEatComplete()
    {
        pluck.isEating = false;
        animator.SetBool(IS_DRINKING, false);
        if (pluck.apple != null)
            pluck.apple.SetActive(false);
        crossReferrence.SetActive(true);

        Debug.Log("Eating complete!");

        pluck.blockade.SetActive(false);

        if (pluck.eatButton != null)
            pluck.eatButton.gameObject.SetActive(false);
    }

    #endregion


    // =====================
    // PLUCK ZONE INTERFACE
    // =====================

    #region PLUCK ZONE INTERFACE

    /// <summary>
    /// Called by PluckZone when the player enters or exits the zone.
    /// </summary>
    public void SetInPluckZone(bool inZone, PluckZone zone)
    {
        pluck.isInPluckZone = inZone;
        pluck.currentPluckZone = zone;

        if (inZone)
        {
            // Full reset on re-entry
            pluck.hitScore = 0;
            pluck.hasTakenFruit = false;
            pluck.isPlucking = false;
            pluck.isPluckRigUp = false;

            if (armPluckRig != null)
                armPluckRig.weight = 1f;

            // Restore cross reference in case it was hidden
            if (crossReferrence != null && !crossReferrence.activeSelf)
                crossReferrence.SetActive(true);

            // Reset cross transform to initial position
            crossReferrence.transform.localPosition = initialTransformCrossOffset;
            crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(
                initialRotationCrossOffset
            );

            if (animator != null)
                animator.SetBool(PluckData.IS_PLUCK, false);

            InitRigBeforePluckCompletion();
        }
        else
        {
            pluck.ExitZone();
            InitRigAfterPluckCompletion();

            if (animator != null)
                animator.SetBool(PluckData.IS_PLUCK, false);

            if (armPluckRig != null)
                armPluckRig.weight = 0f;
        }
    }

    /// <summary>
    /// Called from the UI Pluck Button (Pointer Down).
    /// Starts the looping pluck animation and triggers rig weight rise/fall.
    /// </summary>
    public void OnPluckButtonDown()
    {
        if (
            !pluck.isInPluckZone
            || isDrinking
            || isCasting
            || isGliding
            || isPulling
            || isSleeping
            || isBreathing
        )
            return;

        pluck.hitScore++;

        if (pluck.hitScore == pluck.maxHitScore)
        {
            FruitZone fruitZone = GameObject.FindAnyObjectByType<FruitZone>();

            fruitZone.isFruitFallTrigger = true;
            pluck.eatButton.gameObject.SetActive(true);
            Debug.Log("Hit Score has been reached at " + pluck.hitScore);
            Debug.Log("Drop fruits");
            pluck.pluckButton.gameObject.SetActive(false);
            ResetPluckAnimationStateToDefault();
            InitRigAfterPluckCompletion();
        }
        crossReferrence.transform.localPosition = pluck.crossPluckOffset;
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(
            pluck.crossPluckRotationOffset
        );

        pluck.isPlucking = true;
        pluck.isPluckRigUp = false;
        isMoving = false;

        if (animator != null)
            animator.SetBool(PluckData.IS_PLUCK, true);

        StartCoroutine(PluckRigRoutine());
        CameraShake.Instance.ShakeMedium();
        Debug.Log("Pluck button pressed");
    }

    private IEnumerator PluckRigRoutine()
    {
        armPluckRig.weight = 1f;
        yield return null; // one frame to register
        pluck.isPluckRigUp = true; // hand off to Update()
    }

    void HandlePluckRigDrop()
    {
        if (!pluck.isPluckRigUp || armPluckRig == null)
            return;

        armPluckRig.weight = Mathf.Lerp(
            armPluckRig.weight,
            0f,
            Time.deltaTime * pluck.rigFallSpeed
        );

        if (armPluckRig.weight <= 0.01f)
        {
            armPluckRig.weight = 0f;
            pluck.isPluckRigUp = false;
            pluck.isPlucking = false;

            Debug.Log("Pluck cycle complete - ready for next press");

            if (pluck.hitScore == pluck.maxHitScore)
            {
                ResetPluckAnimationStateToDefault();
                InitRigAfterPluckCompletion();
            }
        }
    }

    private void InitRigAfterPluckCompletion()
    {
        rigBuilder.layers[0].active = true;
        rigBuilder.layers[1].active = true;
        rigBuilder.layers[2].active = true;
        rigBuilder.layers[3].active = false;

        rigBuilder.Build();
    }

    public void InitRigBeforePluckCompletion()
    {
        Debug.Log("InitRigBeforePluckCompletion() is called");
        rigBuilder.layers[0].active = false;
        rigBuilder.layers[1].active = false;
        rigBuilder.layers[2].active = false;
        rigBuilder.layers[3].active = true;
        rigBuilder.Build();
    }

    private void ResetPluckAnimationStateToDefault()
    {
        Debug.Log("ResetPluckAnimationStateToDefault() is called");
        if (animator != null)
            animator.SetBool(PluckData.IS_PLUCK, false);

        crossReferrence.transform.localPosition = initialTransformCrossOffset;
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(
            initialRotationCrossOffset
        );
    }

    public void PlayerHasFruitTaken()
    {
        Debug.Log("Player has taken fruit");
        pluck.hasTakenFruit = true;
    }

    #endregion


    // =====================
    // ROCK / PULL METHODS
    // =====================
    #region ROCK / PULL METHODS

    public void OnRockObstacleTriggerEnter(Vector3 rockPosition)
    {
        isBlockedByRock = true;
        rockObstaclePosition = rockPosition;
        canPull = true;
        Debug.Log("Near rock obstacle - Can now pull");
    }

    public void OnRockObstacleTriggerExit()
    {
        isBlockedByRock = false;
        canPull = false;
        isPulling = false;
        pullAnimationComplete = false;

        ResetRockRotation();

        if (crossRockGO != null && crossRockGO.activeInHierarchy)
            crossRockGO.SetActive(false);

        if (crossReferrence != null && !crossReferrence.activeInHierarchy)
            crossReferrence.SetActive(true);

        Debug.Log("Exited rock obstacle area - can move freely");
    }

    public void OnRockObstacleComplete()
    {
        isBlockedByRock = false;
        canPull = false;
        isPulling = false;
        pullAnimationComplete = false;

        ResetRockRotation();

        if (crossRockGO != null && crossRockGO.activeInHierarchy)
            crossRockGO.SetActive(false);

        if (crossReferrence != null && !crossReferrence.activeInHierarchy)
            crossReferrence.SetActive(true);

        Debug.Log("Rock obstacle cleared - Pull animation stopped");
    }

    public void OnPullButtonDown()
    {
        if (canPull && !isDrinking && !isCasting && !isGliding && !isBreathing)
        {
            isPulling = true;
            isMoving = false;
            pullAnimationComplete = false;

            animator.SetTrigger(PULL_TRIGGER);

            if (crossRockGO.activeInHierarchy == false)
            {
                crossRockGO.SetActive(true);
                crossReferrence.SetActive(false);
            }
            else
            {
                return;
            }

            Debug.Log("Pull triggered - ready for next pull");
        }
    }

    public void OnPullButtonUp()
    {
        Debug.Log("Pull button released");
    }

    public void OnPullButtonPressed()
    {
        if (canPull && !isPulling && !isDrinking && !isCasting && !isGliding && !isBreathing)
            StartPulling();
    }

    void HandlePulling() { }

    void StartPulling()
    {
        isPulling = true;
        isMoving = false;
        Debug.Log("Started pulling");
    }

    private void StartRockRotation()
    {
        isRotatingRock = true;
        rockRotationProgress = 0f;
    }

    public void ResetRockRotation()
    {
        Debug.Log("Resetting rock rotation");
        isRotatingRock = false;
        rockRotationProgress = 0f;

        if (crossRockGO != null)
            crossRockGO.transform.localRotation = UnityEngine.Quaternion.Euler(
                rockPushInitialAngle
            );
    }

    public void AnimationEvent_PullHit()
    {
        pullAnimationComplete = true;
        Debug.Log("Pull hit!");
    }

    public void AnimationEvent_EndPulling()
    {
        isPulling = false;
        ResetRockRotation();

        if (crossRockGO != null && crossRockGO.activeInHierarchy)
            crossRockGO.SetActive(false);

        if (crossReferrence != null && !crossReferrence.activeInHierarchy)
            crossReferrence.SetActive(true);

        Debug.Log("Pull animation ended");
    }

    public void ResetPullCompletion()
    {
        pullAnimationComplete = false;
    }

    #endregion


    // =====================
    // OASIS PARAMS
    // =====================

    #region SAIL PARAMS
    public void OasisZoneOnTrigger(CameraTrigger ct)
    {
        if (!isDrinking && !isBreathing && !isCasting)
        {
            StartSail();
            ct.enableEvents?.Invoke();
            sailData.sailCross.GetComponent<GlideTrigger>().IsPlayerGliding = true;
        }
    }

    public void OasisZoneOnExitTrigger(CameraTrigger ct)
    {
        if (!isDrinking && !isBreathing && !isCasting)
        {
            StopSail();
            ct.disableEvents?.Invoke();
            sailData.sailCross.GetComponent<GlideTrigger>().IsPlayerGliding = false;
        }
    }

    public void StartSail()
    {
        sailData.isSailing = true;
        animator.SetBool(IS_GLIDING, true);
        isMoving = false;
        crossReferrence.SetActive(false);
        InitSail();
    }

    public void StopSail()
    {
        sailData.isSailing = false;
        this.transform.parent = null;
        animator.SetBool(IS_GLIDING, false);
        crossReferrence.SetActive(true);
        sailData.sailCross.SetActive(false);
    }

    void InitSail()
    {
        sailData.sailCross.SetActive(true);
        this.transform.SetParent(sailData.sailCross.transform);
        glideRig.weight = 1;
    }

    #endregion


    // =====================
    // GLIDING
    // =====================

    #region GLIDING
    public void GlideZoneOnTrigger(CameraTrigger ct)
    {
        if (!isDrinking && !isBreathing && !isCasting)
        {
            StartGliding();
            ct.enableEvents?.Invoke();
            crossGliderGO.GetComponent<GlideTrigger>().IsPlayerGliding = true;
        }
    }

    public void StartGliding()
    {
        isGliding = true;
        animator.SetBool(IS_GLIDING, true);
        isMoving = false;
        crossReferrence.SetActive(false);
        InitGlider();
    }

    public void StopGliding()
    {
        isGliding = false;
        this.transform.parent = null;
        animator.SetBool(IS_GLIDING, false);
        glideRig.weight = 0; // ← add this too, was never reset

        crossReferrence.SetActive(true);

        // Reset cross back to original local position/rotation
        crossReferrence.transform.SetParent(handTransform);
        crossReferrence.transform.localPosition = initialTransformCrossOffset;
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(
            initialRotationCrossOffset
        );

        crossGliderGO.SetActive(false);
    }

    void InitGlider()
    {
        crossGliderGO.SetActive(true);
        this.transform.SetParent(crossGliderGO.transform);
        glideRig.weight = 1;
    }

    #endregion

    // =====================
    // DRINKING
    // =====================

    #region DRINKING

    void StartDrinking()
    {
        isDrinking = true;
        animator.SetBool(IS_DRINKING, true);
        isMoving = false;
        crossReferrence.SetActive(false);
        cupGO.SetActive(true);
        Debug.Log("Started drinking water");
    }

    public void StopDrinking()
    {
        isDrinking = false;
        animator.SetBool(IS_DRINKING, false);
        crossReferrence.SetActive(true);
        cupGO.SetActive(false);
    }

    public void AnimationEvent_EndDrinking()
    {
        Debug.Log("Drinking animation ended");
        currentWaterAmount++;
        Debug.Log($"Current water: {currentWaterAmount}/{canvasTrigger.drinkMax}");
        OnDrinkComplete();
    }

    private void OnDrinkComplete()
    {
        if (currentWaterAmount >= canvasTrigger.drinkMax)
        {
            Debug.Log("Max water reached! Drinking complete.");
            CompleteDrinking();
        }
        else
        {
            Debug.Log(
                $"Need more water. Press K to continue drinking. ({currentWaterAmount}/{canvasTrigger.drinkMax})"
            );
            crossReferrence.SetActive(true);
            isDrinking = false;
            animator.SetBool(IS_DRINKING, false);
        }
    }

    private void CompleteDrinking()
    {
        isDrinking = false;
        animator.SetBool(IS_DRINKING, false);
        cupGO.SetActive(false);
        crossReferrence.SetActive(true);
        currentWaterAmount = 0;
        isNearWaterFountain = false;

        Debug.Log("Drinking fully complete! Player can now move.");
        canvasTrigger.DeactivateCanvas();
        currentWaterAmount = 0;
        canvasTrigger = null;

        if (drinkButton != null)
            drinkButton.interactable = false;
    }

    private void OnDrinkingBenefits()
    {
        Debug.Log("Player received drinking benefits!");
    }

    #endregion

    // =====================
    // CASTING ANIMATION EVENTS
    // =====================

    #region CASTING ANIMATION EVENTS

    public void AnimationEvent_StartCasting()
    {
        crossReferrence.transform.SetParent(hitAnchor);
        crossReferrence.transform.localPosition = hitOffset;
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(hitRotationOffset);
    }

    public void AnimationEvent_CastFX()
    {
        crossCol.enabled = true;
        CameraShake.Instance.ShakeHeavy();
    }

    public void AnimationEvent_EndCasting()
    {
        CheckAndDamageEnemies();

        crossCol.enabled = false;
        isCasting = false;
        animator.SetBool(IS_CASTING, false);
        crossReferrence.transform.SetParent(handTransform);
        crossReferrence.transform.localPosition = initialTransformCrossOffset;
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(
            initialRotationCrossOffset
        );
    }

    private void CheckAndDamageEnemies()
    {
        if (crossHitCol == null)
        {
            Debug.LogWarning("CrossHitCol collider is not assigned!");
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(
            crossHitCol.transform.position,
            crossHitCol.radius * crossHitCol.transform.lossyScale.x,
            enemyLayer
        );

        if (hitColliders.Length > 0)
        {
            Debug.Log($"Hit {hitColliders.Length} enemies!");

            foreach (Collider enemyCollider in hitColliders)
            {
                IDamageable damageable = enemyCollider.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    damageable.TakeDamage(damageAmount);
                    Debug.Log($"Dealt {damageAmount} damage to {enemyCollider.gameObject.name}");
                }
                else
                {
                    var enemyHealth = enemyCollider.GetComponent<WolfFSM>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(damageAmount);
                        Debug.Log(
                            $"Dealt {damageAmount} damage to {enemyCollider.gameObject.name}"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Enemy {enemyCollider.gameObject.name} has no damage component!"
                        );
                    }
                }
            }
        }
        else
        {
            Debug.Log("No enemies hit.");
        }
    }

    #endregion


    // =====================
    // SANDSTORM
    // =====================

    #region SANDSTORM

    public void EnterSandStorm()
    {
        stormData.isInStorm = true;
        stormData.Reset();
        Debug.Log("Player entered sandstorm");
    }

    public void ExitSandStorm()
    {
        stormData.isInStorm = false;
        animator.speed = 1f; // Reset animation speed
        stormData.Reset();
        Debug.Log("Player exited sandstorm");
    }

    public void StandTheCross() { 
        stormData.stormCross.transform.localPosition = stormData.stormCrossTransformOffset;
        stormData.stormCross.transform.localRotation = UnityEngine.Quaternion.Euler(
            stormData.stormCrossTransformRotationOffset
        );
    }

    void HandleStorm()
    {
        if (!stormData.isInStorm)
            return;

        bool playerIsMoving = Mathf.Abs(horizontalInput) > 0.01f;
        animator.speed = playerIsMoving ? 1f : 0.4f; // Slow down animation when idle in storm

        if (playerIsMoving)
        {
            stormData.idleTimer = 0f;
            stormData.currentPushbackForce = 0f;
        }
        else
        {
            stormData.idleTimer += Time.deltaTime;

            if (stormData.idleTimer >= stormData.idleTimeBeforePushback)
            {
                stormData.currentPushbackForce = Mathf.MoveTowards(
                    stormData.currentPushbackForce,
                    stormData.maxPushbackForce,
                    stormData.pushbackAcceleration * Time.deltaTime
                );

                // Just translate the player — no rotation, no animation override
                Vector3 pushDirection = new Vector3(0, 0, stormData.pushbackDirection);
                transform.Translate(
                    pushDirection * stormData.currentPushbackForce * Time.deltaTime,
                    Space.World
                );

                Debug.Log($"Storm pushback force: {stormData.currentPushbackForce:F2}");
            }
        }
    }

    #endregion


    // =====================
    // DAMAGE SYSTEM
    // =====================

    #region DAMAGE SYSTEM
    public void OnDamagedTaken(float damage)
    {
        currentHealth -= (int)damage;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log($"Player took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        if (splashDamageImage != null)
        {
            StopAllCoroutines();
            StartCoroutine(DamageFlashEffect());
        }

        if (currentHealth == 0)
            OnPlayerDeath();
    }

    private IEnumerator DamageFlashEffect()
    {
        splashDamageImage.color = flashColor;

        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float fadeProgress = elapsedTime / flashDuration;
            float alpha = Mathf.Lerp(flashColor.a, 0f, fadeProgress);
            splashDamageImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }

        splashDamageImage.color = Color.clear;
    }

    void OnPlayerDeath()
    {
        Debug.Log("Player has died.");
    }

    #endregion
}

// Interface for damageable entities
public interface IDamageable
{
    void TakeDamage(float damage);
}

[System.Serializable]
public class PluckData
{
    public int hitScore;
    public int maxHitScore = 6;
    public bool isInPluckZone = false;
    public bool isPlucking = false;
    public bool isPluckRigUp = false;
    public float rigFallSpeed = 2f;

    public bool isEating = false;
    public bool hasTakenFruit = false;

    public Button pluckButton;
    public Button eatButton;

    public GameObject apple;

    public Vector3 crossPluckOffset;
    public Vector3 crossPluckRotationOffset;
    public GameObject blockade;

    [HideInInspector]
    public PluckZone currentPluckZone;

    public const string IS_PLUCK = "isPluck";

    public void Reset()
    {
        isPlucking = false;
        isPluckRigUp = false;
    }

    public void ExitZone()
    {
        isInPluckZone = false;
        currentPluckZone = null;
        Reset();
    }
}

[System.Serializable]
public class SailData
{
    public bool isSailing = false;
    public GameObject sailCross;
}

[System.Serializable]
public class StormData
{
    public bool isInStorm = false;
    public GameObject stormCross;

    [Header("Speed Settings")]
    public float playerSpeedModifier = 0.5f;

    [Header("Pushback Settings")]
    public float idleTimeBeforePushback = 2f; // seconds idle before pushback starts
    public float pushbackAcceleration = 1.5f; // how fast pushback force builds
    public float maxPushbackForce = 12f; // final blowaway velocity
    public float pushbackDirection = -1f; // -1 = left, 1 = right (storm wind direction)

    [Header("Storm Cross Settings")]
    public Vector3 stormCrossTransformOffset;
    public Vector3 stormCrossTransformRotationOffset;

    [HideInInspector]
    public float currentPushbackForce = 0f;

    [HideInInspector]
    public float idleTimer = 0f;

    public void Reset()
    {
        currentPushbackForce = 0f;
        idleTimer = 0f;
    }
}
