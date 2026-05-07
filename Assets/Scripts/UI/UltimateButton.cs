using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hold-to-charge UI button for the ultimate. Wires PointerDown → BeginCharge,
/// PointerUp/Exit → Release. Optional <see cref="chargeFill"/> reflects
/// PlayerUltimate.ChargeProgress; <see cref="cooldownFill"/> shows cooldown.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UltimateButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PlayerUltimate ultimate;
    [SerializeField] private bool logInput = true;
    [SerializeField] private bool releaseOnPointerExit = true;

    [Header("Optional fill (0..1)")]
    [Tooltip("Single Image used both for charge and cooldown. " +
             "Charging → fills 0→1. Cooldown → drains 1→0. Empty otherwise.")]
    [SerializeField] private Image fill;
    [SerializeField] private Button button;

    private bool isPressed;
    private int activePointerId;

    public void OnPointerDown(PointerEventData eventData)
    {
        LogPointer("Down", eventData);

        if (ultimate == null || !ultimate.CanReceiveInput)
        {
            LogIgnored("Down", eventData);
            return;
        }

        isPressed = true;
        activePointerId = eventData.pointerId;

        ultimate.BeginCharge();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        LogPointer("Up", eventData);
        if (!IsActivePointer(eventData))
        {
            LogIgnored("Up", eventData);
            return;
        }

        isPressed = false;
        if (ultimate != null) ultimate.Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        LogPointer("Exit", eventData);
        if (!releaseOnPointerExit || !IsActivePointer(eventData))
        {
            LogIgnored("Exit", eventData);
            return;
        }

        isPressed = false;
        if (ultimate != null) ultimate.Release();
    }

    private void Update()
    {
        if (ultimate == null) return;

        if (fill != null)
        {
            if (ultimate.IsCharging)        fill.fillAmount = ultimate.ChargeProgress;
            else if (ultimate.IsOnCooldown) fill.fillAmount = ultimate.CooldownProgress;
            else                            fill.fillAmount = 0f;
        }

        if (button != null)
            button.interactable = ultimate.CanReceiveInput || ultimate.IsCharging;
    }

    private void LogPointer(string phase, PointerEventData eventData)
    {
        if (!logInput)
            return;

        Debug.Log(
            $"[UltimateButton] Pointer {phase} on '{name}'. " +
            $"ultimate={(ultimate != null ? ultimate.name : "NULL")}, " +
            $"pointerId={eventData.pointerId}, " +
            $"position={eventData.position}");
    }

    private bool IsActivePointer(PointerEventData eventData)
    {
        return isPressed && eventData.pointerId == activePointerId;
    }

    private void LogIgnored(string phase, PointerEventData eventData)
    {
        if (!logInput)
            return;

        Debug.Log(
            $"[UltimateButton] Pointer {phase} ignored on '{name}'. " +
            $"isPressed={isPressed}, activePointerId={activePointerId}, pointerId={eventData.pointerId}");
    }
}
