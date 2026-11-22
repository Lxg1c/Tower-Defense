using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Draws the low Android profile's soft ground shadows in one pass.</summary>
internal sealed class MobileContactShadows : MonoBehaviour
{
    private const string MaterialResource = "MobileContactShadow";
    private const float FloorClearance = .025f;
    private static MobileContactShadows active;

    private readonly Dictionary<GameObject, Collider> moving = new();
    private readonly List<Bounds> scenery = new();
    private readonly List<Vector3> vertices = new();
    private readonly List<Vector2> uv = new();
    private readonly List<int> triangles = new();
    private Mesh mesh;
    private float floor;

    internal static void StartForLevel(LevelSetup level)
    {
        if (!GamePerformanceSettings.SupportsAndroidQuality ||
            GamePerformanceSettings.MobileQuality != GamePerformanceSettings.AndroidQuality.Low)
            return;

        Material material = Resources.Load<Material>(MaterialResource);
        if (material == null)
        {
            Debug.LogError("[MobileContactShadows] Assign the shared contact shadow material.");
            return;
        }

        var batch = new GameObject("Mobile Contact Shadows");
        batch.transform.SetParent(level.transform, false);
        active = batch.AddComponent<MobileContactShadows>();
        active.Initialize(material);
    }

    internal static void Track(GameObject target)
    {
        if (active == null || target.scene != active.gameObject.scene || active.moving.ContainsKey(target))
            return;
        if (!target.TryGetComponent(out Collider footprint))
            footprint = target.GetComponentInChildren<Collider>(true);
        if (footprint == null) return;
        active.moving.Add(target, footprint);
        // These older prefabs already contain individual shadow planes.
        foreach (var renderer in target.GetComponentsInChildren<MeshRenderer>(true))
            if (renderer.name.IndexOf("shadow", System.StringComparison.OrdinalIgnoreCase) >= 0)
                renderer.forceRenderingOff = true;
    }

    internal static void Untrack(GameObject target)
    {
        if (active != null) active.moving.Remove(target);
    }

    private void Initialize(Material material)
    {
        mesh = new Mesh { name = "Mobile contact shadow quads" };
        mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var target in root.GetComponentsInChildren<Damageable>())
                if (target.isActiveAndEnabled) Track(target.gameObject);

            foreach (var surface in root.GetComponentsInChildren<LevelGridTileSurface>())
                if (surface.ShowTiles && surface.TryGetComponent(out MeshRenderer tiles))
                    floor = tiles.bounds.max.y;

            foreach (var part in root.GetComponentsInChildren<Transform>())
            {
                if (part.name != "Level 1 Environment" && part.name != "High Tech Services") continue;
                foreach (var solid in part.GetComponentsInChildren<Collider>())
                {
                    Bounds footprint = solid.bounds;
                    if (!solid.isTrigger && footprint.size.y >= .55f && footprint.size.y <= 15f)
                        scenery.Add(footprint);
                }
            }
        }
    }

    private void LateUpdate()
    {
        vertices.Clear();
        uv.Clear();
        triangles.Clear();
        foreach (Bounds footprint in scenery)
            AddQuad(footprint.center, footprint.extents.x, footprint.extents.z);

        foreach (var entry in moving)
        {
            if (entry.Key == null || !entry.Key.activeInHierarchy || entry.Value == null) continue;
            Collider footprint = entry.Value;
            Bounds bounds = footprint.bounds;
            // Benchmark actors have their colliders disabled by design.
            if (bounds.size == Vector3.zero)
            {
                Vector3 scale = footprint.transform.lossyScale;
                float radius = footprint switch
                {
                    SphereCollider sphere => sphere.radius,
                    CapsuleCollider capsule => capsule.radius,
                    _ => .75f
                };
                AddQuad(footprint.transform.position, radius * Mathf.Abs(scale.x), radius * Mathf.Abs(scale.z));
            }
            else AddQuad(bounds.center, bounds.extents.x, bounds.extents.z);
        }

        mesh.Clear(keepVertexLayout: true);
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
    }

    private void AddQuad(Vector3 position, float halfWidth, float halfDepth)
    {
        // A slight lateral offset keeps the soft edge visible around the silhouette.
        position = transform.InverseTransformPoint(new Vector3(position.x + .24f, floor + FloorClearance, position.z - .18f));
        halfWidth = Mathf.Max(.45f, halfWidth) * 1.35f + .18f;
        halfDepth = Mathf.Max(.45f, halfDepth) * 1.35f + .18f;
        int index = vertices.Count;
        vertices.Add(position + new Vector3(-halfWidth, 0f, -halfDepth));
        vertices.Add(position + new Vector3(-halfWidth, 0f, halfDepth));
        vertices.Add(position + new Vector3(halfWidth, 0f, halfDepth));
        vertices.Add(position + new Vector3(halfWidth, 0f, -halfDepth));
        uv.Add(Vector2.zero);
        uv.Add(Vector2.up);
        uv.Add(Vector2.one);
        uv.Add(Vector2.right);
        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
        if (mesh != null) Destroy(mesh);
    }
}
