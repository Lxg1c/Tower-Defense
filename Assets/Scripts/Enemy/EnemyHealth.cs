using UnityEngine;

public class EnemyHealth : Damageable
{
    [Header("Reward")]
    [SerializeField] private int coinReward = 10;

    protected override void OnDeath()
    {
        if (coinReward > 0)
        {
            var wallet = FindAnyObjectByType<PlayerWallet>();
            if (wallet != null)
                wallet.AddCoins(coinReward);
        }

        Destroy(gameObject);
    }
}
