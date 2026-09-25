using System;
using UnityEngine;

/// <summary>Selected authored level; retained for restart, cleared on entering the menu.</summary>
public static class RunSelection
{
    private static LevelDefinition level;
    private static string sceneName;

    public static bool TrySelectNext(LevelDefinition current, string targetScene)
    {
        if (current == null) throw new ArgumentNullException(nameof(current));
        if (current.nextLevel == null) return false;
        if (current.nextLevel == current) throw new ArgumentException("A level cannot follow itself.");
        Select(current.nextLevel, targetScene);
        return true;
    }

    public static void Select(LevelDefinition selection, string targetScene)
    {
        if (selection == null) throw new ArgumentNullException(nameof(selection));
        if (string.IsNullOrWhiteSpace(targetScene)) throw new ArgumentException("A target scene is required.");
        selection.Validate();
        level = selection;
        sceneName = targetScene;
    }

    public static LevelDefinition Resolve(string targetScene, LevelDefinition directPlayLevel)
    {
        if (level != null)
        {
            if (sceneName != targetScene) throw new ArgumentException("Selected level targets a different scene.");
            return level;
        }
        if (directPlayLevel == null) throw new ArgumentException("Assign a Direct Play Level for direct scene entry.");
        return directPlayLevel;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear() { level = null; sceneName = null; }
}
