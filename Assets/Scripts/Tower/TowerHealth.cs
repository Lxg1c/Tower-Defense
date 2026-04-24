using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tower HP. When the tower dies mid-wave it isn't destroyed — its visuals and
/// combat components are disabled, and the tower waits for the next build
/// phase to be revived to full HP.
/// </summary>
public class TowerHealth : Damageable
{
    [Header("Revive")]
    [Tooltip("Root containing colliders / renderers that should be disabled while destroyed.")]
    [SerializeField] private GameObject visualRoot;
    [Tooltip("Components turned off on death and re-enabled on revive (e.g. Shooter, Tower, AutoShooter).")]
    [SerializeField] private MonoBehaviour[] combatComponents;

    [Header("Events")]
    /// <summary>Fired when the wave ends and the tower comes back. Use inherited onDied for the death reaction.</summary>
    public UnityEvent onRespawned;

    public bool IsDestroyed { get; private set; }

    protected override bool CanTakeDamage => IsAlive && !IsDestroyed;

    /// <summary>Tower stays in the scene after death and should keep its bar.</summary>
    public override bool DespawnBarOnDeath => false;

    protected override void Start()
    {
        base.Start();
        SubscribeToSpawner();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeToSpawner();
    }

    private void OnDisable() { UnsubscribeFromSpawner(); }

    private void SubscribeToSpawner()
    {
        if (WaveSpawner.Instance == null) return;
        WaveSpawner.Instance.onWaveCompleted.RemoveListener(OnWaveCompleted);
        WaveSpawner.Instance.onWaveCompleted.AddListener(OnWaveCompleted);
    }

    private void UnsubscribeFromSpawner()
    {
        if (WaveSpawner.Instance == null) return;
        WaveSpawner.Instance.onWaveCompleted.RemoveListener(OnWaveCompleted);
    }

    protected override void OnDeath()
    {
        IsDestroyed = true;
        SetActiveParts(false);
    }

    private void OnWaveCompleted(int waveIndex, int reward)
    {
        if (!IsDestroyed) return;
        Revive();
    }

    private void Revive()
    {
        IsDestroyed = false;
        RestoreHealth(MaxHealth);  // heals past 0 without firing OnDeath
        SetActiveParts(true);
        onRespawned?.Invoke();
    }

    private void SetActiveParts(bool on)
    {
        if (visualRoot != null) visualRoot.SetActive(on);
        if (combatComponents != null)
        {
            foreach (var c in combatComponents)
                if (c != null) c.enabled = on;
        }
    }
}
