using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class HealthBarManager : MonoBehaviour
{
    public static HealthBarManager Instance { get; private set; }

    private Canvas worldCanvas;

    [Header("Default Health Bar Prefab")]
    [Tooltip("Used when the Damageable does not specify its own prefab.")]
    [SerializeField] private HealthBar defaultHealthBarPrefab;

    [Header("Layout")]
    [SerializeField] private Vector3 barOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private bool hideWhenFull = true;

    private Camera _mainCam;

    // One pool queue per prefab type (keyed by prefab instance ID).
    private readonly Dictionary<int, Queue<HealthBar>> _pools  = new();

    private class ActiveEntry
    {
        public HealthBar bar;
        public HealthBar prefab;
        public UnityAction<float> hpListener;
        public UnityAction         diedListener;
    }

    private readonly Dictionary<Damageable, ActiveEntry> _active = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _mainCam = Camera.main;
        EnsureCanvas();
    }

    private void LateUpdate()
    {
        foreach (var kv in _active)
        {
            var entity = kv.Key;
            var entry  = kv.Value;
            if (entity == null || entry.bar == null || !entry.bar.gameObject.activeSelf) continue;
            entry.bar.transform.position = ResolveBarPosition(entity);
            entry.bar.transform.forward  = _mainCam.transform.forward;
        }
    }

    private Vector3 ResolveBarPosition(Damageable entity)
    {
        // Per-entity anchor wins; otherwise use the global offset above the entity's pivot.
        var anchor = entity.HealthBarAnchor;
        return anchor != null ? anchor.position : entity.transform.position + barOffset;
    }

    public void Register(Damageable entity)
    {
        if (entity == null || _active.ContainsKey(entity)) return;

        // Pick the prefab: entity override → manager default → error.
        HealthBar prefab = entity.HealthBarPrefab != null ? entity.HealthBarPrefab : defaultHealthBarPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[HealthBarManager] No health bar prefab for {entity.name}. " +
                           "Assign one to the entity or set the Default Health Bar Prefab on HealthBarManager.", this);
            return;
        }

        HealthBar bar = GetFromPool(prefab);
        bar.transform.SetParent(worldCanvas.transform, true);
        bar.transform.position = ResolveBarPosition(entity);
        bar.gameObject.SetActive(true);
        SetBarFillAndVisibility(bar, entity.CurrentHealth / entity.MaxHealth);

        var entry = new ActiveEntry { bar = bar, prefab = prefab };
        entry.hpListener   = hp => OnHealthChanged(entity, hp);
        entry.diedListener = () => Unregister(entity);

        entity.onHealthChanged.AddListener(entry.hpListener);
        if (entity.DespawnBarOnDeath)
            entity.onDied.AddListener(entry.diedListener);

        _active[entity] = entry;
    }

    public void Unregister(Damageable entity)
    {
        if (entity == null || !_active.TryGetValue(entity, out var entry)) return;

        // Detach listeners so the entity doesn't keep references and accumulate duplicates on re-spawn.
        if (entry.hpListener   != null) entity.onHealthChanged.RemoveListener(entry.hpListener);
        if (entry.diedListener != null) entity.onDied.RemoveListener(entry.diedListener);

        _active.Remove(entity);
        ReturnToPool(entry.bar, entry.prefab);
    }

    private void OnHealthChanged(Damageable entity, float current)
    {
        if (_active.TryGetValue(entity, out var entry))
            SetBarFillAndVisibility(entry.bar, current / entity.MaxHealth);
    }

    private void SetBarFillAndVisibility(HealthBar bar, float normalized)
    {
        if (bar == null)
            return;

        float fill = Mathf.Clamp01(normalized);

        if (hideWhenFull)
        {
            bool isFull = fill >= 0.999f;
            if (isFull)
            {
                bar.SetFillInstant(1f);
                bar.gameObject.SetActive(false);
            }
            else
            {
                if (!bar.gameObject.activeSelf)
                    bar.gameObject.SetActive(true);
                bar.SetFill(fill);
            }
        }
        else if (!bar.gameObject.activeSelf)
        {
            bar.gameObject.SetActive(true);
            bar.SetFill(fill);
        }
        else
        {
            bar.SetFill(fill);
        }
    }

    // ── Pool helpers ──────────────────────────────────────────────────────────

    private HealthBar GetFromPool(HealthBar prefab)
    {
        int key = prefab.GetInstanceID();
        if (_pools.TryGetValue(key, out Queue<HealthBar> pool) && pool.Count > 0)
        {
            HealthBar recycled = pool.Dequeue();
            recycled.gameObject.SetActive(true);
            return recycled;
        }
        return Instantiate(prefab, worldCanvas.transform);
    }

    private void ReturnToPool(HealthBar bar, HealthBar prefab)
    {
        bar.gameObject.SetActive(false);
        int key = prefab.GetInstanceID();
        if (!_pools.ContainsKey(key))
            _pools[key] = new Queue<HealthBar>();
        _pools[key].Enqueue(bar);
    }

    // ── Canvas setup ──────────────────────────────────────────────────────────

    private void EnsureCanvas()
    {
        if (worldCanvas != null) return;

        // Always create our own dedicated world-space canvas so we can't
        // accidentally inherit a foreign canvas's scale (e.g. someone else's
        // WorldSpace UI at 1:1 would make our bars appear 100× larger).
        GameObject go = new("HealthBar_SharedCanvas");
        worldCanvas = go.AddComponent<Canvas>();
        worldCanvas.renderMode  = RenderMode.WorldSpace;
        worldCanvas.worldCamera = _mainCam;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(10000f, 10000f);
        rt.localScale = Vector3.one * 0.01f;
    }
}
