using System.Collections;
using UnityEngine;

public class SleepTrigger : MonoBehaviour
{
    [Header("Sleep Settings")]
    [SerializeField] private float sleepDuration = 5f;
    [SerializeField] private float crowCameraDelay = 5f; // Seconds after sleep before crow flies

    [SerializeField] private GameObject sleepCamera;
    [SerializeField] private GameObject mainCamera;

    [Header("UI Settings")]
    [SerializeField] private GameObject sleepPromptUI;
    [SerializeField] private GameObject sleepCanvas;

    [Header("Crow Settings")]
    public GameObject crow;
    public GameObject crowCross;
    public GameObject playerCross;

    private RavenScript ravenScript;
    private PlayerScript player;
    private bool playerInTrigger = false;
    private bool hasSleepTriggered = false;

    void Start()
    {
        if (sleepPromptUI != null)
            sleepPromptUI.SetActive(false);

        // Cache and hook into the raven script
        if (crow != null)
        {
            ravenScript = crow.GetComponent<RavenScript>();

            if (ravenScript != null)
            {
                // Subscribe: when crow arrives, switch from crow cam back to sleep cam
                ravenScript.onReachedTarget += OnCrowReachedTarget;
            }
            else
            {
                Debug.LogWarning("No RavenScript found on crow GameObject!");
            }
        }
    }

    void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks
        if (ravenScript != null)
            ravenScript.onReachedTarget -= OnCrowReachedTarget;
    }

    void Update()
    {
        if (player != null && !player.IsSleeping && !hasSleepTriggered)
        {
            hasSleepTriggered = true;
            StartCoroutine(SleepSequence());
        }
    }

    private IEnumerator SleepSequence()
    {
        // === PHASE 1: Start sleep, show sleep camera ===
        player.StartSleeping(sleepDuration);

        if (sleepCamera != null) sleepCamera.SetActive(true);
        if (mainCamera != null) mainCamera.SetActive(false);

        // === PHASE 2: After 5 seconds, switch to crow camera and send crow flying ===
        yield return new WaitForSeconds(crowCameraDelay);

        if (ravenScript != null)
        {
            // Switch to crow camera
            if (sleepCamera != null) sleepCamera.SetActive(false);
            if (ravenScript.crowCamera != null) ravenScript.crowCamera.SetActive(true);

            // Tell crow to start flying
            ravenScript.StartFlying();
        }

        // === PHASE 3: Wait for remaining sleep duration ===
        float remainingSleep = sleepDuration - crowCameraDelay;
        if (remainingSleep > 0)
            yield return new WaitForSeconds(remainingSleep);

        // === PHASE 4: Sleep finished — switch back to main camera ===
        if (ravenScript != null && ravenScript.crowCamera != null)
            ravenScript.crowCamera.SetActive(false);

        if (sleepCamera != null) sleepCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        if (sleepPromptUI != null) sleepPromptUI.SetActive(false);
        if (sleepCanvas != null) sleepCanvas.SetActive(false);

        // Disable collider so this never triggers again
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        PostSleepPlayerScript postSleepPlayerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PostSleepPlayerScript>();
        postSleepPlayerScript.Activate();
    }

    // Called by RavenScript when crow reaches its target
    private void OnCrowReachedTarget()
    {
        // Switch from crow camera back to sleep camera
        if (ravenScript != null && ravenScript.crowCamera != null)
            ravenScript.crowCamera.SetActive(false);

        if (sleepCamera != null)
            sleepCamera.SetActive(true);

        Debug.Log("Crow reached target - switched back to sleep camera");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerScript>();
            playerInTrigger = true;

            if (sleepPromptUI != null) sleepPromptUI.SetActive(true);
            if (crow != null) crow.SetActive(true);
            if(crowCross != null) crowCross.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = null;
            playerInTrigger = false;

            if (sleepPromptUI != null) sleepPromptUI.SetActive(false);
        }
    }

    public void OnSleepButtonPressed()
    {
        if (player != null && !player.IsSleeping && !hasSleepTriggered)
        {
            hasSleepTriggered = true;
            StartCoroutine(SleepSequence());
        }
    }
}