/// <summary>
/// Numeric building stats shared by upgrades and their UI. A missing value means
/// that the building does not have that capability (for example, mine income).
/// </summary>
public readonly struct BuildingStats
{
    public float? ShooterDamage { get; }
    public float? ShooterFireRate { get; }
    public float? ShooterRange { get; }
    public float? PulseDamage { get; }
    public float? PulseRange { get; }
    public float? PulseInterval { get; }
    public float? MortarDamage { get; }
    public float? MortarInterval { get; }
    public float? MortarRange { get; }
    public float? MaxHealth { get; }
    public int? Income { get; }

    public BuildingStats(float? shooterDamage = null, float? shooterFireRate = null,
        float? shooterRange = null, float? pulseDamage = null, float? pulseRange = null,
        float? pulseInterval = null, float? maxHealth = null, int? income = null,
        float? mortarDamage = null, float? mortarInterval = null, float? mortarRange = null)
    {
        ShooterDamage = shooterDamage;
        ShooterFireRate = shooterFireRate;
        ShooterRange = shooterRange;
        PulseDamage = pulseDamage;
        PulseRange = pulseRange;
        PulseInterval = pulseInterval;
        MortarDamage = mortarDamage;
        MortarInterval = mortarInterval;
        MortarRange = mortarRange;
        MaxHealth = maxHealth;
        Income = income;
    }
}
