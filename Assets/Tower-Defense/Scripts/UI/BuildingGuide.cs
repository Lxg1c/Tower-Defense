using UnityEngine;

/// <summary>Local discovery of the first building unlocked through a core upgrade.</summary>
internal static class BuildingGuide
{
    private const string PulseKey = "TowerDefense.BuildingGuide.Pulse";

    internal static bool PulseDiscovered => PlayerPrefs.GetInt(PulseKey) > 0;
    internal static bool PulseIsNew => PlayerPrefs.GetInt(PulseKey) == 1;

    internal static void DiscoverPulse()
    {
        if (PulseDiscovered) return;
        PlayerPrefs.SetInt(PulseKey, 1);
        PlayerPrefs.Save();
    }

    internal static void ReadPulse()
    {
        if (!PulseIsNew) return;
        PlayerPrefs.SetInt(PulseKey, 2);
        PlayerPrefs.Save();
    }
}
