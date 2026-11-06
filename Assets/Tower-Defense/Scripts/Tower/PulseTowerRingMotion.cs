using UnityEngine;

/// <summary>Rotates the pulse tower's decorative ring around two visible axes.</summary>
[DisallowMultipleComponent]
public sealed class PulseTowerRingMotion : MonoBehaviour
{
    [SerializeField] private float xDegreesPerSecond = 45f;
    [SerializeField] private float zDegreesPerSecond = 60f;

    private Quaternion initialRotation;
    private float xAngle;
    private float zAngle;

    private void Awake()
    {
        initialRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        xAngle = Mathf.Repeat(xAngle + xDegreesPerSecond * Time.deltaTime, 360f);
        zAngle = Mathf.Repeat(zAngle + zDegreesPerSecond * Time.deltaTime, 360f);
        transform.localRotation = Quaternion.Euler(xAngle, 0f, zAngle) * initialRotation;
    }
}
