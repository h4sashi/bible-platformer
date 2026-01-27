using UnityEngine;
using UnityEngine.AI;

public class WolfFSM : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask playerMask;
    public float detectionRadius = 15f;
    public float attackRange = 2f;

    [Header("Movement")]
    public float chaseSpeed = 4.5f;

    [Header("Howl")]
    public float howlCooldown = 6f;
    public float howlDuration = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip howlClip;
    public AudioClip attackClip;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;

    // --- STATE FLAGS ---

    private bool isIdle;

    private bool isHowling;
    private bool isChasing;
    private bool isAttacking;

    // --- TIMERS ---
    private float howlCooldownTimer;
    private float howlTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        DetectPlayer();

        HandleHowl();
        HandleChase();
        HandleAttack();
        HandleIdle();

        ResolveAnimations();
    }

    void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerMask);

        if (hits.Length > 0)
        {
            player = hits[0].transform;
            isChasing = true;
        }
        else
        {
            player = null;
            isChasing = false;
            isAttacking = false;
        }
    }

    void HandleIdle()
    {
        // Idle is true ONLY when nothing else is happening
        isIdle = !isHowling && !isChasing && !isAttacking;
    }

    void HandleHowl()
    {
        howlCooldownTimer -= Time.deltaTime;

        // Start howl ONLY if:
        // - not chasing
        // - not attacking
        // - not already howling
        // - cooldown finished
        if (!isChasing && !isAttacking && !isHowling && howlCooldownTimer <= 0f)
        {
            isHowling = true;
            howlTimer = howlDuration;
            howlCooldownTimer = howlCooldown;

            animator.Play("Howl");
            audioSource.PlayOneShot(howlClip);
        }

        if (isHowling)
        {
            howlTimer -= Time.deltaTime;

            if (howlTimer <= 0f)
            {
                isHowling = false;
            }
        }
    }

    void HandleChase()
    {
        if (!isChasing || isHowling)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            isAttacking = true;
        }
    }

    void HandleAttack()
    {
        if (!isAttacking || isHowling)
            return;

        agent.isStopped = true;

        if (attackClip && !audioSource.isPlaying)
            audioSource.PlayOneShot(attackClip);

        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            isAttacking = false;
        }
    }

  void ResolveAnimations()
{
    if (isAttacking)
    {
        animator.Play("Attack");
        return;
    }

    if (isChasing)
    {
        animator.Play("Run");
        return;
    }

    if (isHowling)
    {
        // already playing howl, do nothing
        return;
    }

    if (isIdle)
    {
        animator.Play("Idle");
    }
}


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
