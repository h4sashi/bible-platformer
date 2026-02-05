using UnityEngine;
using UnityEngine.AI;

public class WolfFSM : MonoBehaviour, IDamageable
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
    
    public GameObject deathCam;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip howlClip;
    public AudioClip attackClip;
    public AudioClip hurtClip;
    public AudioClip deathClip;

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerScript playerScript;
    private Transform player;

    // --- STATE FLAGS ---
    [Header("State Flags")]
    private bool isIdle;
    private bool isHowling;
    private bool isChasing;
    private bool isAttacking;
    private bool isDead;

    // --- TIMERS ---
    private float howlCooldownTimer;
    private float howlTimer;

    [Header("Attack Settings")]
    public int dealDamageAmount = 5;
    public float attackCooldown = 6f;
    private float lastAttackTime = -999f;

    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Death Settings")]
    public float deathDuration = 12f; // Time before destroying the GameObject
    public GameObject deathVFX; // Optional particle effect on death
    public bool dropLoot = false; // Optional loot system
    public GameObject lootPrefab; // Optional loot to drop

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        isDead = false;
    }

    void Start()
    {
        // Find and cache the player reference at start
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerScript = playerObj.GetComponent<PlayerScript>();
            if (playerScript == null)
            {
                Debug.LogError("Player found but PlayerScript component is missing!");
            }
        }
        else
        {
            Debug.LogError("Player object not found! Make sure player has 'Player' tag.");
        }
    }

    void Update()
    {
        // Don't update AI if dead
        if (isDead)
            return;

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

            if (howlClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(howlClip);
            }
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
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            return;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            isAttacking = true;
        }
    }

    // Animation and Attack Handling Event
    public void HandleAttack()
    {
        if (!isAttacking || isHowling)
            return;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        // Check if enough time has passed since last attack
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // Play attack sound
            if (attackClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackClip);
            }

            // Deal damage to player
            if (playerScript != null)
            {
                Debug.Log("Wolf attacks player for " + dealDamageAmount + " damage.");
                playerScript.OnDamagedTaken(dealDamageAmount);
                lastAttackTime = Time.time;
            }
            else
            {
                Debug.LogWarning("PlayerScript reference is null! Cannot deal damage.");
            }
        }

        // Check if player moved out of attack range
        if (player != null && Vector3.Distance(transform.position, player.position) > attackRange)
        {
            isAttacking = false;
        }
    }

    void ResolveAnimations()
    {
        if (isAttacking)
        {
            // Only play attack animation if not already playing
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            {
                animator.Play("Attack");
            }
            return;
        }

        if (isChasing)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
            {
                animator.Play("Run");
            }
            return;
        }

        if (isHowling)
        {
            // Howl animation already started in HandleHowl
            return;
        }

        if (isIdle)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            {
                animator.Play("Idle");
            }
        }
    }

    // IDamageable interface implementation
    public void TakeDamage(float damage)
    {
        // Don't take damage if already dead
        if (isDead)
            return;

        currentHealth -= (int)damage;
        
        Debug.Log($"Wolf took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        // Play hurt sound
        if (hurtClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(hurtClip);
        }

        // Play hurt animation if available
        if (animator != null && currentHealth > 0)
        {
            // You can trigger a hurt animation here if you have one
            // animator.SetTrigger("Hurt");
        }

        // Check if wolf died
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    // Overload for damage with source position (kept for compatibility)
    public void TakeDamage(float damage, Vector3 damageSourcePosition)
    {
        TakeDamage(damage);
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        
        Debug.Log("Wolf has died!");

        // Stop all AI behavior
        isChasing = false;
        isAttacking = false;
        isHowling = false;
        
        // Stop the NavMeshAgent with safety checks
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

         Destroy(this.GetComponent<Rigidbody>());
         Destroy(this.GetComponent<SphereCollider>());
        // Destroy(this.GetComponent<Animator>());

        // Play death sound
        if (deathClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathClip);
        }

        // Play death animation using CrossFade for smoother transition
        if (animator != null)
        {
            animator.CrossFade("Death", 0.1f);
        }

        // Spawn death VFX if assigned
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }

       

       
        // Collider collider = GetComponent<Collider>();
        // if (collider != null)
        // {
        //     collider.enabled = false;
        // }

        
        Destroy(gameObject, deathDuration);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}