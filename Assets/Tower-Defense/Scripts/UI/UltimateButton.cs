using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hold-to-charge UI button for the ultimate. Wires PointerDown → BeginCharge,
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UltimateButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PlayerUltimate ultimate;
    [SerializeField] private bool releaseOnPointerExit = true;

    [Header("Optional radial fill (0..1)")]
    [Tooltip("Single Image used both for charge and cooldown. " +
             "Charging → fills 0→1. Cooldown → drains 1→0. Empty otherwise.")]
    [SerializeField] private Image fill;
    [SerializeField] private bool configureAsRadial360 = true;
    [SerializeField] private Image.Origin360 radialOrigin = Image.Origin360.Top;
    [SerializeField] private bool radialClockwise = true;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text cooldownLabel;
 
    private bool _isPressed;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        ConfigureFillImage();
    }

    private void OnValidate()
    {
        ConfigureFillImage();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (ultimate == null || !ultimate.CanReceiveInput)
            return;

        _isPressed = true;
        ultimate.BeginCharge();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isPressed) return;
        _isPressed = false;
        ultimate.Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isPressed || !releaseOnPointerExit) return;
        _isPressed = false;
        ultimate.Release();
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

        if (cooldownLabel != null)
        {
            bool showCooldown = ultimate.IsOnCooldown;
            GameObject badge = cooldownLabel.transform.parent.gameObject;
            if (badge.activeSelf != showCooldown)
                badge.SetActive(showCooldown);
            if (showCooldown)
                cooldownLabel.text = Mathf.CeilToInt(ultimate.CooldownRemaining).ToString();
        }

        if (button != null)
            button.interactable = ultimate.CanReceiveInput || ultimate.IsCharging;
        
        if (!ultimate.CanReceiveInput && !ultimate.IsCharging)
            _isPressed = false;
    }

    private void ConfigureFillImage()
    {
        if (!configureAsRadial360 || fill == null)
            return;

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)radialOrigin;
        fill.fillClockwise = radialClockwise;
        fill.raycastTarget = false;
    }
    
}
