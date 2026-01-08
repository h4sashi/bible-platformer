using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothRotation = 10f;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    private float horizontalInput;
    private bool isMoving;
    private Vector3 moveDirection;
    
    // Animation parameter names
    private const string MOVE_ANIMATION = "Move";
    private const string IS_MOVING = "IsMoving";
    
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
    }
    
    void Update()
    {
        GetInput();
        HandleMovement();
        HandleRotation();
        HandleAnimation();
    }
    
    void GetInput()
    {
        // Get horizontal input (A/D or Left/Right arrow keys)
        horizontalInput = Input.GetAxisRaw("Horizontal");
        
        // Check if player is moving
        isMoving = Mathf.Abs(horizontalInput) > 0.01f;
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
            // Set the IsMoving boolean parameter
            animator.SetBool(IS_MOVING, isMoving);
            
            // Optionally set a float parameter for move speed
            animator.SetFloat(MOVE_ANIMATION, Mathf.Abs(horizontalInput));
        }
    }
}