using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Scene-wide singleton modal. A zone calls <see cref="Open"/> with a list of options
/// and a callback. The modal instantiates one shared <see cref="cardPrefab"/> per option
/// under <see cref="content"/> and binds it via <see cref="TowerCard.Bind"/> — no per-tower UI
/// prefabs needed. Layout (HorizontalLayoutGroup / GridLayoutGroup) lives on the Content object.
/// </summary>
public class TowerSelectionModal : MonoBehaviour
{
    public static TowerSelectionModal Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject    modalRoot;
    [SerializeField] private RectTransform content;
    [Tooltip("Single card prefab — must have a TowerCard component on root.")]
    [SerializeField] private TowerCard     cardPrefab;
    [SerializeField] private TMPro.TMP_Text heading;

    [Header("Events")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    private readonly List<GameObject> spawnedEntries = new();
    private Action<TowerOption> onPick;
    private int revision;

    public bool IsOpen => modalRoot != null && modalRoot.activeSelf;
    public TowerPlacementZone Owner { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (modalRoot != null)
            modalRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open(IReadOnlyList<TowerOption> options, Action<TowerOption> onPick, TowerPlacementZone owner = null)
    {
        if (PlayerWallet.Instance == null)
        {
            Debug.LogError("[TowerSelectionModal] A PlayerWallet is required to show build options.", this);
            return;
        }
        if (modalRoot == null || content == null || cardPrefab == null)
        {
            Debug.LogError("[TowerSelectionModal] Missing modalRoot / content / cardPrefab.", this);
            return;
        }

        this.onPick = onPick;
        if (heading != null) heading.text = "CHOOSE A BUILDING";
        Owner = owner;
        ClearEntries();

        int coins = PlayerWallet.Instance.Coins;

        foreach (var opt in options)
        {
            if (opt == null || opt.prefab == null) continue;

            TowerCard card = Instantiate(cardPrefab, content);
            card.gameObject.SetActive(true);

            var captured = opt;
            int cardRevision = revision;
            card.Bind(opt, coins >= opt.cost, () =>
            {
                if (IsOpen && revision == cardRevision) HandlePick(captured);
            });

            spawnedEntries.Add(card.gameObject);
        }

        modalRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();
        onOpened?.Invoke();
    }

    public void OpenUpgrade(TowerOption option, int currentLevel, int? cost, bool unlocked,
        Func<bool> purchase, TowerPlacementZone owner)
    {
        if (PlayerWallet.Instance == null || option == null || option.prefab == null
            || modalRoot == null || content == null || cardPrefab == null) return;
        ClearEntries();
        onPick = null;
        Owner = owner;
        if (heading != null) heading.text = "BUILDING LEVELS";
        var card = Instantiate(cardPrefab, content);
        card.gameObject.SetActive(true);
        string action = !cost.HasValue ? "MAX LEVEL" : !unlocked ? "UPGRADE TOWN HALL" : "UPGRADE";
        bool canBuy = cost.HasValue && unlocked && PlayerWallet.Instance.Coins >= cost.Value;
        int cardRevision = revision;
        card.Bind(option, currentLevel, cost, action, canBuy, () =>
        {
            // A retained/stale button must not operate after the owning panel closes.
            if (!IsOpen || Owner != owner || revision != cardRevision || !canBuy) return;
            if (purchase()) Close();
        });
        spawnedEntries.Add(card.gameObject);
        modalRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();
        onOpened?.Invoke();
    }

    public void Close()
    {
        bool wasOpen = IsOpen;
        onPick = null;
        Owner = null;
        ClearEntries();
        if (modalRoot != null)
            modalRoot.SetActive(false);
        if (wasOpen) onClosed?.Invoke();
    }

    private void HandlePick(TowerOption opt)
    {
        var callback = onPick;
        Close();
        callback?.Invoke(opt);
    }

    private void ClearEntries()
    {
        revision++;
        foreach (var go in spawnedEntries)
            if (go != null) { go.SetActive(false); Destroy(go); }
        spawnedEntries.Clear();
    }
}
