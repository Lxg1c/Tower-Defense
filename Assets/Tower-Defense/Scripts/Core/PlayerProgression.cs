using System;
using UnityEngine;

/// <summary>Local permanent progression. Level coins remain a separate per-run currency.</summary>
public static class PlayerProgression
{
    private const string SaveKey = "TowerDefense.Progression.v2";
    private const string LegacySaveKey = "TowerDefense.SkillTree.v1";
    private static SkillTreeState state;
    private static bool openSkillsOnNextMenu;

    public static event Action Changed;

    public static SkillTreeState State
    {
        get
        {
            if (state != null) return state;
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json)) state = JsonUtility.FromJson<SkillTreeState>(json);
            else
            {
                string oldJson = PlayerPrefs.GetString(LegacySaveKey, "");
                state = string.IsNullOrEmpty(oldJson) ? new SkillTreeState() : SkillTreeState.MigrateLegacy(oldJson);
                if (!string.IsNullOrEmpty(oldJson)) Save();
            }
            if (state == null) throw new InvalidOperationException("Saved skill progression could not be read.");
            state.Validate();
            return state;
        }
    }

    public static void RecordVictory(string levelId, int difficulty, int reward)
    {
        State.RecordVictory(levelId, difficulty, reward);
        Save();
    }

    public static bool TrySpend(int amount)
    {
        if (!State.TrySpend(amount)) return false;
        Save();
        return true;
    }

    public static bool TryUpgrade(PlayerSkill skill)
    {
        if (!State.TryUpgrade(skill)) return false;
        Save();
        return true;
    }

    public static void RequestSkillsScreen() => openSkillsOnNextMenu = true;

    public static bool ConsumeSkillsScreenRequest()
    {
        bool requested = openSkillsOnNextMenu;
        openSkillsOnNextMenu = false;
        return requested;
    }

    private static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(State));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        state = null;
        Changed = null;
        openSkillsOnNextMenu = false;
    }
}
