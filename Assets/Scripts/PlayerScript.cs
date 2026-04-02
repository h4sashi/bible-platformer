using System.Collections;
using System.Numerics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public partial class PlayerScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 5f;
    private Rigidbody rb;
    [SerializeField]private Vector3 initialPlayerScale;

    [Header("UI Buttons")]
    public Button drinkButton;
    public Button pullButton;
    public Button SandStormbutton;

    [SerializeField]
    private float smoothRotation = 10f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;
    public float animatorSpeed;

    [Header("Animation Rigging")]
    public Rig walkRig;
    public Rig armRig;
    public Rig armPluckRig;

    [Header("Glide Settings")]
    public GlideData glideData = new GlideData();
    public GlideData mountainGlideData = new GlideData();

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

    private const string IS_NO_CROSS_WALK = "IsNoCrossWalk";
    private const string IS_NO_CROSS_IDLE = "IsNoCrossIdle";
    private bool isNoCrossMoving;
    private const string LEDGE_TRIGGER = "LedgeTrigger";

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

    [Header("Eat Settings")]
    public GameObject eatBlockade;

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

    [Header("Mountain Settings")]
    public MountainClimbData mountainClimbData = new MountainClimbData();

    [Header("Ledge Settings")]
    public LedgeZoneData ledgeZoneData = new LedgeZoneData();

    [Header("Second Ledge Settings")]
    public LedgeZoneData secondLedgeZoneData = new LedgeZoneData();

    // =====================
    // GROUNDING SETTINGS
    // =====================

    [Header("Grounding Settings")]
    [SerializeField]
    private float groundCheckDistance = 0.3f;

    [SerializeField]
    private float gravityForce = 20f;

    [SerializeField]
    private LayerMask groundLayer;
    private bool isGrounded;
    private float originalMoveSpeed; // add this line

    // =====================
    // VFX SETTINGS
    // =====================

    [Header("VFX Settings")]
    public ParticleSystem slashVFX;
    public ParticleSystem hitVFX;
    public Transform vfxSpawnPoint; // assign a transform near the cross/hand

    // =====================
    // LIFECYCLE
    // =====================

    private void Awake()
    {
        glideData.glideRig.weight = 0;
        currentHealth = maxHealth;
        originalMoveSpeed = moveSpeed;
        isSleeping = false;
        rb = GetComponent<Rigidbody>();

        if (splashDamageImage != null)
            splashDamageImage.color = Color.clear;

        lastValidPosition = transform.position;

        if (crossRockGO != null)
            crossRockGO.transform.localRotation = UnityEngine.Quaternion.Euler(
                rockPushInitialAngle
            );
        // if (drinkButton != null)
        //     drinkButton.SetA = false;
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

        initialPlayerScale = transform.localScale;
    }

    // =====================
    // UPDATE
    // =====================

    void Update()
    {
        GetInput();
        HandlePluckRigDrop();

        if (isNearWaterFountain)
        {
            isMoving = false;
            horizontalInput = 0f;
        }

        // Either ledge's active state blocks all movement
        bool anyLedgeActive = ledgeZoneData.isLedgeActive || secondLedgeZoneData.isLedgeActive;

        // Either ledge's no-cross state routes to no-cross movement
        bool anyNoCrossWalk = ledgeZoneData.isNoCrossWalk || secondLedgeZoneData.isNoCrossWalk;

        if (
            !isBreathing
            && !isDrinking
            && !isCasting
            && !isGliding
            && !isPulling
            && !climbData.isInClimbZone
            && !mountainClimbData.isInClimbZone
            && !anyLedgeActive
        )
        {
            if (!anyNoCrossWalk)
            {
                HandleMovement();
                HandleRotation();
            }
            else
            {
                HandleNoCrossMovement();
            }
        }

        HandleAnimation();
        HandleNoCrossAnimation();
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

        if (mountainClimbData.isInClimbZone)
            transform.rotation = UnityEngine.Quaternion.Euler(mountainClimbData.climbSnapRotation);

        if (climbData.isInClimbZone)
            transform.rotation = UnityEngine.Quaternion.Euler(climbData.climbSnapRotation);
    }

    // =====================
    // PHYSICS
    // =====================

    private void FixedUpdate()
    {
        CheckGrounded();
        ApplyGravity();
    }

    private void CheckGrounded()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        isGrounded = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer
        );

        Debug.DrawRay(
            rayOrigin,
            Vector3.down * (groundCheckDistance + 0.1f),
            isGrounded ? Color.green : Color.red
        );
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * gravityForce, ForceMode.Acceleration);
        }
    }

    // =====================
    // INPUT
    // =====================

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
            && !climbData.isInClimbZone
            && !mountainClimbData.isInClimbZone
            && !ledgeZoneData.isLedgeActive
            && !ledgeZoneData.isNoCrossWalk
            && !secondLedgeZoneData.isLedgeActive
            && !secondLedgeZoneData.isNoCrossWalk
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

    void HandlePullingInput()
    {
        if (Input.GetKeyDown(KeyCode.P) && canPull)
            OnPullButtonDown();

        if (Input.GetKeyUp(KeyCode.P))
            OnPullButtonUp();
    }

    // =====================
    // MOVEMENT
    // =====================

    void HandleMovement()
    {
        if (isMoving)
        {
            moveDirection = new Vector3(0, 0, horizontalInput);

            float currentSpeed = stormData.isInStorm
                ? moveSpeed * stormData.playerSpeedModifier
                : moveSpeed;

            Vector3 intendedPosition =
                transform.position + moveDirection * currentSpeed * Time.deltaTime;

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
            Debug.Log("FastStop blocking LEFT");
            return true;
        }
        if (input > 0 && fastStopBlockRight)
        {
            Debug.Log("FastStop blocking RIGHT");
            return true;
        }
        return false;
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

    // =====================
    // VFX ANIMATION EVENTS
    // =====================

    #region VFX ANIMATION EVENTS

    /// <summary>
    /// Call this Animation Event at the moment the cross swings/raises.
    /// Works for both cast and pull animations.
    /// </summary>
    public void AnimationEvent_PlaySlashVFX()
    {
        if (slashVFX == null)
        {
            Debug.LogWarning("SlashVFX is not assigned!");
            return;
        }

        // Spawn at vfxSpawnPoint if assigned, otherwise at player position
        if (vfxSpawnPoint != null)
        {
            slashVFX.transform.position = vfxSpawnPoint.position;
            slashVFX.transform.rotation = vfxSpawnPoint.rotation;
        }

        slashVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        slashVFX.Play();
        Debug.Log("Slash VFX played");
    }

    /// <summary>
    /// Call this Animation Event at the moment of impact.
    /// Works for both cast and pull animations.
    /// </summary>
    public void AnimationEvent_PlayHitVFX()
    {
        if (hitVFX == null)
        {
            Debug.LogWarning("HitVFX is not assigned!");
            return;
        }

        if (vfxSpawnPoint != null)
        {
            hitVFX.transform.position = vfxSpawnPoint.position;
            hitVFX.transform.rotation = vfxSpawnPoint.rotation;
        }

        hitVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        hitVFX.Play();
        Debug.Log("Hit VFX played");
    }

    /// <summary>
    /// Stops both VFX — call if needed on animation exit or interruption.
    /// </summary>
    public void AnimationEvent_StopAllVFX()
    {
        slashVFX?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        hitVFX?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    #endregion


    // =====================
    // ANIMATION & RIGS
    // =====================

    void HandleAnimation()
    {
        if (animator == null)
            return;

        bool anyNoCrossWalk = ledgeZoneData.isNoCrossWalk || secondLedgeZoneData.isNoCrossWalk;
        animator.SetBool(IS_MOVING, isMoving && !anyNoCrossWalk);
    }

    void HandleRigWeight()
    {
        if (walkRig == null || armRig == null || glideData.glideRig == null)
            return;

        targetRigWeight =
            (
                isBreathing
                || isDrinking
                || isCasting
                || isGliding
                || isPulling
                || isSleeping
                || pluck.isPlucking
                || climbData.isInClimbZone
                || mountainClimbData.isInClimbZone
                || ledgeZoneData.isLedgeActive
                || ledgeZoneData.isNoCrossWalk
                || secondLedgeZoneData.isLedgeActive
                || secondLedgeZoneData.isNoCrossWalk
            )
                ? 0f
                : 1f;

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

    // =====================
    // FAST STOP
    // =====================

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

    // =====================
    // TRIGGER DETECTION
    // =====================

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
                drinkButton.gameObject.SetActive(true);
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
                drinkButton.gameObject.SetActive(false);
        }
    }

    // =====================
    // CASTING
    // =====================

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

    // =====================
    // MOBILE CALLBACKS
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

    public void OnCastButtonDown()
    {
        mobileCastPressed = true;
    }

    public void OnCastButtonUp()
    {
        mobileCastPressed = false;
    }

    // =====================
    // UTILITIES
    // =====================

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
    // ROCK ROTATION
    // =====================

    void HandleRockRotation()
    {
        if (!isRotatingRock || crossRockGO == null)
            return;

        rockRotationProgress += Time.deltaTime * rockRotationSpeed;
        rockRotationProgress = Mathf.Clamp01(rockRotationProgress);

        crossRockGO.transform.localRotation = UnityEngine.Quaternion.Lerp(
            UnityEngine.Quaternion.Euler(rockPushInitialAngle),
            UnityEngine.Quaternion.Euler(rockPushAngle),
            rockRotationProgress
        );
    }

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

        if (pullButton != null)
            pullButton.gameObject.SetActive(false);

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
            pullButton.interactable = false;
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
        pullButton.interactable = true;

        if (crossRockGO != null)
            crossRockGO.transform.localRotation = UnityEngine.Quaternion.Euler(
                rockPushInitialAngle
            );
    }

    public void AnimationEvent_PullHit()
    {
        pullAnimationComplete = true;
        AnimationEvent_PlayHitVFX(); // hit fires on impact
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
        // pullButton.interactable = true;
    }

    public void ResetPullCompletion()
    {
        pullAnimationComplete = false;
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
        AnimationEvent_PlaySlashVFX(); // slash fires at swing moment
    }

    public void AnimationEvent_EndCasting()
    {
        CheckAndDamageEnemies();
        AnimationEvent_PlayHitVFX(); // hit fires when damage is dealt

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

        SandStormbutton.gameObject.SetActive(false);
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

        // Don't apply storm effects during climbing
        if (climbData.isInClimbZone || mountainClimbData.isInClimbZone)
        {
            animator.speed = 1f;
            return;
        }

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

    // Add this field to PluckData class
    public bool isPluckAnimating = false; // true while animation is playing
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
        isPluckAnimating = false; // add this
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
    public GameObject crossToClimbGO;

    [Header("Climb Speed")]
    public float climbSpeed = 1.5f;

    [Header("Entry Snap")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("ClimbToTop Exit Snap")]
    public Transform exitSnapTarget;

    [Header("State — read only at runtime")]
    public bool isInClimbZone = false;
    public bool isPlayerClimbing = false;
    public bool isHoldingClimb = false;
    public bool hasReachedTop = false;

    [Header("Ladder Alignment")]
    public Transform ladderTransform;
    public Vector3 ladderAlignOffset;
    public Vector3 climbSnapRotation;
}

