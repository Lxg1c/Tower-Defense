using System;

/// <summary>Owns legal wave transitions and awards each cleared wave exactly once.</summary>
public sealed class GameSession
{
    public enum Phase { Build, Combat, AllCompleted, Defeated }

    private readonly int[] _rewards;
    private readonly Action<int> _awardCoins;
    private bool _awardingReward;
    public Phase CurrentPhase { get; private set; }
    public int NextWaveIndex { get; private set; }

    public GameSession(int[] waveRewards, Action<int> awardCoins)
    {
        if (waveRewards == null) throw new ArgumentNullException(nameof(waveRewards));
        _awardCoins = awardCoins ?? throw new ArgumentNullException(nameof(awardCoins));
        _rewards = (int[])waveRewards.Clone();
        foreach (var reward in _rewards)
            if (reward < 0) throw new ArgumentOutOfRangeException(nameof(waveRewards));
        CurrentPhase = _rewards.Length == 0 ? Phase.AllCompleted : Phase.Build;
    }

    public bool TryStartWave(bool baseAlive, bool selectionOpen)
    {
        if (_awardingReward || CurrentPhase != Phase.Build || !baseAlive || selectionOpen) return false;
        CurrentPhase = Phase.Combat;
        return true;
    }

    public bool TryCompleteWave(int waveIndex)
    {
        if (CurrentPhase != Phase.Combat || waveIndex != NextWaveIndex) return false;
        // Commit before notifying the wallet: its listeners must not be able to award twice.
        NextWaveIndex++;
        CurrentPhase = NextWaveIndex == _rewards.Length ? Phase.AllCompleted : Phase.Build;
        _awardingReward = true;
        try { _awardCoins(_rewards[waveIndex]); }
        finally { _awardingReward = false; }
        return true;
    }

    public bool TryDefeat()
    {
        if (CurrentPhase is Phase.Defeated or Phase.AllCompleted) return false;
        CurrentPhase = Phase.Defeated;
        return true;
    }
}
