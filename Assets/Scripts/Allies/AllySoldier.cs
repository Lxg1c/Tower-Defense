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

    [Header("Audio")]
    [SerializeField] private AudioSource hitAudioSource;
    [SerializeField] private AudioClip hitAudioClip;
    [SerializeField, Range(0f, 1f)] private float hitAudioVolume = 1f;

    [Header("Death VFX")]
    [SerializeField] private GameObject deathExplosionPrefab;
    [SerializeField] private Vector3 deathExplosionScale = Vector3.one;
    [SerializeField] private float deathExplosionLifetime = 2f;

    [Header("Movement")]
    [SerializeField] private float destinationRefreshInterval = 0.2f;
    [SerializeField] private float playerAttackRadius = 10f;
    [SerializeField] private float playerFollowStoppingDistance = 2f;

    [Header("Animation")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string moveStartTriggerName = "MoveStart";
    [SerializeField] private string movingBoolName = "IsMoving";
    [SerializeField] private float movingVelocityThreshold = 0.05f;

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
    private int attackTriggerHash;
    private int moveStartTriggerHash;
    private int movingBoolHash;
    private bool hasAttackTrigger;
    private bool hasMoveStartTrigger;
    private bool hasMovingBool;
    private bool wasMoving;

    public override bool IsTargetable => base.IsTargetable && isActiveAndEnabled;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        if (hitAudioSource == null)
            hitAudioSource = GetComponent<AudioSource>();
        ownColliders = GetComponentsInChildren<Collider>();
        targetBuffer = new Collider[Mathf.Max(4, maxTargets)];
        CacheAnimatorParameters();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResetToMaxHealth();
        attackCooldown = 0f;
        refreshTimer = 0f;
        currentTarget = null;
        wasMoving = false;
        SetMovingAnimation(false);

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
        SetMovingAnimation(false);
        wasMoving = false;
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

        UpdateMovementAnimation();
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

        TriggerAttackAnimation();

        currentTarget.TakeDamage(damage);
        PlayHitAudio();
        attackCooldown = 1f / Mathf.Max(0.01f, attackRate);
    }

    private void PlayHitAudio()
    {
        if (hitAudioSource == null)
            return;

        AudioClip clip = hitAudioClip != null ? hitAudioClip : hitAudioSource.clip;
        if (clip == null)
            return;

        hitAudioSource.PlayOneShot(clip, hitAudioVolume);
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

    private void UpdateMovementAnimation()
    {
        if (agent == null || animator == null)
            return;

        bool isMoving = agent.enabled
            && agent.isOnNavMesh
            && !agent.isStopped
            && agent.velocity.sqrMagnitude > movingVelocityThreshold * movingVelocityThreshold;

        if (isMoving && !wasMoving)
            TriggerMoveStartAnimation();

        if (isMoving != wasMoving)
            SetMovingAnimation(isMoving);

        wasMoving = isMoving;
    }

    private void TriggerAttackAnimation()
    {
        if (animator != null && hasAttackTrigger)
            animator.SetTrigger(attackTriggerHash);
    }

    private void TriggerMoveStartAnimation()
    {
        if (animator != null && hasMoveStartTrigger)
            animator.SetTrigger(moveStartTriggerHash);
    }

    private void SetMovingAnimation(bool moving)
    {
        if (animator != null && hasMovingBool)
            animator.SetBool(movingBoolHash, moving);
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null)
            return;

        attackTriggerHash = Animator.StringToHash(attackTriggerName);
        moveStartTriggerHash = Animator.StringToHash(moveStartTriggerName);
        movingBoolHash = Animator.StringToHash(movingBoolName);

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == attackTriggerName)
                hasAttackTrigger = true;
            else if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == moveStartTriggerName)
                hasMoveStartTrigger = true;
            else if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == movingBoolName)
                hasMovingBool = true;
        }
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
        SetMovingAnimation(false);
        wasMoving = false;
        SpawnDeathExplosion();
        Died?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void SpawnDeathExplosion()
    {
        if (deathExplosionPrefab == null)
            return;

        Vector3 position = GetDeathExplosionPosition();
        GameObject vfx = Instantiate(deathExplosionPrefab, position, Quaternion.identity);
        vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, deathExplosionScale);

        if (deathExplosionLifetime > 0f)
            Destroy(vfx, deathExplosionLifetime);
    }

    private Vector3 GetDeathExplosionPosition()
    {
        Collider col = GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : transform.position;
    }
}
