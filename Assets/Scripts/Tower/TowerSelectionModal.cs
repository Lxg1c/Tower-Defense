using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Scene-wide singleton modal. A zone calls Open() with a list of options and
/// a callback. The modal instantiates each option's own <see cref="TowerOption.buttonPrefab"/>
/// as a child of Content, finds its Button component (self or any child) and
/// hooks the click. No visual template is owned by the modal — every option
/// brings its own look.
///
/// Affordability: if the player can't afford an option, its Button.interactable
/// is set to false. The prefab can show a disabled look however it wants.
/// </summary>
public class TowerSelectionModal : MonoBehaviour
{
    public static TowerSelectionModal Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private RectTransform content;

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
        if (modalRoot == null || content == null) return;

        this.onPick = onPick;
        ClearEntries();

        int coins = PlayerWallet.Instance != null ? PlayerWallet.Instance.Coins : int.MaxValue;

        foreach (var opt in options)
        {
            if (opt == null || opt.prefab == null || opt.buttonPrefab == null) continue;

            GameObject entry = Instantiate(opt.buttonPrefab, content);
            entry.SetActive(true);

            Button btn = entry.GetComponentInChildren<Button>(true);
            if (btn != null)
            {
                btn.interactable = coins >= opt.cost;
                var captured = opt;
                btn.onClick.AddListener(() => HandlePick(captured));
            }
            else
            {
                Debug.LogWarning($"[TowerSelectionModal] '{opt.buttonPrefab.name}' has no Button component.", this);
            }

            spawnedEntries.Add(entry);
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
