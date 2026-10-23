using UnityEditor;
using UnityEngine;

/// <summary>Connects the imported tile texture set to the optional level-grid floor.</summary>
public static class LevelGridTileMaterialSetup
{
    private const string TextureFolder = "Assets/Tower-Defense/Prefabs/";
    private const string MaterialPath = "Assets/Tower-Defense/Materials/LevelGridTile.mat";
    private const string GridPrefabPath = "Assets/Tower-Defense/Prefabs/level/LevelGrid.prefab";

    [InitializeOnLoadMethod]
    private static void ConfigureNewMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null || material.GetTexture("_MetallicGlossMap") != null)
            EditorApplication.delayCall += Configure;
    }

    [MenuItem("Tools/Tower Defense/Configure Level Grid Tile Material")]
    public static void Configure()
    {
        Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "tile_BaseMap.png");
        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "tile_Normal.png");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (baseMap == null || normalMap == null || shader == null)
        {
            Debug.LogError("[LevelGrid] Tile base/normal textures and URP/Lit shader are required.");
            return;
        }

        ConfigureNormalImporter(TextureFolder + "tile_Normal.png");
        normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "tile_Normal.png");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "LevelGridTile" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
            material.shader = shader;

        material.SetTexture("_BaseMap", baseMap);
        material.SetTexture("_BumpMap", normalMap);
        material.SetTexture("_MetallicGlossMap", null);
        material.SetTexture("_OcclusionMap", null);
        material.SetFloat("_Metallic", 0f);
        // tile_MaskMap is uniform: R=0, G=255, A=48. Scalars avoid two extra texture samples.
        material.SetFloat("_Smoothness", 48f / 255f);
        material.SetFloat("_OcclusionStrength", 1f);
        material.EnableKeyword("_NORMALMAP");
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        material.DisableKeyword("_OCCLUSIONMAP");
        EditorUtility.SetDirty(material);

        GameObject prefab = PrefabUtility.LoadPrefabContents(GridPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[LevelGrid] LevelGrid prefab was not found.");
            return;
        }
        try
        {
            LevelGridTileSurface surface = prefab.GetComponent<LevelGridTileSurface>();
            if (surface == null)
            {
                Debug.LogError("[LevelGrid] LevelGrid prefab has no tile surface.");
                return;
            }
            var properties = new SerializedObject(surface);
            properties.FindProperty("tileMaterial").objectReferenceValue = material;
            properties.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefab, GridPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[LevelGrid] Tile material configured. Enable Show Tiles on a grid to display it.");
    }

    private static void ConfigureNormalImporter(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            changed = true;
        }
        if (changed)
            importer.SaveAndReimport();
    }
}
