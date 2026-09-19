using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TowerUpgradeLevel
{
    [Min(0)] public int upgradeCost = 100;

    [Tooltip("Multiplier applied to the BASE damage of Shooter / PulseTower.")]
    [Min(0f)] public float damageMul = 1f;

    [Tooltip("Multiplier applied to the BASE attack range / detection radius.")]
    [Min(0f)] public float rangeMul = 1f;

    [Tooltip("Multiplier applied to the BASE fire rate. For PulseTower this divides the attack interval.")]
    [Min(0f)] public float fireRateMul = 1f;

    [Tooltip("Multiplier applied to the BASE max health.")]
    [Min(0f)] public float hpMul = 1f;

    [Tooltip("Multiplier applied to the BASE mine income.")]
    [Min(0f)] public float incomeMul = 1f;
}

/// <summary>
/// In-place upgrade component. Level 0 is the prefab's base Inspector values.
/// levels[0] is the first purchased upgrade, levels[1] is the second, etc.
/// Each upgrade level applies its multipliers directly to the cached base values.
/// </summary>
[DisallowMultipleComponent]
public class TowerUpgrade : MonoBehaviour
{
    [SerializeField] private TowerUpgradeLevel[] levels;
    [SerializeField] private int currentLevel = 0;

    public UnityEvent onUpgraded;

    public int CurrentLevel => currentLevel;
    public bool HasNextLevel => levels != null && currentLevel < levels.Length;
    public int NextUpgradeLevel => currentLevel + 1;
    public int MaxUnlockedLevel => BaseUpgrade.Instance != null
        ? BaseUpgrade.Instance.MaxBuildUpgradeLevel : 0;
    public bool IsNextLevelUnlocked => BaseUpgrade.Instance != null
        && NextUpgradeLevel <= MaxUnlockedLevel;
    public bool CanUpgrade => HasNextLevel && IsNextLevelUnlocked;

    public TowerUpgradeLevel CurrentLevelData =>
        (levels != null && currentLevel > 0 && currentLevel <= levels.Length)
            ? levels[currentLevel - 1]
            : null;

    public TowerUpgradeLevel NextLevelData =>
        HasNextLevel ? levels[currentLevel] : null;

    private float baseShooterDamage;
    private float baseShooterFireRate;
    private float baseDetectionRadius;
    private float basePulseDamage;
    private float basePulseRange;
    private float basePulseAttackInterval;
    private float baseMaxHealth;
    private int baseMineIncome;

    private Shooter shooter;
    private DetectionZone detection;
    private PulseTower pulse;
    private Damageable hp;
    private MineIncome mineIncome;

    private void Awake()
    {
        shooter = GetComponent<Shooter>();
        detection = GetComponentInChildren<DetectionZone>();
        pulse = GetComponent<PulseTower>();
        hp = GetComponent<Damageable>();
        mineIncome = GetComponent<MineIncome>();

        if (shooter != null)
        {
            baseShooterDamage = shooter.Damage;
            baseShooterFireRate = shooter.FireRate;
        }

        if (detection != null)
            baseDetectionRadius = detection.Radius;

        if (pulse != null)
        {
            basePulseDamage = pulse.Damage;
            basePulseRange = pulse.Range;
            basePulseAttackInterval = pulse.AttackInterval;
        }

        if (hp != null)
            baseMaxHealth = hp.MaxHealth;

        if (mineIncome != null)
            baseMineIncome = mineIncome.CoinsPerWave;
    }

    private void Start()
    {
        currentLevel = Mathf.Clamp(currentLevel, 0, levels != null ? levels.Length : 0);

        if (currentLevel > 0)
            ApplyCurrentLevel(topUpHp: false);
    }

    public bool TryUpgrade()
    {
        if (!HasNextLevel || !IsNextLevelUnlocked)
            return false;

        TowerUpgradeLevel next = NextLevelData;
        if (next == null)
            return false;

        if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpend(next.upgradeCost))
            return false;

        currentLevel++;
        ApplyCurrentLevel(topUpHp: true);
        onUpgraded?.Invoke();
        return true;
    }

    public BuildingStats GetCurrentStats()
    {
        return new BuildingStats(
            shooterDamage: shooter != null ? shooter.Damage : (float?)null,
            shooterFireRate: shooter != null ? shooter.FireRate : (float?)null,
            shooterRange: detection != null && shooter != null ? detection.Radius : (float?)null,
            pulseDamage: pulse != null ? pulse.Damage : (float?)null,
            pulseRange: pulse != null ? pulse.Range : (float?)null,
            pulseInterval: pulse != null ? pulse.AttackInterval : (float?)null,
            maxHealth: hp != null ? hp.MaxHealth : (float?)null,
            income: mineIncome != null ? mineIncome.CoinsPerWave : (int?)null);
    }

    public BuildingStats? GetNextStats()
    {
        TowerUpgradeLevel next = NextLevelData;
        return next != null ? CalculateStats(next) : (BuildingStats?)null;
    }

    private BuildingStats CalculateStats(TowerUpgradeLevel level)
    {
        float dMul = SafeMul(level.damageMul);
        float rMul = SafeMul(level.rangeMul);
        float fMul = SafeMul(level.fireRateMul);
        float hMul = SafeMul(level.hpMul);
        float iMul = SafeMul(level.incomeMul);
        return new BuildingStats(
            shooterDamage: shooter != null ? baseShooterDamage * dMul : (float?)null,
            shooterFireRate: shooter != null ? baseShooterFireRate * fMul : (float?)null,
            shooterRange: detection != null && shooter != null ? baseDetectionRadius * rMul : (float?)null,
            pulseDamage: pulse != null ? basePulseDamage * dMul : (float?)null,
            pulseRange: pulse != null ? basePulseRange * rMul : (float?)null,
            pulseInterval: pulse != null ? basePulseAttackInterval / Mathf.Max(0.0001f, fMul) : (float?)null,
            maxHealth: hp != null ? baseMaxHealth * hMul : (float?)null,
            income: mineIncome != null ? Mathf.RoundToInt(baseMineIncome * iMul) : (int?)null);
    }

    private void ApplyCurrentLevel(bool topUpHp)
    {
        TowerUpgradeLevel level = CurrentLevelData;
        if (level == null)
            return;

        BuildingStats stats = CalculateStats(level);

        if (shooter != null)
        {
            shooter.Damage = stats.ShooterDamage.Value;
            shooter.FireRate = stats.ShooterFireRate.Value;
        }

        if (detection != null)
            detection.Radius = baseDetectionRadius * SafeMul(level.rangeMul);

        if (pulse != null)
        {
            pulse.Damage = stats.PulseDamage.Value;
            pulse.Range = stats.PulseRange.Value;
            pulse.AttackInterval = stats.PulseInterval.Value;
        }

        if (hp != null)
            hp.SetMaxHealth(stats.MaxHealth.Value, topUpToFull: topUpHp);

        if (mineIncome != null)
            mineIncome.CoinsPerWave = stats.Income.Value;
    }

    private float SafeMul(float value)
    {
        return Mathf.Max(0f, value);
    }
}
