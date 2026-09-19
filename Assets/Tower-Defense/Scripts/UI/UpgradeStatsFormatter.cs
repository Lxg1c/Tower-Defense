using System.Text;

/// <summary>Owns the display text for building upgrades; gameplay exposes numbers only.</summary>
public static class UpgradeStatsFormatter
{
    public static string Current(TowerUpgrade tower) => Format(tower.GetCurrentStats());

    public static string Next(TowerUpgrade tower)
    {
        BuildingStats? stats = tower.GetNextStats();
        return stats.HasValue ? Format(stats.Value) : "";
    }

    public static string Locked(TowerUpgrade tower)
    {
        return tower.HasNextLevel
            ? $"Requires Base upgrade limit {tower.NextUpgradeLevel}. Current limit: {tower.MaxUnlockedLevel}."
            : "";
    }

    public static string Current(BaseUpgrade townHall)
    {
        return $"Base Level: {townHall.CurrentLevel}{System.Environment.NewLine}{Format(townHall.GetCurrentStats())}";
    }

    public static string Next(BaseUpgrade townHall)
    {
        BuildingStats? stats = townHall.GetNextStats();
        return stats.HasValue
            ? $"Base Level: {townHall.CurrentLevel + 1}{System.Environment.NewLine}{Format(stats.Value)}"
            : "";
    }

    public static string Format(BuildingStats stats)
    {
        var text = new StringBuilder();
        if (stats.ShooterDamage.HasValue)
            text.AppendLine($"Damage: {stats.ShooterDamage.Value:0.##}");
        if (stats.ShooterFireRate.HasValue)
            text.AppendLine($"Fire Rate: {stats.ShooterFireRate.Value:0.##}/s");
        if (stats.PulseDamage.HasValue)
            text.AppendLine($"Damage: {stats.PulseDamage.Value:0.##}");
        if (stats.PulseRange.HasValue)
            text.AppendLine($"Range: {stats.PulseRange.Value:0.##}");
        if (stats.PulseInterval.HasValue)
            text.AppendLine($"Interval: {stats.PulseInterval.Value:0.##}s");
        if (stats.ShooterRange.HasValue)
            text.AppendLine($"Range: {stats.ShooterRange.Value:0.##}");
        if (stats.MaxHealth.HasValue)
            text.AppendLine($"HP: {stats.MaxHealth.Value:0.##}");
        if (stats.Income.HasValue)
            text.AppendLine($"Income: {stats.Income.Value}");
        return text.ToString().TrimEnd();
    }
}
