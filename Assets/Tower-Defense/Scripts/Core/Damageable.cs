using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public abstract class Damageable : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private float maxHealth = 100f; // Максимальное здоровье. Задаётся в инспекторе и может быть переопределено в подклассах.

    public float MaxHealth     => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool  IsAlive       => CurrentHealth > 0f;

    /// <summary>
    /// Могут ли другие системы (поиск целей у врагов, стрельба) выбирать эту сущность как цель.
    /// По умолчанию равно IsAlive. Переопределите, чтобы скрыть от вражеского ИИ призраков или неуязвимых сущностей.
    /// </summary>
    public virtual bool IsTargetable => IsAlive;

    public UnityEvent<float> onHealthChanged = new();
    public UnityEvent        onDied = new();

    // Подклассы могут переопределить, чтобы заблокировать урон (например, состояние призрака).
    protected virtual bool CanTakeDamage => IsAlive;

    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
    }

    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
    protected virtual void Start() { }

    /// <summary>
    /// Меняет максимальное HP. Если topUpToFull = true, текущее здоровье поднимается до нового максимума
    /// (полезно при апгрейде башни). Иначе текущее HP только клампится.
    /// </summary>
    public void SetMaxHealth(float value, bool topUpToFull = true)
    {
        maxHealth = Mathf.Max(1f, value);
        CurrentHealth = topUpToFull ? maxHealth : Mathf.Min(CurrentHealth, maxHealth);
        onHealthChanged?.Invoke(CurrentHealth);
    }

    /// <summary>Восстанавливает CurrentHealth до MaxHealth. Используется мобами из пула при повторном появлении.</summary>
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
    /// Восстанавливает здоровье, даже когда CurrentHealth равно 0 (используется для восстановления при возрождении).
    /// НЕ вызывает событие смерти. ВЫЗЫВАЕТ onHealthChanged.
    /// </summary>
    protected void RestoreHealth(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
        onHealthChanged?.Invoke(CurrentHealth);
    }

    /// <summary>
    ///  Если вызвалось событие смерти, обрабатываем его здесь, чтобы гарантировать, что оно сработает только один раp
    /// </summary>
    private void HandleDeath()
    {
        onDied?.Invoke();
        OnDeath();
    }

    protected abstract void OnDeath();
}
