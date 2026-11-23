using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class WaveEnvironmentLighting : MonoBehaviour
{
    private static readonly int GroundGlowStrength = Shader.PropertyToID("_Strength");

    [SerializeField] private Color combatAmbientColor = new(0.32f, 0.06f, 0.045f, 1f);
    [SerializeField] private float combatAmbientIntensity = 0.45f;
    [SerializeField] private float transitionDuration = 0.75f;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Volume lightingVolume;
    [SerializeField] private Renderer groundGlow;

    private WaveSpawner spawner;
    private Coroutine transitionRoutine;
    private Color buildAmbientColor;
    private float buildAmbientIntensity;
    private Color dayLightColor;
    private float dayLightIntensity;
    private float dayShadowStrength;
    private VolumeProfile ownedProfile;
    private Bloom bloom;
    private MaterialPropertyBlock glowProperties;
    private float nightBlend;

    private void Awake()
    {
        buildAmbientColor = RenderSettings.ambientLight;
        buildAmbientIntensity = RenderSettings.ambientIntensity;
        if (directionalLight != null)
        {
            dayLightColor = directionalLight.color;
            dayLightIntensity = directionalLight.intensity;
            dayShadowStrength = directionalLight.shadowStrength;
        }

        if (lightingVolume != null)
        {
            bool instantiated = lightingVolume.HasInstantiatedProfile();
            var profile = lightingVolume.profile;
            if (!instantiated) ownedProfile = profile;
            if (profile.TryGet(out bloom))
            {
                bloom.highQualityFiltering.Override(false);
                bloom.maxIterations.Override(4);
            }
        }

        if (groundGlow != null) glowProperties = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        BindSpawner();
        ApplyCurrentPhaseImmediate();
    }

    private void OnDisable()
    {
        UnbindSpawner();

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = null;
    }

    private void OnDestroy()
    {
        if (ownedProfile == null) return;
        if (lightingVolume != null) lightingVolume.profile = null;
        foreach (var component in ownedProfile.components)
            Destroy(component);
        Destroy(ownedProfile);
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
        StartTransition(0f);
    }

    private void OnCombatPhaseStarted(int waveIndex, int totalWaves, int reward)
    {
        StartTransition(1f);
    }

    private void OnAllWavesCompleted()
    {
        StartTransition(0f);
    }

    private void ApplyCurrentPhaseImmediate()
    {
        if (spawner == null)
            return;

        ApplyLighting(spawner.IsCombatPhase ? 1f : 0f);
    }

    private void StartTransition(float target)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        if (Mathf.Approximately(nightBlend, target))
        {
            transitionRoutine = null;
            return;
        }
        transitionRoutine = StartCoroutine(TransitionTo(target));
    }

    private IEnumerator TransitionTo(float target)
    {
        float start = nightBlend;
        float duration = Mathf.Max(0.01f, transitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyLighting(Mathf.Lerp(start, target, t));
            yield return null;
        }

        ApplyLighting(target);
        transitionRoutine = null;
    }

    private void ApplyLighting(float blend)
    {
        nightBlend = blend;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(buildAmbientColor, combatAmbientColor, blend);
        RenderSettings.ambientIntensity = Mathf.Max(0f, Mathf.Lerp(buildAmbientIntensity, combatAmbientIntensity, blend));

        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(dayLightColor, new Color(0.67f, 0.79f, 1f), blend);
            directionalLight.intensity = dayLightIntensity * Mathf.Lerp(1f, 0.66f, blend);
            directionalLight.shadowStrength = dayShadowStrength * Mathf.Lerp(1f, 0.8f, blend);
        }

        if (bloom != null)
        {
            bloom.threshold.Override(Mathf.Lerp(1.28f, 1.06f, blend));
            bloom.intensity.Override(Mathf.Lerp(0.38f, 1.35f, blend));
        }

        if (groundGlow != null)
        {
            glowProperties.SetFloat(GroundGlowStrength, Mathf.Lerp(0.13f, 0.72f, blend));
            groundGlow.SetPropertyBlock(glowProperties);
        }
    }
}
