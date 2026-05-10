using System;
using UnityEngine;

public class GlideTrigger : MonoBehaviour
{
    private const string DefaultGlideDataId = "default";
    private const float InputDeadZone = 0.01f;
    private static readonly Vector3 GlideSeatLocalPosition = new Vector3(0f, 0f, -0.05f);
    private static readonly Quaternion GlideSeatLocalRotation = Quaternion.Euler(0f, 0f, 0f);

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

    [Header("Default Auto Glide")]
    [SerializeField]
    private bool autoMoveDefaultGlide = true;

    [SerializeField]
    [Range(-1f, 1f)]
    private float defaultAutoMoveInput = 1f;

    private float horizontalInput;
    private Vector3 moveDirection;

    private PlayerScript playerScript;

    [Header("Mobile Controls")]
    [SerializeField]
    private bool useMobileControls = false;
    private float mobileHorizontalInput;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerScript = player.GetComponent<PlayerScript>();
    }

    void Update()
    {
        if (IsPlayerGliding == true && player != null)
        {
            GetGlideInput();
            HandleGlideMovement();
        }
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

        if (
            autoMoveDefaultGlide
            && IsDefaultGlideData()
            && Mathf.Abs(horizontalInput) <= InputDeadZone
        )
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

    private bool IsDefaultGlideData()
    {
        return string.IsNullOrWhiteSpace(glideDataId)
            || string.Equals(glideDataId, DefaultGlideDataId, StringComparison.OrdinalIgnoreCase);
    }

    private void SnapPlayerToGlideSeat()
    {
        player.localPosition = GlideSeatLocalPosition;
        player.localRotation = GlideSeatLocalRotation;
    }
}
