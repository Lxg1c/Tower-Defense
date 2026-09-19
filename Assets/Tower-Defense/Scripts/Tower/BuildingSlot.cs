using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the building and the one-action-per-build-phase rule for a placement slot.
/// The zone supplies phase, town hall level and wallet; this class has no UI dependencies.
/// Existing serialized configuration stays on TowerPlacementZone.
/// </summary>
public sealed class BuildingSlot
{
    private readonly TowerLoadout loadout;
    private readonly BuildSlotType allowedType;
    private readonly int requiredTownHallLevel;
    private readonly List<TowerOption> options = new();

    public bool IsBuilt { get; private set; }
    public bool UsedThisPhase { get; private set; }
    public TowerUpgrade BuiltTower { get; private set; }
    public BaseUpgrade BuiltBase { get; private set; }

    public BuildingSlot(TowerLoadout loadout, BuildSlotType allowedType, int requiredTownHallLevel)
    {
        this.loadout = loadout;
        this.allowedType = allowedType;
        this.requiredTownHallLevel = requiredTownHallLevel;
    }

    public void BeginBuildPhase() => UsedThisPhase = false;

    public bool IsUnlocked(int townHallLevel) => townHallLevel >= requiredTownHallLevel;

    public bool CanInteract(bool isBuildPhase, int townHallLevel)
    {
        return isBuildPhase && !UsedThisPhase && IsUnlocked(townHallLevel) && CanUseBuilding;
    }

    private bool CanUseBuilding => !IsBuilt
        || (BuiltBase != null ? BuiltBase.HasNextLevel : BuiltTower != null && BuiltTower.CanUpgrade);

    public IReadOnlyList<TowerOption> GetOptions()
    {
        options.Clear();
        if (loadout != null && loadout.options != null)
            foreach (TowerOption option in loadout.options)
                if (option != null && option.prefab != null && option.slotType == allowedType)
                    options.Add(option);
        return options;
    }

    public bool TryBuild(TowerOption option, PlayerWallet wallet, bool isBuildPhase,
        int townHallLevel, Vector3 position, Quaternion rotation, out GameObject building)
    {
        building = null;
        if (IsBuilt || !CanInteract(isBuildPhase, townHallLevel)
            || option == null || option.prefab == null || option.slotType != allowedType
            || option.cost < 0 || !IsAvailableOption(option) || wallet == null)
            return false;

        if (!wallet.TrySpend(option.cost))
            return false;

        building = Object.Instantiate(option.prefab, position, rotation);
        BuiltTower = building.GetComponentInChildren<TowerUpgrade>();
        BuiltBase = building.GetComponentInChildren<BaseUpgrade>();
        IsBuilt = true;
        UsedThisPhase = true;
        return true;
    }

    public bool TryUpgrade(bool isBuildPhase, int townHallLevel)
    {
        if (!IsBuilt || !CanInteract(isBuildPhase, townHallLevel))
            return false;

        bool upgraded = BuiltBase != null ? BuiltBase.TryUpgrade() : BuiltTower.TryUpgrade();
        if (upgraded)
            UsedThisPhase = true;
        return upgraded;
    }

    private bool IsAvailableOption(TowerOption option)
    {
        if (loadout == null || loadout.options == null)
            return false;
        foreach (TowerOption available in loadout.options)
            if (ReferenceEquals(available, option))
                return true;
        return false;
    }
}
