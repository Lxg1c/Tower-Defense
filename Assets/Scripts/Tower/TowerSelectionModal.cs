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

    [Header("Events")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    private readonly List<GameObject> spawnedEntries = new();
    private Action<TowerOption> onPick;

    public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

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

    public void Open(IReadOnlyList<TowerOption> options, Action<TowerOption> onPick)
    {
        if (modalRoot == null || content == null || cardPrefab == null)
        {
            Debug.LogError("[TowerSelectionModal] Missing modalRoot / content / cardPrefab.", this);
            return;
        }

        this.onPick = onPick;
        ClearEntries();

        int coins = PlayerWallet.Instance != null ? PlayerWallet.Instance.Coins : int.MaxValue;

        foreach (var opt in options)
        {
            if (opt == null || opt.prefab == null) continue;

            TowerCard card = Instantiate(cardPrefab, content);
            card.gameObject.SetActive(true);

            var captured = opt;
            card.Bind(opt, coins >= opt.cost, () => HandlePick(captured));

            spawnedEntries.Add(card.gameObject);
        }

        modalRoot.SetActive(true);
        onOpened?.Invoke();
    }

    public void Close()
    {
        bool wasOpen = IsOpen;
        onPick = null;
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
        foreach (var go in spawnedEntries)
            if (go != null) Destroy(go);
        spawnedEntries.Clear();
    }
}
