using UnityEngine;

/// <summary>
/// Shared list of enemies available for wave setup.
/// Create via Assets -> Create -> Enemies -> Loadout.
/// </summary>
[CreateAssetMenu(menuName = "Enemies/Loadout", fileName = "EnemyLoadout")]
public class EnemyLoadout : ScriptableObject
{
    public EnemyOption[] options;
}
