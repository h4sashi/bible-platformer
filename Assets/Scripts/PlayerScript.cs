using System.Collections;
using System.Numerics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public partial class PlayerScript : MonoBehaviour
{
    //when player is sliding down the slope he should maintain
    //descend without any player input and whatnot

    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 5f;
    private Rigidbody rb;

    [SerializeField]
    private Vector3 initialPlayerScale;

    [Header("UI Buttons")]
    public Button hitButton;
    public Button drinkButton;
    public Button pullButton;
    public Button sprintButton;
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

    [SerializeField]
    private Image healthBar;

    [SerializeField]
    private RectTransform healthBarRect;
    public Image splashDamageImage;
    public float flashDuration = 0.5f;
    public Color flashColor = new Color(1f, 0f, 0f, 0.5f);

    [SerializeField]
    private float healthRegenerationInterval = 5f;

    [SerializeField]
    [Range(0f, 1f)]
    private float healthRegenerationPercent = 0.05f;
    private float healthBarFullWidth;
    private Coroutine damageFlashRoutine;
    private float healthRegenerationTimer;
    private bool isDead;

    [Header("Player Attack Settings")]
    public SphereCollider crossHitCol;
    public LayerMask enemyLayer;
    public float damageAmount = 25f;

    [SerializeField]
    private float wolfAttackFacingSearchRadius = 15f;

    [SerializeField]
    private float wolfAttackFacingRotationSpeed = 25f;
    private Transform currentAttackTarget;

    [Header("Mobile Controls")]
    [SerializeField]
    private bool useMobileControls = false;
    private float mobileHorizontalInput;
    private bool mobileCastPressed = false;

    [Header("Sprint Settings")]
    [SerializeField]
    private KeyCode sprintKey = KeyCode.LeftShift;

    [SerializeField]
    private float sprintSpeedMultiplier = 1.9f;

    [SerializeField]
    private float sprintAnimationSpeedMultiplier = 1.35f;

    [SerializeField]
    private float sprintDuration = 5f;

    [SerializeField]
    private float sprintCooldown = 4f;

    [SerializeField]
    private Color sprintActiveColor = new Color(0.25f, 0.8f, 1f, 1f);

    [SerializeField]
    private Color sprintCooldownColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    private bool mobileSprintHeld = false;
    private bool isSprinting = false;
    private bool sprintInputLockedUntilRelease = false;
    private bool wasSprintRequested = false;
    private bool sprintAdjustedAnimatorSpeed = false;
    private float sprintDirectionInput = 1f;
    private float sprintTimeRemaining;
    private float sprintCooldownRemaining;
    private Graphic sprintButtonGraphic;
    private Image sprintButtonImage;
    private Color sprintButtonDefaultColor = Color.white;
    private bool hasSprintButtonDefaultColor;
    private bool sprintButtonEventsConfigured;

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
        // PlayerPrefs.DeleteAll();

        glideData.glideRig.weight = 0;
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        isDead = false;
        healthRegenerationInterval = Mathf.Max(0.01f, healthRegenerationInterval);
        healthRegenerationTimer = healthRegenerationInterval;
        originalMoveSpeed = moveSpeed;
        CacheHealthBar();
        UpdateHealthBar();
        NormalizeSprintSettings();
        sprintDirectionInput = GetFacingMoveDirection();
        sprintTimeRemaining = sprintDuration;
        isSleeping = false;
        rb = GetComponent<Rigidbody>();
        ApplySavedCheckpointOnSceneLoad();
        CacheSprintButtonVisuals();
        ConfigureSprintButtonEvents();
        UpdateSprintButtonVisual();

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

        HandleSprint();

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
        HandleClimbVelocity();
        HandleMountainClimbVelocity();
        HandleAttackFacing();
        HandleHealthRegeneration();

        if (isGliding == true)
        {
            this.transform.localRotation = UnityEngine.Quaternion.Euler(0, 0f, 0);
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

            float currentSpeed = GetCurrentMoveSpeed();

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

    private float GetCurrentMoveSpeed()
    {
        float currentSpeed = stormData.isInStorm
            ? moveSpeed * stormData.playerSpeedModifier
            : moveSpeed;

        return isSprinting ? currentSpeed * sprintSpeedMultiplier : currentSpeed;
    }

    private void NormalizeSprintSettings()
    {
        sprintSpeedMultiplier = Mathf.Max(1f, sprintSpeedMultiplier);
        sprintAnimationSpeedMultiplier = Mathf.Max(1f, sprintAnimationSpeedMultiplier);
        sprintDuration = Mathf.Max(0.01f, sprintDuration);
        sprintCooldown = Mathf.Max(0f, sprintCooldown);
    }

    private void HandleSprint()
    {
        NormalizeSprintSettings();
        UpdateSprintCooldown();

        bool sprintRequested = IsSprintRequested();
        if (sprintRequested && !wasSprintRequested)
            CaptureSprintDirection();

        if (!sprintRequested)
            sprintInputLockedUntilRelease = false;

        isSprinting =
            sprintRequested
            && !sprintInputLockedUntilRelease
            && sprintCooldownRemaining <= 0f
            && sprintTimeRemaining > 0f
            && CanSprintInCurrentState();

        if (isSprinting)
        {
            horizontalInput = sprintDirectionInput;
            isMoving = true;
            sprintTimeRemaining -= Time.deltaTime;

            if (sprintTimeRemaining <= 0f)
                StartSprintCooldown();
        }

        wasSprintRequested = sprintRequested;
        UpdateSprintButtonVisual();
    }

    private bool IsSprintRequested()
    {
        return mobileSprintHeld || Input.GetKey(sprintKey);
    }

    private void CaptureSprintDirection()
    {
        sprintDirectionInput = GetInputMoveDirection();
    }

    private float GetInputMoveDirection()
    {
        if (Mathf.Abs(horizontalInput) > 0.01f)
            return Mathf.Sign(horizontalInput);

        float rawInput = useMobileControls ? mobileHorizontalInput : Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(rawInput) > 0.01f)
            return Mathf.Sign(rawInput);

        return GetFacingMoveDirection();
    }

    private float GetFacingMoveDirection()
    {
        return Vector3.Dot(transform.forward, Vector3.forward) >= 0f ? 1f : -1f;
    }

    private bool CanSprintInCurrentState()
    {
        return !isBreathing
            && !isDrinking
            && !isCasting
            && !isGliding
            && !isSleeping
            && !isPulling
            && !isNearWaterFountain
            && !sailData.isSailing
            && !stormData.isCrossStanding
            && !pluck.isPlucking
            && !pluck.isEating
            && !climbData.isInClimbZone
            && !mountainClimbData.isInClimbZone
            && !ledgeZoneData.isLedgeActive
            && !ledgeZoneData.isNoCrossWalk
            && !secondLedgeZoneData.isLedgeActive
            && !secondLedgeZoneData.isNoCrossWalk;
    }

    private void UpdateSprintCooldown()
    {
        if (sprintCooldownRemaining <= 0f)
            return;

        sprintCooldownRemaining -= Time.deltaTime;

        if (sprintCooldownRemaining <= 0f)
        {
            sprintCooldownRemaining = 0f;
            sprintTimeRemaining = Mathf.Max(0.01f, sprintDuration);
        }
    }

    private void StartSprintCooldown()
    {
        isSprinting = false;
        mobileSprintHeld = false;
        sprintInputLockedUntilRelease = true;
        wasSprintRequested = false;
        sprintTimeRemaining = 0f;

        sprintCooldownRemaining = Mathf.Max(0f, sprintCooldown);
        if (sprintCooldownRemaining <= 0f)
            sprintTimeRemaining = Mathf.Max(0.01f, sprintDuration);
    }

    private void CacheSprintButtonVisuals()
    {
        if (sprintButton == null)
        {
            GameObject sprintButtonObject = GameObject.Find("SprintButton");
            if (sprintButtonObject != null)
                sprintButton = sprintButtonObject.GetComponent<Button>();
        }

        if (sprintButton == null)
            return;

        sprintButtonGraphic = sprintButton.targetGraphic;
        if (sprintButtonGraphic == null)
            sprintButtonGraphic = sprintButton.GetComponent<Graphic>();

        sprintButtonImage = sprintButton.GetComponent<Image>();

        if (sprintButtonGraphic != null && !hasSprintButtonDefaultColor)
        {
            sprintButtonDefaultColor = sprintButtonGraphic.color;
            hasSprintButtonDefaultColor = true;
        }
    }

    private void ConfigureSprintButtonEvents()
    {
        if (sprintButton == null || sprintButtonEventsConfigured)
            return;

        EventTrigger sprintEventTrigger = sprintButton.GetComponent<EventTrigger>();
        if (sprintEventTrigger == null)
            sprintEventTrigger = sprintButton.gameObject.AddComponent<EventTrigger>();

        AddSprintButtonEvent(
            sprintEventTrigger,
            EventTriggerType.PointerDown,
            _ => OnSprintButtonDown()
        );
        AddSprintButtonEvent(
            sprintEventTrigger,
            EventTriggerType.PointerUp,
            _ => OnSprintButtonUp()
        );
        AddSprintButtonEvent(
            sprintEventTrigger,
            EventTriggerType.PointerExit,
            _ => OnSprintButtonUp()
        );
        sprintButtonEventsConfigured = true;
    }

    private void AddSprintButtonEvent(
        EventTrigger sprintEventTrigger,
        EventTriggerType eventType,
        UnityEngine.Events.UnityAction<BaseEventData> callback
    )
    {
        EventTrigger.Entry entry = sprintEventTrigger.triggers.Find(triggerEntry =>
            triggerEntry.eventID == eventType
        );

        if (entry == null)
        {
            entry = new EventTrigger.Entry { eventID = eventType };
            sprintEventTrigger.triggers.Add(entry);
        }

        entry.callback.AddListener(callback);
    }

    private void UpdateSprintButtonVisual()
    {
        if (sprintButton == null)
            CacheSprintButtonVisuals();

        ConfigureSprintButtonEvents();

        if (sprintButton == null)
            return;

        bool isCoolingDown = sprintCooldownRemaining > 0f;
        sprintButton.interactable = !isCoolingDown;

        if (sprintButtonGraphic != null)
        {
            if (isCoolingDown)
                sprintButtonGraphic.color = sprintCooldownColor;
            else if (isSprinting)
                sprintButtonGraphic.color = sprintActiveColor;
            else
                sprintButtonGraphic.color = sprintButtonDefaultColor;
        }

        if (sprintButtonImage != null)
        {
            if (isCoolingDown && sprintCooldown > 0f)
                sprintButtonImage.fillAmount = 1f - (sprintCooldownRemaining / sprintCooldown);
            else if (sprintDuration > 0f)
                sprintButtonImage.fillAmount = Mathf.Clamp01(sprintTimeRemaining / sprintDuration);
            else
                sprintButtonImage.fillAmount = 1f;
        }
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
        Vector3 effectPosition =
            vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
        GameAudioStarter.PlaySmashSwing(effectPosition);

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
        Vector3 effectPosition =
            vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
        GameAudioStarter.PlaySmashHit(effectPosition);

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
        bool shouldPlayMoveAnimation = (isMoving || isSprinting) && !anyNoCrossWalk;
        animator.SetBool(IS_MOVING, shouldPlayMoveAnimation);
        UpdateSprintAnimationSpeed(shouldPlayMoveAnimation);
    }

    private void UpdateSprintAnimationSpeed(bool shouldPlayMoveAnimation)
    {
        if (animator == null || stormData.isInStorm)
            return;

        if (isSprinting && shouldPlayMoveAnimation)
        {
            animator.speed = sprintAnimationSpeedMultiplier;
            sprintAdjustedAnimatorSpeed = true;
        }
        else if (sprintAdjustedAnimatorSpeed)
        {
            animator.speed = 1f;
            sprintAdjustedAnimatorSpeed = false;
        }
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
            if (hitButton != null)
                hitButton.gameObject.SetActive(false);
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
            if (hitButton != null)
                hitButton.gameObject.SetActive(true);
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
        currentAttackTarget = FindNearestLivingWolf();
        FaceAttackTarget();
        isCasting = true;
        animator.SetBool(IS_CASTING, true);
        isMoving = false;
    }

    public void StopCasting()
    {
        isCasting = false;
        currentAttackTarget = null;
        animator.SetBool(IS_CASTING, false);
    }

    private void HandleAttackFacing()
    {
        if (!isCasting)
            return;

        if (!IsValidAttackTarget(currentAttackTarget))
            currentAttackTarget = FindNearestLivingWolf();

        FaceAttackTarget();
    }

    private void FaceAttackTarget()
    {
        if (!IsValidAttackTarget(currentAttackTarget))
            return;

        float zDirection = currentAttackTarget.position.z - transform.position.z;
        if (Mathf.Abs(zDirection) <= 0.01f)
            return;

        Quaternion targetRotation =
            zDirection < 0f
                ? UnityEngine.Quaternion.Euler(0, 180, 0)
                : UnityEngine.Quaternion.Euler(0, 0, 0);

        transform.rotation = UnityEngine.Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            wolfAttackFacingRotationSpeed * Time.deltaTime
        );
    }

    private Transform FindNearestLivingWolf()
    {
        float searchRadius = Mathf.Max(0f, wolfAttackFacingSearchRadius);
        if (searchRadius <= 0f)
            return null;

        Collider[] wolfColliders = Physics.OverlapSphere(
            transform.position,
            searchRadius,
            enemyLayer
        );

        Transform nearestWolf = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (Collider wolfCollider in wolfColliders)
        {
            WolfFSM wolf = wolfCollider.GetComponentInParent<WolfFSM>();
            if (wolf == null || wolf.IsDead)
                continue;

            float distanceSqr = (wolf.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr >= nearestDistanceSqr)
                continue;

            nearestDistanceSqr = distanceSqr;
            nearestWolf = wolf.transform;
        }

        if (nearestWolf != null)
            return nearestWolf;

        WolfFSM[] wolves = UnityEngine.Object.FindObjectsByType<WolfFSM>(FindObjectsSortMode.None);
        float searchRadiusSqr = searchRadius * searchRadius;

        foreach (WolfFSM wolf in wolves)
        {
            if (wolf == null || wolf.IsDead)
                continue;

            float distanceSqr = (wolf.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr > searchRadiusSqr || distanceSqr >= nearestDistanceSqr)
                continue;

            nearestDistanceSqr = distanceSqr;
            nearestWolf = wolf.transform;
        }

        return nearestWolf;
    }

    private bool IsValidAttackTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        WolfFSM wolf = target.GetComponent<WolfFSM>();
        return wolf != null && !wolf.IsDead;
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

    public void OnSprintButtonDown()
    {
        CaptureSprintDirection();
        mobileSprintHeld = true;
    }

    public void OnSprintButtonUp()
    {
        mobileSprintHeld = false;
        sprintInputLockedUntilRelease = false;
        wasSprintRequested = false;
    }

    // =====================
    // UTILITIES
    // =====================

    void DisableAllMovements()
    {
        isMoving = false;
        isCasting = false;
        currentAttackTarget = null;
        isDrinking = false;
        isPulling = false;
        isSleeping = false;
        isSprinting = false;
        wasSprintRequested = false;
        if (sprintAdjustedAnimatorSpeed && animator != null)
        {
            animator.speed = 1f;
            sprintAdjustedAnimatorSpeed = false;
        }
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
        hitButton.gameObject.SetActive(false);
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

        hitButton.gameObject.SetActive(true);
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

        if (hitButton != null)
            hitButton.gameObject.SetActive(true);

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
        currentAttackTarget = null;
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
    // CHECKPOINT SAVE SYSTEM
    // =====================

    #region CHECKPOINT SAVE SYSTEM

    public void SaveCheckpoint(Vector3 respawnPosition, Quaternion respawnRotation)
    {
        PlayerCheckpointData checkpoint = new PlayerCheckpointData(
            SceneManager.GetActiveScene().name,
            respawnPosition,
            respawnRotation,
            Mathf.Clamp(currentHealth, 1, maxHealth)
        );

        PlayerCheckpointSaveSystem.Save(checkpoint);
        Debug.Log($"Saved checkpoint at {respawnPosition}");
    }

    private void ApplySavedCheckpointOnSceneLoad()
    {
        if (!TryGetSavedCheckpointInCurrentScene(out PlayerCheckpointData checkpoint))
            return;

        transform.SetPositionAndRotation(checkpoint.Position, checkpoint.Rotation);
        currentHealth = Mathf.Clamp(checkpoint.Health, 1, maxHealth);
        isDead = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        UpdateHealthBar();
        ResetHealthRegenerationTimer();
        Debug.Log($"Loaded checkpoint at {checkpoint.Position}");
    }

    private bool TryGetSavedCheckpointInCurrentScene(out PlayerCheckpointData checkpoint)
    {
        if (!PlayerCheckpointSaveSystem.TryLoad(out checkpoint))
            return false;

        return checkpoint.SceneName == SceneManager.GetActiveScene().name;
    }

    #endregion

    // =====================
    // DAMAGE SYSTEM
    // =====================

    #region DAMAGE SYSTEM

    public void OnDamagedTaken(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        SetHealth(currentHealth - Mathf.RoundToInt(damage));
        ResetHealthRegenerationTimer();

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (splashDamageImage != null)
        {
            if (damageFlashRoutine != null)
                StopCoroutine(damageFlashRoutine);

            damageFlashRoutine = StartCoroutine(DamageFlashEffect());
        }

        if (currentHealth == 0)
            OnPlayerDeath();
    }

    public void ReplenishHealthToFull()
    {
        SetHealth(maxHealth);
        isDead = false;
        ResetHealthRegenerationTimer();
        Debug.Log($"Player health replenished to {currentHealth}/{maxHealth}");
    }

    private void HandleHealthRegeneration()
    {
        if (isDead || currentHealth >= maxHealth)
        {
            ResetHealthRegenerationTimer();
            return;
        }

        healthRegenerationTimer -= Time.deltaTime;
        if (healthRegenerationTimer > 0f)
            return;

        if (healthRegenerationPercent <= 0f)
        {
            ResetHealthRegenerationTimer();
            return;
        }

        int regenerationAmount = Mathf.Max(
            1,
            Mathf.RoundToInt(maxHealth * healthRegenerationPercent)
        );
        SetHealth(currentHealth + regenerationAmount);
        ResetHealthRegenerationTimer();
    }

    private void ResetHealthRegenerationTimer()
    {
        healthRegenerationInterval = Mathf.Max(0.01f, healthRegenerationInterval);
        healthRegenerationTimer = healthRegenerationInterval;
    }

    private void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateHealthBar();
    }

    private void CacheHealthBar()
    {
        if (healthBar == null)
            healthBar = FindHealthBarByName();

        if (healthBarRect == null && healthBar != null)
            healthBarRect = healthBar.rectTransform;

        if (healthBarRect != null)
            healthBarFullWidth = GetHealthBarWidth();
    }

    private Image FindHealthBarByName()
    {
        GameObject healthContainer = GameObject.Find("HealthContainer");
        if (healthContainer == null)
            healthContainer = GameObject.Find("healthContainer");

        if (healthContainer == null)
            return null;

        Transform healthBarTransform = FindChildByName(healthContainer.transform, "healthBar");
        return healthBarTransform != null ? healthBarTransform.GetComponent<Image>() : null;
    }

    private Transform FindChildByName(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nestedChild = FindChildByName(child, childName);
            if (nestedChild != null)
                return nestedChild;
        }

        return null;
    }

    private void UpdateHealthBar()
    {
        float healthPercent = Mathf.Clamp01((float)currentHealth / maxHealth);

        if (healthBar != null && healthBar.type == Image.Type.Filled)
        {
            healthBar.fillAmount = healthPercent;
            return;
        }

        if (healthBarRect == null)
            return;

        if (healthBarFullWidth <= 0f)
            healthBarFullWidth = GetHealthBarWidth();

        if (healthBarFullWidth > 0f)
            healthBarRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                healthBarFullWidth * healthPercent
            );
    }

    private float GetHealthBarWidth()
    {
        if (healthBarRect == null)
            return 0f;

        float width = healthBarRect.rect.width;
        if (width <= 0f)
            width = Mathf.Abs(healthBarRect.sizeDelta.x);

        return width;
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
        damageFlashRoutine = null;
    }

    void OnPlayerDeath()
    {
        isDead = true;
        Debug.Log("Player has died.");

        if (TryGetSavedCheckpointInCurrentScene(out _))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        Debug.LogWarning("No checkpoint found for this scene.");
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
public struct PlayerCheckpointData
{
    public string SceneName;
    public Vector3 Position;
    public Quaternion Rotation;
    public int Health;

    public PlayerCheckpointData(string sceneName, Vector3 position, Quaternion rotation, int health)
    {
        SceneName = sceneName;
        Position = position;
        Rotation = rotation;
        Health = health;
    }
}

public static class PlayerCheckpointSaveSystem
{
    private const string HasCheckpointKey = "PLAYER_CHECKPOINT_HAS_SAVE";
    private const string SceneNameKey = "PLAYER_CHECKPOINT_SCENE";
    private const string PositionXKey = "PLAYER_CHECKPOINT_POSITION_X";
    private const string PositionYKey = "PLAYER_CHECKPOINT_POSITION_Y";
    private const string PositionZKey = "PLAYER_CHECKPOINT_POSITION_Z";
    private const string RotationXKey = "PLAYER_CHECKPOINT_ROTATION_X";
    private const string RotationYKey = "PLAYER_CHECKPOINT_ROTATION_Y";
    private const string RotationZKey = "PLAYER_CHECKPOINT_ROTATION_Z";
    private const string RotationWKey = "PLAYER_CHECKPOINT_ROTATION_W";
    private const string HealthKey = "PLAYER_CHECKPOINT_HEALTH";

    public static void Save(PlayerCheckpointData checkpoint)
    {
        PlayerPrefs.SetInt(HasCheckpointKey, 1);
        PlayerPrefs.SetString(SceneNameKey, checkpoint.SceneName);
        PlayerPrefs.SetFloat(PositionXKey, checkpoint.Position.x);
        PlayerPrefs.SetFloat(PositionYKey, checkpoint.Position.y);
        PlayerPrefs.SetFloat(PositionZKey, checkpoint.Position.z);
        PlayerPrefs.SetFloat(RotationXKey, checkpoint.Rotation.x);
        PlayerPrefs.SetFloat(RotationYKey, checkpoint.Rotation.y);
        PlayerPrefs.SetFloat(RotationZKey, checkpoint.Rotation.z);
        PlayerPrefs.SetFloat(RotationWKey, checkpoint.Rotation.w);
        PlayerPrefs.SetInt(HealthKey, checkpoint.Health);
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out PlayerCheckpointData checkpoint)
    {
        checkpoint = new PlayerCheckpointData();

        if (PlayerPrefs.GetInt(HasCheckpointKey, 0) == 0)
            return false;

        checkpoint = new PlayerCheckpointData(
            PlayerPrefs.GetString(SceneNameKey, string.Empty),
            new Vector3(
                PlayerPrefs.GetFloat(PositionXKey, 0f),
                PlayerPrefs.GetFloat(PositionYKey, 0f),
                PlayerPrefs.GetFloat(PositionZKey, 0f)
            ),
            new Quaternion(
                PlayerPrefs.GetFloat(RotationXKey, 0f),
                PlayerPrefs.GetFloat(RotationYKey, 0f),
                PlayerPrefs.GetFloat(RotationZKey, 0f),
                PlayerPrefs.GetFloat(RotationWKey, 1f)
            ),
            PlayerPrefs.GetInt(HealthKey, 100)
        );
        return true;
    }
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
    public GameObject LedgeVFX;
    public Transform ledgeVFXAnchorTransform;
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
