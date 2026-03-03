using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;

public class PostSleepPlayerScript : MonoBehaviour
{
    [Header("Animator Controllers")]
    [SerializeField]
    private RuntimeAnimatorController postSleepController;

    [SerializeField]
    private RuntimeAnimatorController originalController;

    public CapsuleCollider capsuleCollider;

    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float smoothRotation = 10f;

    [Header("Mobile Controls")]
    [SerializeField]
    private bool useMobileControls = false;
    private float mobileHorizontalInput = 0f;

    public Transform handAnchor;

    [Header("UI Buttons")]
    public GameObject buttonPlayerCanvas,
        buttonSleepCanvas,
        playerMoveControl,
        playerSleepControl;
    public Button pickFruitButton;
    public Button drinkButton;

    [Header("Drinking Settings")]
    [SerializeField]
    private GameObject cupGO;

    [SerializeField]
    private GameObject crossReferrence;
    private CanvasTrigger canvasTrigger;
    private int breadAmount = 0;
    private int maxBreadAmount = 2;

    [Header("Crow Settings")]
    // Assign the crow GameObject in the Inspector — the same one used by SleepTrigger
    public GameObject crowGameObject;
    private RavenScript ravenScript;
    private bool hasCrowFlewAway = false; // Only trigger fly-away once per sleep cycle

    // Animation parameter names — must match your new controller exactly
    private const string IS_WALKING = "Walk";
    private const string PICK_FRUIT = "PickFruit";
    private const string IS_DRINKING = "Drink";

    private Animator animator;
    private PlayerScript playerScript;
    private Rigidbody rb;

    private float horizontalInput;
    private bool isMoving;
    private bool isDrinking;
    private bool isPickingFruit;
    private bool isActive = false;

     [Header("Movement Settings")]
     public GameObject blockade;


    // Water fountain

    private CanvasTrigger currentCanvasTrigger;
    public RigBuilder rig;

    public GameObject standingCross;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerScript = GetComponent<PlayerScript>();
        rb = GetComponent<Rigidbody>();

        drinkButton.gameObject.SetActive(false);
        pickFruitButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isActive)
            return;

        GetInput();
        HandleCrowFlyAway(); // Check if player has moved for the first time
        HandleMovement();
        HandleRotation();
        HandleAnimation();
        HandleDrinkingInput();
    }

    // =============================================
    // ACTIVATION — called by SleepTrigger on wake
    // =============================================

    public void Activate()
    {
        isActive = true;
        hasCrowFlewAway = false; // Reset so fly-away fires fresh each sleep cycle

        // Cache the RavenScript reference at activation time
        if (crowGameObject != null)
        {
            ravenScript = crowGameObject.GetComponent<RavenScript>();
            if (ravenScript == null)
                Debug.LogWarning("PostSleepPlayerScript: No RavenScript found on crowGameObject!");
        }

        if (buttonPlayerCanvas != null && buttonSleepCanvas != null)
        {
            buttonPlayerCanvas.SetActive(false);
            playerMoveControl.SetActive(false);
            buttonSleepCanvas.SetActive(true);
            playerSleepControl.SetActive(true);

            playerScript.walkRig.weight = 0f;
            playerScript.armRig.weight = 0f;
            playerScript.targetRigWeight = 0f;
            rig.enabled = false;
            crossReferrence.SetActive(false);
        }

        if (playerScript != null)
            playerScript.enabled = false;

        if (animator != null && postSleepController != null)
            animator.runtimeAnimatorController = postSleepController;

        if (rb != null)
            rb.isKinematic = false;

        drinkButton.gameObject.SetActive(false);
        pickFruitButton.gameObject.SetActive(false);

        Debug.Log("PostSleepPlayerScript activated — new controller applied");
    }

    // =============================================
    // DEACTIVATION — called after drinking completes
    // =============================================

   private void Deactivate()
{
    isActive = false;
    mobileHorizontalInput = 0f;
    blockade.SetActive(false);

    if (buttonPlayerCanvas != null && buttonSleepCanvas != null)
    {
        playerMoveControl.SetActive(true);
        buttonPlayerCanvas.SetActive(true);
        buttonSleepCanvas.SetActive(false);
        playerSleepControl.SetActive(false);
    }

    if (animator != null && originalController != null)
        animator.runtimeAnimatorController = originalController;

    if (playerScript != null)
        playerScript.enabled = true;

    // Restore cross to correct parent and transform
    crossReferrence.transform.SetParent(playerScript.handTransform);
    crossReferrence.transform.localPosition = playerScript.initialTransformCrossOffset;
    crossReferrence.transform.localRotation = Quaternion.Euler(playerScript.initialRotationCrossOffset);
    crossReferrence.SetActive(true);

    standingCross.SetActive(false);

    // Rebind animator to force IK constraints to reattach
    StartCoroutine(RebindRig());

    Debug.Log("PostSleepPlayerScript deactivated — original controller restored");
}

