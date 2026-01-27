using System.Collections;
using UnityEngine;
using Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Cinemachine")]
    [SerializeField]
    private CinemachineVirtualCamera virtualCamera;

    private CinemachineBasicMultiChannelPerlin noise;

    [Header("Shake Presets")]
    [SerializeField]
    private float lightShakeIntensity = 1f;
    [SerializeField]
    private float lightShakeDuration = 0.2f;

    [SerializeField]
    private float mediumShakeIntensity = 2.5f;
    [SerializeField]
    private float mediumShakeDuration = 0.4f;

    [SerializeField]
    private float heavyShakeIntensity = 4f;
    [SerializeField]
    private float heavyShakeDuration = 0.6f;

    private Coroutine currentShake;

    void Awake()
{
    // Singleton pattern
    if (Instance == null)
    {
        Instance = this;
    }
    else
    {
        Destroy(gameObject);
        return;
    }

    // Get the virtual camera if not assigned
    if (virtualCamera == null)
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    // Get the noise component
    if (virtualCamera != null)
    {
        noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        
        if (noise == null)
        {
            Debug.LogError("CinemachineBasicMultiChannelPerlin component not found! Please add it to your Virtual Camera.");
        }
        else
        {
            // DISABLE NOISE ON START
            noise.m_AmplitudeGain = 0f;
            noise.m_FrequencyGain = 0f;
        }
    }
    else
    {
        Debug.LogError("No CinemachineVirtualCamera assigned or found!");
    }
}

    /// <summary>
    /// Trigger a custom shake with specified intensity and duration
    /// </summary>
    public void Shake(float intensity, float duration)
    {
        if (noise == null) return;

        // Stop any existing shake
        if (currentShake != null)
        {
            StopCoroutine(currentShake);
        }

        currentShake = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    /// <summary>
    /// Trigger a light shake (e.g., footsteps, small impacts)
    /// </summary>
    public void ShakeLight()
    {
        Shake(lightShakeIntensity, lightShakeDuration);
    }

    /// <summary>
    /// Trigger a medium shake (e.g., explosions, player damage)
    /// </summary>
    public void ShakeMedium()
    {
        Shake(mediumShakeIntensity, mediumShakeDuration);
    }

    /// <summary>
    /// Trigger a heavy shake (e.g., large explosions, boss attacks)
    /// </summary>
    public void ShakeHeavy()
    {
        Shake(heavyShakeIntensity, heavyShakeDuration);
    }

    /// <summary>
    /// Start a continuous shake that persists until stopped
    /// </summary>
    public void StartContinuousShake(float intensity)
    {
        if (noise == null) return;

        if (currentShake != null)
        {
            StopCoroutine(currentShake);
        }

        noise.m_AmplitudeGain = intensity;
        noise.m_FrequencyGain = intensity;
    }

    /// <summary>
    /// Stop any ongoing shake
    /// </summary>
    public void StopShake()
    {
        if (currentShake != null)
        {
            StopCoroutine(currentShake);
            currentShake = null;
        }

        if (noise != null)
        {
            noise.m_AmplitudeGain = 0f;
            noise.m_FrequencyGain = 0f;
        }
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        noise.m_AmplitudeGain = intensity;
        noise.m_FrequencyGain = intensity;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Optional: fade out the shake over time
            float t = 1f - (elapsed / duration);
            noise.m_AmplitudeGain = Mathf.Lerp(0f, intensity, t);
            noise.m_FrequencyGain = Mathf.Lerp(0f, intensity, t);

            yield return null;
        }

        // Reset the shake
        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;

        currentShake = null;
    }
}