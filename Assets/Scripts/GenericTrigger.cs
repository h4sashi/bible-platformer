using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        if (other.CompareTag("Cross"))
        {
            if (playerScript != null)
            {
                if (playerScript.isCasting == true && !isCompleted)
                {
                    // Check cooldown
                    if (Time.time - lastHitTime >= hitCooldown)
                    {
                        numberOfHits++;
                        lastHitTime = Time.time;

                        Debug.Log($"Hit registered! Total hits: {numberOfHits}/{maxHits}");

                        // Check if we've reached max hits
                        if (numberOfHits >= maxHits)
                        {
                            OnHitComplete();
                        }
                    }
                }
            }
        }
    }

    void OnHitComplete()
    {
        if (isCompleted)
            return; // Prevent multiple calls

        isCompleted = true;

        Debug.Log("Hit complete! Launching objects...");

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
                StartCoroutine(EnableWaterFountain(.1f));
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