private IEnumerator RebindRig()
{
    // Wait one frame for the controller swap to settle
    yield return null;

    animator.Rebind();
    animator.Update(0f);

    playerScript.walkRig.weight = 1f;
    playerScript.armRig.weight = 1f;
    playerScript.targetRigWeight = 1f;

    rig.enabled = true;
    rig.Build();

    Debug.Log("Rig rebound and rebuilt");
}
    // =============================================
    // CROW FLY AWAY — triggers on first movement
    // =============================================

    void HandleCrowFlyAway()
    {
        // Only fire once, and only when the player actually moves
        if (hasCrowFlewAway)
            return;
        if (!isMoving)
            return;

        hasCrowFlewAway = true;

        if (ravenScript != null && crowGameObject != null && crowGameObject.activeInHierarchy)
        {
            ravenScript.FlyAway();
            Debug.Log("Player moved — crow is flying away");
        }
    }

    // =============================================
    // MOBILE UI CALLBACKS
    // =============================================

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

    // =============================================
    // INPUT & MOVEMENT
    // =============================================

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

        // Force zero if any blocking state is active
        if (isDrinking || isPickingFruit)
            horizontalInput = 0f;

        isMoving = Mathf.Abs(horizontalInput) > 0.01f;
    }

    void HandleMovement()
    {
        if (!isMoving)
            return;

        Vector3 moveDirection = new Vector3(0, 0, horizontalInput);
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    void HandleRotation()
    {
        if (!isMoving)
            return;

        Quaternion targetRotation =
            horizontalInput < 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            smoothRotation * Time.deltaTime
        );
    }

    void HandleAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(IS_WALKING, isMoving);

        // Belt-and-suspenders: force Walk off when blocked
        if (isDrinking || isPickingFruit)
            animator.SetBool(IS_WALKING, false);
    }

    // =============================================
    // DRINKING
    // =============================================

    void HandleDrinkingInput()
    {
        if (Input.GetKeyDown(KeyCode.K) && !isDrinking && !isPickingFruit)
            StartDrinking();
    }

    public void OnDrinkButtonDown()
    {
        if (!isDrinking && !isPickingFruit)
            StartDrinking();
    }

    void StartDrinking()
    {
        isDrinking = true;
        isMoving = false;
        mobileHorizontalInput = 0f;

        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DRINKING, true);

        if (crossReferrence != null)
            crossReferrence.SetActive(false);
        if (cupGO != null)
            cupGO.SetActive(true);

        Debug.Log("PostSleep: Started drinking");
    }

    // Animation Event — fires at the end of the Drink animation clip
    public void AnimationEvent_EndEating()
    {
        CompleteEating();
    }

    private void CompleteEating()
    {
        isDrinking = false;
        breadAmount = 0;

        animator.SetBool(IS_DRINKING, false);

        if (cupGO != null)
            cupGO.SetActive(false);
        if (crossReferrence != null)
            crossReferrence.SetActive(true);
        if (drinkButton != null)
            drinkButton.interactable = false;

        Debug.Log("PostSleep: Drinking complete — switching back to original controller");

        Deactivate();
    }

    // =============================================
    // PICKUP FRUIT
    // =============================================

    public void OnPickFruitButtonPressed()
    {
        if (!isDrinking && !isPickingFruit && isActive)
            StartCoroutine(PickFruitRoutine());
    }

    private IEnumerator PickFruitRoutine()
    {
        Debug.Log("PickFruitRoutine() is called");
        isPickingFruit = true;
        isMoving = false;
        mobileHorizontalInput = 0f;

        animator.SetBool(IS_WALKING, false);
        animator.SetBool(PICK_FRUIT, true); // was SetTrigger

        Debug.Log("PostSleep: Picking fruit");

        yield return new WaitForSeconds(GetAnimationClipLength("Pickup Fruit"));

        animator.SetBool(PICK_FRUIT, false); // explicitly stop the animation
        isPickingFruit = false;
        Debug.Log("PostSleep: Fruit pickup complete");
        drinkButton.gameObject.SetActive(true);
    }

    //Animation Event
    public void SnapBreadToHand()
    {
        Destroy(cupGO.GetComponent<Rigidbody>());
        cupGO.transform.SetParent(handAnchor);
        cupGO.transform.localPosition = Vector3.zero;
        cupGO.transform.localRotation = Quaternion.identity; // optional but usually needed
    }

    private float GetAnimationClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 1f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        Debug.LogWarning($"Clip '{clipName}' not found — defaulting to 1 second wait");
        return 1f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive)
            return;

        if (other.CompareTag("Bread"))
        {
            Debug.Log("Player is in bread region");
            pickFruitButton.gameObject.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!isActive)
            return;

        if (other.CompareTag("Bread"))
        {
            pickFruitButton.gameObject.SetActive(false);
        }
    }
}
