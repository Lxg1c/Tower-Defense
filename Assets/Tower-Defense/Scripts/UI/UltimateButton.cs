using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Starts the ultimate on press; release matters only in manual charge mode.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UltimateButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PlayerUltimate ultimate;
    [SerializeField] private bool releaseOnPointerExit = true;

    [Header("Optional radial fill (0..1)")]
    [Tooltip("Single Image used both for charge and cooldown. " +
             "Charging fills to the chosen charge level; cooldown drains from that level to zero.")]
    [SerializeField] private Image fill;
    [SerializeField] private bool configureAsRadial360 = true;
    [SerializeField] private Image.Origin360 radialOrigin = Image.Origin360.Top;
    [SerializeField] private bool radialClockwise = true;
    [SerializeField] private Button button;
 
    private bool _isPressed;
    private bool keyboardPressed;

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
        if (ultimate == null || !ultimate.CanReceiveInput || ultimate.IsCharging || keyboardPressed)
            return;

        _isPressed = !ultimate.IsAutomaticCharge;
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

        if (!Application.isMobilePlatform && Keyboard.current != null)
        {
            var space = Keyboard.current.spaceKey;
            if (space.wasPressedThisFrame && !keyboardPressed && !_isPressed &&
                !ultimate.IsCharging && ultimate.CanReceiveInput)
            {
                keyboardPressed = !ultimate.IsAutomaticCharge;
                ultimate.BeginCharge();
            }
            else if (space.wasReleasedThisFrame && keyboardPressed)
            {
                keyboardPressed = false;
                ultimate.Release();
            }
        }

        if (fill != null)
        {
            if (ultimate.IsCharging)        fill.fillAmount = ultimate.ChargeProgress;
            else if (ultimate.IsOnCooldown) fill.fillAmount = ultimate.CooldownProgress;
            else                            fill.fillAmount = 0f;
        }

        if (button != null)
            button.interactable = ultimate.IsAutomaticCharge
                ? ultimate.CanReceiveInput && !ultimate.IsCharging
                : ultimate.CanReceiveInput || ultimate.IsCharging;
        
        if (!ultimate.CanReceiveInput && !ultimate.IsCharging)
            _isPressed = false;
    }

    private void OnDisable()
    {
        if (!_isPressed && !keyboardPressed) return;
        _isPressed = false;
        keyboardPressed = false;
        if (ultimate != null) ultimate.Release();
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
