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

    [Header("Charge orb")]
    [SerializeField] private GameObject chargeOrbPrefab;
    [Tooltip("Time to fully charge after the orb appears.")]
    [SerializeField] private float maxChargeTime = 5f;
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 5f;

    [Header("Projectile")]
    [Tooltip("Speed of the launched orb (units/second).")]
    [SerializeField] private float projectileSpeed = 10f;
    
    [Tooltip("How long the orb keeps flying after launch (seconds).")]
    [SerializeField] private float projectileLifetime = 4f;
    [Tooltip("Hit radius of the orb's overlap check. Match this roughly to the visual size at full charge.")]
    [SerializeField] private float hitRadius = 1f;
    [Tooltip("Layers the orb damages (enemies, base, towers).")]
    [SerializeField] private LayerMask hitMask = ~0;
    [Tooltip("VFX spawned at the point of the orb's first direct contact with a target.")]
    [SerializeField] private GameObject explosionPrefab;
    [Tooltip("Lifetime of the explosion VFX (seconds).")]
    [SerializeField] private float explosionLifetime = 1.5f;

    // Aim assist during charge is handled by the normal Shooter (it keeps tracking
    // targets and rotates the player via PlayerMovement); we only suppress firing.

    [Header("Damage")]
    [SerializeField] private float minDamage = 30f;
    [SerializeField] private float maxDamage = 250f;

    [Header("Cooldown")]
    [Tooltip("Cooldown for a fully charged ultimate. Scales linearly with how long the orb was charged.")]
    [SerializeField] private float maxCooldown = 5f;
    [Tooltip("Minimum cooldown floor even for the shortest tap.")]
    [SerializeField] private float minCooldown = 0.5f;

    public bool IsCharging { get; private set; }
    public bool IsOnCooldown => Time.time < nextReadyTime;
    public bool CanReceiveInput => isActiveAndEnabled
        && (ownerDamageable == null || ownerDamageable.IsAlive)
        && !IsOnCooldown;
    /// <summary>0..1 charge fill once the orb is up. Useful for UI overlays.</summary>
    public float ChargeProgress { get; private set; }
    /// <summary>
    /// Starts at the chargeNorm of the last shot and decays linearly to 0 as the cooldown ends.
    /// Lets the UI continue exactly from where the charge fill stopped. 0 when ready.
    /// </summary>
    public float CooldownProgress
    {
        get
        {
            if (lastCooldownDuration <= 0f) return 0f;
            float remaining = nextReadyTime - Time.time;
            if (remaining <= 0f) return 0f;
            return lastChargeNorm * Mathf.Clamp01(remaining / lastCooldownDuration);
        }
    }

    private bool released;
    private float nextReadyTime;
    private float lastCooldownDuration;
    private float lastChargeNorm;
    private Coroutine routine;
    private GameObject activeOrb;

    private void Awake()
    {
        if (firePoint == null) firePoint = transform;
        playerAnimator  = GetComponent<PlayerAnimator>();
        normalShooter   = GetComponentInChildren<Shooter>();
        detection       = GetComponentInChildren<DetectionZone>();
        ownerDamageable = GetComponent<Damageable>();
    }

    private void OnEnable()
    {
        Log("OnEnable");

        if (ownerDamageable != null)
            ownerDamageable.onDied.AddListener(CancelOnDeath);

        // Defensive reset — make sure normal Shooter is allowed to fire on (re)enable.
        if (normalShooter != null) normalShooter.SuppressFire = false;
    }


    public void BeginCharge()
    {
        Log($"BeginCharge requested. enabled={isActiveAndEnabled}, IsCharging={IsCharging}, IsOnCooldown={IsOnCooldown}, alive={ownerDamageable == null || ownerDamageable.IsAlive}");

        if (!isActiveAndEnabled)
        {
            Log("BeginCharge ignored: ultimate component is disabled.");
            return;
        }

        if (IsCharging || IsOnCooldown)
        {
            Log("BeginCharge ignored: charging or cooldown.");
            return;
        }

        // Don't start charging if the player is already dead.
        if (ownerDamageable != null && !ownerDamageable.IsAlive)
        {
            Log("BeginCharge ignored: owner is dead.");
            return;
        }

        IsCharging = true;
        released = false;
        ChargeProgress = 0f;
        SetNormalShooterEnabled(false);
        routine = StartCoroutine(Run());
        Log("BeginCharge accepted: charge routine started.");
    }

    public void Release()
    {
        Log($"Release requested. IsCharging={IsCharging}");

        if (!IsCharging)
        {
            Log("Release ignored: not charging.");
            return;
        }

        released = true;
        Log("Release accepted.");
    }

    /// <summary>Hook this to PlayerHealth.onGhostEntered: cancel any ongoing charge so no orb is fired post-mortem.</summary>
    public void CancelOnDeath()
    {
        if (routine != null) StopCoroutine(routine);
        CleanupCancelled();
    }

    private IEnumerator Run()
    {
        Log($"Run started. chargeOrbPrefab={(chargeOrbPrefab != null ? chargeOrbPrefab.name : "NULL")}, firePoint={(firePoint != null ? firePoint.name : "NULL")}");

        // Spawn the charge orb immediately — no pre-delay.
        if (chargeOrbPrefab != null)
        {
            activeOrb = Instantiate(chargeOrbPrefab, firePoint.position, firePoint.rotation, firePoint);
            activeOrb.transform.localScale = Vector3.one * minScale;
        }

        // Grow until release or fully charged.
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
        Log($"Run firing. chargeNorm={chargeNorm:0.00}, activeOrb={(activeOrb != null ? activeOrb.name : "NULL")}");

        // Fire animation and orb on the same frame — no extra wait.
        if (playerAnimator != null) playerAnimator.TriggerUltimate();
        Launch(chargeNorm);

        // Cooldown scales with how long the player held the charge.
        lastCooldownDuration = Mathf.Lerp(minCooldown, maxCooldown, chargeNorm);
        lastChargeNorm       = chargeNorm;
        nextReadyTime        = Time.time + lastCooldownDuration;
        IsCharging = false;
        ChargeProgress = 0f;
        routine = null;
        SetNormalShooterEnabled(true);
        Log($"Run finished. cooldown={lastCooldownDuration:0.00}s");
    }

    private void CleanupCancelled()
    {
        if (activeOrb != null) Destroy(activeOrb);
        activeOrb = null;
        IsCharging = false;
        ChargeProgress = 0f;
        routine = null;
        SetNormalShooterEnabled(true);
    }

    private void SetNormalShooterEnabled(bool on)
    {
        // Don't disable the Shooter — keep it tracking targets so the player
        // auto-aims via the same path as a normal attack. We just suppress firing.
        if (normalShooter != null) normalShooter.SuppressFire = !on;
    }

    private void Launch(float chargeNorm)
{
    float damage = Mathf.Lerp(minDamage, maxDamage, chargeNorm);

    Vector3 origin = firePoint.position;
    Vector3 dir;

    // If the Shooter currently has a locked target, snap precisely at it so the
    // orb actually connects. Otherwise fly straight along player's facing.
    if (normalShooter != null && normalShooter.HasTarget && normalShooter.CurrentTarget != null)
    {
        Vector3 toTarget = normalShooter.CurrentTarget.transform.position - origin;
        toTarget.y = 0f;
        dir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
    }
    else
    {
        dir = transform.forward;
    }

    if (activeOrb == null)
    {
        Log("Launch aborted: activeOrb is NULL.");
        return;
    }

    // Detach from firePoint and convert into a projectile.
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
        owner: ownerDamageable,
        explosionPrefab: explosionPrefab,
        explosionLifetime: explosionLifetime);

        activeOrb = null;
    }
    private void OnDisable()
    {
        Log("OnDisable");

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

    private void Log(string message)
    {
        if (logInput)
            Debug.Log($"[PlayerUltimate] {message}", this);
    }
}
