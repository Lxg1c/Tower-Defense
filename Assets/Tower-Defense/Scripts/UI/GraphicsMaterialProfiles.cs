using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored material variants used by the optional graphics settings.</summary>
public sealed class GraphicsMaterialProfiles : ScriptableObject
{
    [Serializable]
    public struct Variant
    {
        public Material source;
        public Material standardWithoutNormals;
        public Material simple;
        public Material simpleWithoutNormals;

        public Material Select(GamePerformanceSettings.LightingMode lighting, bool normals)
        {
            if (lighting == GamePerformanceSettings.LightingMode.Standard)
                return normals || standardWithoutNormals == null ? source : standardWithoutNormals;
            if (!normals && simpleWithoutNormals != null) return simpleWithoutNormals;
            if (simple != null) return simple;
            return !normals && standardWithoutNormals != null ? standardWithoutNormals : source;
        }
    }

    public const string ResourceName = "GraphicsMaterialProfiles";
    public Variant[] variants = Array.Empty<Variant>();
    private static Dictionary<Material, Variant> lookup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => lookup = null;

    public static void Apply(GameObject root)
    {
        var lighting = GamePerformanceSettings.Lighting;
        bool normals = GamePerformanceSettings.NormalMaps;
        if (lighting == GamePerformanceSettings.LightingMode.Standard && normals) return;

        if (lookup == null)
        {
            var profiles = Resources.Load<GraphicsMaterialProfiles>(ResourceName);
            lookup = new Dictionary<Material, Variant>();
            if (profiles == null)
            {
                Debug.LogError("Graphics material profiles are missing from the build.");
                return;
            }
            foreach (var variant in profiles.variants)
                if (variant.source != null) lookup[variant.source] = variant;
        }

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !lookup.TryGetValue(materials[i], out var variant)) continue;
                var selected = variant.Select(lighting, normals);
                if (selected == materials[i]) continue;
                materials[i] = selected;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }
    }
}
