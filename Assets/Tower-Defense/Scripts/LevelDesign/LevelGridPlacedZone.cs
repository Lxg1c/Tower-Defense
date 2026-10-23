using UnityEngine;

/// <summary>Authoring data for a zone placed with LevelGrid; has no runtime behavior.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]
public sealed class LevelGridPlacedZone : MonoBehaviour
{
    [SerializeField, HideInInspector] private LevelGrid sourceGrid;
    [SerializeField, HideInInspector] private RectInt buildingCells;
    [SerializeField, HideInInspector] private RectInt zoneCell;
    [SerializeField, HideInInspector] private GameObject previewPrefab;

    public LevelGrid SourceGrid => sourceGrid;
    public RectInt BuildingCells => buildingCells;
    public RectInt ZoneCell => zoneCell;
    public GameObject PreviewPrefab => previewPrefab;

    public void Configure(LevelGrid grid, RectInt building, RectInt interactionCell,
        GameObject buildingPreview = null)
    {
        sourceGrid = grid;
        buildingCells = building;
        zoneCell = interactionCell;
        previewPrefab = buildingPreview;
    }

    public bool ContainsCell(RectInt cell) => buildingCells.Overlaps(cell) || zoneCell.Overlaps(cell);
}
