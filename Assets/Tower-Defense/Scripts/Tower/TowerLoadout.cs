using UnityEngine;

/// <summary>
/// Player-owned set of towers available to build.
/// Create via Assets → Create → Towers → Loadout. Assign on TowerPlacementZone (or
/// a global registry) so changing the loadout in one place updates every zone.
/// </summary>
[CreateAssetMenu(menuName = "Towers/Loadout", fileName = "TowerLoadout")]
public class TowerLoadout : ScriptableObject
{
    public TowerOption[] options;
}
