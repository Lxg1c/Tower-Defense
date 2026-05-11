using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class BaseUpgradeLevel
{
    [Min(0)] public int upgradeCost = 200;

    [Tooltip("Multiplier applied to the BASE max health of the Base.")]
    [Min(0f)] public float hpMul = 1f;

    [Tooltip("Highest tower / mine upgrade level allowed after buying this base upgrade.")]
    [Min(0)] public int unlockedBuildUpgradeLevel = 1;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Base))]
public class BaseUpgrade : MonoBehaviour
{
    public static BaseUpgrade Instance { get; private set; }
    public static event System.Action OnTownHallChanged;

    [SerializeField] private BaseUpgradeLevel[] levels;
    [SerializeField] private int currentLevel = 0;
    [Tooltip("Allowed tower / mine upgrade level before buying any base upgrades.")]
    [Min(0)] [SerializeField] private int baseUnlockedBuildUpgradeLevel = 0;

    public UnityEvent onUpgraded;

    public int CurrentLevel => currentLevel;
    public int TownHallLevel => currentLevel + 1;
    public bool HasNextLevel => levels != null && currentLevel < levels.Length;
    public int MaxBuildUpgradeLevel => CurrentLevelData != null
        ? CurrentLevelData.unlockedBuildUpgradeLevel
        : baseUnlockedBuildUpgradeLevel;

    public BaseUpgradeLevel CurrentLevelData =>
        (levels != null && currentLevel > 0 && currentLevel <= levels.Length)
            ? levels[currentLevel - 1]
            : null;

    public BaseUpgradeLevel NextLevelData =>
        HasNextLevel ? levels[currentLevel] : null;

    private Base baseHealth;
    private float baseMaxHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        baseHealth = GetComponent<Base>();
        baseMaxHealth = baseHealth.MaxHealth;
        OnTownHallChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            OnTownHallChanged?.Invoke();
        }
    }

    private void Start()
    {
        currentLevel = Mathf.Clamp(currentLevel, 0, levels != null ? levels.Length : 0);

        if (currentLevel > 0)
            ApplyCurrentLevel(topUpHp: false);
    }

    public bool TryUpgrade()
    {
        if (!HasNextLevel)
            return false;

        BaseUpgradeLevel next = NextLevelData;
        if (next == null)
            return false;

        if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpend(next.upgradeCost))
            return false;

        currentLevel++;
        ApplyCurrentLevel(topUpHp: true);
        onUpgraded?.Invoke();
        OnTownHallChanged?.Invoke();
        return true;
    }

    public string BuildCurrentStatsText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Base Level: {currentLevel}");
        sb.AppendLine($"HP: {baseHealth.MaxHealth:0.##}");
        sb.AppendLine($"Build Upgrade Limit: {MaxBuildUpgradeLevel}");
        return sb.ToString().TrimEnd();
    }

    public string BuildNextStatsText()
    {
        BaseUpgradeLevel next = NextLevelData;
        if (next == null)
            return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Base Level: {currentLevel + 1}");
        sb.AppendLine($"HP: {baseMaxHealth * Mathf.Max(0f, next.hpMul):0.##}");
        sb.AppendLine($"Build Upgrade Limit: {next.unlockedBuildUpgradeLevel}");
        return sb.ToString().TrimEnd();
    }

    private void ApplyCurrentLevel(bool topUpHp)
    {
        BaseUpgradeLevel level = CurrentLevelData;
        if (level == null)
            return;

        baseHealth.SetMaxHealth(baseMaxHealth * Mathf.Max(0f, level.hpMul), topUpToFull: topUpHp);
    }
}
