using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GraphicsMaterialProfileBuilder
{
    private const string Lit = "Universal Render Pipeline/Lit";
    private const string SimpleLit = "Universal Render Pipeline/Simple Lit";
    private const string AssetPath = "Assets/Tower-Defense/Resources/GraphicsMaterialProfiles.asset";

    [MenuItem("Tools/Tower Defense/Refresh Graphics Materials")]
    public static void Refresh()
    {
        var shader = Shader.Find(SimpleLit);
        if (shader == null) throw new InvalidOperationException($"Shader not found: {SimpleLit}");

        var profiles = AssetDatabase.LoadAssetAtPath<GraphicsMaterialProfiles>(AssetPath);
        if (profiles == null)
        {
            profiles = ScriptableObject.CreateInstance<GraphicsMaterialProfiles>();
            AssetDatabase.CreateAsset(profiles, AssetPath);
        }

        var previous = AssetDatabase.LoadAllAssetsAtPath(AssetPath).OfType<Material>()
            .ToDictionary(material => material.name);
        var variants = new List<GraphicsMaterialProfiles.Variant>();
        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled &&
            !scene.path.EndsWith("/MainMenu.unity", StringComparison.Ordinal)).Select(scene => scene.path).ToArray();
        foreach (string path in AssetDatabase.GetDependencies(scenes).OrderBy(path => path, StringComparer.Ordinal))
        {
            if (!path.StartsWith("Assets/Tower-Defense/", StringComparison.Ordinal) ||
                !path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) continue;
            var source = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (source == null || source.shader == null) continue;
            bool isLit = source.shader.name == Lit;
            if ((!isLit && source.shader.name != SimpleLit) || source.GetFloat("_Surface") != 0f ||
                source.IsKeywordEnabled("_ALPHATEST_ON")) continue;
            bool hasNormals = source.IsKeywordEnabled("_NORMALMAP") && source.GetTexture("_BumpMap") != null;
            if (!isLit && !hasNormals) continue;

            string key = AssetDatabase.AssetPathToGUID(path);
            var variant = new GraphicsMaterialProfiles.Variant { source = source };
            if (hasNormals)
                variant.standardWithoutNormals = Store(WithoutNormals(source), $"{key}-standard-no-normal");
            if (isLit)
            {
                variant.simple = Store(CreateSimple(source, shader, hasNormals), $"{key}-simple");
                if (hasNormals)
                    variant.simpleWithoutNormals = Store(WithoutNormals(variant.simple), $"{key}-simple-no-normal");
            }
            variants.Add(variant);
        }

        profiles.variants = variants.ToArray();
        EditorUtility.SetDirty(profiles);
        foreach (var unused in previous.Values) UnityEngine.Object.DestroyImmediate(unused, true);
        AssetDatabase.SaveAssetIfDirty(profiles);
        Debug.Log($"Prepared graphics variants for {variants.Count} materials.");

        Material Store(Material material, string name)
        {
            material.name = name;
            if (!previous.Remove(name, out var existing))
            {
                AssetDatabase.AddObjectToAsset(material, profiles);
                return material;
            }
            EditorUtility.CopySerialized(material, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(material);
            return existing;
        }
    }

    private static Material WithoutNormals(Material source)
    {
        var material = new Material(source);
        material.SetTexture("_BumpMap", null);
        material.DisableKeyword("_NORMALMAP");
        return material;
    }

    private static Material CreateSimple(Material source, Shader shader, bool hasNormals)
    {
        var material = new Material(shader) { renderQueue = source.renderQueue };
        foreach (string property in new[] { "_BaseMap", "_BumpMap", "_EmissionMap" })
        {
            material.SetTexture(property, source.GetTexture(property));
            material.SetTextureScale(property, source.GetTextureScale(property));
            material.SetTextureOffset(property, source.GetTextureOffset(property));
        }
        material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
        material.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
        material.SetFloat("_BumpScale", source.GetFloat("_BumpScale"));
        material.SetFloat("_Cull", source.GetFloat("_Cull"));
        material.SetFloat("_ReceiveShadows", source.GetFloat("_ReceiveShadows"));
        material.SetFloat("_QueueOffset", source.GetFloat("_QueueOffset"));
        material.doubleSidedGI = source.doubleSidedGI;
        material.enableInstancing = source.enableInstancing;
        material.globalIlluminationFlags = source.globalIlluminationFlags;
        float smoothness = source.GetFloat("_Smoothness");
        var specular = source.GetFloat("_WorkflowMode") == 0f ? source.GetColor("_SpecColor") :
            Color.Lerp(new Color(.04f, .04f, .04f), source.GetColor("_BaseColor"), source.GetFloat("_Metallic"));
        specular.a = smoothness;
        material.SetColor("_SpecColor", specular);
        material.SetFloat("_Smoothness", smoothness);
        bool specularEnabled = !source.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF");
        material.SetFloat("_SpecularHighlights", specularEnabled ? 0f : 1f);
        if (specularEnabled) material.EnableKeyword("_SPECULAR_COLOR");
        if (hasNormals) material.EnableKeyword("_NORMALMAP");
        if (source.IsKeywordEnabled("_EMISSION")) material.EnableKeyword("_EMISSION");
        if (source.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF")) material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
        return material;
    }
}