[System.Serializable]
public class MountainClimbData
{
    public Button climbBtn;

    [Header("Climb Cross")]
    public GameObject crossToClimbGO;

    [Header("Climb Speed")]
    public float climbSpeed = 1.5f;

    [Header("Climb Direction (normalized slope vector, e.g. 0, 0.8, 0.6 for a slope)")]
    public Vector3 climbDirection = new Vector3(0f, 0.8f, 0.6f); // tune in Inspector

    [Header("Entry Snap")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("ClimbToTop Exit Snap")]
    public Transform exitSnapTarget;

    [Header("State — read only at runtime")]
    public bool isInClimbZone = false;
    public bool isPlayerClimbing = false;
    public bool isHoldingClimb = false;
    public bool hasReachedTop = false;

    [Header("Ladder Alignment")]
    public Transform ladderTransform;
    public Vector3 ladderAlignOffset;
    public Vector3 climbSnapRotation;
}

[System.Serializable]
public class LedgeZoneData
{
    public Button ledgeBtn;
    public GameObject crossLedge;
    public GameObject crossLedgeDefault;
    public GameObject blockade;
    public GameObject descriptiveCanvas;
    public Vector3 crossLedgeFinalPosition;

    public bool isLedgeActive = false;
    public bool isLedgeFinished = false;
    public bool isNoCrossWalk = false;
}

[System.Serializable]
public class GlideData
{
    [Header("ID")]
    public string id = "default"; // e.g. "glide1", "mountain", "oasis"

    [Header("Glider Cross")]
    public GameObject gliderCross;

    [Header("Rig")]
    public Rig glideRig;

    [Header("State — read only at runtime")]
    public bool isGliding = false;
}
