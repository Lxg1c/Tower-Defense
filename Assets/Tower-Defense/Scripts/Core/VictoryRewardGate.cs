using System;

/// <summary>Allows a completed run to grant permanent money only once.</summary>
public sealed class VictoryRewardGate
{
    public bool HasAwarded { get; private set; }

    public bool TryAward(Action award)
    {
        if (award == null) throw new ArgumentNullException(nameof(award));
        if (HasAwarded) return false;
        // Commit before invoking listeners to reject re-entrant victory UI calls.
        HasAwarded = true;
        try { award(); }
        catch
        {
            HasAwarded = false;
            throw;
        }
        return true;
    }
}
