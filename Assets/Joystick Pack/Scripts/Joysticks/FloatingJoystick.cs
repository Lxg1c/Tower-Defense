using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : Joystick
{
    private Vector2 _defaultPosition;

    protected override void Start()
    {
        base.Start();
        // Remember where the joystick sits in the Canvas
        _defaultPosition = background.anchoredPosition;
        background.gameObject.SetActive(true);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        // Move joystick to where the player tapped
        background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        // Return to default position instead of hiding
        background.anchoredPosition = _defaultPosition;
        base.OnPointerUp(eventData);
    }
}