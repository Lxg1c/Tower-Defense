using UnityEngine;

/// <summary>
/// A single tower choice offered to the player inside a placement zone.
///
/// Each option carries its own UI prefab — typically a GameObject with a Button
/// component somewhere inside (button + icon + label + cost, laid out however
/// you like). The modal just instantiates this prefab, finds the Button and
/// hooks the click. Everything visual is owned by the prefab, not the modal.
/// </summary>
[System.Serializable]
public class TowerOption
{
    [Tooltip("The tower that will be built if this option is chosen.")]
    public GameObject prefab;

    [Min(0)] public int cost = 50;

    [Tooltip("GameObject with a Button component (on itself or a child). Used as the modal entry for this tower.")]
    public GameObject buttonPrefab;
}
