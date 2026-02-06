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

    [SerializeField]
    private float smoothRotation = 10f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [Header("Animation Rigging")]
    [SerializeField]
    private Rig walkRig;

    [SerializeField]
    private Rig armRig;

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

    private float targetRigWeight;

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
    [SerializeField]
    private float pullSpeedIncreaseRate = 0.5f; // How fast pull speed increases (adjust in inspector)

    [SerializeField]
    private float pullSpeedDecreaseRate = 1.0f; // How fast pull speed decreases when released

    // Add this constant with your other animation parameter names
    private const string PULL_SPEED = "Blend";

    private bool isPulling; // Track if player is pulling
    private bool canPull; // Track if player is near rock obstacle
    private bool isPullButtonHeld; // Track if pull button is being held
    private float currentPullSpeed = 0f; // Current pull speed value

    private void Awake()
    {
        glideRig.weight = 0;
        currentHealth = maxHealth;

        if (splashDamageImage != null)
        {
            splashDamageImage.color = Color.clear;
        }

        // Store initial position as valid
        lastValidPosition = transform.position;
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
    HandlePulling(); // Add this - handles the lerping

    if (isGliding == true)
    {
        this.transform.localRotation = UnityEngine.Quaternion.Euler(0, -90f, 0);
        DisableAllMovements();
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
    isPullButtonHeld = false;
    currentPullSpeed = 0f;
    animator.SetFloat(PULL_SPEED, 0f);
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
        // if (Input.GetKeyDown(KeyCode.G) && !isCasting && !isBreathing && !isDrinking)
        // {
        //     StartCasting();
        // }
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

    // void HandleRigWeight()
    // {
    //     if (walkRig == null || armRig == null || glideRig == null)
    //         return;

    //     if (isBreathing || isDrinking || isCasting || isGliding)
    //     {
    //         targetRigWeight = 0f;
    //     }
    //     else
    //     {
    //         targetRigWeight = 1f;
    //     }

    //     walkRig.weight = Mathf.Lerp(
    //         walkRig.weight,
    //         targetRigWeight,
    //         Time.deltaTime * rigTransitionSpeed
    //     );

    //     armRig.weight = Mathf.Lerp(
    //         armRig.weight,
    //         targetRigWeight,
    //         Time.deltaTime * rigTransitionSpeed
    //     );
    // }

    void HandleRigWeight()
    {
        if (walkRig == null || armRig == null || glideRig == null)
            return;

        if (isBreathing || isDrinking || isCasting || isGliding || isPulling) // Add isPulling
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
        }
    }

    public void OnRockObstacleTriggerEnter(Vector3 rockPosition)
    {
        isBlockedByRock = true;
        rockObstaclePosition = rockPosition;
        canPull = true; // Enable pulling

        Debug.Log("Near rock obstacle - Can now pull");
    }

    // Modify the existing OnRockObstacleTriggerExit method:
    public void OnRockObstacleTriggerExit()
    {
        isBlockedByRock = false;
        canPull = false;
        isPulling = false;
        isPullButtonHeld = false;
        currentPullSpeed = 0f;

        // Reset the pull speed to 0
        animator.SetFloat(PULL_SPEED, 0f);

        Debug.Log("Exited rock obstacle area - can move freely");
    }

    // Public method to be called when hits are complete:
    public void OnRockObstacleComplete()
    {
        isBlockedByRock = false;
        canPull = false;
        isPulling = false;
        isPullButtonHeld = false;
        currentPullSpeed = 0f;

        // Reset the pull speed to 0
        animator.SetFloat(PULL_SPEED, 0f);

        Debug.Log("Rock obstacle cleared - Pull animation stopped");
    }

    // PUBLIC METHOD FOR PULL BUTTON - Call this from UI button or input

    // Add to your MOBILE UI CALL BACKS section
    // PUBLIC METHOD FOR PULL BUTTON DOWN - Call when button is pressed
    public void OnPullButtonDown()
    {
        if (canPull && !isDrinking && !isCasting && !isGliding && !isBreathing)
        {
            isPullButtonHeld = true;
            isPulling = true;
            isMoving = false;

            Debug.Log("Pull button pressed - Starting pull");
        }
    }

    // PUBLIC METHOD FOR PULL BUTTON UP - Call when button is released
    public void OnPullButtonUp()
    {
        isPullButtonHeld = false;

        Debug.Log("Pull button released");
    }

    public void OnPullButtonPressed()
    {
        if (canPull && !isPulling && !isDrinking && !isCasting && !isGliding && !isBreathing)
        {
            StartPulling();
        }
    }

    // Handle the smooth lerping of pull speed
    void HandlePulling()
    {
        if (isPulling && isPullButtonHeld)
        {
            // Smoothly increase pull speed to 1
            currentPullSpeed = Mathf.Lerp(
                currentPullSpeed,
                1f,
                pullSpeedIncreaseRate * Time.deltaTime
            );
        }
        else if (isPulling && !isPullButtonHeld)
        {
            // Smoothly decrease pull speed back to 0
            currentPullSpeed = Mathf.Lerp(
                currentPullSpeed,
                0f,
                pullSpeedDecreaseRate * Time.deltaTime
            );

            // When pull speed is close to 0, stop pulling
            if (currentPullSpeed < 0.01f)
            {
                currentPullSpeed = 0f;
                isPulling = false;
            }
        }

        // Update animator parameter
        animator.SetFloat(PULL_SPEED, currentPullSpeed);
    }

    void StartPulling()
    {
        isPulling = true;
        isMoving = false;

        // Set the pull speed to 1 to trigger blend tree
        animator.SetFloat(PULL_SPEED, 1.0f);

        Debug.Log("Started pulling");
    }

    // ANIMATION EVENT - Add this to the END of your Pull animation
    public void AnimationEvent_EndPulling()
    {
        isPulling = false;

        // Reset pull speed back to 0
        animator.SetFloat(PULL_SPEED, 0f);

        Debug.Log("Pull animation ended");
    }

    public void GlideZoneOnTrigger(CameraTrigger ct)
    {
        if (!isDrinking && !isBreathing && !isCasting)
        {
            StartGliding();
            // glideRig.weight = 0;
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
