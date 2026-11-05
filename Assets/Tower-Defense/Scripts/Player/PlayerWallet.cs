using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    public int Coins { get; private set; }

    public UnityEvent<int> onCoinsChanged = new();
    private bool initialized;
    private bool startingCoinsConfigured;
    private int startingCoins;

    public void ConfigureStartingCoins(int amount)
    {
        if (initialized) throw new System.InvalidOperationException("Configure the wallet before Awake.");
        if (amount < 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
        startingCoins = amount;
        startingCoinsConfigured = true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (!startingCoinsConfigured)
        {
            Debug.LogError("[PlayerWallet] Configure starting coins through LevelSetup before enabling the wallet.", this);
            enabled = false;
            return;
        }

        Instance = this;

        initialized = true;
        Coins = startingCoins;
        onCoinsChanged?.Invoke(Coins);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0)
            throw new System.ArgumentOutOfRangeException(nameof(amount), "Spending cannot add coins.");
        if (amount > Coins)
            return false;

        Coins -= amount;
        onCoinsChanged?.Invoke(Coins);
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount < 0)
            throw new System.ArgumentOutOfRangeException(nameof(amount), "Rewards must be non-negative.");
        Coins += amount;
        onCoinsChanged?.Invoke(Coins);
    }
}
