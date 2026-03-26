using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GenericTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        GlideZoneOn,
        GlideZoneOff,
        WaterFountain,
        RockObstacle,
    }

    public TriggerType triggerType;

    public int numberOfHits;
    public int maxHits;
    public GameObject[] targetObjects;
    public GameObject waterFountain;
    public GameObject waterCanvas;

    public Button actionBtn;

    [Header("Force Settings")]
    [SerializeField]
    private float minUpwardForce = 5f;

    [SerializeField]
    private float maxUpwardForce = 10f;

    [SerializeField]
    private float minSideForce = 2f;

    [SerializeField]
    private float maxSideForce = 5f;

    [SerializeField]
    private float minTorque = 50f;

    [SerializeField]
    private float maxTorque = 150f;

    [Header("Hit Detection")]
    [SerializeField]
    private float hitCooldown = 0.5f; // Cooldown between hits

    [Header("Optional")]
    [SerializeField]
    private bool destroyAfterDelay = true;

    [SerializeField]
    private float destroyDelay = 3f;

    private PlayerScript playerScript;
    private bool isCompleted = false;
    private float lastHitTime = -999f; // Track last hit time

    private void Start()
    {
        playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Debug.Log("Player entered trigger zone");
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.RockObstacle)
        {
            Debug.Log("Player has hit Rock Obstacle");
            other.GetComponent<PlayerScript>().OnRockObstacleTriggerEnter(this.transform.position);


                if (actionBtn != null)
                {
                    actionBtn.gameObject.SetActive(true);
                }
            else
            {
                Debug.LogWarning("Action button reference is missing!");
            }
        }
    }

    

  void OnTriggerStay(Collider other)
{
    if (other.CompareTag("Player") && triggerType == TriggerType.RockObstacle)
    {
        if (playerScript != null && !isCompleted)
        {
            // Check if pull animation just completed (one-time hit per pull)
            if (playerScript.IsPullAnimationComplete && Time.time - lastHitTime >= hitCooldown)
            {
                numberOfHits++;
                lastHitTime = Time.time;

                Debug.Log($"Pull hit registered! Total hits: {numberOfHits}/{maxHits}");
                other.GetComponent<PlayerScript>().ResetRockRotation();

                // Reset the completion flag so it doesn't register multiple times
                playerScript.ResetPullCompletion();

                // Visual feedback
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.ShakeLight();
                }

                // Check if we've reached max hits
                if (numberOfHits >= maxHits)
                {
                    OnHitComplete();
                    actionBtn.interactable = false; // Disable button after completion
                }
            }
        }
    }
}

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && triggerType == TriggerType.RockObstacle)
        {
            Debug.Log("Player has exited Rock Obstacle Zone");
            other.GetComponent<PlayerScript>().OnRockObstacleTriggerExit();
                if (actionBtn != null)
                {
                    actionBtn.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("Action button reference is missing!");
                }
        }
    }

    void OnHitComplete()
    {
        if (isCompleted)
            return; // Prevent multiple calls

        isCompleted = true;

        Debug.Log("Hit complete! Launching objects...");

        // Notify player that pulling is complete
        if (playerScript != null && triggerType == TriggerType.RockObstacle)
        {
            playerScript.OnRockObstacleComplete();
        }

        foreach (GameObject obj in targetObjects)
        {
            if (obj == null)
                continue;

            // Get or add Rigidbody
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = obj.AddComponent<Rigidbody>();
            }

            // Enable physics
            rb.isKinematic = false;
            rb.useGravity = true;

            // Calculate random force direction
            float upwardForce = Random.Range(minUpwardForce, maxUpwardForce);
            float sideForceX = Random.Range(-minSideForce, maxSideForce);
            float sideForceZ = Random.Range(-minSideForce, maxSideForce);

            Vector3 randomForce = new Vector3(sideForceX, upwardForce, sideForceZ);

            // Apply force
            rb.AddForce(randomForce, ForceMode.Impulse);

            // Add random torque for spinning effect
            Vector3 randomTorque = new Vector3(
                Random.Range(minTorque, maxTorque),
                Random.Range(minTorque, maxTorque),
                Random.Range(minTorque, maxTorque)
            );
            rb.AddTorque(randomTorque);

            // Optional: Destroy after delay
            if (destroyAfterDelay)
            {
                waterCanvas.SetActive(false);
                Destroy(obj, destroyDelay);
                StartCoroutine(EnableWaterFountain((.1f)));
            }
        }

        // Optional: Add camera shake effect
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeMedium();
        }
    }

    IEnumerator EnableWaterFountain(float y)
    {
        yield return new WaitForSeconds(y);
        waterFountain.SetActive(true);
    }
}
