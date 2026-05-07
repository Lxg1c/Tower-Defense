using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TowerUpgradeLevel
{
    [Min(0)] public int upgradeCost = 100;

    [Tooltip("Multiplier applied to the BASE damage of Shooter / PulseTower. 1.5 = +50% damage.")]
    [Min(0f)] public float damageMul = 1f;

    [Tooltip("Multiplier applied to the BASE attack range / detection radius.")]
    [Min(0f)] public float rangeMul = 1f;

    [Tooltip("Multiplier applied to the BASE fire rate. >1 = shoots more often. " +
             "For PulseTower this divides the attack interval (faster pulses).")]
    [Min(0f)] public float fireRateMul = 1f;

    [Tooltip("Multiplier applied to the BASE max health.")]
    [Min(0f)] public float hpMul = 1f;
}

/// <summary>
/// In-place tower upgrade. Caches base stats on Awake; on level change, multiplies
/// base × current level multipliers and writes the result back to the components.
/// One prefab — many levels.
/// </summary>
[DisallowMultipleComponent]
public class TowerUpgrade : MonoBehaviour
{
    [SerializeField] private TowerUpgradeLevel[] levels;
    [SerializeField] private int currentLevel = 0;

    public UnityEvent onUpgraded;

    public int CurrentLevel => currentLevel;
    public bool CanUpgrade => levels != null && currentLevel + 1 < levels.Length;

    public TowerUpgradeLevel CurrentLevelData =>
        (levels != null && currentLevel >= 0 && currentLevel < levels.Length) ? levels[currentLevel] : null;

    public TowerUpgradeLevel NextLevelData =>
        CanUpgrade ? levels[currentLevel + 1] : null;

    // Base stats cached at Awake (before any level multipliers applied).
    private float    baseShooterDamage;
    private float    baseShooterFireRate;
    private float    baseDetectionRadius;
    private float    basePulseDamage;
    private float    basePulseRange;
    private float    basePulseAttackInterval;
    private float    baseMaxHealth;

    private Shooter        shooter;
    private DetectionZone  detection;
    private PulseTower     pulse;
    private Damageable     hp;

    private void Awake()
    {
        shooter   = GetComponent<Shooter>();
        detection = GetComponentInChildren<DetectionZone>();
        pulse     = GetComponent<PulseTower>();
        hp        = GetComponent<Damageable>();

        if (shooter   != null) { baseShooterDamage   = shooter.Damage;   baseShooterFireRate    = shooter.FireRate; }
        if (detection != null) { baseDetectionRadius = detection.Radius; }
        if (pulse     != null) { basePulseDamage     = pulse.Damage;     basePulseRange         = pulse.Range;
                                  basePulseAttackInterval = pulse.AttackInterval; }
        if (hp        != null) { baseMaxHealth       = hp.MaxHealth; }
    }

    private void Start()
    {
        // Level 0 IS the base — components keep their Inspector values, no scaling needed.
        // Multipliers from levels[] are only applied when upgrading beyond level 0.
        if (currentLevel > 0)
            ApplyCurrentLevel(topUpHp: false);
    }

    public bool TryUpgrade()
    {
        if (!CanUpgrade) return false;
        var next = levels[currentLevel + 1];
        if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpend(next.upgradeCost))
            return false;

        currentLevel++;
        ApplyCurrentLevel(topUpHp: true);
        onUpgraded?.Invoke();
        return true;
    }

    /// <summary>
    /// Additive multiplier: starts at 1, each applied level adds (mul - 1) on top.
    /// Example with mul=1.5 every level → 1.5x, 2.0x, 2.5x ...
    /// To get +1.0 per level use mul=2.0 every level → 1.0x, 2.0x, 3.0x ...
    /// </summary>
    private float CumulativeMul(System.Func<TowerUpgradeLevel, float> selector, int upToLevel)
    {
        float total = 1f;
        if (levels == null) return total;
        for (int i = 1; i <= upToLevel && i < levels.Length; i++)
            total += (selector(levels[i]) - 1f);
        return Mathf.Max(0f, total);
    }

    /// <summary>Multi-line text describing the tower's CURRENT stats (live values).</summary>
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
        return sb.ToString().TrimEnd();
    }

    /// <summary>Multi-line text describing what stats would look like AFTER the next upgrade.</summary>
    public string BuildNextStatsText()
    {
        if (!CanUpgrade) return "";

        int next = currentLevel + 1;
        float dMul = CumulativeMul(l => l.damageMul,   next);
        float rMul = CumulativeMul(l => l.rangeMul,    next);
        float fMul = CumulativeMul(l => l.fireRateMul, next);
        float hMul = CumulativeMul(l => l.hpMul,       next);

        var sb = new System.Text.StringBuilder();
        if (shooter != null)
        {
            sb.AppendLine($"Damage: {baseShooterDamage   * dMul:0.##}");
            sb.AppendLine($"Fire Rate: {baseShooterFireRate * fMul:0.##}/s");
        }
        if (pulse != null)
        {
            sb.AppendLine($"Damage: {basePulseDamage * dMul:0.##}");
            sb.AppendLine($"Range: {basePulseRange  * rMul:0.##}");
            sb.AppendLine($"Interval: {basePulseAttackInterval / Mathf.Max(0.0001f, fMul):0.##}s");
        }
        if (detection != null && shooter != null)
            sb.AppendLine($"Range: {baseDetectionRadius * rMul:0.##}");
        if (hp != null)
            sb.AppendLine($"HP: {baseMaxHealth * hMul:0.##}");
        return sb.ToString().TrimEnd();
    }

    private void ApplyCurrentLevel(bool topUpHp)
    {
        if (currentLevel <= 0) return;

        // Cumulative product of all level multipliers from 1..currentLevel.
        float dMul = CumulativeMul(l => l.damageMul,   currentLevel);
        float rMul = CumulativeMul(l => l.rangeMul,    currentLevel);
        float fMul = CumulativeMul(l => l.fireRateMul, currentLevel);
        float hMul = CumulativeMul(l => l.hpMul,       currentLevel);

        if (shooter != null)
        {
            shooter.Damage   = baseShooterDamage   * dMul;
            shooter.FireRate = baseShooterFireRate * fMul;
        }
        if (detection != null)
        {
            detection.Radius = baseDetectionRadius * rMul;
        }
        if (pulse != null)
        {
            pulse.Damage         = basePulseDamage * dMul;
            pulse.Range          = basePulseRange  * rMul;
            pulse.AttackInterval = basePulseAttackInterval / Mathf.Max(0.0001f, fMul);
        }
        if (hp != null)
        {
            hp.SetMaxHealth(baseMaxHealth * hMul, topUpToFull: topUpHp);
        }
    }
}
