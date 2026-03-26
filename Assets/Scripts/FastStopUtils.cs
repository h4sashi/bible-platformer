using UnityEngine;

public class FastStopUtils : MonoBehaviour
{
    public bool isFastStoppingLeft = false;
    public bool isFastStoppingRight = false;

    private PlayerScript playerScript;
    private PostSleepPlayerScript postSleepPlayerScript;

    private bool isPlayerInTrigger = false;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerScript = player.GetComponent<PlayerScript>();
            postSleepPlayerScript = player.GetComponent<PostSleepPlayerScript>();

            if (playerScript == null)
                Debug.LogWarning("FastStopUtils: No PlayerScript found on Player!");
            if (postSleepPlayerScript == null)
                Debug.LogWarning("FastStopUtils: No PostSleepPlayerScript found on Player!");
        }
        else
        {
            Debug.LogWarning("FastStopUtils: Player GameObject not found in scene!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInTrigger = true;

        // Lazy-fetch in case Start() ran before player was ready
        if (playerScript == null)
            playerScript = other.GetComponent<PlayerScript>();
        if (postSleepPlayerScript == null)
            postSleepPlayerScript = other.GetComponent<PostSleepPlayerScript>();

        playerScript?.OnFastStopTriggerEnter(transform.position, isFastStoppingLeft, isFastStoppingRight);
        postSleepPlayerScript?.OnFastStopTriggerEnter(transform.position, isFastStoppingLeft, isFastStoppingRight);

        Debug.Log($"FastStop entered — Left: {isFastStoppingLeft}, Right: {isFastStoppingRight}");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInTrigger = false;

        playerScript?.OnFastStopTriggerExit();
        postSleepPlayerScript?.OnFastStopTriggerExit();

        Debug.Log("FastStop exited — player can move freely");
    }
}