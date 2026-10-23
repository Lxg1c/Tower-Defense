using UnityEngine;

/// <summary>Applies purchased permanent skills once to the player in each level scene.</summary>
[DisallowMultipleComponent]
public sealed class PlayerSkillBonus : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private Shooter shooter;

    private void Start()
    {
        if (health == null || shooter == null)
        {
            Debug.LogError("[PlayerSkillBonus] Assign player health and shooter.", this);
            enabled = false;
            return;
        }

        SkillTreeState skills = PlayerProgression.State;
        health.SetMaxHealth(health.MaxHealth * skills.HealthMultiplier);
        shooter.Damage *= skills.DamageMultiplier;
        shooter.FireRate *= skills.FireRateMultiplier;
    }
}
