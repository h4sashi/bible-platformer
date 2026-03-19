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
    public float animatorSpeed;

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

    private bool isNearWaterFountain = false;
    private GameObject currentWaterFountain;

    private bool isBlockedByRock = false;
    private Vector3 rockObstaclePosition;
    private Vector3 lastValidPosition;

    private bool isBlockedByFastStop = false;
    private Vector3 fastStopPosition;
    private bool fastStopBlockLeft = false;
    private bool fastStopBlockRight = false;
    public RigBuilder rigBuilder;

    private const string MOVE_ANIMATION = "Walk";
    private const string IS_MOVING = "IsWalking";
    private const string IS_DRINKING = "IsDrinking";
    private const string IS_CASTING = "IsCasting";
    private const string IS_GLIDING = "IsGlide";
    private const string IS_CLIMBING = "IsClimbing";
    private const string IS_CLIMB_IDLE = "IsClimbIdle";
    private const string IS_CLIMB_TO_TOP = "IsClimbToTop";

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

    public bool IsPulling => isPulling;
    public bool IsPullAnimationComplete => pullAnimationComplete;

    [Header("Sleep Settings")]
    public Vector3 sleepOffset;
    private bool isSleeping;
    private const string IS_SLEEPING = "isSleeping";
    public bool IsSleeping => isSleeping;

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

    [Header("Climb Settings")]
    public ClimbData climbData = new ClimbData();

    [Header("Mountai Settings")]
    public MountainClimbData mountainClimbData = new MountainClimbData();

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

        if (
            !isBreathing
            && !isDrinking
            && !isCasting
            && !isGliding
            && !isPulling
            && !climbData.isInClimbZone
            && !mountainClimbData.isInClimbZone
        )
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
        HandleMountainClimbAlignment();

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
        if (stormData.isCrossStanding == true)
        {
            DisableAllMovements();
        }
        else
        {
            return;
        }

        if (isRotatingRock == false && crossRockGO != null)
            crossRockGO.transform.localRotation = UnityEngine.Quaternion.Euler(
                rockPushInitialAngle
            );

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

    // =====================
    // MOBILE UI CALLBACKS
    // =====================

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
        pluck.Reset();
        pullAnimationComplete = false;
    }

    // =====================
    // SLEEPING
    // =====================

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
            && !climbData.isInClimbZone // blocked during any climb state
            && !mountainClimbData.isInClimbZone
            && Mathf.Abs(horizontalInput) > 0.01f;
    }

    void HandleCastingInput()
    {
        bool castRequested = useMobileControls ? mobileCastPressed : Input.GetKeyDown(KeyCode.G);

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
            Debug.Log("Movement blocked: FastStop blocking LEFT");
            return true;
        }
        if (input > 0 && fastStopBlockRight)
        {
            Debug.Log("Movement blocked: FastStop blocking RIGHT");
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

        string dir =
            (blockLeft && blockRight) ? "BOTH"
            : blockLeft ? "LEFT"
            : blockRight ? "RIGHT"
            : "NONE";
        Debug.Log($"Entered FastStop zone - Blocking: {dir}");
    }

    public void OnFastStopTriggerExit()
    {
        isBlockedByFastStop = false;
        fastStopBlockLeft = false;
        fastStopBlockRight = false;
        Debug.Log("Exited FastStop zone");
    }

    void HandleRotation()
    {
        if (isMoving)
        {
            Quaternion targetRotation =
                horizontalInput < 0
                    ? UnityEngine.Quaternion.Euler(0, 180, 0)
                    : UnityEngine.Quaternion.Euler(0, 0, 0);

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

        // Rigs are fully suppressed while climbing (no rig needed)
        if (
            isBreathing
            || isDrinking
            || isCasting
            || isGliding
            || isPulling
            || isSleeping
            || pluck.isPlucking
            || climbData.isInClimbZone
            || mountainClimbData.isInClimbZone
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
    // CLIMBING
    // =====================

    #region CLIMBING

    /// <summary>
    /// Called by CameraTrigger when the player enters the ClimbUpZone.
    /// Automatically transitions to climb idle — no button press needed.
    /// </summary>
    public void ClimbUpZoneOnTriggerEnter()
    {
        if (isDrinking || isBreathing || isCasting || isGliding || isPulling || isSleeping)
            return;

        climbData.isInClimbZone = true;
        climbData.isPlayerClimbing = false;
        climbData.isHoldingClimb = false;

        // Freeze physics — climbing is fully animation-driven
        // rb.isKinematic = true;
        isMoving = false;
        animator.applyRootMotion = true;

        // Swap cross for the climb cross if assigned
        if (climbData.crossToClimbGO != null)
        {
            crossReferrence.SetActive(false);
            climbData.crossToClimbGO.SetActive(true);
        }

        // Optional snap to a fixed climb entry position/rotation
        if (climbData.positionOffset != Vector3.zero)
            transform.localPosition = climbData.positionOffset;

        if (climbData.rotationOffset != Vector3.zero)
            transform.localRotation = UnityEngine.Quaternion.Euler(climbData.rotationOffset);

        // Enter climb idle immediately
        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("ClimbUpZone entered — climb idle active. Hold climb button to climb.");
    }

    /// <summary>
    /// Called by CameraTrigger when the player exits via ClimbOffZone (reached the top).
    /// Restores the player to normal locomotion.
    /// </summary>
    ///
    /// <summary>
    /// Called by the UI Climb Button — Pointer Down.
    /// Switches from climb idle to the active climbing animation.
    /// </summary>
    public void OnClimbButtonDown()
    {
        if (!climbData.isInClimbZone || isDrinking || isCasting || isGliding || isBreathing)
            return;

        climbData.isHoldingClimb = true;
        climbData.isPlayerClimbing = true;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMBING, true);
        }

        Debug.Log("Climbing — hold to continue.");
    }

    /// <summary>
    /// Called by the UI Climb Button — Pointer Up.
    /// Returns to climb idle while still on the wall.
    /// </summary>
    public void OnClimbButtonUp()
    {
        // If ClimbToTop has already been triggered, ignore the button release entirely
        if (!climbData.isInClimbZone || climbData.hasReachedTop)
            return;

        climbData.isHoldingClimb = false;
        climbData.isPlayerClimbing = false;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("Climb button released — climb idle.");
    }

    public void ClimbOffZoneOnTriggerEnter()
    {
        if (!climbData.isInClimbZone)
            return;

        climbData.hasReachedTop = true; // ← guard against button-up interference
        climbData.isPlayerClimbing = false;
        climbData.isHoldingClimb = false;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMB_TO_TOP, true);
        }

        StartCoroutine(ClimbToTopRoutine());
        Debug.Log("ClimbOffZone reached — auto-playing ClimbToTop.");
    }

    private IEnumerator ClimbToTopRoutine()
    {
        // Wait one frame for animator to register the bool change
        yield return null;

        // Wait for ClimbToTop state to begin
        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("ClimbToTop")
        );

        // Wait for ClimbToTop to finish playing
        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("ClimbToTop")
            && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
        );

        // Snap player to the exit position once animation is done
        if (climbData.exitSnapTarget != null)
        {
            transform.position = climbData.exitSnapTarget.position;
            transform.rotation = climbData.exitSnapTarget.rotation;
            climbData.climbBtn.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "ClimbData: exitSnapTarget is not assigned — player will not be snapped."
            );
        }

        StopClimbing();
        Debug.Log("ClimbToTop complete — player snapped to exit position.");
    }

    private void StopClimbing()
    {
        climbData.isInClimbZone = false;
        climbData.isPlayerClimbing = false;
        climbData.isHoldingClimb = false;
        climbData.hasReachedTop = true;

        rb.isKinematic = false;
        animator.applyRootMotion = false;

        if (climbData.crossToClimbGO != null)
            climbData.crossToClimbGO.SetActive(false);

        if (crossReferrence != null)
            crossReferrence.SetActive(true);

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMB_TO_TOP, false);
        }

        Debug.Log("Climbing fully stopped — player free.");
    }

    /// <summary>
    /// Optional animation event — wire to footstep sounds or camera shake per climb step.
    /// </summary>
    public void AnimationEvent_ClimbStep()
    {
        Debug.Log("Climb step");
        // CameraShake.Instance.ShakeLight();
    }

    #endregion


    //========================
    //MOUNTAIN CLIMB DATA
    // ========================

    #region MOUNTAIN CLIMB


    /// <summary>
    /// Called by CameraTrigger when the player enters the ClimbUpZone.
    /// Automatically transitions to climb idle — no button press needed.
    /// </summary>
    public void MountainUpZoneOnTriggerEnter()
    {
        if (isDrinking || isBreathing || isCasting || isGliding || isPulling || isSleeping)
            return;

        mountainClimbData.isInClimbZone = true;
        mountainClimbData.isPlayerClimbing = false;
        mountainClimbData.isHoldingClimb = false;

        // Freeze physics — climbing is fully animation-driven
        // rb.isKinematic = true;
        isMoving = false;
        animator.applyRootMotion = true;

        // Swap cross for the climb cross if assigned
        if (mountainClimbData.crossToClimbGO != null)
        {
            crossReferrence.SetActive(false);
            mountainClimbData.crossToClimbGO.SetActive(true);
        }

        // Optional snap to a fixed climb entry position/rotation
        if (mountainClimbData.positionOffset != Vector3.zero)
            transform.localPosition = mountainClimbData.positionOffset;

        if (mountainClimbData.rotationOffset != Vector3.zero)
            transform.localRotation = UnityEngine.Quaternion.Euler(
                mountainClimbData.rotationOffset
            );

        // Enter climb idle immediately
        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("MountainUpZone entered — climb idle active. Hold climb button to climb.");
    }

    void HandleMountainClimbAlignment()
    {
        if (!mountainClimbData.isInClimbZone)
            return;

        // Smoothly push the player's Z position toward the ladder's Z position
        Vector3 pos = transform.position;
        pos.z = Mathf.Lerp(
            pos.z,
            mountainClimbData.ladderZPosition,
            Time.deltaTime * mountainClimbData.alignmentSpeed
        );
        transform.position = pos;
    }

    /// <summary>
    /// Called by CameraTrigger when the player exits via ClimbOffZone (reached the top).
    /// Restores the player to normal locomotion.
    /// </summary>
    ///
    /// <summary>
    /// Called by the UI Climb Button — Pointer Down.
    /// Switches from climb idle to the active climbing animation.
    /// </summary>
    public void OnMountainClimbButtonDown()
    {
        if (!mountainClimbData.isInClimbZone || isDrinking || isCasting || isGliding || isBreathing)
            return;

        mountainClimbData.isHoldingClimb = true;
        climbData.isPlayerClimbing = true;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMBING, true);
        }

        Debug.Log("Climbing — hold to continue.");
    }

    /// <summary>
    /// Called by the UI Climb Button — Pointer Up.
    /// Returns to climb idle while still on the wall.
    /// </summary>
    public void OnMountainClimbButtonUp()
    {
        // If ClimbToTop has already been triggered, ignore the button release entirely
        if (!mountainClimbData.isInClimbZone || mountainClimbData.hasReachedTop)
            return;

        mountainClimbData.isHoldingClimb = false;
        mountainClimbData.isPlayerClimbing = false;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("Climb button released — climb idle.");
    }

    public void MountainClimbOffZoneOnTriggerEnter()
    {
        if (!mountainClimbData.isInClimbZone)
            return;

        mountainClimbData.hasReachedTop = true; // ← guard against button-up interference
        mountainClimbData.isPlayerClimbing = false;
        mountainClimbData.isHoldingClimb = false;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMB_TO_TOP, true);
        }

        StartCoroutine(MountainClimbToTopRoutine());
        Debug.Log("MountainClimbOffZone reached — auto-playing MountainClimbToTop.");
    }

    private IEnumerator MountainClimbToTopRoutine()
    {
        // Wait one frame for animator to register the bool change
        yield return null;

        // Wait for ClimbToTop state to begin
        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("ClimbToTop")
        );

        // Wait for ClimbToTop to finish playing
        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("ClimbToTop")
            && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
        );

        // Snap player to the exit position once animation is done
        if (mountainClimbData.exitSnapTarget != null)
        {
            transform.position = mountainClimbData.exitSnapTarget.position;
            transform.rotation = mountainClimbData.exitSnapTarget.rotation;
            mountainClimbData.climbBtn.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "MountainClimbData: exitSnapTarget is not assigned — player will not be snapped."
            );
        }

        StopMountainClimbing();
        Debug.Log("MountainClimbToTop complete — player snapped to exit position.");
    }

    private void StopMountainClimbing()
    {
        mountainClimbData.isInClimbZone = false;
        mountainClimbData.isPlayerClimbing = false;
        mountainClimbData.isHoldingClimb = false;
        mountainClimbData.hasReachedTop = true;

        rb.isKinematic = false;
        animator.applyRootMotion = false;

        if (mountainClimbData.crossToClimbGO != null)
            mountainClimbData.crossToClimbGO.SetActive(false);

        if (crossReferrence != null)
            crossReferrence.SetActive(true);

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMB_TO_TOP, false);
        }

        Debug.Log("Climbing fully stopped — player free.");
    }
    #endregion
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
        animator.SetBool(IS_DRINKING, true);
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

    public void SetInPluckZone(bool inZone, PluckZone zone)
    {
        pluck.isInPluckZone = inZone;
        pluck.currentPluckZone = zone;

        if (inZone)
        {
            pluck.hitScore = 0;
            pluck.hasTakenFruit = false;
            pluck.isPlucking = false;
            pluck.isPluckRigUp = false;

            if (armPluckRig != null)
                armPluckRig.weight = 1f;

            if (crossReferrence != null && !crossReferrence.activeSelf)
                crossReferrence.SetActive(true);

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
        yield return null;
        pluck.isPluckRigUp = true;
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

        Debug.Log("Exited rock obstacle area");
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

        Debug.Log("Rock obstacle cleared");
    }

    public void OnPullButtonDown()
    {
        if (canPull && !isDrinking && !isCasting && !isGliding && !isBreathing)
        {
            isPulling = true;
            isMoving = false;
            pullAnimationComplete = false;

            animator.SetTrigger(PULL_TRIGGER);

            if (!crossRockGO.activeInHierarchy)
            {
                crossRockGO.SetActive(true);
                crossReferrence.SetActive(false);
            }

            Debug.Log("Pull triggered");
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
    // OASIS / SAIL
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
        glideRig.weight = 0;

        crossReferrence.SetActive(true);
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
            Debug.Log($"Need more water. ({currentWaterAmount}/{canvasTrigger.drinkMax})");
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

        Debug.Log("Drinking fully complete!");
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
        animator.speed = 0.4f;
        stormData.Reset();
        Debug.Log("Player entered sandstorm");
    }

    public void ExitSandStorm()
    {
        foreach (var item in stormData.disablePlayables)
            item.SetActive(false);

        stormData.isInStorm = false;
        animator.speed = 1f;
        stormData.Reset();
        Debug.Log("Player exited sandstorm");
    }

    public void StandTheCross()
    {
        if (stormData.bibleTrigger != null)
            stormData.bibleTrigger.CallTyping();
        else
            return;

        stormData.stormCross.transform.localPosition = stormData.stormCrossTransformOffset;
        stormData.stormCross.transform.localRotation = UnityEngine.Quaternion.Euler(
            stormData.stormCrossTransformRotationOffset
        );

        stormData.isCrossStanding = true;
        StartCoroutine(StormRoutine(stormData.timeTakenForStomeToBlowOver));
    }

    IEnumerator StormRoutine(float timeTaken)
    {
        Debug.Log("StormRoutine() called");
        stormData.stormObject2.SetActive(true);
        stormData.stormObject3.SetActive(true);
        stormData.isInStorm = false;

        StartCoroutine(StormShake(4));
        yield return new WaitForSeconds(timeTaken);

        stormData.isCrossStanding = false;
        stormData.stormObject1.SetActive(false);
        stormData.stormObject2.SetActive(false);
        stormData.stormObject3.SetActive(false);

        stormData.stormCross.transform.localPosition = initialTransformCrossOffset;
        stormData.stormCross.transform.localRotation = UnityEngine.Quaternion.Euler(
            initialRotationCrossOffset
        );

        ExitSandStorm();
        StopAllCoroutines();
    }

    IEnumerator StormShake(float x)
    {
        CameraShake.Instance.ShakeHeavy();
        yield return new WaitForSeconds(x);
        CameraShake.Instance.ShakeHeavy();
        yield return new WaitForSeconds(x);
        CameraShake.Instance.ShakeHeavy();
        yield return new WaitForSeconds(x);
        CameraShake.Instance.ShakeHeavy();
    }

    void HandleStorm()
    {
        if (!stormData.isInStorm)
            return;

        bool playerIsMoving = Mathf.Abs(horizontalInput) > 0.01f;
        animator.speed = 0.4f;

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

                Vector3 pushDirection = new Vector3(0, 0, stormData.pushbackDirection);
                transform.Translate(
                    pushDirection * stormData.currentPushbackForce * Time.deltaTime,
                    Space.World
                );
                Debug.Log($"Storm pushback: {stormData.currentPushbackForce:F2}");
            }
        }
    }

    #endregion

    // =====================
    // AFTER SANDSTORM
    // =====================

    #region AFTER SANDSTORM

    public void AfterEnterSandStorm()
    {
        stormData.isInStorm = true;
        animator.speed = 0.4f;
        stormData.Reset();
        Debug.Log("Player entered sandstorm");
    }

    public void AfterStormStartSleeping(float duration)
    {
        if (!isSleeping && !isDrinking && !isCasting && !isGliding && !isPulling)
            StartCoroutine(AfterStormSleepRoutine(duration));
    }

    private IEnumerator AfterStormSleepRoutine(float duration)
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

        StopStormSleeping();
    }

    public void StopStormSleeping()
    {
        isSleeping = false;

        if (animator != null)
            animator.SetBool(IS_SLEEPING, false);

        transform.position -= sleepOffset;
        rb.isKinematic = false;

        if (crossReferrence != null && !climbData.isInClimbZone)
            crossReferrence.SetActive(true);
        else
            crossReferrence.SetActive(false);

        Debug.Log("Player woke up!");
    }

    public void AnimationEvent_EndSleepingAfterStorm()
    {
        Debug.Log("Sleep animation ended via Animation Event");
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

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

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
            float alpha = Mathf.Lerp(flashColor.a, 0f, elapsedTime / flashDuration);
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

// =====================
// INTERFACES & DATA CLASSES
// =====================

public interface IDamageable
{
    void TakeDamage(float damage);
}

[System.Serializable]
public class PluckData
{
    public int hitScore = 0;
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
    public float idleTimeBeforePushback = 2f;
    public float pushbackAcceleration = 1.5f;
    public float maxPushbackForce = 12f;
    public float pushbackDirection = -1f;

    [Header("Storm Settings")]
    public GameObject stormObject1;
    public GameObject stormObject2;
    public GameObject stormObject3;
    public BibleTrigger bibleTrigger;
    public GameObject[] disablePlayables;

    public bool isCrossStanding = false;
    public float timeTakenForStomeToBlowOver = 18f;
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

[System.Serializable]
public class ClimbData
{
    public Button climbBtn;

    [Header("Climb Cross")]
    public GameObject crossToClimbGO; // Swap-in cross used during climbing

    [Header("Entry Snap")]
    public Vector3 positionOffset; // Optional position nudge on zone entry
    public Vector3 rotationOffset; // Optional rotation snap on zone entry

    [Header("ClimbToTop Exit Snap")]
    public Transform exitSnapTarget; // assign in Inspector — where the player lands after climbing

    [Header("State — read only at runtime")]
    public bool isInClimbZone = false;
    public bool isPlayerClimbing = false; // true only while button held
    public bool isHoldingClimb = false; // mirrors isPlayerClimbing, for external checks
    public bool hasReachedTop = false;
}

[System.Serializable]
public class MountainClimbData
{
    public Button climbBtn;

    [Header("Climb Cross")]
    public GameObject crossToClimbGO; // Swap-in cross used during climbing

    [Header("Entry Snap")]
    public Vector3 positionOffset; // Optional position nudge on zone entry
    public Vector3 rotationOffset; // Optional rotation snap on zone entry

    [Header("ClimbToTop Exit Snap")]
    public Transform exitSnapTarget; // assign in Inspector — where the player lands after climbing

    [Header("State — read only at runtime")]
    public bool isInClimbZone = false;
    public bool isPlayerClimbing = false; // true only while button held
    public bool isHoldingClimb = false; // mirrors isPlayerClimbing, for external checks
    public bool hasReachedTop = false;

    [Header("Ladder Alignment")]
    public float ladderZPosition; // the exact world Z position of the ladder
    public float alignmentSpeed = 10f; // how fast the player snaps to the ladder Z
}
