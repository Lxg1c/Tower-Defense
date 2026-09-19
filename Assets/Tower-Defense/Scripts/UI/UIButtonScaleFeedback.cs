using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonScaleFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Transform target;
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.12f;
    [SerializeField] private Ease pressEase = Ease.OutQuad;
    [SerializeField] private Ease releaseEase = Ease.OutBack;
    [Tooltip("Useful for pause menu buttons because Time.timeScale is 0 during pause.")]
    [SerializeField] private bool ignoreTimeScale = true;

    private Button button;
    private Vector3 initialScale;
    private Tween scaleTween;

    private void Awake()
    {
        if (target == null)
            target = transform;

        button = GetComponent<Button>();
        initialScale = target.localScale;
    }

    private void OnDisable()
    {
        KillTween();

        if (target != null)
            target.localScale = initialScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate())
            return;

        AnimateTo(initialScale * Mathf.Max(0f, pressedScale), pressDuration, pressEase);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void Release()
    {
        if (target == null)
            return;

        AnimateTo(initialScale, releaseDuration, releaseEase);
    }

    private bool CanAnimate()
    {
        return target != null && (button == null || button.interactable);
    }

    private void AnimateTo(Vector3 scale, float duration, Ease ease)
    {
        KillTween();
        scaleTween = target
            .DOScale(scale, Mathf.Max(0f, duration))
            .SetEase(ease)
            .SetUpdate(ignoreTimeScale);
    }

    private void KillTween()
    {
        if (scaleTween != null && scaleTween.IsActive())
            scaleTween.Kill();

        scaleTween = null;
    }
}
