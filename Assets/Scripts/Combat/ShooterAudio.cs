using UnityEngine;

[DisallowMultipleComponent]
public class ShooterAudio : MonoBehaviour
{
    [SerializeField] private Shooter shooter;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool skipIfAlreadyPlaying = true;

    private void Awake()
    {
        if (shooter == null)
            shooter = GetComponent<Shooter>();
        if (shooter == null)
            shooter = GetComponentInChildren<Shooter>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (shooter != null)
            shooter.onFired.AddListener(PlayShot);
    }

    private void OnDisable()
    {
        if (shooter != null)
            shooter.onFired.RemoveListener(PlayShot);
    }

    private void PlayShot()
    {
        if (audioSource == null || audioSource.clip == null)
            return;

        if (skipIfAlreadyPlaying && audioSource.isPlaying)
            return;

        audioSource.Play();
    }
}
