using UnityEngine;

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

    void Start()
    {
        anim = GetComponent<Animation>();

        if (anim == null)
        {
            Debug.LogError("No Animation component found!");
            return;
        }

        // Start flying animation
        anim.Play(flyAnimName);
    }

    void Update()
    {
        if (target == null || hasReachedTarget)
            return;

        MoveToTarget();
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

        // Face movement direction
        transform.rotation = Quaternion.LookRotation(direction);

        // Move forward
        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
    }

    void ReachTarget()
    {
        hasReachedTarget = true;

        anim.Stop();
        anim.Play(idleAnimName);
    }
}