using UnityEngine;

/// <summary>
/// Damageable implementation for mobs. Awards coins on death and notifies MobCore
/// so it can despawn / return to pool.
/// </summary>
[DisallowMultipleComponent]
public class MobHealth : Damageable
{
    [Header("Reward")]
    [SerializeField] private int coinReward = 0;

    public int CoinReward => coinReward;

    public System.Action<MobHealth> OnDeathHandled;

    protected override void OnEnable()
    {
        // Reset state first so listeners (including HealthBarManager) see full HP.
        ResetToMaxHealth();
        base.OnEnable();
    }

    protected override void OnDeath()
    {
        if (coinReward > 0 && PlayerWallet.Instance != null)
            PlayerWallet.Instance.AddCoins(coinReward);

        OnDeathHandled?.Invoke(this);
    }
}
