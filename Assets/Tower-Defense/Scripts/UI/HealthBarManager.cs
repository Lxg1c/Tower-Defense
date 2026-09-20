using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public class HealthBarManager : MonoBehaviour
{

    private Canvas worldCanvas;

    [Header("Default Health Bar Prefab")]
    [Tooltip("Used by bindings without a prefab override.")]
    [SerializeField] private HealthBar defaultHealthBarPrefab;

    [Header("Layout")]
    [SerializeField] private Vector3 barOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private bool hideWhenFull = true;

    [SerializeField] private Camera worldCamera;

    // One pool queue per prefab type (keyed by prefab instance ID).
    private readonly Dictionary<int, Queue<HealthBar>> _pools  = new();

    private class ActiveEntry
    {
        public HealthBar bar;
        public int poolKey;
        public UnityAction<float> hpListener;
    }

    private readonly Dictionary<HealthBarBinding, ActiveEntry> _active = new();

    private void Awake()
    {
        EnsureCanvas();
    }

    public int ActiveBarCount => _active.Count;

    private void OnEnable()
    {
        if (worldCamera == null)
        {
            Debug.LogError("[HealthBarManager] Assign the world camera.", this);
            enabled = false;
            return;
        }
        HealthBarBinding.Added += Register;
        HealthBarBinding.Removed += Unregister;
        foreach (var binding in HealthBarBinding.Active) Register(binding);
    }

    private void OnDisable()
    {
        HealthBarBinding.Added -= Register;
        HealthBarBinding.Removed -= Unregister;
        foreach (var binding in new List<HealthBarBinding>(_active.Keys)) Unregister(binding);
    }

    private void OnDestroy()
    {
        _pools.Clear();
        if (worldCanvas != null) Destroy(worldCanvas.gameObject);
    }

    private void LateUpdate()
    {
        foreach (var kv in _active)
        {
            var entity = kv.Key;
            var entry  = kv.Value;
            if (entity == null || entry.bar == null || !entry.bar.gameObject.activeSelf) continue;
            entry.bar.transform.position = ResolveBarPosition(entity);
            entry.bar.transform.forward = worldCamera.transform.forward;
        }
    }

    private Vector3 ResolveBarPosition(HealthBarBinding entity)
    {
        // Per-entity anchor wins; otherwise use the global offset above the entity's pivot.
        var anchor = entity.Anchor;
        return anchor != null ? anchor.position : entity.transform.position + barOffset;
    }

    private void Register(HealthBarBinding entity)
    {
        if (entity == null || entity.gameObject.scene != gameObject.scene || _active.ContainsKey(entity)) return;

        // Pick the prefab: entity override → manager default → error.
        HealthBar prefab = entity.Prefab != null ? entity.Prefab : defaultHealthBarPrefab;
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
        SetBarFillAndVisibility(bar, entity.Target.CurrentHealth / entity.Target.MaxHealth, entity.HideOnDeath);

        var entry = new ActiveEntry { bar = bar, poolKey = prefab.GetInstanceID() };
        entry.hpListener   = hp => OnHealthChanged(entity, hp);
        entity.Target.onHealthChanged.AddListener(entry.hpListener);

        _active[entity] = entry;
    }

    private void Unregister(HealthBarBinding entity)
    {
        if (entity == null || !_active.TryGetValue(entity, out var entry)) return;

        // Detach listeners so the entity doesn't keep references and accumulate duplicates on re-spawn.
        if (entity.Target != null) entity.Target.onHealthChanged.RemoveListener(entry.hpListener);

        _active.Remove(entity);
        ReturnToPool(entry.bar, entry.poolKey);
    }

    private void OnHealthChanged(HealthBarBinding entity, float current)
    {
        if (_active.TryGetValue(entity, out var entry))
            SetBarFillAndVisibility(entry.bar, current / entity.Target.MaxHealth, entity.HideOnDeath);
    }

    private void SetBarFillAndVisibility(HealthBar bar, float normalized, bool hideOnDeath)
    {
        if (bar == null)
            return;

        float fill = Mathf.Clamp01(normalized);
        if (hideOnDeath && fill <= 0f)
        {
            bar.SetFillInstant(0f);
            bar.gameObject.SetActive(false);
            return;
        }

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
        if (_pools.TryGetValue(key, out Queue<HealthBar> pool))
        {
            while (pool.Count > 0)
            {
                HealthBar recycled = pool.Dequeue();
                // Unity can destroy scene objects before their owners receive cleanup callbacks.
                // A destroyed object is no longer a pool resource.
                if (recycled == null) continue;
                recycled.gameObject.SetActive(true);
                return recycled;
            }
        }
        return Instantiate(prefab, worldCanvas.transform);
    }

    private void ReturnToPool(HealthBar bar, int key)
    {
        // During scene unload or Play-mode exit, destruction order is not guaranteed.
        // Listener cleanup still happens, but destroyed resources must not be recycled.
        if (bar == null) return;
        bar.gameObject.SetActive(false);
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
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        worldCanvas = go.AddComponent<Canvas>();
        worldCanvas.renderMode  = RenderMode.WorldSpace;
        worldCanvas.worldCamera = worldCamera;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(10000f, 10000f);
        rt.localScale = Vector3.one * 0.01f;
        // Keep world scale while making the manager the canvas's lifetime owner.
        rt.SetParent(transform, true);
    }
}
