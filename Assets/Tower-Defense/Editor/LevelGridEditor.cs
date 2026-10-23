using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

[CustomEditor(typeof(LevelGrid))]
public sealed class LevelGridEditor : Editor
{
    private static readonly Color GridColor = new(1f, 0.72f, 0.18f, 1f);
    private static readonly Color GridBacking = new(0.06f, 0.07f, 0.09f, 0.9f);
    private static readonly Color PlacedGhostColor = new(0.82f, 1f, 1f, 0.9f);
    private static GUIStyle readableLabelStyle;
    private bool painting;
    private GameObject activeGhostPrefab;
    private GameObject ghost;
    private Material ghostMaterial;
    private readonly Dictionary<LevelGridPlacedZone, PlacedPreview> placedPreviews = new();
    private bool choosingZone;
    private bool movingBuilding;
    private LevelGridPlacedZone selectedPlacement;
    private LevelGridPlacedZone editingPlacement;
    private RectInt chosenBuilding;
    private BuildSlotType chosenType;

    private sealed class PlacedPreview
    {
        public GameObject prefab;
        public GameObject instance;
    }

    private void OnDisable() => DestroyAllGhosts();

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
            ((LevelGrid)target).GetComponent<LevelGridTileSurface>()?.Rebuild();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            choosingZone
                ? "Click a highlighted adjacent cell for the interaction zone. Esc cancels the move."
                : movingBuilding
                    ? "Click a free footprint for the selected building, then choose its interaction zone. Esc cancels."
                    : "Click a building or its interaction zone to edit the pair. Click a free footprint to add a zone. 1-4 choose building type; Delete/Backspace removes the zone under the pointer; Ctrl+Z undoes edits.",
            MessageType.Info);
        EditorGUILayout.LabelField("Cyan models: placed previews. Red cells: occupied building space; orange cells: occupied interaction zones.",
            EditorStyles.wordWrappedMiniLabel);

        var grid = (LevelGrid)target;
        bool validPrefab = grid.ZonePrefab != null
            && EditorUtility.IsPersistent(grid.ZonePrefab)
            && grid.ZonePrefab.GetComponent<TowerPlacementZone>() != null;
        if (!validPrefab)
            EditorGUILayout.HelpBox("Assign the TowerBuildZone prefab asset before painting.", MessageType.Warning);
        else if (GetDefaultPreview(grid, grid.SelectedType) == null)
            EditorGUILayout.HelpBox("No preview model for this building type in the zone loadout.",
                MessageType.Warning);

        using (new EditorGUI.DisabledScope(Application.isPlaying || !validPrefab))
            if (GUILayout.Button(painting ? "Stop placing zones" : "Place building zones"))
            {
                painting = !painting;
                if (!painting)
                {
                    choosingZone = false;
                    movingBuilding = false;
                    editingPlacement = null;
                    DestroyGhost();
                }
                SceneView.RepaintAll();
            }
        if ((choosingZone || movingBuilding) && GUILayout.Button("Cancel move"))
        {
            choosingZone = false;
            movingBuilding = false;
            editingPlacement = null;
            SceneView.RepaintAll();
        }
        DrawSelectedPlacement(grid);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            if (GUILayout.Button("Face existing buildings toward their zones"))
                AlignExistingPlacements(grid);
    }

    private void OnSceneGUI()
    {
        var grid = (LevelGrid)target;
        if (grid == null || Application.isPlaying)
        {
            DestroyAllGhosts();
            return;
        }

        DrawGrid(grid);
        DrawOccupiedZones(grid);
        if (selectedPlacement == null || selectedPlacement.SourceGrid != grid)
        {
            selectedPlacement = null;
            movingBuilding = false;
            editingPlacement = null;
        }
        else
            DrawSelectedPair(grid, selectedPlacement);
        if (!painting || grid.ZonePrefab == null)
        {
            choosingZone = false;
            movingBuilding = false;
            editingPlacement = null;
            DestroyGhost();
            return;
        }

        Event current = Event.current;
        if (choosingZone && ((editingPlacement == null && chosenType != grid.SelectedType)
                             || !grid.Contains(chosenBuilding)))
        {
            choosingZone = false;
            editingPlacement = null;
        }
        if (current.type == EventType.KeyDown && !current.alt && !current.control && !current.command
            && TryGetShortcutType(current.keyCode, out BuildSlotType shortcutType))
        {
            var serializedGrid = new SerializedObject(grid);
            serializedGrid.FindProperty("selectedType").enumValueIndex = (int)shortcutType;
            serializedGrid.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(grid);
            choosingZone = false;
            movingBuilding = false;
            editingPlacement = null;
            DestroyGhost();
            current.Use();
            Repaint();
            SceneView.RepaintAll();
            return;
        }
        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
        {
            choosingZone = false;
            movingBuilding = false;
            editingPlacement = null;
            selectedPlacement = null;
            DestroyGhost();
            current.Use();
            Repaint();
            SceneView.RepaintAll();
            return;
        }
        if (current.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (current.alt || !TryGetMousePoint(grid, current.mousePosition, out Vector3 point))
        {
            if (ghost != null)
                ghost.SetActive(false);
            return;
        }

        LevelGridPlacedZone hoveredZone = !choosingZone && !movingBuilding
            ? FindPlacedZoneAt(grid, point) : null;
        if (hoveredZone != null && current.type == EventType.KeyDown
            && (current.keyCode == KeyCode.Delete || current.keyCode == KeyCode.Backspace))
        {
            if (hoveredZone == selectedPlacement)
                selectedPlacement = null;
            Undo.DestroyObjectImmediate(hoveredZone.gameObject);
            current.Use();
            Repaint();
            SceneView.RepaintAll();
            return;
        }

        if (choosingZone)
        {
            TowerPlacementZone ignored = editingPlacement != null
                ? editingPlacement.GetComponent<TowerPlacementZone>() : null;
            DrawFootprint(grid, chosenBuilding, true, chosenType);
            DrawAdjacentZones(grid, chosenBuilding, ignored);
            bool near = grid.TryGetAdjacentZoneCell(chosenBuilding, point, out RectInt zoneCell);
            bool free = near && IsZoneCellFree(grid, zoneCell, ignored);
            if (near)
                DrawZoneCell(grid, zoneCell, free, true);
            Quaternion rotation = near
                ? grid.GetBuildingRotation(chosenBuilding, zoneCell)
                : grid.transform.rotation;
            UpdateGhost(grid, chosenBuilding, true, true, GetEditingPreview(grid), rotation);

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                current.Use();
                if (free)
                {
                    if (editingPlacement != null)
                        MovePlacement(grid, editingPlacement, chosenBuilding, zoneCell);
                    else
                        PlaceZone(grid, chosenBuilding, zoneCell);
                    choosingZone = false;
                    movingBuilding = false;
                    editingPlacement = null;
                    if (ghost != null)
                        ghost.SetActive(false);
                    Repaint();
                }
            }
        }
        else
        {
            if (hoveredZone != null)
            {
                if (hoveredZone != selectedPlacement)
                    DrawSelectedPair(grid, hoveredZone);
                Handles.Label(grid.GetPlacementCenter(hoveredZone.BuildingCells)
                    + grid.transform.up * 0.5f, "CLICK TO EDIT", ReadableLabelStyle);
            }
            BuildSlotType type = movingBuilding ? GetPlacementType(selectedPlacement) : grid.SelectedType;
            bool inBounds = grid.TryGetPlacement(point, type, out RectInt cells);
            TowerPlacementZone ignored = movingBuilding && selectedPlacement != null
                ? selectedPlacement.GetComponent<TowerPlacementZone>() : null;
            bool free = inBounds && IsBuildingFootprintFree(grid, cells, ignored);
            if (inBounds && (hoveredZone == null || movingBuilding))
                DrawFootprint(grid, cells, free, type);
            UpdateGhost(grid, inBounds ? cells : default, inBounds && (hoveredZone == null || movingBuilding),
                free, movingBuilding ? GetEditingPreview(grid)
                    : GetDefaultPreview(grid, grid.SelectedType), grid.transform.rotation);

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                current.Use();
                if (hoveredZone != null && !movingBuilding)
                {
                    selectedPlacement = hoveredZone;
                    Repaint();
                }
                else if (free)
                {
                    if (!movingBuilding)
                        selectedPlacement = null;
                    chosenBuilding = cells;
                    chosenType = type;
                    choosingZone = true;
                    if (movingBuilding)
                    {
                        editingPlacement = selectedPlacement;
                        movingBuilding = false;
                    }
                }
            }
        }

        if (current.type == EventType.MouseMove || current.type == EventType.MouseDrag)
            SceneView.RepaintAll();
    }

    private void UpdateGhost(LevelGrid grid, RectInt cells, bool inBounds, bool free,
        GameObject model, Quaternion rotation)
    {
        if (!inBounds || model == null)
        {
            if (ghost != null)
                ghost.SetActive(false);
            return;
        }

        if (ghost == null || activeGhostPrefab != model)
            CreateGhost(model);
        if (ghost == null)
            return;

        ghost.transform.SetPositionAndRotation(grid.GetPlacementCenter(cells), rotation);
        ghost.SetActive(true);
        Color color = free ? new Color(1f, 0.12f, 0.8f, 0.86f)
            : new Color(1f, 0.16f, 0.12f, 0.86f);
        TintGhost(ghost, color);
    }

    private static void TintGhost(GameObject model, Color color)
    {
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            renderer.SetPropertyBlock(block);
    }

    private void CreateGhost(GameObject model)
    {
        DestroyGhost();
        ghost = CreatePreviewInstance(model);
        activeGhostPrefab = ghost != null ? model : null;
    }

    private GameObject CreatePreviewInstance(GameObject prefab)
    {
        if (prefab == null || !EnsureGhostMaterial())
            return null;

        GameObject instance = Object.Instantiate(prefab);
        instance.name = $"Placement preview: {prefab.name}";
        foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = HideFlags.HideAndDontSave;
        foreach (Behaviour behaviour in instance.GetComponentsInChildren<Behaviour>(true))
            behaviour.enabled = false;
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            body.detectCollisions = false;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (renderer is ParticleSystemRenderer)
            {
                renderer.enabled = false;
                continue;
            }
            var materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = ghostMaterial;
            renderer.sharedMaterials = materials;
        }
        return instance;
    }

    private bool EnsureGhostMaterial()
    {
        if (ghostMaterial != null)
            return true;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            return false;

        ghostMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        ghostMaterial.SetFloat("_Surface", 1f);
        ghostMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        ghostMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        ghostMaterial.SetFloat("_ZWrite", 0f);
        ghostMaterial.SetOverrideTag("RenderType", "Transparent");
        ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        ghostMaterial.renderQueue = (int)RenderQueue.Transparent;
        return true;
    }

    private void DestroyGhost()
    {
        if (ghost != null)
            Object.DestroyImmediate(ghost);
        ghost = null;
        activeGhostPrefab = null;
    }

    private void DestroyAllGhosts()
    {
        DestroyGhost();
        foreach (PlacedPreview preview in placedPreviews.Values)
            if (preview.instance != null)
                Object.DestroyImmediate(preview.instance);
        placedPreviews.Clear();
        if (ghostMaterial != null)
            Object.DestroyImmediate(ghostMaterial);
        ghostMaterial = null;
    }

    private void DrawOccupiedZones(LevelGrid grid)
    {
        var visiblePlacements = new HashSet<LevelGridPlacedZone>();
        foreach (TowerPlacementZone zone in Object.FindObjectsByType<TowerPlacementZone>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (zone.gameObject.scene != grid.gameObject.scene
                || (zone.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0)
                continue;

            var serialized = new SerializedObject(zone);
            var spawn = serialized.FindProperty("spawnPoint").objectReferenceValue as Transform;
            BuildSlotType type = (BuildSlotType)serialized.FindProperty("allowedType").enumValueIndex;
            if (spawn != null && grid.TryGetPlacement(spawn.position, type, out RectInt building))
                DrawOccupiedArea(grid, building, new Color(1f, 0.22f, 0.24f), "BLOCKED");
            if (grid.TryGetPlacement(zone.transform.position, BuildSlotType.Defense,
                    out RectInt interaction))
                DrawOccupiedArea(grid, interaction, new Color(1f, 0.62f, 0.12f), "ZONE");

            LevelGridPlacedZone placement = zone.GetComponent<LevelGridPlacedZone>();
            if (placement == null || placement.SourceGrid != grid || spawn == null)
                continue;
            visiblePlacements.Add(placement);
            GameObject model = placement.PreviewPrefab != null
                ? placement.PreviewPrefab : FindDefaultPreview(serialized, type);
            UpdatePlacedPreview(placement, model, spawn);
        }

        var stale = new List<LevelGridPlacedZone>();
        foreach (var pair in placedPreviews)
            if (!visiblePlacements.Contains(pair.Key))
            {
                if (pair.Value.instance != null)
                    Object.DestroyImmediate(pair.Value.instance);
                stale.Add(pair.Key);
            }
        foreach (LevelGridPlacedZone placement in stale)
            placedPreviews.Remove(placement);
    }

    private void UpdatePlacedPreview(LevelGridPlacedZone placement, GameObject prefab, Transform spawn)
    {
        if (placedPreviews.TryGetValue(placement, out PlacedPreview existing)
            && (existing.prefab != prefab || existing.instance == null))
        {
            if (existing.instance != null)
                Object.DestroyImmediate(existing.instance);
            placedPreviews.Remove(placement);
        }

        if (!placedPreviews.TryGetValue(placement, out PlacedPreview preview) && prefab != null)
        {
            GameObject instance = CreatePreviewInstance(prefab);
            if (instance == null)
                return;
            preview = new PlacedPreview { prefab = prefab, instance = instance };
            placedPreviews.Add(placement, preview);
            TintGhost(instance, PlacedGhostColor);
        }
        if (preview?.instance == null)
            return;

        preview.instance.transform.SetPositionAndRotation(spawn.position,
            placement.SourceGrid.GetBuildingRotation(placement.BuildingCells, placement.ZoneCell));
        preview.instance.SetActive(true);
    }

    private static GameObject FindDefaultPreview(SerializedObject zone, BuildSlotType type)
    {
        var loadout = zone.FindProperty("loadout").objectReferenceValue as TowerLoadout;
        if (loadout?.options == null)
            return null;
        foreach (TowerOption option in loadout.options)
            if (option != null && option.slotType == type && option.prefab != null)
                return option.prefab;
        return null;
    }

    private static GameObject GetDefaultPreview(LevelGrid grid, BuildSlotType type)
    {
        if (grid.ZonePrefab == null)
            return null;
        TowerPlacementZone zone = grid.ZonePrefab.GetComponent<TowerPlacementZone>();
        return zone == null ? null : FindDefaultPreview(new SerializedObject(zone), type);
    }

    private static void DrawOccupiedArea(LevelGrid grid, RectInt cells, Color color, string label)
    {
        Vector3 lift = grid.transform.up * 0.23f;
        Vector3[] corners =
        {
            grid.GetCorner(cells.xMin, cells.yMin) + lift,
            grid.GetCorner(cells.xMin, cells.yMax) + lift,
            grid.GetCorner(cells.xMax, cells.yMax) + lift,
            grid.GetCorner(cells.xMax, cells.yMin) + lift
        };
        var fill = new Color(color.r, color.g, color.b, 0.34f);
        Handles.DrawSolidRectangleWithOutline(corners, fill, color);
        Color previous = Handles.color;
        Handles.color = color;
        Handles.DrawAAPolyLine(3f, corners[0], corners[2]);
        Handles.DrawAAPolyLine(3f, corners[1], corners[3]);
        Handles.color = previous;
        Handles.Label(grid.GetPlacementCenter(cells) + grid.transform.up * 0.3f,
            label, ReadableLabelStyle);
    }

    private static GUIStyle ReadableLabelStyle
    {
        get
        {
            if (readableLabelStyle != null)
                return readableLabelStyle;

            readableLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                padding = new RectOffset(6, 6, 3, 3)
            };
            readableLabelStyle.normal.background = Texture2D.blackTexture;
            readableLabelStyle.normal.textColor = Color.white;
            return readableLabelStyle;
        }
    }

    private static void DrawGrid(LevelGrid grid)
    {
        Color previous = Handles.color;
        CompareFunction previousDepth = Handles.zTest;
        Handles.zTest = CompareFunction.LessEqual;
        Vector3 lift = grid.transform.up * 0.12f;
        for (int x = 0; x <= grid.Columns; x++)
            DrawContrastingLine(grid.GetCorner(x, 0) + lift,
                grid.GetCorner(x, grid.Rows) + lift, x == 0 || x == grid.Columns);
        for (int y = 0; y <= grid.Rows; y++)
            DrawContrastingLine(grid.GetCorner(0, y) + lift,
                grid.GetCorner(grid.Columns, y) + lift, y == 0 || y == grid.Rows);
        Handles.zTest = previousDepth;
        Handles.color = previous;
    }

    private static void DrawContrastingLine(Vector3 start, Vector3 end, bool boundary)
    {
        Handles.color = GridBacking;
        Handles.DrawAAPolyLine(boundary ? 8f : 6f, start, end);
        Handles.color = GridColor;
        Handles.DrawAAPolyLine(boundary ? 4f : 2.5f, start, end);
    }

    private static bool TryGetMousePoint(LevelGrid grid, Vector2 mousePosition, out Vector3 point)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        var plane = new Plane(grid.transform.up, grid.transform.position);
        if (plane.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = default;
        return false;
    }

    private static void DrawFootprint(LevelGrid grid, RectInt cells, bool free,
        BuildSlotType type)
    {
        Color previous = Handles.color;
        Vector3 lift = grid.transform.up * 0.16f;
        Vector3[] corners =
        {
            grid.GetCorner(cells.xMin, cells.yMin) + lift,
            grid.GetCorner(cells.xMin, cells.yMax) + lift,
            grid.GetCorner(cells.xMax, cells.yMax) + lift,
            grid.GetCorner(cells.xMax, cells.yMin) + lift
        };
        Color outline = free ? new Color(0.2f, 1f, 0.65f) : new Color(1f, 0.3f, 0.25f);
        Color fill = new(outline.r, outline.g, outline.b, 0.32f);
        Handles.DrawSolidRectangleWithOutline(corners, fill, outline);
        Handles.color = GridBacking;
        Handles.DrawAAPolyLine(7f, corners[0], corners[1], corners[2], corners[3], corners[0]);
        Handles.color = outline;
        Handles.DrawAAPolyLine(4f, corners[0], corners[1], corners[2], corners[3], corners[0]);
        Handles.color = previous;
        Handles.Label(grid.GetPlacementCenter(cells) + grid.transform.up * 0.2f,
            free ? type.ToString() : "BLOCKED", ReadableLabelStyle);
    }

    private static void DrawAdjacentZones(LevelGrid grid, RectInt building,
        TowerPlacementZone ignored)
    {
        foreach (RectInt cell in grid.GetAdjacentZoneCells(building))
            DrawZoneCell(grid, cell, IsAreaFree(grid, cell, ignored), false);
    }

    private static void DrawZoneCell(LevelGrid grid, RectInt cell, bool free, bool selected)
    {
        Vector3 lift = grid.transform.up * 0.2f;
        Vector3[] corners =
        {
            grid.GetCorner(cell.xMin, cell.yMin) + lift,
            grid.GetCorner(cell.xMin, cell.yMax) + lift,
            grid.GetCorner(cell.xMax, cell.yMax) + lift,
            grid.GetCorner(cell.xMax, cell.yMin) + lift
        };
        Color outline = free ? new Color(1f, 0.9f, 0.25f) : new Color(1f, 0.22f, 0.2f);
        Color fill = new(outline.r, outline.g, outline.b, selected ? 0.45f : 0.18f);
        Handles.DrawSolidRectangleWithOutline(corners, fill, outline);
        if (selected)
        {
            Color previous = Handles.color;
            Handles.color = outline;
            Handles.DrawAAPolyLine(5f, corners[0], corners[1], corners[2], corners[3], corners[0]);
            Handles.color = previous;
            Handles.Label(grid.GetPlacementCenter(cell) + grid.transform.up * 0.25f,
                free ? "ZONE" : "BLOCKED", ReadableLabelStyle);
        }
    }

    private static bool IsBuildingFootprintFree(LevelGrid grid, RectInt footprint,
        TowerPlacementZone ignored = null)
        => IsAreaFree(grid, footprint, ignored)
            && HasAdjacentFreeZoneCell(grid, footprint, ignored);

    private static bool HasAdjacentFreeZoneCell(LevelGrid grid, RectInt footprint,
        TowerPlacementZone ignored)
    {
        foreach (RectInt cell in grid.GetAdjacentZoneCells(footprint))
            if (IsAreaFree(grid, cell, ignored))
                return true;
        return false;
    }

    private static bool IsZoneCellFree(LevelGrid grid, RectInt cell,
        TowerPlacementZone ignored)
        => grid.Contains(cell) && IsAreaFree(grid, cell, ignored);

    private static bool IsAreaFree(LevelGrid grid, RectInt candidate,
        TowerPlacementZone ignored = null)
    {
        foreach (TowerPlacementZone zone in Object.FindObjectsByType<TowerPlacementZone>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (zone == ignored || zone.gameObject.scene != grid.gameObject.scene)
                continue;

            var serialized = new SerializedObject(zone);
            var spawn = serialized.FindProperty("spawnPoint").objectReferenceValue as Transform;
            BuildSlotType type = (BuildSlotType)serialized.FindProperty("allowedType").enumValueIndex;
            if (spawn != null && grid.TryGetPlacement(spawn.position, type, out RectInt occupied)
                && LevelGrid.Overlaps(candidate, occupied))
                return false;
            if (grid.TryGetPlacement(zone.transform.position, BuildSlotType.Defense,
                    out RectInt marker) && LevelGrid.Overlaps(candidate, marker))
                return false;
        }
        return true;
    }

    private static bool TryGetShortcutType(KeyCode key, out BuildSlotType type)
    {
        type = key switch
        {
            KeyCode.Alpha1 or KeyCode.Keypad1 => BuildSlotType.TownHall,
            KeyCode.Alpha2 or KeyCode.Keypad2 => BuildSlotType.Defense,
            KeyCode.Alpha3 or KeyCode.Keypad3 => BuildSlotType.Mine,
            KeyCode.Alpha4 or KeyCode.Keypad4 => BuildSlotType.Barracks,
            _ => (BuildSlotType)(-1)
        };
        return (int)type >= 0;
    }

    private void DrawSelectedPlacement(LevelGrid grid)
    {
        if (selectedPlacement == null || selectedPlacement.SourceGrid != grid)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selected building zone", EditorStyles.boldLabel);
        EditorGUILayout.ObjectField("Zone", selectedPlacement, typeof(LevelGridPlacedZone), true);
        EditorGUILayout.LabelField("Building cells", selectedPlacement.BuildingCells.ToString());
        EditorGUILayout.LabelField("Interaction cell", selectedPlacement.ZoneCell.ToString());
        using (new EditorGUI.DisabledScope(!painting || Application.isPlaying))
        {
            if (GUILayout.Button("Move building"))
            {
                choosingZone = false;
                movingBuilding = true;
                editingPlacement = selectedPlacement;
                DestroyGhost();
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Move interaction zone"))
            {
                movingBuilding = false;
                editingPlacement = selectedPlacement;
                chosenBuilding = selectedPlacement.BuildingCells;
                chosenType = GetPlacementType(selectedPlacement);
                choosingZone = true;
                DestroyGhost();
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Delete selected zone"))
            {
                Undo.DestroyObjectImmediate(selectedPlacement.gameObject);
                selectedPlacement = null;
                editingPlacement = null;
                choosingZone = false;
                movingBuilding = false;
                DestroyGhost();
                SceneView.RepaintAll();
            }
        }
    }

    private static BuildSlotType GetPlacementType(LevelGridPlacedZone placement)
    {
        if (placement == null)
            return BuildSlotType.Defense;
        var zone = placement.GetComponent<TowerPlacementZone>();
        return zone == null ? BuildSlotType.Defense
            : (BuildSlotType)new SerializedObject(zone).FindProperty("allowedType").enumValueIndex;
    }

    private GameObject GetEditingPreview(LevelGrid grid)
    {
        if (editingPlacement == null)
            return GetDefaultPreview(grid, chosenType);
        if (editingPlacement.PreviewPrefab != null)
            return editingPlacement.PreviewPrefab;
        var zone = editingPlacement.GetComponent<TowerPlacementZone>();
        return zone == null ? null : FindDefaultPreview(new SerializedObject(zone),
            GetPlacementType(editingPlacement));
    }

    private static void DrawSelectedPair(LevelGrid grid, LevelGridPlacedZone placement)
    {
        Color previous = Handles.color;
        CompareFunction previousDepth = Handles.zTest;
        Handles.zTest = CompareFunction.Always;
        Vector3 building = grid.GetPlacementCenter(placement.BuildingCells) + grid.transform.up * 0.55f;
        Vector3 interaction = grid.GetPlacementCenter(placement.ZoneCell) + grid.transform.up * 0.55f;
        Handles.color = new Color(0.05f, 0.08f, 0.1f);
        Handles.DrawAAPolyLine(9f, building, interaction);
        Handles.color = new Color(0.15f, 1f, 0.95f);
        Handles.DrawAAPolyLine(5f, building, interaction);
        Handles.SphereHandleCap(0, building, Quaternion.identity, 0.55f, EventType.Repaint);
        Handles.SphereHandleCap(0, interaction, Quaternion.identity, 0.55f, EventType.Repaint);
        Handles.zTest = previousDepth;
        Handles.color = previous;
        DrawOccupiedArea(grid, placement.BuildingCells, new Color(0.1f, 0.9f, 0.95f), "BUILDING");
        DrawOccupiedArea(grid, placement.ZoneCell, new Color(1f, 0.9f, 0.22f), "ZONE");
    }

    private static LevelGridPlacedZone FindPlacedZoneAt(LevelGrid grid, Vector3 point)
    {
        if (!grid.TryGetPlacement(point, BuildSlotType.Defense, out RectInt cell))
            return null;
        foreach (LevelGridPlacedZone placement in Object.FindObjectsByType<LevelGridPlacedZone>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (placement.SourceGrid == grid && placement.gameObject.scene == grid.gameObject.scene
                && placement.ContainsCell(cell))
                return placement;
        return null;
    }

    private void MovePlacement(LevelGrid grid, LevelGridPlacedZone placement,
        RectInt building, RectInt zoneCell)
    {
        TowerPlacementZone zone = placement.GetComponent<TowerPlacementZone>();
        Transform spawn = new SerializedObject(zone).FindProperty("spawnPoint").objectReferenceValue
            as Transform;
        if (spawn == null)
        {
            Debug.LogError("[LevelGrid] The selected zone has no spawnPoint.", zone);
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Move building zone on grid");
        Undo.RecordObjects(new Object[] { placement, zone.gameObject, zone.transform, spawn },
            "Move building zone on grid");
        zone.transform.SetPositionAndRotation(grid.GetPlacementCenter(zoneCell)
            + grid.transform.up * 0.12f, grid.transform.rotation);
        spawn.SetPositionAndRotation(grid.GetPlacementCenter(building),
            grid.GetBuildingRotation(building, zoneCell));
        placement.Configure(grid, building, zoneCell, placement.PreviewPrefab);
        zone.gameObject.name = $"{GetPlacementType(placement)} Zone [{building.x},{building.y}]";
        PrefabUtility.RecordPrefabInstancePropertyModifications(placement);
        PrefabUtility.RecordPrefabInstancePropertyModifications(zone.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(spawn);
        Undo.CollapseUndoOperations(undoGroup);
        selectedPlacement = placement;
        SceneView.RepaintAll();
    }

    private void PlaceZone(LevelGrid grid, RectInt building, RectInt zoneCell)
    {
        var source = new SerializedObject(grid.ZonePrefab.GetComponent<TowerPlacementZone>());
        if (source.FindProperty("spawnPoint").objectReferenceValue == null)
        {
            Debug.LogError("[LevelGrid] TowerBuildZone needs an assigned spawnPoint.", grid);
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place building zone on grid");

        var instance = PrefabUtility.InstantiatePrefab(grid.ZonePrefab, grid.gameObject.scene) as GameObject;
        if (instance == null)
            return;

        Undo.RegisterCreatedObjectUndo(instance, "Place building zone on grid");
        Transform parent = grid.ZonesParent != null ? grid.ZonesParent : grid.transform;
        Undo.SetTransformParent(instance.transform, parent, "Parent building zone");
        instance.name = $"{grid.SelectedType} Zone [{building.x},{building.y}]";

        TowerPlacementZone zone = instance.GetComponent<TowerPlacementZone>();
        var placement = Undo.AddComponent<LevelGridPlacedZone>(instance);
        Undo.RecordObject(placement, "Record grid placement");
        placement.Configure(grid, building, zoneCell,
            GetDefaultPreview(grid, grid.SelectedType));
        PrefabUtility.RecordPrefabInstancePropertyModifications(placement);
        var serialized = new SerializedObject(zone);
        serialized.FindProperty("allowedType").enumValueIndex = (int)grid.SelectedType;
        serialized.FindProperty("requiredTownHallLevel").intValue = grid.SelectedType switch
        {
            BuildSlotType.TownHall => 0,
            BuildSlotType.Defense => 1,
            _ => 2
        };
        Transform spawn = serialized.FindProperty("spawnPoint").objectReferenceValue as Transform;
        serialized.ApplyModifiedProperties();

        Vector3 marker = grid.GetPlacementCenter(zoneCell);
        Undo.RecordObject(instance.transform, "Position building zone");
        instance.transform.SetPositionAndRotation(marker + grid.transform.up * 0.12f,
            grid.transform.rotation);
        Undo.RecordObject(spawn, "Align building spawn point");
        spawn.SetPositionAndRotation(grid.GetPlacementCenter(building),
            grid.GetBuildingRotation(building, zoneCell));
        PrefabUtility.RecordPrefabInstancePropertyModifications(spawn);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        Undo.CollapseUndoOperations(undoGroup);
        selectedPlacement = placement;
        SceneView.RepaintAll();
    }

    public static int AlignExistingPlacements(LevelGrid grid)
    {
        int changed = 0;
        foreach (LevelGridPlacedZone placement in Object.FindObjectsByType<LevelGridPlacedZone>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (placement.SourceGrid != grid || placement.gameObject.scene != grid.gameObject.scene)
                continue;
            var zone = placement.GetComponent<TowerPlacementZone>();
            if (zone == null)
                continue;
            var spawn = new SerializedObject(zone).FindProperty("spawnPoint").objectReferenceValue
                as Transform;
            if (spawn == null)
                continue;
            Quaternion rotation = grid.GetBuildingRotation(placement.BuildingCells, placement.ZoneCell);
            if (Quaternion.Angle(spawn.rotation, rotation) < 0.01f)
                continue;
            Undo.RecordObject(spawn, "Face building toward interaction zone");
            spawn.rotation = rotation;
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawn);
            changed++;
        }
        SceneView.RepaintAll();
        return changed;
    }
}

[CustomEditor(typeof(LevelGridTileSurface))]
public sealed class LevelGridTileSurfaceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
            ((LevelGridTileSurface)target).Rebuild();

        var surface = (LevelGridTileSurface)target;
        if (surface.ShowTiles && surface.TileMaterial == null)
            EditorGUILayout.HelpBox("Assign a material with your tile texture to show the floor.",
                MessageType.Warning);
        EditorGUILayout.HelpBox(
            "Each cell repeats the material texture once. Solid Floor adds one collider; " +
            "rebake the NavMesh if this surface becomes walkable geometry.", MessageType.Info);
    }
}
