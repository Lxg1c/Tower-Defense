using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public abstract class Damageable : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private float maxHealth = 100f; // Максимальное здоровье. Задаётся в инспекторе и может быть переопределено в подклассах.

    [Header("Полоса здоровья")]
    [Tooltip("Оставьте пустым, чтобы использовать префаб по умолчанию из HealthBarManager.")]
    [SerializeField] private HealthBar healthBarPrefab;
    [Tooltip("Опционально. Если задан — полоса здоровья следует за этим Transform. " +
             "Если пусто — берётся позиция объекта + глобальный offset из HealthBarManager.")]
    [SerializeField] private Transform healthBarAnchor;

    public float MaxHealth     => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool  IsAlive       => CurrentHealth > 0f;

    public HealthBar HealthBarPrefab => healthBarPrefab;
    public Transform HealthBarAnchor => healthBarAnchor;

    /// <summary>
    /// Если true, HealthBarManager автоматически убирает полосу здоровья при смерти сущности.
    /// Переопределен как false для сущностей, которые возрождаются (например, PlayerHealth).
    /// </summary>
    public virtual bool DespawnBarOnDeath => true;

    /// <summary>
    /// Могут ли другие системы (поиск целей у врагов, стрельба) выбирать эту сущность как цель.
    /// По умолчанию равно IsAlive. Переопределите, чтобы скрыть от вражеского ИИ призраков или неуязвимых сущностей.
    /// </summary>
    public virtual bool IsTargetable => IsAlive;

    public UnityEvent<float> onHealthChanged;
    public UnityEvent        onDied;

    // Подклассы могут переопределить, чтобы заблокировать урон (например, состояние призрака).
    protected virtual bool CanTakeDamage => IsAlive;

    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
    }

    protected virtual void OnEnable()
    {
        // Повторно регистрируемся при каждой активации, чтобы экземпляры из пула возвращали свою полосу здоровья.
        TryRegister();
    }

    protected virtual void OnDisable()
    {
        if (HealthBarManager.Instance != null)
            HealthBarManager.Instance.Unregister(this);
    }

    protected virtual void Start()
    {
        // Запасной вариант для самого первого включения (HealthBarManager мог ещё не проснуться, когда сработал OnEnable).
        TryRegister();
    }

    private void TryRegister()
    {
        if (HealthBarManager.Instance != null)
            HealthBarManager.Instance.Register(this);
    }

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
