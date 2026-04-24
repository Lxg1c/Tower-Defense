using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public abstract class Damageable : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Health Bar")]
    [Tooltip("Leave empty to use the HealthBarManager default prefab.")]
    [SerializeField] private HealthBar healthBarPrefab;

    public float MaxHealth     => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool  IsAlive       => CurrentHealth > 0f;

    public HealthBar HealthBarPrefab => healthBarPrefab;

    /// <summary>
    /// If true, HealthBarManager auto-removes the bar when this entity dies.
    /// Override to false for entities that respawn (e.g. PlayerHealth).
    /// </summary>
    public virtual bool DespawnBarOnDeath => true;

    /// <summary>
    /// Whether other systems (enemy target selectors, shooters) may pick this as a target.
    /// Defaults to IsAlive. Override to hide ghost / invulnerable entities from enemy AI.
    /// </summary>
    public virtual bool IsTargetable => IsAlive;

    public UnityEvent<float> onHealthChanged;
    public UnityEvent        onDied;

    // Subclasses can override to block damage (e.g. ghost state).
    protected virtual bool CanTakeDamage => IsAlive;

    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
    }

    protected virtual void OnEnable()
    {
        // Re-register every time we become active so pooled instances get their bar back.
        TryRegister();
    }

    protected virtual void Start()
    {
        // Fallback for the very first activation (HealthBarManager may not have Awoken yet when OnEnable ran).
        TryRegister();
    }

    private void TryRegister()
    {
        if (HealthBarManager.Instance != null)
            HealthBarManager.Instance.Register(this);
    }

    /// <summary>Restore CurrentHealth to MaxHealth. Used by pooled mobs on re-spawn.</summary>
    public void ResetToMaxHealth()
    {
        CurrentHealth = maxHealth;
        onHealthChanged?.Invoke(CurrentHealth);
    }

    public void TakeDamage(float damage)
    {
        if (!CanTakeDamage) return;

        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0f);
        onHealthChanged?.Invoke(CurrentHealth);

        if (!IsAlive)
            HandleDeath();
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        onHealthChanged?.Invoke(CurrentHealth);
    }

    /// <summary>
    /// Restore health even when CurrentHealth is 0 (used for respawn regen).
    /// Does NOT trigger OnDeath. Does fire onHealthChanged.
    /// </summary>
    protected void RestoreHealth(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
        onHealthChanged?.Invoke(CurrentHealth);
    }

    private void HandleDeath()
    {
        onDied?.Invoke();
        OnDeath();
    }

    protected abstract void OnDeath();
}
