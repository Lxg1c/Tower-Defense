using UnityEngine;

/// <summary>Marks the tappable order icon on an already built barracks.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class BarracksOrderIcon : MonoBehaviour
{
    public BarracksBuilding Barracks => GetComponentInParent<BarracksBuilding>();

    private Camera gameplayCamera;

    private void Start() => gameplayCamera = Camera.main;

    private void LateUpdate()
    {
        if (gameplayCamera == null) return;
        Vector3 towardCamera = gameplayCamera.transform.position - transform.position;
        if (towardCamera.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(towardCamera, gameplayCamera.transform.up);
    }
}
