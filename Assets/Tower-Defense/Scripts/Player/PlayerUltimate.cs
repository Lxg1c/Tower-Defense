using System.Collections;
using UnityEngine;

/// <summary>
/// Charge-and-release ultimate.
///
/// Press (BeginCharge) → small <see cref="startDelay"/> → spawn a charge orb at
/// <see cref="firePoint"/>. While held, the orb scales from <see cref="minScale"/>
/// to <see cref="maxScale"/> over <see cref="maxChargeTime"/>; damage scales the
/// same way. Release → orb detaches and flies forward as a projectile that
/// pierces through targets (first target = full damage, the rest = percentage).
/// </summary>
[DisallowMultipleComponent]
public class PlayerUltimate : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Origin of the charge orb and its launch point.")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private bool logInput = true;

    private PlayerAnimator playerAnimator;
    private Shooter        normalShooter;
    private DetectionZone  detection;
    private Damageable     ownerDamageable;
    private PlayerHealth   playerHealth;

    [Header("Charge orb")]
    [SerializeField] private GameObject chargeOrbPrefab;
    [Tooltip("Time to fully charge after the orb appears.")]
    [SerializeField] private float maxChargeTime = 5f;
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 5f;

    [Header("Projectile")]
    [Tooltip("Speed of the launched orb (units/second).")]
    [SerializeField] private float projectileSpeed = 30f;
    
    [Tooltip("How long the orb keeps flying after launch (seconds).")]
    [SerializeField] private float projectileLifetime = 4f;
    [Tooltip("Hit radius of the orb's overlap check. Match this roughly to the visual size at full charge.")]
    [SerializeField] private float hitRadius = 1f;
    [Tooltip("Layers the orb damages (enemies, base, towers).")]
    [SerializeField] private LayerMask hitMask = ~0;
    [Tooltip("Layers that block the orb and explosion damage.")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("VFX spawned at the point of the orb's first direct contact with a target.")]
    [SerializeField] private GameObject explosionPrefab;
    [Tooltip("Lifetime of the explosion VFX (seconds).")]
    [SerializeField] private float explosionLifetime = 1.5f;
    [SerializeField] private float ultimateCooldown = 5f;

    [Header("Damage")]
    [SerializeField] private float minDamage = 30f;
    [SerializeField] private float maxDamage = 250f;

    [Header("Cooldown")]
    [Tooltip("Minimum cooldown after every launched orb, even for a tap.")]
    [SerializeField] private float cooldownSeconds = 3f;
    [Tooltip("Cooldown after a fully charged orb.")]
    [SerializeField] private float maxCooldownSeconds = 5f;

    [Header("Audio")]
    [SerializeField] private AudioSource chargeAudioSource;
    [SerializeField] private AudioSource fireAudioSource;
    [SerializeField] private bool loopChargeAudio = true;
    [SerializeField, Range(0f, 1f)] private float minChargeForFireAudio = 0.05f;

    public bool IsCharging { get; private set; }
    public bool IsOnCooldown => Time.time < nextReadyTime;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);
    public bool CanReceiveInput => isActiveAndEnabled
        && (ownerDamageable == null || ownerDamageable.IsAlive)
        && (playerHealth == null || !playerHealth.IsGhost)
        && !IsOnCooldown;
    public float ChargeProgress { get; private set; }
    public float CooldownProgress
    {
        get
        {
            return lastCooldownDuration > 0f
                ? lastChargeProgress * Mathf.Clamp01(CooldownRemaining / lastCooldownDuration)
                : 0f;
        }
    }

    private bool released;
    private float nextReadyTime;
    private float lastCooldownDuration;
    private float lastChargeProgress;
    private Coroutine routine;
    private GameObject activeOrb;

    private void Awake()
    {
        if (firePoint == null) firePoint = transform;
        playerAnimator  = GetComponent<PlayerAnimator>();
        normalShooter   = GetComponentInChildren<Shooter>();
        detection       = GetComponentInChildren<DetectionZone>();
        ownerDamageable = GetComponent<Damageable>();
        playerHealth    = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (ownerDamageable != null)
            ownerDamageable.onDied.AddListener(CancelOnDeath);
        
        if (normalShooter != null) normalShooter.SuppressFire = false;
    }


    public void BeginCharge()
    {
        if (!isActiveAndEnabled || IsCharging || IsOnCooldown
            || ownerDamageable != null && !ownerDamageable.IsAlive 
            || playerHealth != null && playerHealth.IsGhost) return;
        
        IsCharging = true;
        released = false;
        ChargeProgress = 0f;
        PlayChargeAudio();
        SetNormalShooterEnabled(false);
        routine = StartCoroutine(Run());
    }

    public void Release()
    {
        if (!IsCharging) return;
        released = true;
    } 
    
    void CancelOnDeath()
    {
        if (routine != null) StopCoroutine(routine);
        CleanupCancelled();
    }

    private IEnumerator Run()
    {
        if (chargeOrbPrefab != null)
        {
            activeOrb = Instantiate(chargeOrbPrefab, firePoint.position, firePoint.rotation, firePoint);
            activeOrb.transform.localScale = Vector3.one * minScale;
        }
        
        float charge = 0f;
        while (charge < maxChargeTime && !released)
        {
            charge += Time.deltaTime;
            ChargeProgress = Mathf.Clamp01(charge / maxChargeTime);
            float s = Mathf.Lerp(minScale, maxScale, ChargeProgress);
            if (activeOrb != null) activeOrb.transform.localScale = Vector3.one * s;

            yield return null;
        }

        float chargeNorm = Mathf.Clamp01(charge / maxChargeTime);
        
        if (playerAnimator != null) playerAnimator.TriggerUltimate();
        Launch(chargeNorm);

        lastCooldownDuration = Mathf.Lerp(
            cooldownSeconds, Mathf.Max(cooldownSeconds, maxCooldownSeconds), chargeNorm);
        lastChargeProgress = chargeNorm;
        nextReadyTime = Time.time + lastCooldownDuration + ultimateCooldown;
        IsCharging = false;
        ChargeProgress = 0f;
        routine = null;
        SetNormalShooterEnabled(true);
    }

    private void CleanupCancelled()
    {
        if (activeOrb != null) Destroy(activeOrb);
        activeOrb = null;
        IsCharging = false;
        ChargeProgress = 0f;
        routine = null;
        StopChargeAudio();
        SetNormalShooterEnabled(true);
    }

    private void SetNormalShooterEnabled(bool on)
    {
        if (normalShooter != null) normalShooter.SuppressFire = !on;
    }

    private void Launch(float chargeNorm)
{
    float damage = Mathf.Lerp(minDamage, maxDamage, chargeNorm);

    Vector3 origin = firePoint.position;
    Vector3 dir;
    
    if (normalShooter != null && normalShooter.HasTarget && normalShooter.CurrentTarget != null)
    {
        Vector3 aimPoint = GetAimPoint(normalShooter.CurrentTarget);
        Vector3 toTarget = aimPoint - origin;
        dir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : GetFallbackLaunchDirection();
    }
    else
    {
        dir = GetFallbackLaunchDirection();
    }

    if (activeOrb == null)
    {
        StopChargeAudio();
        return;
    }

    StopChargeAudio();
    PlayFireAudio(chargeNorm);
    
    activeOrb.transform.SetParent(null, true);
    activeOrb.transform.rotation = Quaternion.LookRotation(dir);

    var projectile = activeOrb.GetComponent<ChargeOrbProjectile>();
    if (projectile == null) projectile = activeOrb.AddComponent<ChargeOrbProjectile>();

    projectile.Init(
        direction: dir,
        speed: projectileSpeed,
        fullDamage: damage,       
        lifetime: projectileLifetime,
        explosionRadius: hitRadius,
        hitMask: hitMask,
        obstacleMask: obstacleMask,
        owner: ownerDamageable,
        explosionPrefab: explosionPrefab,
        explosionLifetime: explosionLifetime);

        activeOrb = null;
    }

    private void PlayChargeAudio()
    {
        if (chargeAudioSource == null) return;

        chargeAudioSource.loop = loopChargeAudio;
        chargeAudioSource.Stop();
        chargeAudioSource.Play();
    }

    private void StopChargeAudio()
    {
        if (chargeAudioSource == null)
            return;

        chargeAudioSource.Stop();
    }

    private void PlayFireAudio(float chargeNorm)
    {
        if (fireAudioSource == null || fireAudioSource.clip == null) return;
        if (chargeNorm < minChargeForFireAudio) return;

        fireAudioSource.Stop();
        fireAudioSource.PlayOneShot(fireAudioSource.clip);
    }

    private Vector3 GetAimPoint(Damageable target)
    {
        if (target == null)
            return transform.position + GetFallbackLaunchDirection();

        Collider col = target.GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : target.transform.position;
    }

    private Vector3 GetFallbackLaunchDirection()
    {
        Transform origin = firePoint != null ? firePoint : transform;
        return origin.forward.sqrMagnitude > 0.001f ? origin.forward.normalized : transform.forward;
    }

    private void OnDisable()
    {
        if (ownerDamageable != null)
            ownerDamageable.onDied.RemoveListener(CancelOnDeath);
        if (routine != null) StopCoroutine(routine);
        CleanupCancelled();
    }

    private void OnDrawGizmosSelected()
    {
        Transform o = firePoint != null ? firePoint : transform;
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(o.position, transform.forward * (projectileSpeed * projectileLifetime));
    }
}
