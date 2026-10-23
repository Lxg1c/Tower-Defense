using System.Collections.Generic;
using UnityEngine;

/// <summary>Authoring coordinates for placing building zones in the Scene view.</summary>
[DisallowMultipleComponent]
public sealed class LevelGrid : MonoBehaviour
{
    [Header("Grid")]
    [Min(1)] [SerializeField] private int columns = 20;
    [Min(1)] [SerializeField] private int rows = 20;
    [Min(0.1f)] [SerializeField] private float cellSize = 3f;

    [Header("Building zones")]
    [SerializeField] private GameObject zonePrefab;
    [SerializeField] private Transform zonesParent;
    [SerializeField] private BuildSlotType selectedType = BuildSlotType.Defense;

    [Header("Footprints in cells")]
    [SerializeField] private Vector2Int townHallFootprint = new(2, 2);
    [SerializeField] private Vector2Int defenseFootprint = Vector2Int.one;
    [SerializeField] private Vector2Int mineFootprint = Vector2Int.one;
    [SerializeField] private Vector2Int barracksFootprint = new(2, 2);

    public int Columns => Mathf.Max(1, columns);
    public int Rows => Mathf.Max(1, rows);
    public float CellSize => Mathf.Max(0.1f, cellSize);
    public GameObject ZonePrefab => zonePrefab;
    public Transform ZonesParent => zonesParent;
    public BuildSlotType SelectedType => selectedType;

    public Vector2Int GetFootprint(BuildSlotType type)
    {
        Vector2Int size = type switch
        {
            BuildSlotType.TownHall => townHallFootprint,
            BuildSlotType.Defense => defenseFootprint,
            BuildSlotType.Mine => mineFootprint,
            BuildSlotType.Barracks => barracksFootprint,
            _ => Vector2Int.one
        };
        return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
    }

    public bool TryGetPlacement(Vector3 worldPosition, BuildSlotType type, out RectInt cells)
    {
        Vector2Int footprint = GetFootprint(type);
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float size = CellSize;
        float left = -Columns * size * 0.5f;
        float bottom = -Rows * size * 0.5f;
        int x = Mathf.RoundToInt((local.x - left) / size - footprint.x * 0.5f);
        int y = Mathf.RoundToInt((local.z - bottom) / size - footprint.y * 0.5f);
        cells = new RectInt(x, y, footprint.x, footprint.y);
        return Contains(cells);
    }

    public bool Contains(RectInt cells) => cells.xMin >= 0 && cells.yMin >= 0
        && cells.xMax <= Columns && cells.yMax <= Rows;

    public Vector3 GetPlacementCenter(RectInt cells)
    {
        float size = CellSize;
        return transform.TransformPoint(new Vector3(
            (cells.x + cells.width * 0.5f - Columns * 0.5f) * size,
            0f,
            (cells.y + cells.height * 0.5f - Rows * 0.5f) * size));
    }

    public Quaternion GetBuildingRotation(RectInt building, RectInt interactionCell)
    {
        Vector3 side;
        if (interactionCell.yMax == building.yMin
            && interactionCell.xMin >= building.xMin && interactionCell.xMax <= building.xMax)
            side = Vector3.back;
        else if (interactionCell.yMin == building.yMax
            && interactionCell.xMin >= building.xMin && interactionCell.xMax <= building.xMax)
            side = Vector3.forward;
        else if (interactionCell.xMax == building.xMin
            && interactionCell.yMin >= building.yMin && interactionCell.yMax <= building.yMax)
            side = Vector3.left;
        else if (interactionCell.xMin == building.xMax
            && interactionCell.yMin >= building.yMin && interactionCell.yMax <= building.yMax)
            side = Vector3.right;
        else
            throw new System.ArgumentException("Interaction cell must touch a side of the building.");

        return transform.rotation * Quaternion.LookRotation(side, Vector3.up);
    }

    public Vector3 GetCorner(int x, int y)
    {
        float size = CellSize;
        return transform.TransformPoint(new Vector3(
            (x - Columns * 0.5f) * size, 0f,
            (y - Rows * 0.5f) * size));
    }

    public static bool Overlaps(RectInt first, RectInt second) => first.Overlaps(second);

    public IEnumerable<RectInt> GetAdjacentZoneCells(RectInt building)
    {
        if (!Contains(building))
            yield break;

        for (int x = building.xMin; x < building.xMax; x++)
        {
            if (building.yMin > 0)
                yield return new RectInt(x, building.yMin - 1, 1, 1);
            if (building.yMax < Rows)
                yield return new RectInt(x, building.yMax, 1, 1);
        }
        for (int y = building.yMin; y < building.yMax; y++)
        {
            if (building.xMin > 0)
                yield return new RectInt(building.xMin - 1, y, 1, 1);
            if (building.xMax < Columns)
                yield return new RectInt(building.xMax, y, 1, 1);
        }
    }

    public bool TryGetAdjacentZoneCell(RectInt building, Vector3 worldPointer, out RectInt cell)
    {
        cell = default;
        float nearestSquared = float.PositiveInfinity;
        foreach (RectInt candidate in GetAdjacentZoneCells(building))
        {
            float distanceSquared = (GetPlacementCenter(candidate) - worldPointer).sqrMagnitude;
            if (distanceSquared >= nearestSquared)
                continue;
            nearestSquared = distanceSquared;
            cell = candidate;
        }

        float snapRadius = CellSize * 0.85f;
        return nearestSquared <= snapRadius * snapRadius;
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        cellSize = Mathf.Max(0.1f, cellSize);
    }
}
