using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class CoinsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string format = "{0}";

    private PlayerWallet wallet;

    private void Start()
    {
        wallet = PlayerWallet.Instance;
        if (wallet == null || label == null) return;

        wallet.onCoinsChanged.AddListener(UpdateLabel);
        UpdateLabel(wallet.Coins);
    }

    private void OnDestroy()
    {
        if (wallet != null)
            wallet.onCoinsChanged.RemoveListener(UpdateLabel);
    }

    private void UpdateLabel(int coins)
    {
        label.text = string.Format(format, coins);
    }
}
