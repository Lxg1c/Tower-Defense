using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : Damageable
{
    [Header("Respawn")]
    [Tooltip("Delay after death before health starts refilling and the player can return.")]
    [SerializeField] private float respawnCooldown = 2f;
    [SerializeField] private float respawnTime = 5f;

    [Header("Ghost Collision")]
    [Tooltip("Layer names to ignore physical collisions with while in ghost form (mobs). " +
             "Ground layers must NOT be listed here or the player will fall through the map.")]
    [SerializeField] private string[] ignoreLayersWhileGhost = { "Enemy" };

    [Header("Death animation")]
    [Tooltip("Animator bool parameter set to true on death and false on respawn. Leave empty to skip.")]
    [SerializeField] private string isDeadParam = "IsDead";

    [Header("Out-of-combat Regeneration")]
    [Tooltip("Seconds without taking damage before regen kicks in.")]
    [SerializeField] private float regenDelay = 4f;
    [Tooltip("HP per second regenerated while out of combat.")]
    [SerializeField] private float regenPerSecond = 10f;

    private float    lastHealth;
    private float    lastDamageTime = -999f;
    private Shooter  shooter;
    private Animator animator;
    private int      isDeadHash;
    private bool     hasIsDeadParam;

    /// <summary>True while the player is dead and regenerating.</summary>
    public bool  IsGhost         { get; private set; }

    /// <summary>0 → 1 regen progress. Use this to drive a UI timer.</summary>
    public float RespawnProgress { get; private set; }

    public UnityEvent onGhostEntered;   // wire up: make transparent, disable shooter…
    public UnityEvent onRespawned;      // wire up: restore visuals, re-enable shooter…

    // Block all damage while ghost (health is 0 but we don't want double-death).
    protected override bool CanTakeDamage => IsAlive && !IsGhost;

    // Enemy AI should not pick the ghost as a target.
    public override bool IsTargetable => base.IsTargetable && !IsGhost;

    // Player respawns — keep the health bar so it acts as a respawn timer.
    public override bool DespawnBarOnDeath => false;

    protected override void Awake()
    {
        base.Awake();
        shooter  = GetComponentInChildren<Shooter>();
        animator = GetComponentInChildren<Animator>();
        isDeadHash = Animator.StringToHash(isDeadParam);
        hasIsDeadParam = HasAnimatorBool(isDeadParam);
    }

    private void SetIsDeadParam(bool dead)
    {
        if (animator == null || !hasIsDeadParam) return;
        animator.SetBool(isDeadHash, dead);
    }

    private bool HasAnimatorBool(string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
                return true;

        return false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        lastHealth = CurrentHealth;
        onHealthChanged.AddListener(OnHealthChangedTracker);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        onHealthChanged.RemoveListener(OnHealthChangedTracker);
    }

    private void OnHealthChangedTracker(float current)
    {
        if (current < lastHealth) lastDamageTime = Time.time;
        lastHealth = current;
    }

    private void Update()
    {
        if (IsGhost || !IsAlive) return;
        if (CurrentHealth >= MaxHealth) return;
        if (Time.time - lastDamageTime < regenDelay) return;

        RestoreHealth(regenPerSecond * Time.deltaTime);
    }

    protected override void OnDeath()
    {
        IsGhost         = true;
        RespawnProgress = 0f;
        SetGhostCollisionIgnore(true);
        if (shooter != null) shooter.enabled = false;
        SetIsDeadParam(true);
        onGhostEntered?.Invoke();
        StartCoroutine(RespawnRoutine());
    }

    private void SetGhostCollisionIgnore(bool ignore)
    {
        int myLayer = gameObject.layer;
        if (ignoreLayersWhileGhost == null) return;

        foreach (var layerName in ignoreLayersWhileGhost)
        {
            int other = LayerMask.NameToLayer(layerName);
            if (other < 0) continue;
            Physics.IgnoreLayerCollision(myLayer, other, ignore);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        if (respawnCooldown > 0f)
        {
            float cooldownElapsed = 0f;
            while (cooldownElapsed < respawnCooldown)
            {
                cooldownElapsed += Time.deltaTime;
                RespawnProgress = 0f;
                yield return null;
            }
        }

        float elapsed       = 0f;
        float safeRespawnTime = Mathf.Max(0.01f, respawnTime);
        float healPerSecond = MaxHealth / safeRespawnTime;

        while (elapsed < safeRespawnTime)
        {
            elapsed         += Time.deltaTime;
            RespawnProgress  = Mathf.Clamp01(elapsed / safeRespawnTime);
            RestoreHealth(healPerSecond * Time.deltaTime);
            yield return null;
        }

        // Guarantee full health
        RestoreHealth(MaxHealth);
        IsGhost         = false;
        RespawnProgress = 1f;
        SetGhostCollisionIgnore(false);
        if (shooter != null) shooter.enabled = true;
        SetIsDeadParam(false);
        onRespawned?.Invoke();
    }
}
