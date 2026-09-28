using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class SlowAmmoCounter : MonoBehaviour
{
    [SerializeField] private SlowAmmoInventory inventory;
    [SerializeField] private UnityEngine.UI.Image ring;
    private CanvasGroup group;

    private void Awake() => group = GetComponent<CanvasGroup>();

    private void OnEnable()
    {
        if (inventory == null || ring == null)
        {
            Debug.LogError("[SlowAmmoCounter] Assign the player bonus, timer label and progress ring.", this);
            enabled = false;
            return;
        }

        group.blocksRaycasts = false;
        group.interactable = false;
        Refresh();
    }

    private void Update() => Refresh();

    private void Refresh()
    {
        float remaining = inventory.RemainingSeconds;
        ring.fillAmount = inventory.Progress;
        group.alpha = remaining > 0f ? 1f : 0f;
    }
}
