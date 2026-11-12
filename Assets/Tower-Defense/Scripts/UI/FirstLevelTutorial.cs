using System;
using UnityEngine;

/// <summary>Remembers which contextual hints have appeared on the first campaign level.</summary>
internal sealed class FirstLevelTutorial
{
    [Flags]
    internal enum Hint
    {
        None = 0, Approach = 1, Core = 2, Economy = 4, Mine = 8,
        Turret = 16, StartWave = 32, AutoFire = 64, Ultimate = 128
    }

    private const string SeenKey = "TowerDefense.Tutorial.Seen";
    private const string ReplayKey = "TowerDefense.Tutorial.Replay";
    private Hint seen;
    private Hint active;
    private float autoFireTime;
    private bool usedUltimate;

    internal static bool ReplayQueued => PlayerPrefs.GetInt(ReplayKey) != 0;

    internal static void QueueReplay()
    {
        PlayerPrefs.SetInt(ReplayKey, 1);
        PlayerPrefs.Save();
    }

    internal FirstLevelTutorial()
    {
        if (ReplayQueued)
        {
            PlayerPrefs.DeleteKey(ReplayKey);
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
        }
        seen = (Hint)PlayerPrefs.GetInt(SeenKey);
    }

    internal Hint Update(bool visible, bool preparing, bool hasCore, bool hasDefense,
        BuildSlotType? offer, bool hasTarget, bool ultimateCharging, bool ultimateReady, float deltaTime)
    {
        usedUltimate |= ultimateCharging;
        if (!visible) return Hint.None;

        Hint next;
        if (preparing)
        {
            next = offer switch
            {
                BuildSlotType.TownHall => Hint.Core,
                BuildSlotType.Mine => Hint.Mine,
                BuildSlotType.Defense => Hint.Turret,
                _ => !hasCore ? Hint.Approach : hasDefense ? Hint.StartWave : Hint.Economy
            };
        }
        else if ((active == Hint.AutoFire && autoFireTime < 4f) || ((seen & Hint.AutoFire) == 0 && hasTarget))
        {
            autoFireTime += deltaTime;
            next = Hint.AutoFire;
        }
        else
        {
            next = !usedUltimate && ultimateReady ? Hint.Ultimate : Hint.None;
        }

        if (next == active) return active;
        active = (seen & next) == 0 ? next : Hint.None;
        if (active != Hint.None)
        {
            seen |= active;
            PlayerPrefs.SetInt(SeenKey, (int)seen);
            PlayerPrefs.Save();
        }
        return active;
    }
}
