using UnityEngine;
using System;

public class RavenScript : MonoBehaviour
{
    [Header("Movement")]
    public Transform target;
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.2f;

    private Animation anim;
    private bool hasReachedTarget = false;

    public string flyAnimName = "CrowFlap";
    public string idleAnimName = "CrowIdle";

    public GameObject crowCamera;

    // SleepTrigger will subscribe to this to know when crow has arrived
    public Action onReachedTarget;

    private bool isFlying = false; // Crow only moves when told to

    void Start()
    {
        anim = GetComponent<Animation>();
        if (anim == null)
        {
            Debug.LogError("No Animation component found!");
            return;
        }

        // Don't auto-play on start — wait until SleepTrigger activates it
    }

    void Update()
    {
        if (target == null || hasReachedTarget || !isFlying)
            return;

        MoveToTarget();
    }

    // Called by SleepTrigger when it's time for crow to fly
    public void StartFlying()
    {
        isFlying = true;
        hasReachedTarget = false;

        if (anim != null)
            anim.Play(flyAnimName);
    }

    void MoveToTarget()
    {
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

        if (anim != null)
        {
            anim.Stop();
            anim.Play(idleAnimName);
        }

        // Notify SleepTrigger that crow has arrived
        onReachedTarget?.Invoke();
    }
}