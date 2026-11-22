using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    private const float FadeSeconds = 1.1f;

    public static BackgroundMusic Instance { get; private set; }

    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip combatClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float combatVolume = 0.28f;
    [SerializeField] private bool playOnAwake = true;

    private AudioSource calmSource;
    private AudioSource combatSource;
    private WaveSpawner spawner;
    private bool playing;
    private bool inCombat;
    private float combatMix;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        calmSource = GetComponent<AudioSource>();
        combatSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(calmSource, musicClip);
        ConfigureSource(combatSource, combatClip);
    }

    private void Start()
    {
        if (playOnAwake)
            Play();
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        GameAudioSettings.Changed += ApplyVolume;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        ApplyVolume();
    }

    private void OnDisable()
    {
        GameAudioSettings.Changed -= ApplyVolume;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindSpawner();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        UnbindSpawner();
        spawner = WaveSpawner.Instance;
        if (spawner != null && spawner.gameObject.scene == scene)
        {
            spawner.onBuildPhaseStarted.AddListener(OnBuildStarted);
            spawner.onCombatPhaseStarted.AddListener(OnCombatStarted);
            spawner.onAllWavesCompleted.AddListener(OnCombatEnded);
            spawner.onDefeated.AddListener(OnCombatEnded);
        }
        else
        {
            spawner = null;
        }
        SetCombat(spawner != null && spawner.IsCombatPhase);
    }

    private void UnbindSpawner()
    {
        if (spawner == null) return;
        spawner.onBuildPhaseStarted.RemoveListener(OnBuildStarted);
        spawner.onCombatPhaseStarted.RemoveListener(OnCombatStarted);
        spawner.onAllWavesCompleted.RemoveListener(OnCombatEnded);
        spawner.onDefeated.RemoveListener(OnCombatEnded);
        spawner = null;
    }

    private void OnBuildStarted(int wave, int total, int reward) => SetCombat(false);

    private void OnCombatStarted(int wave, int total, int reward) => SetCombat(true);

    private void OnCombatEnded() => SetCombat(false);

    private void SetCombat(bool active)
    {
        bool useCombat = active && combatClip != null;
        if (inCombat == useCombat) return;
        inCombat = useCombat;
        if (!playing) return;

        PlaySource(inCombat ? combatSource : calmSource);
        if (combatMix == (inCombat ? 1f : 0f))
            (inCombat ? calmSource : combatSource).Stop();
    }

    private void Update()
    {
        if (!playing) return;
        float target = inCombat ? 1f : 0f;
        if (combatMix == target) return;

        combatMix = Mathf.MoveTowards(combatMix, target, Time.unscaledDeltaTime / FadeSeconds);
        ApplyVolume();
        if (combatMix == target)
            (inCombat ? calmSource : combatSource).Stop();
    }

    private void ApplyVolume()
    {
        float gain = GameAudioSettings.Master * GameAudioSettings.Music;
        calmSource.volume = gain * volume * (1f - combatMix);
        combatSource.volume = gain * combatVolume * combatMix;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Play()
    {
        if (Instance != this || playing) return;
        playing = true;
        combatMix = inCombat ? 1f : 0f;
        ApplyVolume();
        PlaySource(inCombat ? combatSource : calmSource);
    }

    public void Stop()
    {
        if (Instance != this) return;
        playing = false;
        calmSource.Stop();
        combatSource.Stop();
    }

    private static void PlaySource(AudioSource source)
    {
        if (source.clip != null && !source.isPlaying)
            source.Play();
    }

    private static void ConfigureSource(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.ignoreListenerVolume = true;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }
}
