using System.Collections;
using System.Numerics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
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
    private Rig walkRig; // Assign your rig component here

    [SerializeField]
    private Rig armRig;

    [SerializeField]
    private float rigTransitionSpeed = 5f;

    private float horizontalInput;
    private bool isMoving;
    private bool isDrinking;
    private bool isCasting;

    public bool isGliding;

    private bool isBreathing;
    private Vector3 moveDirection;
    private float targetRigWeight;

    public GameObject crossReferrence;
    public GameObject cupGO;

    // Animation parameter names
    private const string MOVE_ANIMATION = "Walk";
    private const string IS_MOVING = "IsWalking";

    private const string IS_DRINKING = "IsDrinking";

    private const string IS_CASTING = "IsCasting";

    private const string IS_GLIDING = "IsGlide";

    [Header("Cross Setup")]
    public Transform hitAnchor;
    public Transform handTransform;
    public Vector3 hitOffset;
    public Vector3 hitRotationOffset;
    public Vector3 crossOffset;
    public Vector3 crossRotationOffset;

    [Header("Glider Seetings")]
    public GameObject crossGliderGO;

    void Start()
    {
        // Get the Animator component if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("No Animator component found on player!");
            }
        }

        // Get the Rig component if not assigned
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
    }

    void Update()
    {
        GetInput();

        // Only allow movement when not breathing, drinking, or casting
        if (!isBreathing && !isDrinking && !isCasting && !isGliding)
        {
            HandleMovement();
            HandleRotation();
        }

        HandleAnimation();
        HandleRigWeight();
        HandleCastingInput();

        if (isGliding == true)
        {
            this.transform.localRotation = UnityEngine.Quaternion.Euler(0, -90f, 0);
            DisableAllMovements();
        }
    }

    void DisableAllMovements()
    {
        isMoving = false;
        isCasting = false;
        isDrinking = false;
    }

    void GetInput()
    {
        // Get horizontal input (A/D or Left/Right arrow keys)
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Check if player is moving (only when not breathing)
        isMoving = !isBreathing && !isDrinking && !isCasting && Mathf.Abs(horizontalInput) > 0.01f;
    }

    void HandleCastingInput()
    {
        if (Input.GetKeyDown(KeyCode.G) && !isCasting && !isBreathing && !isDrinking)
        {
            StartCasting();
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
            // Move along the Z-axis (left/right in 2.5D perspective)
            moveDirection = new Vector3(0, 0, horizontalInput);
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    void HandleRotation()
    {
        if (isMoving)
        {
            // Determine target rotation based on movement direction
            Quaternion targetRotation;

            if (horizontalInput < 0)
            {
                // Moving left - rotate to face left (180 degrees on Y-axis)
                targetRotation = UnityEngine.Quaternion.Euler(0, 180, 0);
            }
            else
            {
                // Moving right - rotate to face right (0 degrees on Y-axis)
                targetRotation = UnityEngine.Quaternion.Euler(0, 0, 0);
            }

            // Smoothly rotate towards target rotation
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
            // Set walking state (only when not breathing)
            animator.SetBool(IS_MOVING, isMoving);

            // Set walk intensity
            // animator.SetFloat(MOVE_ANIMATION, isMoving ? Mathf.Abs(horizontalInput) : 0f);
        }
    }

    void HandleRigWeight()
    {
        if (walkRig == null || armRig == null)
            return;

        // Disable rigs for Breath, Drink, OR Cast
        if (isBreathing || isDrinking || isCasting || isGliding)
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

        if (
            other.CompareTag("ActionTrigger")
            && other.name == "Water Fountain"
            && !isDrinking
            && !isBreathing
        )
        {
            StartDrinking();
        }

        if (
            other.CompareTag("ActionTrigger")
            && other.name == "Glide Zone"
            && !isDrinking
            && !isBreathing
            && !isCasting
        )
        {
            StartGliding();
        }

          if (
            other.CompareTag("ActionTrigger")
            && other.name == "Glide Zone Off"
            && !isDrinking
            && !isBreathing
            && !isCasting
        )
        {
          StopGliding();
        }
        
    }

    public void StartGliding()
    {
        isGliding = true;
        animator.SetBool(IS_GLIDING, true);

        // Optional: stop movement immediately
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
        // this.transform.localPosition = Vector3.zero;

        // Rigidbody crossRB = crossGliderGO.GetComponent<Rigidbody>();
        // if (crossRB == null)
        // {
        //     crossRB = crossGliderGO.AddComponent<Rigidbody>();
        // }

        // crossRB.useGravity = false;

        // Set to a layer that doesn't collide with player
        // crossGliderGO.layer = LayerMask.NameToLayer("Glider"); // Create this layer in Unity
    }

    void StartDrinking()
    {
        isDrinking = true;
        animator.SetBool(IS_DRINKING, true);

        // Optional: stop movement immediately
        isMoving = false;
        crossReferrence.SetActive(false);
        cupGO.SetActive(true);

        //To Add:
        // - VFX
        // - Button Press for Interactivity
    }

    public void StopDrinking()
    {
        isDrinking = false;
        animator.SetBool(IS_DRINKING, false);
        crossReferrence.SetActive(true);
        cupGO.SetActive(false);
    }

    // == ANIMATION EVENTS ==
    public void AnimationEvent_StartCasting()
    {
        crossReferrence.transform.SetParent(hitAnchor);
        crossReferrence.transform.localPosition = hitOffset;
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(hitRotationOffset);
    }

    public void AnimationEvent_EndCasting()
    {
        crossReferrence.transform.SetParent(null);
        // crossReferrence.transform.SetParent(handTransform);
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(crossRotationOffset);
        crossReferrence.transform.position = crossOffset;
    }
}
