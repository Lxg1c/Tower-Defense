using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Bakes the authored Level 1 service equipment and floor inlays into shared meshes.</summary>
public static class HighTechEnvironmentSetup
{
    private const string Source = "Assets/Tower-Defense/Art/Environment/HighTech/KenneySpaceKit/";
    private const string Output = "Assets/Tower-Defense/Art/Environment/HighTech/Generated/";
    private const string PrefabPath = "Assets/Tower-Defense/Prefabs/LevelDesign/HighTechServices.prefab";
    private static readonly string[] Palette = { "Hull", "Frame", "Recess", "Cyan", "Amber", "Marking" };

    [MenuItem("Tools/Tower Defense/Rebuild Level 1 High Tech Services")]
    public static void Rebuild()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring scenery.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Tower-Defense/Scenes/Level 1.unity")
            throw new InvalidOperationException("Open Level 1 before rebuilding its service scenery.");
        var parts = Palette.ToDictionary(k => k, _ => new List<CombineInstance>());
        var transientMeshes = new List<Mesh>();
        var solidBounds = new List<Bounds>();
        var colors = new[] { new Color(.43f,.51f,.57f), new Color(.16f,.23f,.29f),
            new Color(.065f,.105f,.14f), new Color(.20f,.69f,.72f), new Color(.82f,.48f,.12f), new Color(.38f,.46f,.49f) };
        var shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) throw new InvalidOperationException("URP Simple Lit is required.");
        foreach (string name in new[]{"machine_generator","machine_generatorLarge","machine_barrelLarge","machine_wireless","satelliteDish_detailed","barrels","barrels_rail","pipe_straight","hangar_smallA","craft_cargoA","hangar_largeA","structure_detailed","satelliteDish_large"})
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Source + name + ".fbx") == null)
                throw new InvalidOperationException("Missing Kenney source: " + name);

        void Model(string name, Vector3 position, float width, float yaw = 0, bool solid = false)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + name + ".fbx");
            var renderers = source.GetComponentsInChildren<MeshRenderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = width / bounds.size.x;
            Vector3 bottom = new(bounds.center.x, bounds.min.y, bounds.center.z);
            Matrix4x4 placement = Matrix4x4.TRS(position, Quaternion.Euler(0,yaw,0), Vector3.one * scale)
                * Matrix4x4.Translate(-bottom);
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    string original = renderer.sharedMaterials[i].name;
                    string palette = original.StartsWith("metalRed") ? "Cyan"
                        : original.StartsWith("metalDark") ? "Frame"
                        : original.StartsWith("dark") ? "Recess" : "Hull";
                    parts[palette].Add(new CombineInstance { mesh = mesh, subMeshIndex = i,
                        transform = placement * renderer.transform.localToWorldMatrix });
                }
            }
            if (solid)
            {
                Vector3 size = bounds.size * scale;
                if (Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) > .5f) size = new Vector3(size.z,size.y,size.x);
                solidBounds.Add(new Bounds(position + Vector3.up * size.y * .5f,size));
            }
        }
        void Strip(string palette, float x, float z, float width, float length, float yaw = 0)
        {
            // Overlaid stripes and grille slats must not share the backing plate's depth.
            float height = palette switch
            {
                "Frame" => .155f,
                "Marking" => .175f,
                "Amber" => .185f,
                "Cyan" => .195f,
                _ => throw new ArgumentOutOfRangeException(nameof(palette), palette, "Unknown inlay layer.")
            };
            var mesh = new Mesh { name = "Service inlay" };
            mesh.vertices = new[] {new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};
            mesh.triangles = new[]{0,1,2,0,2,3}; mesh.RecalculateNormals();
            transientMeshes.Add(mesh);
            parts[palette].Add(new CombineInstance {mesh=mesh,transform=Matrix4x4.TRS(new Vector3(x,height,z),Quaternion.Euler(0,yaw,0),new Vector3(width,1,length))});
        }
        // Keep equipment clear of every authored building footprint and interaction cell.
        Model("machine_generatorLarge", new Vector3(7.2f,3.46f,-24.2f),2.1f,90);
        Model("satelliteDish_detailed",new Vector3(-8.5f,3.46f,-24.4f),2.2f,135);
        Model("machine_wireless",new Vector3(19f,7.7f,-22.3f),1.8f,180);
        Model("machine_generator",new Vector3(-23.9f,5.1f,-16),1.8f);
        Model("machine_wireless",new Vector3(-31.2f,8.1f,-10.9f),1.8f,90);
        Model("hangar_smallA", new Vector3(-48f, .08f, 8.5f), 5.5f, 180, true);
        Model("craft_cargoA", new Vector3(-48f, .08f, 2.5f), 2.7f, 180, true);
        Model("barrels_rail", new Vector3(-52.5f, .08f, 6f), 2.2f, 90, true);
        Model("machine_generatorLarge",new Vector3(19f,.08f,4.3f),2.8f,90,true);
        Model("machine_barrelLarge",new Vector3(23f,.08f,4.5f),2f,0,true);
        Model("barrels",new Vector3(18.5f,.08f,8.5f),2.1f,0,true);
        Model("machine_generatorLarge", new Vector3(-43.5f, .08f, -3.5f), 2.4f, solid: true);
        Model("machine_barrelLarge",new Vector3(-40.8f,.08f,-1.8f),2.1f,0,true);
        // Keep the central arena and the paths just inside the fences open.
        // Standalone service equipment belongs in the outer yards.
        Model("machine_generatorLarge", new Vector3(32f, .08f, -3f), 2.5f, 90, true);
        Model("machine_barrelLarge", new Vector3(36f, .08f, -4f), 2.1f, solid: true);
        Model("machine_generatorLarge", new Vector3(-46.5f, .08f, -6f), 2.6f, -90, true);
        Model("barrels_rail", new Vector3(-46.5f, .08f, -10f), 2.2f, 90, true);
        // Logistics and power sit outside the perimeter. Keep the western portal approaches open.
        Model("hangar_largeA", new Vector3(18f, .08f, 19f), 9f, 180, true);
        Model("craft_cargoA", new Vector3(11f, .08f, 18f), 3.8f, 180, true);
        Model("barrels_rail", new Vector3(24.5f, .08f, 16.5f), 3f, 90, true);
        Model("structure_detailed", new Vector3(26f, .08f, 25f), 4.8f, 90, true);
        Model("machine_generatorLarge", new Vector3(32f, .08f, -10f), 4f, solid: true);
        Model("machine_generatorLarge", new Vector3(32f, .08f, -17f), 4f, solid: true);
        Model("machine_barrelLarge", new Vector3(36f, .08f, -12f), 3.2f, solid: true);
        Model("machine_barrelLarge", new Vector3(36f, .08f, -17f), 3.2f, solid: true);
        for (int segment = 0; segment < 5; segment++)
            Model("pipe_straight", new Vector3(39f, .08f, -19f + segment * 2.9f), 2f, solid: true);
        Model("satelliteDish_large", new Vector3(-17f, .08f, -33f), 6.5f, 180, true);
        Model("structure_detailed", new Vector3(-23f, .08f, -33f), 4.5f, solid: true);
        Model("machine_wireless", new Vector3(-17f, .08f, -27.5f), 2.4f, solid: true);
        Model("hangar_smallA", new Vector3(-45f, .08f, -21f), 6f, 90, true);
        Model("barrels", new Vector3(-42f, .08f, -13.5f), 2.7f, 90, true);
        foreach(var zone in UnityEngine.Object.FindObjectsByType<LevelGridPlacedZone>(FindObjectsSortMode.None))
        foreach(var cells in new[]{zone.BuildingCells,zone.ZoneCell})
        {
            var a=zone.SourceGrid.GetCorner(cells.xMin,cells.yMin);
            var b=zone.SourceGrid.GetCorner(cells.xMax,cells.yMax);
            var reserved=Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.z,b.z),Mathf.Max(a.x,b.x),Mathf.Max(a.z,b.z));
            foreach(var bounds in solidBounds)
                if(reserved.Overlaps(Rect.MinMaxRect(bounds.min.x,bounds.min.z,bounds.max.x,bounds.max.z)))
                    throw new InvalidOperationException("Service equipment overlaps reserved cells: "+zone.name);
        }
        // Recessed service channels and interrupted lane markings leave the original tile texture visible.
        foreach(float z in new[]{-7.3f,-8.5f}) Strip("Frame",-5,z,38,.18f);
        for(int i=0;i<12;i++) Strip("Cyan",-22+i*3.1f,-7.3f,1.2f,.055f);
        foreach(float x in new[]{-20f,-11f,-2f,7f,16f})
        {
            Strip("Frame",x,-7.9f,2.2f,.85f);
            for(int j=0;j<6;j++) Strip("Marking",x-.85f+j*.34f,-7.9f,.09f,.62f);
        }
        foreach(float x in new[]{-15.5f,-6.5f,2.5f,11.5f})
        {
            Strip("Frame",x,-14.6f,2.7f,.7f);
            Strip("Marking",x,-14.6f,2.45f,.045f);
            Strip("Cyan",x-1.2f,-14.6f,.045f,.55f);
            Strip("Cyan",x+1.2f,-14.6f,.045f,.55f);
        }
        for(int i=0;i<14;i++) Strip("Amber",-24+i*3f,-.25f,.95f,.10f);
        for(int i=0;i<8;i++) Strip("Marking",-.35f,-4.8f-i*2.1f,.10f,.8f);
        foreach(float x in new[]{-25f,19f})
        {
            Strip("Frame",x,-12,1.25f,13);
            for(int i=0;i<9;i++) Strip("Amber",x,-18+i*1.5f,.9f,.10f,40);
        }
        // Painted service boundaries link the outer yards visually without raising the floor.
        AddYardMarkings(18f, 18f, 17f, 13f);
        AddYardMarkings(34f, -13f, 12f, 20f);
        AddYardMarkings(-20f, -33f, 14f, 12f);
        AddYardMarkings(-49f, 6f, 11f, 13f);

        void AddYardMarkings(float x, float z, float width, float length)
        {
            float left = x - width * .5f;
            float right = x + width * .5f;
            Strip("Frame", left, z, .18f, length);
            Strip("Frame", right, z, .18f, length);
            Strip("Frame", x, z - length * .5f, width, .18f);
            for (float offset = 1f; offset < width; offset += 1.25f)
                Strip("Amber", left + offset, z + length * .5f, .75f, .15f);
        }

        var root = new GameObject("High Tech Services");
        try
        {
            for(int i=0;i<Palette.Length;i++)
            {
                string key = Palette[i];
                var material = AssetDatabase.LoadAssetAtPath<Material>(Output+key+".mat");
                if(material == null) {material=new Material(shader);AssetDatabase.CreateAsset(material,Output+key+".mat");}
                material.SetColor("_BaseColor",colors[i]);material.SetFloat("_Smoothness",.18f);
                material.SetFloat("_SpecularHighlights",0); material.enableInstancing=true;
                EditorUtility.SetDirty(material);
                var combined = new Mesh { name = "HighTech_" + key, indexFormat = IndexFormat.UInt32 };
                combined.CombineMeshes(parts[key].ToArray(),true,true);
                string meshPath=Output+key+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(existing!=null)
                {
                    existing.Clear();existing.indexFormat=IndexFormat.UInt32;
                    existing.vertices=combined.vertices;existing.normals=combined.normals;
                    existing.uv=combined.uv;existing.triangles=combined.triangles;
                    existing.RecalculateBounds();existing.UploadMeshData(false);
                    EditorUtility.SetDirty(existing);
                    UnityEngine.Object.DestroyImmediate(combined);combined=existing;
                }
                else AssetDatabase.CreateAsset(combined,meshPath);
                var child = new GameObject(key);child.transform.SetParent(root.transform,false);
                child.AddComponent<MeshFilter>().sharedMesh=combined;
                var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
                renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            }
            foreach(var bounds in solidBounds)
            {
                var solid=new GameObject("Service equipment collision");solid.transform.SetParent(root.transform,false);
                solid.layer=LayerMask.NameToLayer("Obstacle");
                var collider=solid.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;
                var obstacle=solid.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;
                obstacle.center=bounds.center;obstacle.size=bounds.size;
                obstacle.carving=true;obstacle.carveOnlyStationary=true;
            }
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach(var mesh in transientMeshes) UnityEngine.Object.DestroyImmediate(mesh);
        }
        if (!scene.GetRootGameObjects().Any(g =>
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == PrefabPath))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "High Tech Services";
            Undo.RegisterCreatedObjectUndo(instance, "Add high tech services");
            EditorSceneManager.MarkSceneDirty(scene);
        }
        AssetDatabase.SaveAssets();
    }
}
