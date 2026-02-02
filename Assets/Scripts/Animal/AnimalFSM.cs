using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public abstract class AnimalFSM : MonoBehaviour
{
    public enum AnimalState
    {
        Idle,
        Howl,
        Chase,
        Attack,
    }

    [Header("Detection")]
    public LayerMask playerMask;
    public float detectionRadius = 15f;
    public float attackRange = 2f;

    [Header("Movement Speeds")]
    public float idleSpeed = 0f;
    public float chaseSpeed = 5.5f;
    public float attackSpeed = 0f;

    protected Transform player;
    protected NavMeshAgent agent;
    protected Animator animator;

    protected AnimalState currentState;

   

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        ChangeState(AnimalState.Idle);
    }

    protected virtual void Update()
    {
        UpdateState();
    }

    protected void ChangeState(AnimalState newState)
    {
        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
    }

    protected virtual void EnterState(AnimalState state) { }

    protected virtual void UpdateState() { }

    protected virtual void ExitState(AnimalState state) { }

    protected bool DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerMask);
        if (hits.Length > 0)
        {
            player = hits[0].transform;
            return true;
        }
        return false;
    }

    protected bool PlayerInAttackRange()
    {
        if (player == null)
            return false;
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }
}
