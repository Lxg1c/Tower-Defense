using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tower HP. When the tower dies mid-wave it stays in the scene with its death
/// animation playing — the model is NOT hidden. Only the health bar is removed
/// and combat components are disabled. The wave-complete event revives it.
/// </summary>
public class TowerHealth : Damageable
{
    [Header("Revive")]
    [Tooltip("Optional. If empty, EVERY MonoBehaviour on this GameObject (except TowerHealth itself) is auto-disabled on death and re-enabled on revive. " +
             "Fill manually only if you need finer control.")]
    [SerializeField] private MonoBehaviour[] combatComponents;

    [Header("Death animation")]
    [Tooltip("Animator bool parameter set to true on death and false on revive. Leave empty to skip.")]
    [SerializeField] private string isDeadParam = "IsDead";

    [Header("Destroyed particles")]
    [Tooltip("One-shot explosion spawned immediately when the tower breaks.")]
    [SerializeField] private GameObject deathExplosionPrefab;
    [SerializeField] private Vector3 deathExplosionScale = Vector3.one;
    [SerializeField] private float deathExplosionLifetime = 2f;
    [Tooltip("Delay before broken tower particles start after the explosion.")]
    [SerializeField] private float destroyedParticlesDelay = 0.35f;
    [Tooltip("Optional root for broken tower particles. If empty, TowerHealth searches children with names like DeathParticles, DestroyedParticles, BrokenParticles, Smoke or Fire.")]
    [SerializeField] private GameObject destroyedParticlesRoot;
    [SerializeField] private bool hideDestroyedParticlesOnRevive = true;

    [Header("Events")]
    /// <summary>Fired when the wave ends and the tower comes back. Use inherited onDied for the death reaction.</summary>
    public UnityEvent onRespawned;

    public bool IsDestroyed { get; private set; }

    protected override bool CanTakeDamage => IsAlive && !IsDestroyed;

    private Animator           animator;
    private MonoBehaviour[]    autoCombat;   // cached when no explicit combatComponents are set
    private ParticleSystem[]   destroyedParticles;
    private Coroutine          destroyedParticlesRoutine;

    /// <summary>
    /// Tower keeps its model visible (death animation plays) but the health bar is
    /// removed on death and brought back on revive.
    /// </summary>
    public override bool DespawnBarOnDeath => true;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponentInChildren<Animator>();
        ResolveDestroyedParticles();
        SetDestroyedParticles(false);
    }

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

    protected override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeFromSpawner();
    }

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
        // Combat components off immediately — dead towers must not shoot.
        DisableCombat();
        SetIsDeadParam(true);
        SpawnDeathExplosion();
        StartDestroyedParticles();
        // Health bar is removed automatically by HealthBarManager because
        // DespawnBarOnDeath returns true. The visual model stays so the
        // death animation can play in place.
    }

    private void OnWaveCompleted(int waveIndex, int reward)
    {
        if (IsDestroyed)
        {
            Revive();
            return;
        }
        // Alive but possibly damaged → top up HP to full between waves.
        if (CurrentHealth < MaxHealth)
            ResetToMaxHealth();
    }

    private void Revive()
    {
        IsDestroyed = false;
        RestoreHealth(MaxHealth);  // heals past 0 without firing OnDeath
        EnableCombat();
        SetIsDeadParam(false);
        if (hideDestroyedParticlesOnRevive)
            SetDestroyedParticles(false);
        StopDestroyedParticlesRoutine();
        // Re-create the health bar that was removed on death.
        if (HealthBarManager.Instance != null)
            HealthBarManager.Instance.Register(this);
        onRespawned?.Invoke();
    }

    private void SetIsDeadParam(bool dead)
    {
        if (animator == null || string.IsNullOrEmpty(isDeadParam)) return;
        animator.SetBool(isDeadParam, dead);
    }

    private void ResolveDestroyedParticles()
    {
        if (destroyedParticlesRoot == null)
            destroyedParticlesRoot = FindDestroyedParticlesRoot();

        destroyedParticles = destroyedParticlesRoot != null
            ? destroyedParticlesRoot.GetComponentsInChildren<ParticleSystem>(true)
            : System.Array.Empty<ParticleSystem>();
    }

    private GameObject FindDestroyedParticlesRoot()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == transform)
                continue;

            string childName = child.name.ToLowerInvariant();
            bool looksLikeDestroyedParticles = childName.Contains("death")
                || childName.Contains("destroy")
                || childName.Contains("broken")
                || childName.Contains("smoke")
                || childName.Contains("fire");

            if (looksLikeDestroyedParticles && child.GetComponentInChildren<ParticleSystem>(true) != null)
                return child.gameObject;
        }

        return null;
    }

    private void SetDestroyedParticles(bool on)
    {
        if (destroyedParticlesRoot != null)
            destroyedParticlesRoot.SetActive(on);

        if (destroyedParticles == null)
            return;

        for (int i = 0; i < destroyedParticles.Length; i++)
        {
            ParticleSystem particles = destroyedParticles[i];
            if (particles == null)
                continue;

            if (on)
                particles.Play(true);
            else
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void SpawnDeathExplosion()
    {
        if (deathExplosionPrefab == null)
            return;

        Vector3 position = GetDeathVfxPosition();
        GameObject vfx = Instantiate(deathExplosionPrefab, position, Quaternion.identity);
        vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, deathExplosionScale);

        if (deathExplosionLifetime > 0f)
            Destroy(vfx, deathExplosionLifetime);
    }

    private Vector3 GetDeathVfxPosition()
    {
        Collider col = GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : transform.position;
    }

    private void StartDestroyedParticles()
    {
        StopDestroyedParticlesRoutine();

        if (destroyedParticlesDelay <= 0f)
        {
            SetDestroyedParticles(true);
            return;
        }

        destroyedParticlesRoutine = StartCoroutine(ShowDestroyedParticlesAfterDelay());
    }

    private IEnumerator ShowDestroyedParticlesAfterDelay()
    {
        yield return new WaitForSeconds(destroyedParticlesDelay);
        destroyedParticlesRoutine = null;

        if (IsDestroyed)
            SetDestroyedParticles(true);
    }

    private void StopDestroyedParticlesRoutine()
    {
        if (destroyedParticlesRoutine == null)
            return;

        StopCoroutine(destroyedParticlesRoutine);
        destroyedParticlesRoutine = null;
    }

    private void EnableCombat()  => SetCombatEnabled(true);
    private void DisableCombat() => SetCombatEnabled(false);

    private void SetCombatEnabled(bool on)
    {
        var list = ResolveCombatComponents();
        foreach (var c in list)
            if (c != null) c.enabled = on;
    }

    private MonoBehaviour[] ResolveCombatComponents()
    {
        if (combatComponents != null && combatComponents.Length > 0)
            return combatComponents;

        if (autoCombat == null)
        {
            // Cache every MonoBehaviour on the same GameObject except this TowerHealth.
            var all = GetComponents<MonoBehaviour>();
            int count = 0;
            for (int i = 0; i < all.Length; i++) if (all[i] != this) count++;
            autoCombat = new MonoBehaviour[count];
            int j = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != this) autoCombat[j++] = all[i];
        }
        return autoCombat;
    }

}
