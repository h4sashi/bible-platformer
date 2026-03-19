using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SleepTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        BeforeStorm,
        AfterStorm,
    }

    [Header("Sleep Settings")]
    [SerializeField]
    private float sleepDuration = 5f;

    [SerializeField]
    private float afterSleepDuration = 9f;

    [SerializeField]
    private float crowCameraDelay = 5f; // Seconds after sleep before crow flies

    [SerializeField]
    private GameObject sleepCamera;

    [SerializeField]
    private GameObject mainCamera;

    [Header("UI Settings")]
    [SerializeField]
    private GameObject sleepPromptUI;

    [SerializeField]
    private GameObject sleepCanvas;

    [Header("Crow Settings")]
    public GameObject crow;
    public GameObject crowCross;
    public GameObject playerCross;

    private RavenScript ravenScript;
    private PlayerScript player;
    private bool hasSleepTriggered = false;

    public TriggerType triggerType;

    public UnityEvent enableEvents;
     public UnityEvent onEventExecution;
     public UnityEvent disableEvents;
     public UnityEvent OnSleepingFinish;

    void Start()
    {
        if (triggerType == TriggerType.BeforeStorm)
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
        else if (triggerType == TriggerType.AfterStorm)
        {
            if (sleepPromptUI != null)
                sleepPromptUI.SetActive(false);
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
            StartSleepSequence();
        }
    }

    private IEnumerator SleepSequence()
    {
        // === PHASE 1: Start sleep, show sleep camera ===
        player.StartSleeping(sleepDuration);

        if (sleepCamera != null)
            sleepCamera.SetActive(true);
        if (mainCamera != null)
            mainCamera.SetActive(false);

        // === PHASE 2: After 5 seconds, switch to crow camera and send crow flying ===
        yield return new WaitForSeconds(crowCameraDelay);

        if (ravenScript != null)
        {
            // Switch to crow camera
            if (sleepCamera != null)
                sleepCamera.SetActive(false);
            if (ravenScript.crowCamera != null)
                ravenScript.crowCamera.SetActive(true);

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

        if (sleepCamera != null)
            sleepCamera.SetActive(false);
        if (mainCamera != null)
            mainCamera.SetActive(true);

        if (sleepPromptUI != null)
            sleepPromptUI.SetActive(false);
        if (sleepCanvas != null)
            sleepCanvas.SetActive(false);

        // Disable collider so this never triggers again
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        PostSleepPlayerScript postSleepPlayerScript = GameObject
            .FindGameObjectWithTag("Player")
            .GetComponent<PostSleepPlayerScript>();
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
        if (other.CompareTag("Player") && triggerType == TriggerType.BeforeStorm)
        {
            Debug.Log("BeforeStorm() is called");

            player = other.GetComponent<PlayerScript>();

            if (sleepPromptUI != null)
                sleepPromptUI.SetActive(true);
            if (crow != null)
                crow.SetActive(true);
            if (crowCross != null)
                crowCross.SetActive(true);
        }
        if (other.CompareTag("Player") && triggerType == TriggerType.AfterStorm)
        {
            if (sleepPromptUI != null)
                sleepPromptUI.SetActive(true);

            player = other.GetComponent<PlayerScript>();
            player.stormData.isInStorm = false;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = null;

            if (sleepPromptUI != null)
                sleepPromptUI.SetActive(false);
        }
    }

    public void OnSleepButtonPressed()
    {
        if (player != null && !player.IsSleeping && !hasSleepTriggered)
        {
            hasSleepTriggered = true;
            StartSleepSequence();
        }
    }

    private void StartSleepSequence()
    {
        if (triggerType == TriggerType.AfterStorm)
        {
            StartCoroutine(AfterStormSleepSequence());
            return;
        }

        StartCoroutine(SleepSequence());
    }

    private IEnumerator AfterStormSleepSequence()
    {
        // === PHASE 1: Start sleep, show sleep camera ===
        player.AfterStormStartSleeping(sleepDuration);

        if (sleepCamera != null)
            sleepCamera.SetActive(true);
        if (mainCamera != null)
            mainCamera.SetActive(false);

        // === PHASE 2: After 5 seconds, switch to crow camera and send crow flying ===
        yield return new WaitForSeconds(7f);
        onEventExecution?.Invoke();
        // === PHASE 3: Wait for remaining sleep duration ===
        float remainingSleep = afterSleepDuration - 0.15f;
        if (remainingSleep > 0)
            yield return new WaitForSeconds(remainingSleep);

        // === PHASE 4: Sleep finished — switch back to main camera ===

        if (sleepCamera != null)
            sleepCamera.SetActive(false);
        if (mainCamera != null)
            mainCamera.SetActive(true);

        if (sleepPromptUI != null)
            sleepPromptUI.SetActive(false);
        if (sleepCanvas != null)
            sleepCanvas.SetActive(false);
            enableEvents?.Invoke();
            disableEvents?.Invoke();

        // Disable collider so this never triggers again
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

 OnSleepingFinish?.Invoke();
        player.StopStormSleeping();
        


        // PostSleepPlayerScript postSleepPlayerScript = GameObject
        //     .FindGameObjectWithTag("Player")
        //     .GetComponent<PostSleepPlayerScript>();
        // postSleepPlayerScript.Activate();
    }
}
