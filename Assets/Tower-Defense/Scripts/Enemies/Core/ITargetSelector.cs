/// <summary>
/// Selects the current attack target for a mob.
/// Implementations encapsulate priority rules (closest defense, walls-first, base-only, etc.).
/// </summary>
public interface ITargetSelector
{
    /// <summary>
    /// Returns the best current target, or null if nothing valid is in awareness range.
    /// May be called frequently — implementations are expected to cache internally.
    /// </summary>
    Damageable GetTarget(MobCore mob);
}
