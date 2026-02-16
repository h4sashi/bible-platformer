using UnityEngine;

public class FastStopUtils : MonoBehaviour
{
    public bool isFastStoppingLeft = false;
    public bool isFastStoppingRight = false;

    private PlayerScript playerScript;
    private bool isPlayerInTrigger = false;

    void Start()
    {
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerScript = player.GetComponent<PlayerScript>();
            if (playerScript == null)
            {
                Debug.LogWarning("PlayerScript component not found on Player!");
            }
        }
        else
        {
            Debug.LogWarning("Player GameObject not found in scene!");
        }
    }

    void Update()
    {
        // Continuously check if player should be blocked while in trigger
        if (isPlayerInTrigger && playerScript != null)
        {
            // This ensures the blocking persists even if player tries to move
            CheckAndBlockPlayer();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;

            if (playerScript == null)
            {
                playerScript = other.GetComponent<PlayerScript>();
            }

            if (playerScript != null)
            {
                // Call the player's obstacle method similar to RockObstacle
                playerScript.OnFastStopTriggerEnter(
                    transform.position,
                    isFastStoppingLeft,
                    isFastStoppingRight
                );

                Debug.Log($"Player entered FastStop zone - Left: {isFastStoppingLeft}, Right: {isFastStoppingRight}");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;

            if (playerScript != null)
            {
                // Notify player they left the fast stop zone
                playerScript.OnFastStopTriggerExit();
                Debug.Log("Player exited FastStop zone - can move freely");
            }
        }
    }

    private void CheckAndBlockPlayer()
    {
        // Additional runtime check if needed
        // This can be used for dynamic blocking logic
    }
}