using System;

/// <summary>Owns legal wave transitions and awards each cleared wave exactly once.</summary>
public sealed class GameSession
{
    public enum Phase { Build, Combat, AllCompleted, Defeated }

    private readonly int[] rewards;
    private readonly Action<int> awardCoins;
    private bool awardingReward;
    public Phase CurrentPhase { get; private set; }
    public int NextWaveIndex { get; private set; }

    public GameSession(int[] waveRewards, Action<int> awardCoins)
    {
        if (waveRewards == null) throw new ArgumentNullException(nameof(waveRewards));
        this.awardCoins = awardCoins ?? throw new ArgumentNullException(nameof(awardCoins));
        rewards = (int[])waveRewards.Clone();
        foreach (int reward in rewards)
            if (reward < 0) throw new ArgumentOutOfRangeException(nameof(waveRewards));
        CurrentPhase = rewards.Length == 0 ? Phase.AllCompleted : Phase.Build;
    }

    public bool TryStartWave(bool baseAlive, bool selectionOpen)
    {
        if (awardingReward || CurrentPhase != Phase.Build || !baseAlive || selectionOpen) return false;
        CurrentPhase = Phase.Combat;
        return true;
    }

    public bool TryCompleteWave(int waveIndex)
    {
        if (CurrentPhase != Phase.Combat || waveIndex != NextWaveIndex) return false;
        // Commit before notifying the wallet: its listeners must not be able to award twice.
        NextWaveIndex++;
        CurrentPhase = NextWaveIndex == rewards.Length ? Phase.AllCompleted : Phase.Build;
        awardingReward = true;
        try { awardCoins(rewards[waveIndex]); }
        finally { awardingReward = false; }
        return true;
    }

    public bool TryDefeat()
    {
        if (CurrentPhase == Phase.Defeated || CurrentPhase == Phase.AllCompleted) return false;
        CurrentPhase = Phase.Defeated;
        return true;
    }
}
