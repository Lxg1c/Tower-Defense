using System;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class AllySoldier : Damageable
{
    public enum Mode
    {
        Formation,
        Attack,
        FollowPlayer
    }

    [Header("Targeting")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float awarenessRadius = 18f;
    [SerializeField] private int maxTargets = 24;

    [Header("Combat")]
    [SerializeField] private float damage = 8f;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float attackRange = 1.4f;

    [Header("Movement")]
    [SerializeField] private float destinationRefreshInterval = 0.2f;
    [SerializeField] private float playerAttackRadius = 10f;
    [SerializeField] private float playerFollowStoppingDistance = 2f;
    [SerializeField] private string attackTriggerName = "Attack";

    public event Action<AllySoldier> Died;

    private NavMeshAgent agent;
    private Animator animator;
    private Collider[] ownColliders;
    private Collider[] ignoredPlayerColliders = Array.Empty<Collider>();
    private Collider[] targetBuffer;
    private Transform player;
    private Vector3 formationPoint;
    private Vector3 slotOffset;
    private Mode mode;
    private Damageable currentTarget;
    private float attackCooldown;
    private float refreshTimer;

    public override bool IsTargetable => base.IsTargetable && isActiveAndEnabled;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        ownColliders = GetComponentsInChildren<Collider>();
        targetBuffer = new Collider[Mathf.Max(4, maxTargets)];
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResetToMaxHealth();
        attackCooldown = 0f;
        refreshTimer = 0f;
        currentTarget = null;

        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.stoppingDistance = attackRange * 0.9f;
            }
        }
    }

    protected override void OnDisable()
    {
        SetPlayerCollisionIgnore(false);
        base.OnDisable();
    }

    public void Init(Vector3 formationPoint, Vector3 slotOffset, Transform player)
    {
        this.formationPoint = formationPoint;
        this.slotOffset = slotOffset;
        this.player = player != null ? player : FindPlayer();
        SetFormationMode();
    }

    public void SetFormationMode()
    {
        mode = Mode.Formation;
        currentTarget = null;
        SetDestination(formationPoint + slotOffset, 0f);
    }

    public void SetAttackMode()
    {
        mode = Mode.Attack;
        refreshTimer = 0f;
    }

    public void SetFollowPlayerMode()
    {
        mode = Mode.FollowPlayer;
        currentTarget = null;
        refreshTimer = 0f;
    }

    private void Update()
    {
        if (!IsAlive)
            return;

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = destinationRefreshInterval;
            RefreshDestination();
        }

        if ((mode == Mode.Attack || mode == Mode.FollowPlayer) && currentTarget != null && currentTarget.IsTargetable)
            TryAttackCurrentTarget();
    }

    private void RefreshDestination()
    {
        switch (mode)
        {
            case Mode.Formation:
                SetDestination(formationPoint + slotOffset, 0f);
                break;
            case Mode.FollowPlayer:
                RefreshFollowPlayerDestination();
                break;
            case Mode.Attack:
                currentTarget = FindNearestEnemy();
                if (currentTarget != null)
                    SetDestination(GetTargetPoint(currentTarget), attackRange * 0.9f);
                break;
        }
    }

    private void TryAttackCurrentTarget()
    {
        Vector3 targetPoint = GetTargetPoint(currentTarget);
        float distance = Vector3.Distance(transform.position, targetPoint);
        if (distance > attackRange)
            return;

        Face(targetPoint);

        attackCooldown -= Time.deltaTime;
        if (attackCooldown > 0f)
            return;

        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
            animator.SetTrigger(attackTriggerName);

        currentTarget.TakeDamage(damage);
        attackCooldown = 1f / Mathf.Max(0.01f, attackRate);
    }

    private Damageable FindNearestEnemy()
    {
        return FindNearestEnemy(transform.position, awarenessRadius);
    }

    private Damageable FindNearestEnemy(Vector3 center, float radius)
    {
        int count = Physics.OverlapSphereNonAlloc(center, radius, targetBuffer, enemyMask, QueryTriggerInteraction.Collide);
        Damageable best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider hit = targetBuffer[i];
            if (hit == null || hit.transform.IsChildOf(transform))
                continue;

            Damageable candidate = hit.GetComponentInParent<Damageable>();
            if (candidate == null || !candidate.IsTargetable || candidate == this)
                continue;

            float sqr = (candidate.transform.position - center).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = candidate;
            }
        }

        return best;
    }

    private void RefreshFollowPlayerDestination()
    {
        Transform playerTransform = ResolvePlayer();
        if (playerTransform == null)
            return;

        currentTarget = FindNearestEnemy(playerTransform.position, playerAttackRadius);
        if (currentTarget != null)
        {
            SetDestination(GetTargetPoint(currentTarget), attackRange * 0.9f);
            return;
        }

        currentTarget = null;
        SetDestination(playerTransform.position, playerFollowStoppingDistance);
    }

    private Transform ResolvePlayer()
    {
        if (player != null)
        {
            SetPlayerCollisionIgnore(true);
            return player;
        }

        player = FindPlayer();
        SetPlayerCollisionIgnore(true);
        return player;
    }

    private void SetPlayerCollisionIgnore(bool ignore)
    {
        if (ownColliders == null || ownColliders.Length == 0)
            ownColliders = GetComponentsInChildren<Collider>();

        if (!ignore)
        {
            SetCollisionIgnore(ignoredPlayerColliders, false);
            ignoredPlayerColliders = Array.Empty<Collider>();
            return;
        }

        if (player == null)
            return;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
        SetCollisionIgnore(playerColliders, true);
        ignoredPlayerColliders = playerColliders;
    }

    private void SetCollisionIgnore(Collider[] otherColliders, bool ignore)
    {
        if (otherColliders == null)
            return;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            Collider own = ownColliders[i];
            if (own == null)
                continue;

            for (int j = 0; j < otherColliders.Length; j++)
            {
                Collider other = otherColliders[j];
                if (other == null || other == own)
                    continue;

                Physics.IgnoreCollision(own, other, ignore);
            }
        }
    }

    private static Transform FindPlayer()
    {
        PlayerWallet wallet = PlayerWallet.Instance;
        if (wallet != null)
            return wallet.transform;

        PlayerWallet foundWallet = FindFirstObjectByType<PlayerWallet>();
        return foundWallet != null ? foundWallet.transform : null;
    }

    private Vector3 GetTargetPoint(Damageable target)
    {
        Collider col = target != null ? target.GetComponentInChildren<Collider>() : null;
        return col != null ? col.ClosestPoint(transform.position) : target.transform.position;
    }

    private void SetDestination(Vector3 destination, float stoppingDistance)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            destination = hit.position;

        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    private void Face(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
    }

    protected override void OnDeath()
    {
        Died?.Invoke(this);
        gameObject.SetActive(false);
    }
}
