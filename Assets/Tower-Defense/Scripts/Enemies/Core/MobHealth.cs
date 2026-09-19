using UnityEngine;

/// <summary>
/// Damageable implementation for mobs. Awards coins on death and notifies MobCore
/// so it can despawn / return to pool.
/// </summary>
[DisallowMultipleComponent]
public class MobHealth : Damageable
{
    [Header("Reward")]
    [SerializeField] private int coinReward = 0;

    [Header("Death VFX")]
    [SerializeField] private GameObject deathExplosionPrefab;
    [SerializeField] private Vector3 deathExplosionScale = Vector3.one;
    [SerializeField] private float deathExplosionLifetime = 2f;
    [SerializeField] private float deathExplosionSoundCooldown = 0.08f;
    [SerializeField] private bool muteSkippedDeathExplosionSounds = true;

    public int CoinReward => coinReward;

    public System.Action<MobHealth> OnDeathHandled;

    private static float nextAllowedDeathExplosionSoundTime;

    protected override void OnEnable()
    {
        // Reset state first so listeners (including HealthBarManager) see full HP.
        ResetToMaxHealth();
        base.OnEnable();
    }

    protected override void OnDeath()
    {
        SpawnDeathExplosion();

        if (coinReward > 0 && PlayerWallet.Instance != null)
            PlayerWallet.Instance.AddCoins(coinReward);

        OnDeathHandled?.Invoke(this);
    }

    private void SpawnDeathExplosion()
    {
        if (deathExplosionPrefab == null)
            return;

        Vector3 position = GetExplosionPosition();
        GameObject vfx = Instantiate(deathExplosionPrefab, position, Quaternion.identity);
        vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, deathExplosionScale);
        ThrottleDeathExplosionAudio(vfx);

        if (deathExplosionLifetime > 0f)
            Destroy(vfx, deathExplosionLifetime);
    }

    private void ThrottleDeathExplosionAudio(GameObject vfx)
    {
        if (!muteSkippedDeathExplosionSounds || vfx == null || deathExplosionSoundCooldown <= 0f)
            return;

        if (Time.time >= nextAllowedDeathExplosionSoundTime)
        {
            nextAllowedDeathExplosionSoundTime = Time.time + deathExplosionSoundCooldown;
            return;
        }

        AudioSource[] sources = vfx.GetComponentsInChildren<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
            sources[i].mute = true;
    }

    private Vector3 GetExplosionPosition()
    {
        Collider col = GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : transform.position;
    }
}
