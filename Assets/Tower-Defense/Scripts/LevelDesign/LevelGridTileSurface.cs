using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Optional textured floor for an authored LevelGrid. One quad and UV island per cell.</summary>
[ExecuteAlways]
[RequireComponent(typeof(LevelGrid), typeof(MeshFilter), typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public sealed class LevelGridTileSurface : MonoBehaviour
{
    [SerializeField] private bool showTiles;
    [SerializeField] private Material tileMaterial;
    [Min(0f)] [SerializeField] private float heightAboveGrid = 0.04f;
    [SerializeField] private bool solidFloor;

    private Mesh generatedMesh;

    public bool ShowTiles => showTiles;
    public Material TileMaterial => tileMaterial;

    private void OnEnable() => Rebuild();

    private void OnDisable() => Clear();

    public void Rebuild()
    {
        Clear();

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (!showTiles || tileMaterial == null)
            return;

        LevelGrid grid = GetComponent<LevelGrid>();
        int columns = grid.Columns;
        int rows = grid.Rows;
        float size = grid.CellSize;
        int count = columns * rows;
        var vertices = new Vector3[count * 4];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[count * 6];

        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            int cell = y * columns + x;
            int v = cell * 4;
            int t = cell * 6;
            float left = (x - columns * 0.5f) * size;
            float bottom = (y - rows * 0.5f) * size;
            float right = left + size;
            float top = bottom + size;

            vertices[v] = new Vector3(left, heightAboveGrid, bottom);
            vertices[v + 1] = new Vector3(left, heightAboveGrid, top);
            vertices[v + 2] = new Vector3(right, heightAboveGrid, top);
            vertices[v + 3] = new Vector3(right, heightAboveGrid, bottom);
            uv[v] = new Vector2(0f, 0f);
            uv[v + 1] = new Vector2(0f, 1f);
            uv[v + 2] = new Vector2(1f, 1f);
            uv[v + 3] = new Vector2(1f, 0f);
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v;
            triangles[t + 4] = v + 2;
            triangles[t + 5] = v + 3;
        }

        generatedMesh = new Mesh { name = "Level grid tiles", hideFlags = HideFlags.DontSave };
        if (vertices.Length > 65535)
            generatedMesh.indexFormat = IndexFormat.UInt32;
        generatedMesh.vertices = vertices;
        generatedMesh.uv = uv;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateTangents();
        generatedMesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        renderer.sharedMaterial = tileMaterial;
        renderer.enabled = true;

        MeshCollider floorCollider = GetComponent<MeshCollider>();
        if (floorCollider != null)
        {
            floorCollider.sharedMesh = solidFloor ? generatedMesh : null;
            floorCollider.enabled = solidFloor;
        }
    }

    private void Clear()
    {
        MeshCollider floorCollider = GetComponent<MeshCollider>();
        if (floorCollider != null)
        {
            floorCollider.sharedMesh = null;
            floorCollider.enabled = false;
        }

        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh == generatedMesh)
            filter.sharedMesh = null;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;

        if (generatedMesh == null)
            return;
        if (Application.isPlaying)
            Destroy(generatedMesh);
        else
            DestroyImmediate(generatedMesh);
        generatedMesh = null;
    }
}
