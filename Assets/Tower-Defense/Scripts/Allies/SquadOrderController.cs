using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Opens a map view from a barracks icon, then stations its squad at a reachable point.</summary>
[DisallowMultipleComponent]
public sealed class SquadOrderController : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Behaviour cameraBrain;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Collider orderSurface;
    [SerializeField] private LevelGrid mapGrid;
    [SerializeField] private Canvas orderCanvas;
    [SerializeField] private UnityEngine.UI.Button cancelButton;
    [SerializeField] private LineRenderer routeLine;

    [Header("Input and feedback")]
    [SerializeField] private float maximumTapMovement = 30f;
    [SerializeField] private float routeDisplaySeconds = 3f;
    [SerializeField] private float mapViewPadding = 1.2f;
    [Min(1f)] [SerializeField] private float cameraDistanceMultiplier = 1.8f;

    private readonly List<Canvas> hiddenCanvases = new();
    private BarracksBuilding selected;
    private GameObject selectionMarker;
    private Coroutine routeHideRoutine;
    private bool orderModeActive;
    public bool IsOrderModeActive => orderModeActive;
    private PlayerHealth playerHealth;
    private bool brainWasEnabled;
    private bool pressStarted;
    private bool pressedOnUi;
    private Vector2 pressPosition;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private float cameraOrthographicSize;

    private void Awake()
    {
        if (gameplayCamera == null || playerTarget == null || orderSurface == null || mapGrid == null ||
            orderCanvas == null || cancelButton == null || routeLine == null || cameraBrain == null)
        {
            Debug.LogError("[SquadOrderController] Assign the camera with CinemachineBrain, level grid, order surface, order canvas, cancel button and route line.", this);
            enabled = false;
            return;
        }

        playerHealth = playerTarget.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[SquadOrderController] Player target needs PlayerHealth.", this);
            enabled = false;
            return;
        }

        orderCanvas.enabled = false;
        routeLine.enabled = false;
        cancelButton.onClick.AddListener(CancelOrder);
    }

    private void OnDisable()
    {
        StopRouteFeedback();
        EndOrderMode();
    }

    private void OnDestroy()
    {
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelOrder);
    }

    private void Update()
    {
        if (orderModeActive && playerHealth.IsGhost)
        {
            CancelOrder();
            return;
        }
        if (orderModeActive && selected == null)
        {
            CancelOrder();
            return;
        }
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

    public void StartOrder(BarracksBuilding barracks)
    {
        if (!isActiveAndEnabled || orderModeActive || barracks == null || !barracks.isActiveAndEnabled ||
            playerHealth == null || playerHealth.IsGhost ||
            Time.timeScale <= 0f || TowerSelectionModal.Instance != null && TowerSelectionModal.Instance.IsOpen)
            return;

        StopRouteFeedback();
        selected = barracks;
        orderModeActive = true;
        if (barracks.OrderMarkerPrefab != null)
        {
            selectionMarker = Instantiate(barracks.OrderMarkerPrefab);
            selectionMarker.transform.position = barracks.SelectionMarkerPosition;
        }
        HideGameplayCanvases();
        EnterMapView();
    }

    public void CancelOrder()
    {
        StopRouteFeedback();
        EndOrderMode();
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
        BarracksOrderIcon icon = null;
        if (Physics.Raycast(ray, out RaycastHit iconHit, 1000f, ~0, QueryTriggerInteraction.Ignore))
            icon = iconHit.collider.GetComponentInParent<BarracksOrderIcon>();
        if (!orderModeActive)
        {
            if (icon != null)
                StartOrder(icon.Barracks);
            return;
        }

        if (icon != null)
        {
            if (icon.Barracks == selected)
            {
                selected.ClearGuardPosition();
                CancelOrder();
            }
            return;
        }

        if (orderSurface.Raycast(ray, out RaycastHit groundHit, 1000f) &&
            selected.TryAssignGuardPosition(groundHit.point, out Vector3[] corners))
        {
            ShowRoute(corners);
            EndOrderMode();
        }
    }

    private void HideGameplayCanvases()
    {
        hiddenCanvases.Clear();
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas == null || canvas == orderCanvas || !canvas.isActiveAndEnabled ||
                !canvas.gameObject.scene.IsValid())
                continue;
            hiddenCanvases.Add(canvas);
            canvas.enabled = false;
        }
        orderCanvas.enabled = true;
    }

    private void EnterMapView()
    {
        cameraPosition = gameplayCamera.transform.position;
        cameraRotation = gameplayCamera.transform.rotation;
        cameraOrthographicSize = gameplayCamera.orthographicSize;
        brainWasEnabled = cameraBrain.enabled;
        cameraBrain.enabled = false;

        float halfWidth = mapGrid.Columns * mapGrid.CellSize * 0.5f * mapViewPadding;
        float halfDepth = mapGrid.Rows * mapGrid.CellSize * 0.5f * mapViewPadding;
        float visibleHalfHeight = Mathf.Max(halfDepth, halfWidth / Mathf.Max(0.1f, gameplayCamera.aspect));
        if (gameplayCamera.orthographic)
            gameplayCamera.orthographicSize = visibleHalfHeight;
        else
        {
            float halfFov = gameplayCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            Vector3 fromPlayer = cameraPosition - playerTarget.position;
            float distance = Mathf.Max(fromPlayer.magnitude * cameraDistanceMultiplier,
                visibleHalfHeight / Mathf.Tan(halfFov));
            gameplayCamera.transform.position = playerTarget.position + fromPlayer.normalized * distance;
        }
    }

    private void EndOrderMode()
    {
        if (!orderModeActive) return;
        orderModeActive = false;
        selected = null;
        pressStarted = false;
        if (selectionMarker != null)
            Destroy(selectionMarker);
        selectionMarker = null;

        if (gameplayCamera != null)
        {
            gameplayCamera.transform.position = cameraPosition;
            gameplayCamera.transform.rotation = cameraRotation;
            gameplayCamera.orthographicSize = cameraOrthographicSize;
        }
        if (cameraBrain != null)
            cameraBrain.enabled = brainWasEnabled;

        if (orderCanvas != null)
            orderCanvas.enabled = false;
        foreach (Canvas canvas in hiddenCanvases)
            if (canvas != null)
                canvas.enabled = true;
        hiddenCanvases.Clear();
    }

    private void ShowRoute(Vector3[] corners)
    {
        if (corners == null || corners.Length < 2) return;
        routeLine.positionCount = corners.Length;
        for (int i = 0; i < corners.Length; i++)
            routeLine.SetPosition(i, corners[i] + Vector3.up * 0.2f);
        routeLine.enabled = true;
        routeHideRoutine = StartCoroutine(HideRouteAfterDelay());
    }

    private IEnumerator HideRouteAfterDelay()
    {
        yield return new WaitForSeconds(routeDisplaySeconds);
        routeLine.enabled = false;
        routeHideRoutine = null;
    }

    private void StopRouteFeedback()
    {
        if (routeHideRoutine != null)
            StopCoroutine(routeHideRoutine);
        routeHideRoutine = null;
        if (routeLine != null)
            routeLine.enabled = false;
    }

    private static bool IsOverUi(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        PointerEventData pointer = new(EventSystem.current) { position = position };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        return results.Count > 0;
    }
}
