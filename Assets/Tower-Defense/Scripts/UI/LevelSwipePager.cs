using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Single-card level selection with horizontal dragging and snap-back.</summary>
[DisallowMultipleComponent]
public sealed class LevelSwipePager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private MainMenu menu;
    [SerializeField] private RectTransform card;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField, Range(.05f, .5f)] private float swipeThreshold = .15f;
    private Vector2 origin;
    private Vector2 dragStart;
    private float velocity;
    private bool dragging;
    private int pointerId;

    private void Awake()
    {
        if (menu == null || card == null || previousButton == null || nextButton == null)
        {
            Debug.LogError("[LevelSwipePager] Assign the menu, card and navigation buttons.", this);
            enabled = false;
            return;
        }
        origin = card.anchoredPosition;
    }

    private void Update()
    {
        previousButton.interactable = menu.enabled && menu.SelectedIndex > 0;
        nextButton.interactable = menu.enabled && menu.SelectedIndex < menu.LevelCount - 1;
        if (!dragging)
        {
            var position = card.anchoredPosition;
            position.x = Mathf.SmoothDamp(position.x, origin.x, ref velocity, .10f, Mathf.Infinity, Time.unscaledDeltaTime);
            card.anchoredPosition = position;
        }
    }

    public void Previous() => Step(-1);
    public void Next() => Step(1);

    private void Step(int direction)
    {
        if (!enabled || !menu.enabled) return;
        int index = menu.SelectedIndex + direction;
        if (index < 0 || index >= menu.LevelCount) return;
        menu.SelectLevel(index);
        card.anchoredPosition = origin + Vector2.right * (direction * card.rect.width * .45f);
        velocity = 0;
    }

    public void OnBeginDrag(PointerEventData data)
    {
        if (!enabled || dragging || data.button != PointerEventData.InputButton.Left) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)card.parent,
            data.position, data.pressEventCamera, out dragStart);
        dragging = true;
        pointerId = data.pointerId;
        velocity = 0;
    }

    public void OnDrag(PointerEventData data)
    {
        if (!dragging || data.pointerId != pointerId) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)card.parent,
            data.position, data.pressEventCamera, out Vector2 current);
        float offset = Mathf.Clamp(current.x - dragStart.x, -card.rect.width * .6f, card.rect.width * .6f);
        card.anchoredPosition = origin + Vector2.right * offset;
    }

    public void OnEndDrag(PointerEventData data)
    {
        if (!dragging || data.pointerId != pointerId) return;
        OnDrag(data);
        dragging = false;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)card.parent,
            data.position, data.pressEventCamera, out Vector2 end);
        Vector2 delta = end - dragStart;
        if (Mathf.Abs(delta.x) >= card.rect.width * swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            Step(delta.x < 0 ? 1 : -1);
    }

    private void OnDisable()
    {
        dragging = false;
        velocity = 0;
        if (card != null) card.anchoredPosition = origin;
    }
}
