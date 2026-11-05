using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Select a barracks in the world, then tap reachable ground to station its squad.</summary>
[DisallowMultipleComponent]
public sealed class SquadOrderController : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Collider orderSurface;
    [SerializeField] private float maximumTapMovement = 30f;

    private BarracksBuilding selected;
    private GameObject selectionMarker;
    private bool pressStarted;
    private bool pressedOnUi;
    private Vector2 pressPosition;

    private void Awake()
    {
        if (gameplayCamera != null && orderSurface != null) return;

        Debug.LogError("[SquadOrderController] Assign the gameplay camera and level surface.", this);
        enabled = false;
    }

    private void OnDisable() => ClearSelection();

    private void Update()
    {
        if (selected == null && selectionMarker != null)
            ClearSelection();
        if (Time.timeScale <= 0f || TowerSelectionModal.Instance != null && TowerSelectionModal.Instance.IsOpen)
            return;

        if (Touchscreen.current != null &&
            (Touchscreen.current.primaryTouch.press.isPressed ||
             Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
        {
            ReadTouch();
            return;
        }

        ReadMouse();
    }

    private void ReadTouch()
    {
        var touch = Touchscreen.current.primaryTouch;
        Vector2 position = touch.position.ReadValue();
        if (touch.press.wasPressedThisFrame)
            BeginPress(position);
        if (touch.press.wasReleasedThisFrame)
            EndPress(position);
    }

    private void ReadMouse()
    {
        if (Mouse.current == null) return;

        Vector2 position = Mouse.current.position.ReadValue();
        if (Mouse.current.leftButton.wasPressedThisFrame)
            BeginPress(position);
        if (Mouse.current.leftButton.wasReleasedThisFrame)
            EndPress(position);
    }

    private void BeginPress(Vector2 position)
    {
        pressStarted = true;
        pressPosition = position;
        pressedOnUi = IsOverUi(position);
    }

    private void EndPress(Vector2 position)
    {
        if (!pressStarted) return;
        pressStarted = false;
        if (pressedOnUi || IsOverUi(position) ||
            (position - pressPosition).sqrMagnitude > maximumTapMovement * maximumTapMovement)
            return;

        Ray ray = gameplayCamera.ScreenPointToRay(position);
        BarracksBuilding tapped = FindBarracks(ray);
        if (tapped != null)
        {
            if (tapped == selected)
            {
                tapped.ClearGuardPosition();
                ClearSelection();
            }
            else
                Select(tapped);
            return;
        }

        if (selected != null && orderSurface.Raycast(ray, out RaycastHit hit, 1000f) &&
            selected.TryAssignGuardPosition(hit.point))
            ClearSelection();
    }

    private void Select(BarracksBuilding barracks)
    {
        ClearSelection();
        selected = barracks;
        if (barracks.OrderMarkerPrefab == null) return;

        selectionMarker = Instantiate(barracks.OrderMarkerPrefab);
        selectionMarker.transform.position = barracks.SelectionMarkerPosition;
    }

    private void ClearSelection()
    {
        selected = null;
        pressStarted = false;
        if (selectionMarker == null) return;

        Destroy(selectionMarker);
        selectionMarker = null;
    }

    private static BarracksBuilding FindBarracks(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
            return null;

        BarracksBuilding barracks = hit.collider.GetComponentInParent<BarracksBuilding>();
        if (barracks == null)
        {
            AllySoldier soldier = hit.collider.GetComponentInParent<AllySoldier>();
            barracks = soldier != null ? soldier.Barracks : null;
        }

        return barracks != null && barracks.isActiveAndEnabled ? barracks : null;
    }

    private static bool IsOverUi(Vector2 position)
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointer = new(EventSystem.current) { position = position };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        return results.Count > 0;
    }
}
