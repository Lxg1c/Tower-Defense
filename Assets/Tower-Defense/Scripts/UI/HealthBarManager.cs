using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Projects entity health onto the gameplay UI Toolkit panel.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public sealed class HealthBarManager : MonoBehaviour
{
    [SerializeField] private Vector3 barOffset = new(0f, 1.8f, 0f);
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private Camera worldCamera;

    private sealed class Entry
    {
        public VisualElement Root;
        public VisualElement Health;
        public VisualElement Shield;
    }

    private readonly Dictionary<HealthBarBinding, Entry> active = new();
    private VisualElement layer;
    public int ActiveBarCount => active.Count;

    public void SetPresentationRoot(VisualElement root)
    {
        layer?.RemoveFromHierarchy();
        layer = null;
        foreach (Entry entry in active.Values)
        {
            entry.Root = null;
            entry.Health = null;
            entry.Shield = null;
        }
        if (root == null) return;
        layer = new VisualElement { pickingMode = PickingMode.Ignore };
        layer.AddToClassList("health-bar-layer");
        VisualElement container = root.Q("gameRoot") ?? root;
        container.Insert(Mathf.Min(1, container.childCount), layer);
        foreach (Entry entry in active.Values) CreateVisual(entry);
    }

    public void SetVisible(bool visible) => layer?.EnableInClassList("is-hidden", !visible);

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
        foreach (HealthBarBinding binding in HealthBarBinding.Active) Register(binding);
    }

    private void OnDisable()
    {
        HealthBarBinding.Added -= Register;
        HealthBarBinding.Removed -= Unregister;
        active.Clear();
        SetPresentationRoot(null);
    }

    private void Register(HealthBarBinding binding)
    {
        if (binding == null || binding.gameObject.scene != gameObject.scene || active.ContainsKey(binding)) return;
        var entry = new Entry();
        active.Add(binding, entry);
        if (layer != null) CreateVisual(entry);
    }

    private void Unregister(HealthBarBinding binding)
    {
        if (binding == null || !active.TryGetValue(binding, out Entry entry)) return;
        entry.Root?.RemoveFromHierarchy();
        active.Remove(binding);
    }

    private void CreateVisual(Entry entry)
    {
        if (layer == null) return;
        entry.Root = new VisualElement { pickingMode = PickingMode.Ignore };
        entry.Root.AddToClassList("entity-health-bar");
        entry.Health = new VisualElement { pickingMode = PickingMode.Ignore };
        entry.Health.AddToClassList("entity-health-fill");
        entry.Shield = new VisualElement { pickingMode = PickingMode.Ignore };
        entry.Shield.AddToClassList("entity-shield-fill");
        entry.Root.Add(entry.Health);
        entry.Root.Add(entry.Shield);
        layer.Add(entry.Root);
    }

    private void LateUpdate()
    {
        if (layer == null) return;
        float width = layer.resolvedStyle.width;
        float height = layer.resolvedStyle.height;
        if (width <= 0f || height <= 0f || worldCamera == null) return;
        foreach (var pair in active)
        {
            HealthBarBinding binding = pair.Key;
            Entry entry = pair.Value;
            if (binding == null || entry.Root == null || binding.Target == null) continue;
            float health = binding.Target.MaxHealth > 0f
                ? Mathf.Clamp01(binding.Target.CurrentHealth / binding.Target.MaxHealth) : 0f;
            PlayerHealth player = binding.Target as PlayerHealth;
            float shield = player != null && player.MaxShield > 0f
                ? Mathf.Clamp01(player.CurrentShield / player.MaxShield) : 0f;
            Vector3 position = binding.Anchor != null ? binding.Anchor.position : binding.transform.position + barOffset;
            Vector3 screen = worldCamera.WorldToScreenPoint(position);
            float x = screen.x * width / Screen.width;
            float y = screen.y * height / Screen.height;
            bool show = binding.isActiveAndEnabled && screen.z > 0f && x >= 30f && x <= width - 30f &&
                y >= 12f && y <= height - 12f && !(binding.HideOnDeath && health <= 0f) &&
                !(hideWhenFull && health >= 0.999f && shield <= 0f);
            entry.Root.EnableInClassList("is-hidden", !show);
            if (!show) continue;
            entry.Root.style.left = x - 23f;
            entry.Root.style.top = height - y - 4f;
            entry.Health.style.width = Length.Percent(health * 100f);
            entry.Shield.style.width = Length.Percent(shield * 100f);
            entry.Shield.EnableInClassList("is-hidden", shield <= 0f);
        }
    }
}
