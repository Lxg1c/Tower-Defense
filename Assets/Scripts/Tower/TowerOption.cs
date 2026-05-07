using UnityEngine;

/// <summary>
/// Pure data describing one buildable tower:
/// the prefab to spawn, its display name, icon and cost.
/// The UI is built by <see cref="TowerSelectionModal"/> from a shared card prefab.
/// </summary>
[System.Serializable]
public class TowerOption
{
    [Tooltip("Tower prefab that will be built if this option is chosen.")]
    public GameObject prefab;

    [Tooltip("Name shown on the card.")]
    public string displayName;

    [Tooltip("Icon shown on the card.")]
    public Sprite icon;

    [Min(0)] public int cost = 50;
}
