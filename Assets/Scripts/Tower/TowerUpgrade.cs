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
    public bool IsNextLevelUnlocked => BaseUpgrade.Instance != null
        && NextUpgradeLevel <= BaseUpgrade.Instance.MaxBuildUpgradeLevel;
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

    public string BuildCurrentStatsText()
    {
        var sb = new System.Text.StringBuilder();

        if (shooter != null)
        {
            sb.AppendLine($"Damage: {shooter.Damage:0.##}");
            sb.AppendLine($"Fire Rate: {shooter.FireRate:0.##}/s");
        }

        if (pulse != null)
        {
            sb.AppendLine($"Damage: {pulse.Damage:0.##}");
            sb.AppendLine($"Range: {pulse.Range:0.##}");
            sb.AppendLine($"Interval: {pulse.AttackInterval:0.##}s");
        }

        if (detection != null && shooter != null)
            sb.AppendLine($"Range: {detection.Radius:0.##}");

        if (hp != null)
            sb.AppendLine($"HP: {hp.MaxHealth:0.##}");

        if (mineIncome != null)
            sb.AppendLine($"Income: {mineIncome.CoinsPerWave}");

        return sb.ToString().TrimEnd();
    }

    public string BuildNextStatsText()
    {
        TowerUpgradeLevel next = NextLevelData;
        if (next == null)
            return "";

        var sb = new System.Text.StringBuilder();

        float dMul = SafeMul(next.damageMul);
        float rMul = SafeMul(next.rangeMul);
        float fMul = SafeMul(next.fireRateMul);
        float hMul = SafeMul(next.hpMul);
        float iMul = SafeMul(next.incomeMul);

        if (shooter != null)
        {
            sb.AppendLine($"Damage: {baseShooterDamage * dMul:0.##}");
            sb.AppendLine($"Fire Rate: {baseShooterFireRate * fMul:0.##}/s");
        }

        if (pulse != null)
        {
            sb.AppendLine($"Damage: {basePulseDamage * dMul:0.##}");
            sb.AppendLine($"Range: {basePulseRange * rMul:0.##}");
            sb.AppendLine($"Interval: {basePulseAttackInterval / Mathf.Max(0.0001f, fMul):0.##}s");
        }

        if (detection != null && shooter != null)
            sb.AppendLine($"Range: {baseDetectionRadius * rMul:0.##}");

        if (hp != null)
            sb.AppendLine($"HP: {baseMaxHealth * hMul:0.##}");

        if (mineIncome != null)
            sb.AppendLine($"Income: {Mathf.RoundToInt(baseMineIncome * iMul)}");

        return sb.ToString().TrimEnd();
    }

    public string BuildLockedText()
    {
        if (!HasNextLevel)
            return "";

        int baseLimit = BaseUpgrade.Instance != null ? BaseUpgrade.Instance.MaxBuildUpgradeLevel : 0;
        return $"Requires Base upgrade limit {NextUpgradeLevel}. Current limit: {baseLimit}.";
    }

    private void ApplyCurrentLevel(bool topUpHp)
    {
        TowerUpgradeLevel level = CurrentLevelData;
        if (level == null)
            return;

        float dMul = SafeMul(level.damageMul);
        float rMul = SafeMul(level.rangeMul);
        float fMul = SafeMul(level.fireRateMul);
        float hMul = SafeMul(level.hpMul);
        float iMul = SafeMul(level.incomeMul);

        if (shooter != null)
        {
            shooter.Damage = baseShooterDamage * dMul;
            shooter.FireRate = baseShooterFireRate * fMul;
        }

        if (detection != null)
            detection.Radius = baseDetectionRadius * rMul;

        if (pulse != null)
        {
            pulse.Damage = basePulseDamage * dMul;
            pulse.Range = basePulseRange * rMul;
            pulse.AttackInterval = basePulseAttackInterval / Mathf.Max(0.0001f, fMul);
        }

        if (hp != null)
            hp.SetMaxHealth(baseMaxHealth * hMul, topUpToFull: topUpHp);

        if (mineIncome != null)
            mineIncome.CoinsPerWave = Mathf.RoundToInt(baseMineIncome * iMul);
    }

    private float SafeMul(float value)
    {
        return Mathf.Max(0f, value);
    }
}
