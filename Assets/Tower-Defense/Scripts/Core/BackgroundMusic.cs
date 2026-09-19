using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance { get; private set; }

    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField] private bool playOnAwake = true;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        ConfigureSource();
    }

    private void Start()
    {
        if (playOnAwake)
            Play();
    }

    private void OnValidate()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            ConfigureSource();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Play()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ConfigureSource();

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    public void Stop()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    private void ConfigureSource()
    {
        audioSource.clip = musicClip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }
}
