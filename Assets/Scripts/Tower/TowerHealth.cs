using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tower HP. When the tower dies mid-wave it stays in the scene with its death
/// animation playing — the model is NOT hidden. Only the health bar is removed
/// and combat components are disabled. The wave-complete event revives it.
/// </summary>
public class TowerHealth : Damageable
{
    [Header("Revive")]
    [Tooltip("Optional. If empty, EVERY MonoBehaviour on this GameObject (except TowerHealth itself) is auto-disabled on death and re-enabled on revive. " +
             "Fill manually only if you need finer control.")]
    [SerializeField] private MonoBehaviour[] combatComponents;

    [Header("Death animation")]
    [Tooltip("Animator bool parameter set to true on death and false on revive. Leave empty to skip.")]
    [SerializeField] private string isDeadParam = "IsDead";

    [Header("Events")]
    /// <summary>Fired when the wave ends and the tower comes back. Use inherited onDied for the death reaction.</summary>
    public UnityEvent onRespawned;

    public bool IsDestroyed { get; private set; }

    protected override bool CanTakeDamage => IsAlive && !IsDestroyed;

    private Animator           animator;
    private MonoBehaviour[]    autoCombat;   // cached when no explicit combatComponents are set

    /// <summary>
    /// Tower keeps its model visible (death animation plays) but the health bar is
    /// removed on death and brought back on revive.
    /// </summary>
    public override bool DespawnBarOnDeath => true;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        SubscribeToSpawner();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeToSpawner();
    }

    private void OnDisable() { UnsubscribeFromSpawner(); }

    private void SubscribeToSpawner()
    {
        if (WaveSpawner.Instance == null) return;
        WaveSpawner.Instance.onWaveCompleted.RemoveListener(OnWaveCompleted);
        WaveSpawner.Instance.onWaveCompleted.AddListener(OnWaveCompleted);
    }

    private void UnsubscribeFromSpawner()
    {
        if (WaveSpawner.Instance == null) return;
        WaveSpawner.Instance.onWaveCompleted.RemoveListener(OnWaveCompleted);
    }

    protected override void OnDeath()
    {
        IsDestroyed = true;
        // Combat components off immediately — dead towers must not shoot.
        DisableCombat();
        SetIsDeadParam(true);
        // Health bar is removed automatically by HealthBarManager because
        // DespawnBarOnDeath returns true. The visual model stays so the
        // death animation can play in place.
    }

    private void OnWaveCompleted(int waveIndex, int reward)
    {
        if (IsDestroyed)
        {
            Revive();
            return;
        }
        // Alive but possibly damaged → top up HP to full between waves.
        if (CurrentHealth < MaxHealth)
            ResetToMaxHealth();
    }

    private void Revive()
    {
        IsDestroyed = false;
        RestoreHealth(MaxHealth);  // heals past 0 without firing OnDeath
        EnableCombat();
        SetIsDeadParam(false);
        // Re-create the health bar that was removed on death.
        if (HealthBarManager.Instance != null)
            HealthBarManager.Instance.Register(this);
        onRespawned?.Invoke();
    }

    private void SetIsDeadParam(bool dead)
    {
        if (animator == null || string.IsNullOrEmpty(isDeadParam)) return;
        animator.SetBool(isDeadParam, dead);
    }

    private void EnableCombat()  => SetCombatEnabled(true);
    private void DisableCombat() => SetCombatEnabled(false);

    private void SetCombatEnabled(bool on)
    {
        var list = ResolveCombatComponents();
        foreach (var c in list)
            if (c != null) c.enabled = on;
    }

    private MonoBehaviour[] ResolveCombatComponents()
    {
        if (combatComponents != null && combatComponents.Length > 0)
            return combatComponents;

        if (autoCombat == null)
        {
            // Cache every MonoBehaviour on the same GameObject except this TowerHealth.
            var all = GetComponents<MonoBehaviour>();
            int count = 0;
            for (int i = 0; i < all.Length; i++) if (all[i] != this) count++;
            autoCombat = new MonoBehaviour[count];
            int j = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != this) autoCombat[j++] = all[i];
        }
        return autoCombat;
    }

}
