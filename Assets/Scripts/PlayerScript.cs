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
    public Button drinkButton; // Reference to the UI button for actions (e.g., drink)

    [SerializeField]
    private float smoothRotation = 10f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [Header("Animation Rigging")]
    public  Rig walkRig;

    public Rig armRig;

    public Rig glideRig;

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

    public LayerMask enemyLayer; // Layer mask to identify enemies
    public float damageAmount = 25f; // Damage to apply to enemies

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
    public float rockRotationSpeed = 2f; // Speed of rotation lerp
    private bool isRotatingRock = false;
    private float rockRotationProgress = 0f;

    private void Awake()
    {
        glideRig.weight = 0;
        currentHealth = maxHealth;
        isSleeping = false;
        rb = GetComponent<Rigidbody>();

        if (splashDamageImage != null)
        {
            splashDamageImage.color = Color.clear;
        }

        // Store initial position as valid
        lastValidPosition = transform.position;

        // Initialize rock rotation
        if (crossRockGO != null)
        {
            crossRockGO.transform.localRotation = UnityEngine.Quaternion.Euler(
                rockPushInitialAngle
            );
        }
        if (drinkButton != null)
        {
            drinkButton.interactable = false; // Disable drink button at start
        }
        else
        {
            Debug.LogWarning("Drink button reference is missing!");
        }
    }

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("No Animator component found on player!");
            }
        }

        if (walkRig == null)
        {
            RigBuilder rigBuilder = GetComponent<RigBuilder>();
            if (rigBuilder != null && rigBuilder.layers.Count > 0)
            {
                walkRig = rigBuilder.layers[0].rig;
            }
            else
            {
                Debug.LogWarning("No Rig component assigned or found!");
            }
        }
        if (armRig == null)
        {
            RigBuilder rigBuilder = GetComponent<RigBuilder>();
            if (rigBuilder != null && rigBuilder.layers.Count > 0)
            {
                armRig = rigBuilder.layers[1].rig;
            }
            else
            {
                Debug.LogWarning("No Rig component assigned or found!");
            }
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
        HandlePullingInput(); // Keyboard input
        HandleRockRotation(); // NEW: Handle rock rotation lerp

        if (isGliding == true)
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
    }

    void HandlePullingInput()
    {
        // Hold P key to pull
        if (Input.GetKeyDown(KeyCode.P) && canPull)
        {
            OnPullButtonDown();
        }

        if (Input.GetKeyUp(KeyCode.P))
        {
            OnPullButtonUp();
        }
    }

    // NEW METHOD: Handle the smooth rotation of the rock
    void HandleRockRotation()
    {
        if (!isRotatingRock || crossRockGO == null)
            return;

        // Increment the lerp progress
        rockRotationProgress += Time.deltaTime * rockRotationSpeed;
        rockRotationProgress = Mathf.Clamp01(rockRotationProgress);

        // Lerp between initial and final rotation
        Quaternion initialRot = UnityEngine.Quaternion.Euler(rockPushInitialAngle);
        Quaternion targetRot = UnityEngine.Quaternion.Euler(rockPushAngle);

        crossRockGO.transform.localRotation = UnityEngine.Quaternion.Lerp(
            initialRot,
            targetRot,
            rockRotationProgress
        );
    }

    // MOBILE UI CALL BACKS

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
        pullAnimationComplete = false;
    }

    public void StartSleeping(float duration)
    {
        if (!isSleeping && !isDrinking && !isCasting && !isGliding && !isPulling)
        {
            StartCoroutine(SleepRoutine(duration));
        }
    }

    /// <summary>
    /// Sleep coroutine - handles the sleep animation and duration
    /// </summary>
    private IEnumerator SleepRoutine(float duration)
    {
        isSleeping = true;
        isMoving = false;

        // Set animator bool
        if (animator != null)
        {
            animator.SetBool(IS_SLEEPING, true);
        }

        // Apply sleep offset to position - remains active during entire sleep
        transform.position += sleepOffset;
        rb.isKinematic = true;
        

        // Optional: Disable cross during sleep
        if (crossReferrence != null)
        {
            crossReferrence.SetActive(false);
        }

        Debug.Log($"Player is sleeping for {duration} seconds...");

        // Wait for sleep duration
        yield return new WaitForSeconds(duration);

        // End sleep
        StopSleeping();
    }

    /// <summary>
    /// Stop sleeping and return to normal state
    /// </summary>
    public void StopSleeping()
    {
        isSleeping = false;

        if (animator != null)
        {
            animator.SetBool(IS_SLEEPING, false);
        }

        // Remove sleep offset when waking up
        transform.position -= sleepOffset;
        rb.isKinematic = false;

        // Re-enable cross
        if (crossReferrence != null)
        {
            crossReferrence.SetActive(true);
        }

        Debug.Log("Player woke up!");
    }

    public void AnimationEvent_EndSleeping()
    {
        // This would be called from the animation if you want precise control
        Debug.Log("Sleep animation ended via Animation Event");
        // You can add additional logic here if needed
    }

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
        {
            horizontalInput = mobileHorizontalInput;
        }
        else
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }

        isMoving =
            !isBreathing
            && !isDrinking
            && !isCasting
            && !isGliding
            && !isSleeping // ADD THIS LINE
            && Mathf.Abs(horizontalInput) > 0.01f;
    }

    void HandleCastingInput()
    {
        bool castRequested = false;

        if (useMobileControls)
        {
            castRequested = mobileCastPressed;
        }
        else
        {
            castRequested = Input.GetKeyDown(KeyCode.G);
        }
        if (castRequested && !isCasting && !isBreathing && !isDrinking && !isGliding)
        {
            StartCasting();
        }

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
        {
            StartDrinking();
        }
    }

    // PUBLIC METHOD FOR DRINK BUTTON - Call this from UI button Event Trigger (Pointer Down)
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
            Vector3 intendedPosition =
                transform.position + (moveDirection * moveSpeed * Time.deltaTime);

            // Check if blocked by rock obstacle
            if (isBlockedByRock && IsMovingTowardRock(intendedPosition))
            {
                // Block forward movement toward rock
                return;
            }

            // Check if blocked by FastStop
            if (isBlockedByFastStop && IsBlockedByFastStopDirection(horizontalInput))
            {
                // Block movement in the blocked direction
                return;
            }

            // Store position before moving (for potential rollback)
            lastValidPosition = transform.position;

            // Apply movement
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    /// <summary>
    /// Determines if the intended movement is toward the rock obstacle
    /// </summary>
    private bool IsMovingTowardRock(Vector3 intendedPosition)
    {
        // Calculate direction vectors
        Vector3 currentToRock = rockObstaclePosition - transform.position;
        Vector3 intendedToRock = rockObstaclePosition - intendedPosition;

        // If moving closer to the rock (distance decreasing), block movement
        // We use sqrMagnitude for performance (avoids sqrt calculation)
        return intendedToRock.sqrMagnitude < currentToRock.sqrMagnitude;
    }

    /// <summary>
    /// Determines if movement direction is blocked by FastStop
    /// </summary>
    private bool IsBlockedByFastStopDirection(float input)
    {
        // Block left movement (negative input)
        if (input < 0 && fastStopBlockLeft)
        {
            Debug.Log("Movement blocked: FastStop blocking LEFT direction");
            return true;
        }

        // Block right movement (positive input)
        if (input > 0 && fastStopBlockRight)
        {
            Debug.Log("Movement blocked: FastStop blocking RIGHT direction");
            return true;
        }

        return false;
    }

    // PUBLIC METHODS FOR FASTSTOP TRIGGER

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
            {
                targetRotation = UnityEngine.Quaternion.Euler(0, 180, 0);
            }
            else
            {
                targetRotation = UnityEngine.Quaternion.Euler(0, 0, 0);
            }

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
        {
            animator.SetBool(IS_MOVING, isMoving);
        }
    }

    void HandleRigWeight()
    {
        if (walkRig == null || armRig == null || glideRig == null)
            return;

        if (isBreathing || isDrinking || isCasting || isGliding || isPulling || isSleeping) // Add isSleeping
        {
            targetRigWeight = 0f;
        }
        else
        {
            targetRigWeight = 1f;
        }

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

        if (other.CompareTag("ActionTrigger") && other.name == "Water Fountain")
        {
            isNearWaterFountain = true;
            currentWaterFountain = other.gameObject;
            Debug.Log("Near water fountain - Press K to drink");
            canvasTrigger = other.GetComponent<CanvasTrigger>();
            canvasTrigger?.ActivateCanvas();

            // Enable drink button when near water fountain
            if (drinkButton != null)
            {
                drinkButton.interactable = true;
            }
        }
    }

    public void OnRockObstacleTriggerEnter(Vector3 rockPosition)
    {
        isBlockedByRock = true;
        rockObstaclePosition = rockPosition;
        canPull = true; // Enable pulling

        Debug.Log("Near rock obstacle - Can now pull");
    }

    public void OnRockObstacleTriggerExit()
    {
        isBlockedByRock = false;
        canPull = false;
        isPulling = false;
        pullAnimationComplete = false;

        // UPDATED: Reset rock rotation and crosses
        ResetRockRotation();

        // Ensure proper cross is active
        if (crossRockGO != null && crossRockGO.activeInHierarchy)
        {
            crossRockGO.SetActive(false);
        }

        if (crossReferrence != null && !crossReferrence.activeInHierarchy)
        {
            crossReferrence.SetActive(true);
        }

        Debug.Log("Exited rock obstacle area - can move freely");
    }

    public void OnRockObstacleComplete()
    {
        isBlockedByRock = false;
        canPull = false;
        isPulling = false;
        pullAnimationComplete = false;

        // UPDATED: Reset rock rotation and crosses
        ResetRockRotation();

        // Ensure proper cross is active
        if (crossRockGO != null && crossRockGO.activeInHierarchy)
        {
            crossRockGO.SetActive(false);
        }

        if (crossReferrence != null && !crossReferrence.activeInHierarchy)
        {
            crossReferrence.SetActive(true);
        }

        Debug.Log("Rock obstacle cleared - Pull animation stopped");
    }

    // PUBLIC METHOD FOR PULL BUTTON - Call this from UI button or input

    public void OnPullButtonDown()
    {
        if (canPull && !isDrinking && !isCasting && !isGliding && !isBreathing)
        {
            // Allow re-triggering by resetting isPulling immediately
            isPulling = true;
            isMoving = false;
            pullAnimationComplete = false;

            // Trigger the pull animation
            animator.SetTrigger(PULL_TRIGGER);
            if (crossRockGO.activeInHierarchy == false)
            {
                crossRockGO.SetActive(true);
                crossReferrence.SetActive(false);

                // UPDATED: Start rotating the rock
                // StartRockRotation(); - do not uncomment this line, it works as expected
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
        {
            StartPulling();
        }
    }

    void HandlePulling()
    {
        // Method kept for compatibility but no longer used
    }

    void StartPulling()
    {
        isPulling = true;
        isMoving = false;

        Debug.Log("Started pulling");
    }

    // NEW METHOD: Start the rock rotation
    private void StartRockRotation()
    {
        isRotatingRock = true;
        rockRotationProgress = 0f;
    }

    // NEW METHOD: Reset rock rotation instantly
    public void ResetRockRotation()
    {
        Debug.Log("Resetting rock rotation");
        isRotatingRock = false;
        rockRotationProgress = 0f;

        if (crossRockGO != null)
        {
            crossRockGO.transform.localRotation = UnityEngine.Quaternion.Euler(
                rockPushInitialAngle
            );
        }
    }

    // Animation Event at the peak of Pull animation
    public void AnimationEvent_PullHit()
    {
        pullAnimationComplete = true;
        Debug.Log("Pull hit!");
    }

    // Animation Event at the end of Pull animation
    public void AnimationEvent_EndPulling()
    {
        isPulling = false;

        // UPDATED: Snap rock back to initial rotation and swap crosses
        ResetRockRotation();

        // Deactivate rock cross and reactivate normal cross
        if (crossRockGO != null && crossRockGO.activeInHierarchy)
        {
            crossRockGO.SetActive(false);
        }

        if (crossReferrence != null && !crossReferrence.activeInHierarchy)
        {
            crossReferrence.SetActive(true);
        }

        Debug.Log("Pull animation ended");
    }

    public void ResetPullCompletion()
    {
        pullAnimationComplete = false;
    }

    public void GlideZoneOnTrigger(CameraTrigger ct)
    {
        if (!isDrinking && !isBreathing && !isCasting)
        {
            StartGliding();
            ct.enableEvents?.Invoke();
            crossGliderGO.GetComponent<GlideTrigger>().IsPlayerGliding = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ActionTrigger") && other.name == "Water Fountain")
        {
            isNearWaterFountain = false;
            currentWaterFountain = null;
            Debug.Log("Left water fountain area");

            // Disable drink button when leaving water fountain
            if (drinkButton != null)
            {
                drinkButton.interactable = false;
            }
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
        crossReferrence.SetActive(true);
        crossGliderGO.SetActive(false);
    }

    void InitGlider()
    {
        crossGliderGO.SetActive(true);
        this.transform.SetParent(crossGliderGO.transform);
        glideRig.weight = 1;
    }

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

    // == ANIMATION EVENTS ==

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

        // Disable drink button after completing
        if (drinkButton != null)
        {
            drinkButton.interactable = false;
        }
    }

    private void OnDrinkingBenefits()
    {
        Debug.Log("Player received drinking benefits!");
    }

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
        // Check for enemies in hitPoint's radius and apply damage
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

    // Check for enemies within hitPoint collider and apply damage
    private void CheckAndDamageEnemies()
    {
        if (crossHitCol == null)
        {
            Debug.LogWarning("CrossHitCol collider is not assigned!");
            return;
        }

        // Get all colliders overlapping with the crossHitCol sphere
        Collider[] hitColliders = Physics.OverlapSphere(
            crossHitCol.transform.position,
            crossHitCol.radius * crossHitCol.transform.lossyScale.x, // Account for scale
            enemyLayer
        );

        if (hitColliders.Length > 0)
        {
            Debug.Log($"Hit {hitColliders.Length} enemies!");

            foreach (Collider enemyCollider in hitColliders)
            {
                // Try to get a damage interface or component from the enemy
                IDamageable damageable = enemyCollider.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    damageable.TakeDamage(damageAmount);
                    Debug.Log($"Dealt {damageAmount} damage to {enemyCollider.gameObject.name}");
                }
                else
                {
                    // Alternative: if enemies use a different damage method
                    // Try to find EnemyHealth or similar component
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

    // DAMAGE SYSTEM - Splash effect directly in OnDamagedTaken
    public void OnDamagedTaken(float damage)
    {
        currentHealth -= (int)damage;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log($"Player took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        // Immediately show damage splash effect
        if (splashDamageImage != null)
        {
            StopAllCoroutines(); // Stop any existing fade coroutines
            StartCoroutine(DamageFlashEffect());
        }

        if (currentHealth == 0)
        {
            OnPlayerDeath();
        }
    }

    // Damage flash effect coroutine - runs immediately when damage is taken
    private IEnumerator DamageFlashEffect()
    {
        // Instant flash to full color
        splashDamageImage.color = flashColor;

        float elapsedTime = 0f;

        // Fade out over flashDuration
        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;

            // Calculate how far through the fade we are (0 to 1)
            float fadeProgress = elapsedTime / flashDuration;

            // Smoothly interpolate alpha from flashColor.a to 0
            float alpha = Mathf.Lerp(flashColor.a, 0f, fadeProgress);

            // Apply the fading color
            splashDamageImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);

            yield return null; // Wait one frame
        }

        // Ensure it ends completely transparent
        splashDamageImage.color = Color.clear;
    }

    void OnPlayerDeath()
    {
        Debug.Log("Player has died.");
        // Implement death behavior (e.g., respawn, game over screen, etc.)
    }
}

// Interface for damageable entities
public interface IDamageable
{
    void TakeDamage(float damage);
}