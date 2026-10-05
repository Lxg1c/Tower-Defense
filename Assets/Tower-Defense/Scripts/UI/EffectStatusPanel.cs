using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EffectStatusPanel : MonoBehaviour
{
    [SerializeField] private SlowAmmoInventory slowAmmo;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject slowIndicator;
    [SerializeField] private UnityEngine.UI.Image slowRing;
    [SerializeField] private TMP_Text slowTime;
    [SerializeField] private GameObject shieldIndicator;
    [SerializeField] private UnityEngine.UI.Image shieldRing;
    [SerializeField] private TMP_Text shieldTime;
    [SerializeField] private CompanionTurretController companionTurrets;
    [SerializeField] private GameObject companionIndicator;
    [SerializeField] private UnityEngine.UI.Image companionRing;
    [SerializeField] private TMP_Text companionTime;
    [SerializeField] private TMP_Text companionCount;

    private void Awake()
    {
        if (slowAmmo == null || playerHealth == null || slowIndicator == null ||
            slowRing == null || slowTime == null || shieldIndicator == null ||
            shieldRing == null || shieldTime == null || companionTurrets == null ||
            companionIndicator == null || companionRing == null || companionTime == null ||
            companionCount == null)
        {
            Debug.LogError("[EffectStatusPanel] Assign all effect sources, indicators, rings and labels.", this);
            enabled = false;
            return;
        }

        Refresh();
    }

    private void Update() => Refresh();

    private void Refresh()
    {
        float slowRemaining = slowAmmo.RemainingSeconds;
        float shieldRemaining = playerHealth.RemainingShieldSeconds;
        bool showSlow = slowRemaining > 0f;
        bool showShield = playerHealth.CurrentShield > 0f && shieldRemaining > 0f;
        float companionRemaining = companionTurrets.RemainingSeconds;
        int companionCountValue = companionTurrets.ActiveCount;
        bool showCompanion = companionCountValue > 0 && companionRemaining > 0f;

        if (slowIndicator.activeSelf != showSlow) slowIndicator.SetActive(showSlow);
        if (shieldIndicator.activeSelf != showShield) shieldIndicator.SetActive(showShield);
        if (companionIndicator.activeSelf != showCompanion) companionIndicator.SetActive(showCompanion);
        if (showSlow)
        {
            slowRing.fillAmount = slowAmmo.Progress;
            slowTime.text = Mathf.CeilToInt(slowRemaining).ToString();
        }
        if (showShield)
        {
            shieldRing.fillAmount = playerHealth.ShieldProgress;
            shieldTime.text = Mathf.CeilToInt(shieldRemaining).ToString();
        }
        if (showCompanion)
        {
            companionRing.fillAmount = companionTurrets.Progress;
            companionTime.text = Mathf.CeilToInt(companionRemaining).ToString();
            companionCount.text = $"×{companionCountValue}";
        }
    }
}
