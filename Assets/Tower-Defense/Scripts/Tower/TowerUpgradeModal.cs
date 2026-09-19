using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Scene-wide singleton modal for upgrading a tower.
/// Shows current stats vs next stats with an arrow, plus the upgrade cost.
/// </summary>
public class TowerUpgradeModal : MonoBehaviour
{
    public static TowerUpgradeModal Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private TMP_Text   currentStats;
    [SerializeField] private TMP_Text   nextStats;
    [SerializeField] private TMP_Text   costLabel;
    [SerializeField] private Button     upgradeButton;

    [Header("Format")]
    [SerializeField] private string costFormat = "Cost: {0}";
    [SerializeField] private string maxedText  = "MAX";

    [Header("Events")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    private TowerUpgrade target;
    private Action<TowerUpgrade> onUpgraded;
    private BaseUpgrade baseTarget;
    private Action<BaseUpgrade> onBaseUpgraded;
    private Func<bool> tryUpgrade;

    public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        
        Instance = this;

        if (modalRoot != null) 
        {
            modalRoot.SetActive(false);
        }
        
        if (upgradeButton != null) 
        {
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open(TowerUpgrade tower, Action<TowerUpgrade> onUpgraded = null, Func<bool> tryUpgrade = null)
    {
        if (!HasWallet()) return;
        if (tower == null || modalRoot == null) return;

        baseTarget = null;
        onBaseUpgraded = null;
        this.target     = tower;
        this.onUpgraded = onUpgraded;
        this.tryUpgrade = tryUpgrade;

        Refresh();
        modalRoot.SetActive(true);
        onOpened?.Invoke();
    }

    public void Open(BaseUpgrade baseUpgrade, Action<BaseUpgrade> onUpgraded = null, Func<bool> tryUpgrade = null)
    {
        if (!HasWallet()) return;
        if (baseUpgrade == null || modalRoot == null) return;

        target = null;
        this.onUpgraded = null;
        baseTarget = baseUpgrade;
        onBaseUpgraded = onUpgraded;
        this.tryUpgrade = tryUpgrade;

        Refresh();
        modalRoot.SetActive(true);
        onOpened?.Invoke();
    }

    public void Close()
    {
        target = null;
        onUpgraded = null;
        baseTarget = null;
        onBaseUpgraded = null;
        tryUpgrade = null;
        
        if (modalRoot != null) 
        {
            modalRoot.SetActive(false);
        }
        
        onClosed?.Invoke();
    }

    private void Refresh()
    {
        if (target == null && baseTarget == null) return;

        if (baseTarget != null)
        {
            RefreshBaseUpgrade();
            return;
        }

        if (currentStats != null) 
        {
            currentStats.text = UpgradeStatsFormatter.Current(target);
        }

        var next = target.NextLevelData;
        
        if (next != null)
        {
            if (nextStats != null) 
            {
                nextStats.text = target.IsNextLevelUnlocked
                    ? UpgradeStatsFormatter.Next(target)
                    : UpgradeStatsFormatter.Locked(target);
            }
            
            if (costLabel != null) 
            {
                costLabel.text = target.IsNextLevelUnlocked
                    ? string.Format(costFormat, next.upgradeCost)
                    : "";
            }

            bool canAfford = PlayerWallet.Instance != null
                && PlayerWallet.Instance.Coins >= next.upgradeCost && target.IsNextLevelUnlocked;
            
            if (upgradeButton != null) 
            {
                upgradeButton.interactable = canAfford;
            }
        }
        else
        {
            if (nextStats != null) 
            {
                nextStats.text = maxedText;
            }
            
            if (costLabel != null) 
            {
                costLabel.text = "";
            }
            
            if (upgradeButton != null) 
            {
                upgradeButton.interactable = false;
            }
        }
    }

    private void OnUpgradeClicked()
    {
        if (baseTarget != null)
        {
            if (tryUpgrade != null ? tryUpgrade() : baseTarget.TryUpgrade())
            {
                Refresh();
                onBaseUpgraded?.Invoke(baseTarget);
            }

            return;
        }

        if (target == null) return;
        
        if (tryUpgrade != null ? tryUpgrade() : target.TryUpgrade())
        {
            Refresh();
            onUpgraded?.Invoke(target);
        }
    }

    private void RefreshBaseUpgrade()
    {
        if (baseTarget == null) return;

        if (currentStats != null)
            currentStats.text = UpgradeStatsFormatter.Current(baseTarget);

        BaseUpgradeLevel next = baseTarget.NextLevelData;
        if (next != null)
        {
            if (nextStats != null)
                nextStats.text = UpgradeStatsFormatter.Next(baseTarget);

            if (costLabel != null)
                costLabel.text = string.Format(costFormat, next.upgradeCost);

            if (upgradeButton != null)
                upgradeButton.interactable = PlayerWallet.Instance != null
                    && PlayerWallet.Instance.Coins >= next.upgradeCost;
        }
        else
        {
            if (nextStats != null)
                nextStats.text = maxedText;

            if (costLabel != null)
                costLabel.text = "";

            if (upgradeButton != null)
                upgradeButton.interactable = false;
        }
    }
    private bool HasWallet()
    {
        if (PlayerWallet.Instance != null) return true;
        Debug.LogError("[TowerUpgradeModal] A PlayerWallet is required to show upgrades.", this);
        return false;
    }
}
