using Pinwheel.Poseidon;
using UnityEngine;

public class GlideTrigger : MonoBehaviour
{
    private const float InputDeadZone = 0.01f;
    private const float WaterContactPadding = 0.05f;
    private static readonly Vector3 GlideSeatLocalPosition = new Vector3(0f, 0f, -0.05f);
    private static readonly Quaternion GlideSeatLocalRotation = Quaternion.Euler(0f, 0f, 0f);

    public Collider waterCollider;
    public enum TriggerType
    {
        GlideZoneOn,
        GlideZoneOff,
        WaterFountain,
        RockObstacle,
        Cross,
    }

    public TriggerType triggerType;

    [Header("Gliding Settings")]
    public bool IsPlayerGliding = false;
    public Transform player;

    [Header("Glide Data ID")]
    public string glideDataId = "default"; // Set to "mountain" on mountain glide triggers

    [SerializeField]
    private float glideSpeed = 5f;

    [SerializeField]
    private float smoothRotation = 10f;
    public float rotGlideAxes = 0;

    [Header("Auto Glide")]
    [SerializeField]
    private bool autoMoveDefaultGlide = true;

    [SerializeField]
    [Range(-1f, 1f)]
    private float defaultAutoMoveInput = 1f;

    private float horizontalInput;
    private Vector3 moveDirection;

    private PlayerScript playerScript;
    private Collider[] glideColliders;
    private Collider[] playerColliders;
    private bool isTouchingWater;
    private Quaternion dryLocalRotation;
    private float waveSeed;

    [Header("Water Floating")]
    [SerializeField]
    private bool floatOnWater = true;

    [SerializeField]
    private float waterSurfaceOffset = 0.2f;

    [SerializeField]
    private float floatSmoothing = 6f;

    [SerializeField]
    private float waveAmplitude = 0.18f;

    [SerializeField]
    private float waveFrequency = 1.4f;

    [SerializeField]
    private float waveHorizontalScale = 0.35f;

    [SerializeField]
    private float waveTiltAngle = 3f;

    [SerializeField]
    private PWater poseidonWater;

    [SerializeField]
    private bool applyPoseidonRipple = false;

