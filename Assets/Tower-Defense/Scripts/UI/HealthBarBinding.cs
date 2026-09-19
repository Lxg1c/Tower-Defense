using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Presentation settings and active lifetime for an entity's health bar.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class HealthBarBinding : MonoBehaviour
{
    [SerializeField] private Damageable target;
    [SerializeField] private HealthBar prefab;
    [SerializeField] private Transform anchor;
    [SerializeField] private bool hideOnDeath = true;

    // UI-only discovery, scoped by scene in the manager. Both enable orders are supported.
    private static readonly HashSet<HealthBarBinding> active = new();
    public static IEnumerable<HealthBarBinding> Active => active;
    public static event Action<HealthBarBinding> Added;
    public static event Action<HealthBarBinding> Removed;
    public Damageable Target => target;
    public HealthBar Prefab => prefab;
    public Transform Anchor => anchor;
    public bool HideOnDeath => hideOnDeath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        active.Clear();
        Added = null;
        Removed = null;
    }

    private void OnEnable()
    {
        if (target == null || target.gameObject != gameObject)
        {
            Debug.LogError("[HealthBarBinding] Assign the Damageable on this GameObject.", this);
            enabled = false;
            return;
        }
        if (active.Add(this)) Added?.Invoke(this);
    }

    private void OnDisable()
    {
        if (active.Remove(this)) Removed?.Invoke(this);
    }
}
