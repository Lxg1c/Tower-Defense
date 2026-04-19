using System.Collections.Generic;
using UnityEngine;

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

    private Camera _mainCam;

    // One pool queue per prefab type (keyed by prefab instance ID).
    private readonly Dictionary<int, Queue<HealthBar>> _pools  = new();

    // Active bars: entity → (bar instance, prefab used to create it)
    private readonly Dictionary<Damageable, (HealthBar bar, HealthBar prefab)> _active = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _mainCam = Camera.main;
        EnsureCanvas();
    }

    private void LateUpdate()
    {
        foreach (var (entity, entry) in _active)
        {
            if (entity == null) continue;
            entry.bar.transform.position = entity.transform.position + barOffset;
            entry.bar.transform.forward  = _mainCam.transform.forward;
        }
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
        bar.transform.position = entity.transform.position + barOffset;
        bar.gameObject.SetActive(true);
        bar.SetFill(entity.CurrentHealth / entity.MaxHealth);

        _active[entity] = (bar, prefab);

        entity.onHealthChanged.AddListener(hp => OnHealthChanged(entity, hp));
        if (entity.DespawnBarOnDeath)
            entity.onDied.AddListener(() => Unregister(entity));
    }

    public void Unregister(Damageable entity)
    {
        if (!_active.TryGetValue(entity, out var entry)) return;
        _active.Remove(entity);
        ReturnToPool(entry.bar, entry.prefab);
    }

    private void OnHealthChanged(Damageable entity, float current)
    {
        if (_active.TryGetValue(entity, out var entry))
            entry.bar.SetFill(current / entity.MaxHealth);
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

        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode == RenderMode.WorldSpace)
            {
                worldCanvas = c;
                return;
            }
        }

        GameObject go = new("HealthBar_SharedCanvas");
        worldCanvas = go.AddComponent<Canvas>();
        worldCanvas.renderMode  = RenderMode.WorldSpace;
        worldCanvas.worldCamera = _mainCam;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(10000f, 10000f);
        rt.localScale = Vector3.one * 0.01f;
    }
}
