using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int startingCoins = 100;

    public int Coins { get; private set; }

    public UnityEvent<int> onCoinsChanged;

    private void Awake()
    {
        Coins = startingCoins;
        onCoinsChanged?.Invoke(Coins);
    }

    public bool TrySpend(int amount)
    {
        if (amount > Coins)
            return false;

        Coins -= amount;
        onCoinsChanged?.Invoke(Coins);
        return true;
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
        onCoinsChanged?.Invoke(Coins);
    }
}
