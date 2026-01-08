using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothRotation = 10f;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("Animation Rigging")]
    [SerializeField] private Rig walkRig; // Assign your rig component here
    [SerializeField] private float rigTransitionSpeed = 5f;
    
    private float horizontalInput;
    private bool isMoving;
    private bool isBreathing;
    private Vector3 moveDirection;
    private float targetRigWeight;
    
    // Animation parameter names
    private const string MOVE_ANIMATION = "Walk";
    private const string IS_MOVING = "IsWalking";
    private const string BREATH_ANIMATION = "Breath";
    private const string IS_BREATHING = "IsBreathing";
    
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
    }
    
    void Update()
    {
        GetInput();
        HandleBreathing();
        
        // Only allow movement when not breathing
        if (!isBreathing)
        {
            HandleMovement();
            HandleRotation();
        }
        
        HandleAnimation();
        HandleRigWeight();
    }
    
    void GetInput()
    {
        // Get horizontal input (A/D or Left/Right arrow keys)
        horizontalInput = Input.GetAxisRaw("Horizontal");
        
        // Check if player is moving (only when not breathing)
        isMoving = !isBreathing && Mathf.Abs(horizontalInput) > 0.01f;
    }
    
    void HandleBreathing()
    {
        // Press Space to start breathing
        if (Input.GetKeyDown(KeyCode.Space) && !isBreathing)
        {
            isBreathing = true;
        }
        
        // Press M to stop breathing and return to idle
        if (Input.GetKeyDown(KeyCode.M) && isBreathing)
        {
            isBreathing = false;
        }
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
                targetRotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                // Moving right - rotate to face right (0 degrees on Y-axis)
                targetRotation = Quaternion.Euler(0, 0, 0);
            }
            
            // Smoothly rotate towards target rotation
            transform.rotation = Quaternion.Lerp(
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
            // Set breathing state
            animator.SetBool(IS_BREATHING, isBreathing);
            
            // Set walking state (only when not breathing)
            animator.SetBool(IS_MOVING, isMoving);
            
            // Set walk intensity
            animator.SetFloat(MOVE_ANIMATION, isMoving ? Mathf.Abs(horizontalInput) : 0f);
        }
    }
    
    void HandleRigWeight()
    {
        if (walkRig == null) return;
        
        // Rig is active (weight = 1) for Idle and Walk animations
        // Rig is inactive (weight = 0) for all other animations (Breath, etc.)
        if (isBreathing)
        {
            // Breathing animation - disable rig
            targetRigWeight = 0f;
        }
        else
        {
            // Idle or Walking - enable rig
            targetRigWeight = 1f;
        }
        
        // Smoothly transition the rig weight
        walkRig.weight = Mathf.Lerp(walkRig.weight, targetRigWeight, Time.deltaTime * rigTransitionSpeed);
    }
}


























// using UnityEngine;

// public class PlayerScript : MonoBehaviour
// {
//     [Header("Movement Settings")]
//     [SerializeField] private float moveSpeed = 5f;
//     [SerializeField] private float smoothRotation = 10f;
    
//     [Header("Animation")]
//     [SerializeField] private Animator animator;
    
//     private float horizontalInput;
//     private bool isMoving;
//     private bool isBreathing;
//     private Vector3 moveDirection;
    
//     // Animation parameter names
//     private const string MOVE_ANIMATION = "Walk";
//     private const string IS_MOVING = "IsWalking";
//     private const string BREATH_ANIMATION = "Breath";
//     private const string IS_BREATHING = "IsBreathing";
    
//     void Start()
//     {
//         // Get the Animator component if not assigned
//         if (animator == null)
//         {
//             animator = GetComponent<Animator>();
//             if (animator == null)
//             {
//                 Debug.LogWarning("No Animator component found on player!");
//             }
//         }
//     }
    
//     void Update()
//     {
//         GetInput();
//         HandleBreathing();
        
//         // Only allow movement when not breathing
//         if (!isBreathing)
//         {
//             HandleMovement();
//             HandleRotation();
//         }
        
//         HandleAnimation();
//     }
    
//     void GetInput()
//     {
//         // Get horizontal input (A/D or Left/Right arrow keys)
//         horizontalInput = Input.GetAxisRaw("Horizontal");
        
//         // Check if player is moving (only when not breathing)
//         isMoving = !isBreathing && Mathf.Abs(horizontalInput) > 0.01f;
//     }
    
//     void HandleBreathing()
//     {
//         // Press Space to start breathing
//         if (Input.GetKeyDown(KeyCode.Space) && !isBreathing)
//         {
//             isBreathing = true;
//         }
        
//         // Press M to stop breathing and return to idle
//         if (Input.GetKeyDown(KeyCode.M) && isBreathing)
//         {
//             isBreathing = false;
//         }
//     }
    
//     void HandleMovement()
//     {
//         if (isMoving)
//         {
//             // Move along the Z-axis (left/right in 2.5D perspective)
//             moveDirection = new Vector3(0, 0, horizontalInput);
//             transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
//         }
//     }
    
//     void HandleRotation()
//     {
//         if (isMoving)
//         {
//             // Determine target rotation based on movement direction
//             Quaternion targetRotation;
            
//             if (horizontalInput < 0)
//             {
//                 // Moving left - rotate to face left (180 degrees on Y-axis)
//                 targetRotation = Quaternion.Euler(0, 180, 0);
//             }
//             else
//             {
//                 // Moving right - rotate to face right (0 degrees on Y-axis)
//                 targetRotation = Quaternion.Euler(0, 0, 0);
//             }
            
//             // Smoothly rotate towards target rotation
//             transform.rotation = Quaternion.Lerp(
//                 transform.rotation, 
//                 targetRotation, 
//                 smoothRotation * Time.deltaTime
//             );
//         }
//     }
    
//     void HandleAnimation()
//     {
//         if (animator != null)
//         {
//             // Set breathing state
//             animator.SetBool(IS_BREATHING, isBreathing);
            
//             // Set walking state (only when not breathing)
//             animator.SetBool(IS_MOVING, isMoving);
            
//             // Set walk intensity
//             animator.SetFloat(MOVE_ANIMATION, isMoving ? Mathf.Abs(horizontalInput) : 0f);
//         }
//     }
// }


///
/// 

