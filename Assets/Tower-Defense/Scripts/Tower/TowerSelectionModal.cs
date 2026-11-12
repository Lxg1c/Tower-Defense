using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Build choices and purchase callbacks presented by GameplayToolkitView.</summary>
public sealed class TowerSelectionModal : MonoBehaviour
{
    public static TowerSelectionModal Instance { get; private set; }
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    private readonly List<BuildOffer> offers = new();
    private Action<TowerOption> onPick;
    private int revision;
    private bool isOpen;
    private (string Key, string Fallback) heading = ("choose-building", "ВЫБЕРИТЕ ПОСТРОЙКУ");

    public sealed class BuildOffer
    {
        public TowerOption Option { get; }
        public int CurrentLevel { get; }
        public int? Cost { get; }
        public string ActionText => GameLocalization.Text(actionText.Key, actionText.Fallback);
        private readonly (string Key, string Fallback) actionText;
        public bool Available { get; }
        private readonly Action activate;

        public BuildOffer(TowerOption option, int currentLevel, int? cost, (string Key, string Fallback) actionText,
            bool available, Action activate)
        {
            Option = option;
            CurrentLevel = currentLevel;
            Cost = cost;
            this.actionText = actionText;
            Available = available;
            this.activate = activate;
        }

        public void Activate() { if (Available) activate?.Invoke(); }
    }

    public bool IsOpen => isOpen;
    public int Revision => revision;
    public string Heading => GameLocalization.Text(heading.Key, heading.Fallback);
    public IReadOnlyList<BuildOffer> ActiveOffers => offers;
    public TowerPlacementZone Owner { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable() { if (Instance == null) Instance = this; }
    private void OnDisable() { if (Instance == this) Instance = null; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Open(IReadOnlyList<TowerOption> options, Action<TowerOption> onPick,
        TowerPlacementZone owner = null)
    {
        if (PlayerWallet.Instance == null)
        {
            Debug.LogError("[TowerSelectionModal] A PlayerWallet is required to show build options.", this);
            return;
        }
        this.onPick = onPick;
        Owner = owner;
        ClearEntries();
        heading = options.Count == 1 ? ("build-single", "СТРОИТЕЛЬСТВО") : ("choose-building", "ВЫБЕРИТЕ ПОСТРОЙКУ");
        int coins = PlayerWallet.Instance.Coins;
        foreach (TowerOption option in options)
        {
            if (option == null || option.prefab == null) continue;
            TowerOption captured = option;
            int cardRevision = revision;
            int cost = owner != null ? owner.GetOptionCost(option) : option.cost;
            offers.Add(new BuildOffer(option, 0, cost, ("build", "ПОСТРОИТЬ"), coins >= cost,
                () => { if (isOpen && revision == cardRevision) HandlePick(captured); }));
        }
        isOpen = true;
        onOpened?.Invoke();
    }

    public void OpenUpgrade(TowerOption option, int currentLevel, int? cost, bool unlocked,
        Func<bool> purchase, TowerPlacementZone owner)
    {
        if (PlayerWallet.Instance == null || option == null || option.prefab == null) return;
        ClearEntries();
        onPick = null;
        Owner = owner;
        heading = ("building-levels", "УЛУЧШЕНИЯ ПОСТРОЙКИ");
        (string, string) action = !cost.HasValue ? ("building-max", "МАКС. УРОВЕНЬ") :
            !unlocked ? ("upgrade-core", "УЛУЧШИТЕ СЕТЕВОЙ УЗЕЛ") : ("building-upgrade", "УЛУЧШИТЬ");
        bool canBuy = cost.HasValue && unlocked && PlayerWallet.Instance.Coins >= cost.Value;
        int cardRevision = revision;
        offers.Add(new BuildOffer(option, currentLevel, cost, action, canBuy, () =>
        {
            if (!isOpen || Owner != owner || revision != cardRevision || !canBuy) return;
            if (purchase()) Close();
        }));
        isOpen = true;
        onOpened?.Invoke();
    }

    public void Close()
    {
        bool wasOpen = isOpen;
        onPick = null;
        Owner = null;
        ClearEntries();
        isOpen = false;
        if (wasOpen) onClosed?.Invoke();
    }

    private void HandlePick(TowerOption option)
    {
        Action<TowerOption> callback = onPick;
        Close();
        callback?.Invoke(option);
    }

    private void ClearEntries()
    {
        revision++;
        offers.Clear();
    }
}
