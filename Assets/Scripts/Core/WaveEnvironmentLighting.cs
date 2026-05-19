using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveEnvironmentLighting : MonoBehaviour
{
    [SerializeField] private Color combatAmbientColor = new(0.32f, 0.06f, 0.045f, 1f);
    [SerializeField] private float combatAmbientIntensity = 0.45f;
    [SerializeField] private float transitionDuration = 0.75f;

    private WaveSpawner spawner;
    private Coroutine transitionRoutine;
    private Color buildAmbientColor;
    private float buildAmbientIntensity;

    private void Awake()
    {
        buildAmbientColor = RenderSettings.ambientLight;
        buildAmbientIntensity = RenderSettings.ambientIntensity;
    }

    private void OnEnable()
    {
        BindSpawner();
        ApplyCurrentPhaseImmediate();
    }

    private void OnDisable()
    {
        UnbindSpawner();

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);
    }

    private void Update()
    {
        if (spawner == null)
            BindSpawner();
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
        ApplyCurrentPhaseImmediate();
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
        StartTransition(buildAmbientColor, buildAmbientIntensity);
    }

    private void OnCombatPhaseStarted(int waveIndex, int totalWaves, int reward)
    {
        StartTransition(combatAmbientColor, combatAmbientIntensity);
    }

    private void OnAllWavesCompleted()
    {
        StartTransition(buildAmbientColor, buildAmbientIntensity);
    }

    private void ApplyCurrentPhaseImmediate()
    {
        if (spawner == null)
            return;

        if (spawner.IsCombatPhase)
            ApplyLighting(combatAmbientColor, combatAmbientIntensity);
        else
            ApplyLighting(buildAmbientColor, buildAmbientIntensity);
    }

    private void StartTransition(Color targetColor, float targetIntensity)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionTo(targetColor, targetIntensity));
    }

    private IEnumerator TransitionTo(Color targetColor, float targetIntensity)
    {
        Color startColor = RenderSettings.ambientLight;
        float startIntensity = RenderSettings.ambientIntensity;
        float duration = Mathf.Max(0.01f, transitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyLighting(
                Color.Lerp(startColor, targetColor, t),
                Mathf.Lerp(startIntensity, targetIntensity, t));
            yield return null;
        }

        ApplyLighting(targetColor, targetIntensity);
        transitionRoutine = null;
    }

    private static void ApplyLighting(Color color, float intensity)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = color;
        RenderSettings.ambientIntensity = Mathf.Max(0f, intensity);
    }
}
