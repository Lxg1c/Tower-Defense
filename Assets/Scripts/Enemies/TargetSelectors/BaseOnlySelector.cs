using UnityEngine;

/// <summary>
/// Target selector that always returns the main Base, ignoring everything else.
/// Useful for a "rush" mob archetype or fallback when nothing else is alive.
/// </summary>
[DisallowMultipleComponent]
public class BaseOnlySelector : MonoBehaviour, ITargetSelector
{
    [SerializeField] private Base targetBase;

    private void Awake()
    {
        if (targetBase == null)
            targetBase = FindFirstObjectByType<Base>();
    }

    public Damageable GetTarget(MobCore mob)
    {
        if (targetBase == null || !targetBase.IsTargetable) return null;
        return targetBase;
    }
}
