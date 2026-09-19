using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BaseUpgradeModal : MonoBehaviour
{
    public static BaseUpgradeModal Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private TMP_Text currentStats;
    [SerializeField] private TMP_Text nextStats;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private Button upgradeButton;

    [Header("Format")]
    [SerializeField] private string costFormat = "Cost: {0}";
    [SerializeField] private string maxedText = "MAX";

    public UnityEvent onOpened;
    public UnityEvent onClosed;

    private BaseUpgrade target;
    private Action<BaseUpgrade> onUpgraded;
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
            modalRoot.SetActive(false);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Open(BaseUpgrade baseUpgrade = null, Action<BaseUpgrade> onUpgraded = null)
    {
        target = baseUpgrade != null ? baseUpgrade : BaseUpgrade.Instance;
        if (target == null || modalRoot == null)
            return;

        this.onUpgraded = onUpgraded;
        Refresh();
        modalRoot.SetActive(true);
        onOpened?.Invoke();
    }

    public void Close()
    {
        target = null;
        onUpgraded = null;

        if (modalRoot != null)
            modalRoot.SetActive(false);

        onClosed?.Invoke();
    }

    private void Refresh()
    {
        if (target == null)
            return;

        if (currentStats != null)
            currentStats.text = target.BuildCurrentStatsText();

        BaseUpgradeLevel next = target.NextLevelData;
        if (next != null)
        {
            if (nextStats != null)
                nextStats.text = target.BuildNextStatsText();

            if (costLabel != null)
                costLabel.text = string.Format(costFormat, next.upgradeCost);

            int coins = PlayerWallet.Instance != null ? PlayerWallet.Instance.Coins : int.MaxValue;
            if (upgradeButton != null)
                upgradeButton.interactable = coins >= next.upgradeCost;
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

    private void OnUpgradeClicked()
    {
        if (target == null)
            return;

        if (target.TryUpgrade())
        {
            onUpgraded?.Invoke(target);
            Refresh();
        }
    }
}
