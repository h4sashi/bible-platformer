using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlideTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        GlideZoneOn,
        GlideZoneOff,
        WaterFountain,
        RockObstacle,
        Cross
    }

    public TriggerType triggerType;

    [Header("Gliding Settings")]
    public bool IsPlayerGliding = false;
    public Transform player;

    [SerializeField]
    private float glideSpeed = 5f;

    [SerializeField]
    private float smoothRotation = 10f;
    public float rotGlideAxes = 0;

    private float horizontalInput;
    private Vector3 moveDirection;

    [Header("Mobile Controls")]
    [SerializeField]
    private bool useMobileControls = false;
    private float mobileHorizontalInput;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (IsPlayerGliding == true && player != null)
        {
            // Keep player at local position zero relative to this trigger
            player.localPosition = new Vector3(0, 0, -.05f);
            player.transform.rotation = Quaternion.Euler(0, 0, 0);

            // Handle gliding input and movement
            GetGlideInput();
            HandleGlideMovement();
            HandleGlideRotation();
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

    void GetGlideInput()
    {
        if (useMobileControls)
        {
            horizontalInput = mobileHorizontalInput;
        }
        else
        {
            // Get horizontal input (A/D or Left/Right arrow keys)
            horizontalInput = horizontalInput = Input.GetAxisRaw("Horizontal");
        }
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
                targetRotation = Quaternion.Euler(0, 90f, 0);
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player") && triggerType == TriggerType.Cross)
        {

            Debug.Log("Player has entered cross");
            
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
    public void StopGliding() { }
}
