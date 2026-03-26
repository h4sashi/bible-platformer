using System;
using System.Collections;
using UnityEngine;

public class RavenScript : MonoBehaviour
{
    public GameObject playerControlUI;

    [Header("Movement")]
    public Transform target;
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.2f;

    private Animation anim;
    private bool hasReachedTarget = false;

    public string flyAnimName = "CrowFlap";
    public string idleAnimName = "CrowIdle";

    public GameObject crowCamera;

    public GameObject breadTransform;

    // SleepTrigger will subscribe to this to know when crow has arrived
    public Action onReachedTarget;

    private bool isFlying = false;

    [Header("Fly Away Settings")]
    public float flyAwayHeight = 10f; // How high the crow flies when leaving
    public float flyAwayHorizontalRange = 8f; // Random horizontal spread
    public float flyAwaySpeed = 6f; // Speed of the fly-away movement
    public float disableDelay = 3f; // Seconds after reaching fly-away point before disabling

    private bool isFlyingAway = false;
    private Vector3 flyAwayTarget;

    void Start()
    {
        anim = GetComponent<Animation>();
        if (anim == null)
        {
            Debug.LogError("No Animation component found!");
            return;
        }
    }

    void Update()
    {
        if (isFlyingAway)
        {
            MoveFlyAway();
            return;
        }

        if (target == null || hasReachedTarget || !isFlying)
            return;

        MoveToTarget();
    }

    // Called by SleepTrigger when it's time for crow to fly to its perch
    public void StartFlying()
    {
        if (breadTransform.gameObject != null)
            breadTransform.gameObject.SetActive(true);
        isFlying = true;
        hasReachedTarget = false;

        if (anim != null)
            anim.Play(flyAnimName);
    }

    void MoveToTarget()
    {
        playerControlUI.SetActive(false);
        Vector3 direction = (target.position - transform.position);
        float distance = direction.magnitude;

        if (distance <= stoppingDistance)
        {
            ReachTarget();
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction);
        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
    }

    void ReachTarget()
    {
        hasReachedTarget = true;
        isFlying = false;
        playerControlUI.SetActive(true);

        if (anim != null)
        {
            anim.Stop();
            anim.Play(idleAnimName);
        }

        // Notify SleepTrigger that crow has arrived at perch
        onReachedTarget?.Invoke();
    }

    // =============================================
    // FLY AWAY — called by PostSleepPlayerScript
    // on the player's first movement after sleep
    // =============================================

    public void FlyAway()
    {
        if (isFlyingAway)
            return; // Already flying away, don't re-trigger

        isFlyingAway = true;
        isFlying = false;

        // Pick a random point upward and slightly to the side
        float randomX = UnityEngine.Random.Range(-flyAwayHorizontalRange, flyAwayHorizontalRange);
        float randomZ = UnityEngine.Random.Range(-flyAwayHorizontalRange, flyAwayHorizontalRange);

        flyAwayTarget = new Vector3(
            transform.position.x + randomX,
            transform.position.y + flyAwayHeight,
            transform.position.z + randomZ
        );

        // Switch back to flying animation
        if (anim != null)
        {
            anim.Stop();
            anim.Play(flyAnimName);
        }

        Debug.Log($"Crow flying away to {flyAwayTarget}");
    }

    void MoveFlyAway()
    {
        Vector3 direction = (flyAwayTarget - transform.position);
        float distance = direction.magnitude;

        breadTransform.transform.parent = null;
        breadTransform.GetComponent<Rigidbody>().isKinematic = false;

        if (distance <= stoppingDistance)
        {
            // Reached the fly-away point — disable after a short delay
            StartCoroutine(DisableAfterDelay());
            isFlyingAway = false; // Stop updating so coroutine isn't spammed
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction);
        transform.position += direction.normalized * flyAwaySpeed * Time.deltaTime;
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(disableDelay);

        Debug.Log("Crow has flown away — disabling GameObject");
        gameObject.SetActive(false);
    }
}
