using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlideTrigger : MonoBehaviour
{
    [Header("Gliding Settings")]
    public bool IsPlayerGliding = false;
    public Transform player;
    
    [SerializeField]
    private float glideSpeed = 5f;
    
    [SerializeField]
    private float smoothRotation = 10f;
    
    private float horizontalInput;
    private Vector3 moveDirection;
    
    void Update()
    {
        if (IsPlayerGliding == true && player != null)
        {
            // Keep player at local position zero relative to this trigger
            player.localPosition = new Vector3(0, 0, -.05f);
            
            // Handle gliding input and movement
            GetGlideInput();
            HandleGlideMovement();
            HandleGlideRotation();
        }
    }
    
    void GetGlideInput()
    {
        // Get horizontal input (A/D or Left/Right arrow keys)
        horizontalInput = Input.GetAxisRaw("Horizontal");
    }
    
    void HandleGlideMovement()
    {
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            // Move the trigger (and player with it) along Z-axis
            moveDirection = new Vector3(0, 0, horizontalInput);
            transform.Translate(moveDirection * glideSpeed * Time.deltaTime, Space.World);
        }
    }
    
    void HandleGlideRotation()
    {
        if (Mathf.Abs(horizontalInput) > 0.01f && player != null)
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
            
            // // Smoothly rotate player towards target rotation
            // player.rotation = Quaternion.Lerp(
            //     player.rotation,
            //     targetRotation,
            //     smoothRotation * Time.deltaTime
            // );
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerScript playerScript = other.gameObject.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                playerScript.StartGliding();
            }
            
            player = other.transform;
            
            // Parent player to this trigger
            player.SetParent(transform);
            
            IsPlayerGliding = true;
        }
    }
    
    // Optional: Add a method to stop gliding
    public void StopGliding()
    {
      
    }
}