    [Header("Mobile Controls")]
    [SerializeField]
    private bool useMobileControls = false;
    private float mobileHorizontalInput;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerScript = player.GetComponent<PlayerScript>();
            playerColliders = player.GetComponentsInChildren<Collider>();
        }
        else
        {
            Debug.LogWarning($"{nameof(GlideTrigger)} on {name} could not find a Player tag.");
        }

        glideColliders = GetComponentsInChildren<Collider>();
        dryLocalRotation = transform.localRotation;
        waveSeed = transform.GetInstanceID() * 0.137f;

        if (poseidonWater == null && waterCollider != null)
        {
            poseidonWater = waterCollider.GetComponentInParent<PWater>();
        }

        if (floatOnWater && waterCollider == null)
        {
            Debug.LogWarning($"{nameof(GlideTrigger)} on {name} needs a waterCollider to float.");
        }
    }

    void Update()
    {
        if (IsPlayerGliding == true && player != null)
        {
            GetGlideInput();
            HandleGlideMovement();
        }

        HandleWaterFloating();
    }

    private void LateUpdate()
    {
        if (IsPlayerGliding == true && player != null)
        {
            SnapPlayerToGlideSeat();
        }
    }

    // =====================
    // MOBILE UI CALLBACKS
    // =====================

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

    // =====================
    // GLIDE INPUT & MOVEMENT
    // =====================

    void GetGlideInput()
    {
        horizontalInput = useMobileControls
            ? mobileHorizontalInput
            : Input.GetAxisRaw("Horizontal");

        if (autoMoveDefaultGlide && Mathf.Abs(horizontalInput) <= InputDeadZone)
        {
            horizontalInput = defaultAutoMoveInput >= 0f ? 1f : -1f;
        }
    }

    void HandleGlideMovement()
    {
        if (Mathf.Abs(horizontalInput) > InputDeadZone)
        {
            moveDirection = new Vector3(0, 0, horizontalInput);
            transform.Translate(moveDirection * glideSpeed * Time.deltaTime, Space.World);
        }
    }

    void HandleGlideRotation()
    {
        if (Mathf.Abs(horizontalInput) > InputDeadZone && player != null)
        {
            Quaternion targetRotation =
                horizontalInput < 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 90f, 0);

            player.rotation = Quaternion.Lerp(
                player.rotation,
                targetRotation,
                smoothRotation * Time.deltaTime
            );
        }
    }

    // =====================
    // COLLISION
    // =====================

    private void OnTriggerEnter(Collider other)
    {
        if (IsAssignedWaterCollider(other))
        {
            isTouchingWater = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsAssignedWaterCollider(other))
        {
            isTouchingWater = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsAssignedWaterCollider(other))
        {
            isTouchingWater = false;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player") || triggerType != TriggerType.Cross)
            return;

        Debug.Log("Player has entered cross");

        PlayerScript ps = other.gameObject.GetComponent<PlayerScript>();
        if (ps != null)
        {
            GlideData data = ps.GetGlideDataById(glideDataId);
            ps.StartGliding(data);
        }

        player = other.transform;
        player.SetParent(transform);
        SnapPlayerToGlideSeat();
        IsPlayerGliding = true;
    }

    // =====================
    // STOP GLIDING
    // =====================

    public void StopGliding()
    {
        if (playerScript == null)
            return;

        GlideData data = playerScript.GetGlideDataById(glideDataId);
        playerScript.StopGliding(data);
        IsPlayerGliding = false;
    }

    private void HandleWaterFloating()
    {
        if (!floatOnWater || waterCollider == null || !waterCollider.enabled)
        {
            return;
        }

        isTouchingWater = IsOverlappingWater();

        if (!isTouchingWater)
        {
            dryLocalRotation = transform.localRotation;
            return;
        }

        float surfaceY = waterCollider.bounds.max.y + waterSurfaceOffset;
        float wavePhase = GetWavePhase(transform.position);
        float targetY = GetWaterSurfaceY(transform.position, surfaceY, wavePhase);
        float smoothing = GetSmoothingFactor(floatSmoothing);

        Vector3 targetPosition = transform.position;
        targetPosition.y = Mathf.Lerp(targetPosition.y, targetY, smoothing);
        transform.position = targetPosition;

        ApplyWaveTilt(wavePhase, smoothing);
    }

    private bool IsOverlappingWater()
    {
        Bounds waterBounds = waterCollider.bounds;

        if (DoAnyCollidersOverlapWater(glideColliders, waterBounds))
            return true;

        if (IsPlayerGliding && DoAnyCollidersOverlapWater(playerColliders, waterBounds))
            return true;

        return IsPointOverWaterFootprint(transform.position, waterBounds);
    }

    private bool DoAnyCollidersOverlapWater(Collider[] colliders, Bounds waterBounds)
    {
        if (colliders == null)
            return false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider checkedCollider = colliders[i];
            if (
                checkedCollider == null
                || !checkedCollider.enabled
                || checkedCollider == waterCollider
            )
                continue;

            if (waterBounds.Intersects(checkedCollider.bounds))
                return true;
        }

        return false;
    }

    private bool IsPointOverWaterFootprint(Vector3 point, Bounds waterBounds)
    {
        float allowedAboveSurface =
            Mathf.Abs(waterSurfaceOffset) + waveAmplitude + WaterContactPadding;

        return point.x >= waterBounds.min.x - WaterContactPadding
            && point.x <= waterBounds.max.x + WaterContactPadding
            && point.z >= waterBounds.min.z - WaterContactPadding
            && point.z <= waterBounds.max.z + WaterContactPadding
            && point.y >= waterBounds.min.y - WaterContactPadding
            && point.y <= waterBounds.max.y + allowedAboveSurface;
    }

    private bool IsAssignedWaterCollider(Collider other)
    {
        return waterCollider != null && other == waterCollider;
    }

    private float GetWaterSurfaceY(Vector3 worldPosition, float fallbackSurfaceY, float wavePhase)
    {
        if (poseidonWater != null && poseidonWater.Profile != null)
        {
            Vector3 localWaterPosition = poseidonWater.transform.InverseTransformPoint(worldPosition);
            localWaterPosition.y = 0f;

            Vector3 localSurfacePosition = poseidonWater.GetLocalVertexPosition(
                localWaterPosition,
                applyPoseidonRipple
            );
            Vector3 worldSurfacePosition = poseidonWater.transform.TransformPoint(localSurfacePosition);

            return worldSurfacePosition.y + waterSurfaceOffset;
        }

        return fallbackSurfaceY + Mathf.Sin(wavePhase) * waveAmplitude;
    }

    private float GetWavePhase(Vector3 position)
    {
        return Time.time * waveFrequency
            + waveSeed
            + (position.x + position.z) * waveHorizontalScale;
    }

    private float GetSmoothingFactor(float speed)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0.01f, speed) * Time.deltaTime);
    }

    private void ApplyWaveTilt(float wavePhase, float smoothing)
    {
        if (waveTiltAngle <= 0f)
            return;

        Quaternion waveTilt = Quaternion.Euler(
            Mathf.Sin(wavePhase * 0.73f) * waveTiltAngle,
            0f,
            Mathf.Cos(wavePhase) * waveTiltAngle
        );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            dryLocalRotation * waveTilt,
            smoothing
        );
    }

    private void SnapPlayerToGlideSeat()
    {
        player.localPosition = GlideSeatLocalPosition;
        player.localRotation = GlideSeatLocalRotation;
    }
}
