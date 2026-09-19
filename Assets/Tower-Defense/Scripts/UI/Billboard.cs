using UnityEngine;

[DisallowMultipleComponent]
public class Billboard : MonoBehaviour
{
    [SerializeField] private bool lockY = true;

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCam == null)
            return;

        Vector3 dir = transform.position - mainCam.transform.position;
        if (lockY)
            dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
