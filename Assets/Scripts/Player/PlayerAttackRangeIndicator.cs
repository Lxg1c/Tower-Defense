using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAttackRangeIndicator : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private Color color = new(0f, 0.9f, 1f, 0.55f);
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField, Min(12)] private int segments = 96;

    private DetectionZone detectionZone;
    private LineRenderer lineRenderer;
    private WaveSpawner spawner;
    private PlayerHealth playerHealth;
    private float lastRadius = -1f;
    private int lastSegments = -1;
    private Vector3 lastCenter;

    private void Awake()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();

        if (detectionZone == null)
            detectionZone = GetComponentInChildren<DetectionZone>();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        ConfigureLineRenderer();
        SetVisible(false);
    }

    private void OnEnable()
    {
        BindSpawner();
        RefreshVisibility();
    }

    private void OnDisable()
    {
        UnbindSpawner();
    }

    private void LateUpdate()
    {
        if (spawner == null)
            BindSpawner();

        RefreshVisibility();
        RefreshCircleIfNeeded();
    }

    private void BindSpawner()
    {
        if (spawner != null)
            return;

        spawner = WaveSpawner.Instance;
        if (spawner == null)
            return;

        spawner.onBuildPhaseStarted.AddListener(OnBuildPhaseStarted);
        spawner.onCombatPhaseStarted.AddListener(OnCombatPhaseStarted);
        spawner.onAllWavesCompleted.AddListener(OnAllWavesCompleted);
    }

    private void UnbindSpawner()
    {
        if (spawner == null)
            return;

        spawner.onBuildPhaseStarted.RemoveListener(OnBuildPhaseStarted);
        spawner.onCombatPhaseStarted.RemoveListener(OnCombatPhaseStarted);
        spawner.onAllWavesCompleted.RemoveListener(OnAllWavesCompleted);
        spawner = null;
    }

    private void OnBuildPhaseStarted(int nextWaveIndex, int totalWaves, int reward)
    {
        RefreshVisibility();
    }

    private void OnCombatPhaseStarted(int waveIndex, int totalWaves, int reward)
    {
        RefreshVisibility();
    }

    private void OnAllWavesCompleted()
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool playerCanAttack = playerHealth == null || (!playerHealth.IsGhost && playerHealth.IsAlive);
        SetVisible(spawner != null && spawner.IsCombatPhase && playerCanAttack);
    }

    private void SetVisible(bool visible)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }

    private void ConfigureLineRenderer()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        if (material != null)
            lineRenderer.sharedMaterial = material;
        else if (lineRenderer.sharedMaterial == null)
            lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    private void RefreshCircleIfNeeded()
    {
        if (detectionZone == null || lineRenderer == null)
            return;

        float radius = detectionZone.Radius;
        Vector3 center = detectionZone.transform.position + Vector3.up * yOffset;
        if (Mathf.Approximately(radius, lastRadius)
            && segments == lastSegments
            && (center - lastCenter).sqrMagnitude < 0.0001f)
            return;

        lastRadius = radius;
        lastSegments = segments;
        lastCenter = center;
        lineRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments * Mathf.PI * 2f;
            Vector3 point = center + new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius);
            lineRenderer.SetPosition(i, point);
        }
    }
}
