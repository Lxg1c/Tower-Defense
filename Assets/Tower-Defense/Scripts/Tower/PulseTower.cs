using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Tower that periodically emits a circular pulse around itself: damages
/// every Damageable in <see cref="range"/> and applies a fading slow to mobs.
///
/// Pure logic — animator is driven separately by <see cref="PulseTowerAnimator"/>
/// via <see cref="onAttackWindup"/> and <see cref="onAttack"/>.
/// </summary>
[DisallowMultipleComponent]
public class PulseTower : MonoBehaviour
{
    [Header("Pulse")]
    [SerializeField] private float damage         = 15f;
    [SerializeField] private float range          = 4f;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField, Min(0f)] private float attackWindup;
    [SerializeField] private LayerMask targetMask = ~0;
    [Tooltip("Tag of targets the pulse should NEVER affect (e.g. \"Flying\"). Leave empty to hit everything in the mask.")]
    [SerializeField] private string ignoreTag = "Flying";

    [Header("Slow")]
    [Tooltip("Enemy speed multiplier immediately after a pulse. 0.4 = 60% slower.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float slowMultiplier = 0.4f;
    [Tooltip("How long the slow lasts before fading back to normal speed.")]
    [SerializeField] private float slowDuration = 2f;

    [Header("Events")]
    public UnityEvent onAttackWindup = new();

    /// <summary>Fired once each time a pulse goes off. Wire animation / SFX / VFX here.</summary>
    public UnityEvent onAttack;

    /// <summary>True while at least one valid target sits inside <see cref="range"/>.</summary>
    public bool HasTarget { get; private set; }
    public float Damage         { get => damage;         set => damage         = value; }
    public float Range          { get => range;          set => range          = Mathf.Max(0f, value); }
    public float AttackInterval { get => attackInterval; set => attackInterval = Mathf.Max(0.01f, value); }
    public float SlowMultiplier { get => slowMultiplier; set => slowMultiplier = Mathf.Clamp(value, 0.01f, 1f); }
    public float SlowDuration   { get => slowDuration;   set => slowDuration   = Mathf.Max(0.01f, value); }

    private float    nextAttackTime;
    private float    impactTime = -1f;
    private static Collider[] buffer = new Collider[32];
    private readonly HashSet<Damageable> hitThisPulse = new();

    private int FindCollidersInRange()
    {
        while (true)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, range, buffer,
                targetMask, QueryTriggerInteraction.Collide);
            if (count < buffer.Length) return count;
            Array.Resize(ref buffer, buffer.Length * 2);
        }
    }

    private void OnDisable()
    {
        HasTarget = false;
        impactTime = -1f;
    }

    private void Update()
    {
        HasTarget = HasValidTargetInRange();
        if (impactTime >= 0f)
        {
            if (Time.time < impactTime) return;
            impactTime = -1f;
            Pulse();
            return;
        }

        if (!HasTarget || Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + Mathf.Max(attackWindup, attackInterval);
        onAttackWindup?.Invoke();
        if (!isActiveAndEnabled) return;
        if (attackWindup > 0f)
            impactTime = Time.time + attackWindup;
        else
            Pulse();
    }

    /// <summary>True if there's at least one Damageable in range that is NOT tagged ignoreTag.</summary>
    private bool HasValidTargetInRange()
    {
        int count = FindCollidersInRange();
        for (int i = 0; i < count; i++)
        {
            if (IsIgnoredByTag(buffer[i].gameObject)) continue;
            var d = buffer[i].GetComponentInParent<Damageable>();
            if (d != null && d.IsTargetable) return true;
        }
        return false;
    }

    private void Pulse()
    {
        int count = FindCollidersInRange();
        hitThisPulse.Clear();
        for (int i = 0; i < count; i++)
        {
            if (IsIgnoredByTag(buffer[i].gameObject)) continue;

            var d = buffer[i].GetComponentInParent<Damageable>();
            if (d == null || !d.IsTargetable || !hitThisPulse.Add(d)) continue;

            d.TakeDamage(damage);
            ApplySlow(d.transform);
        }

        onAttack?.Invoke();
    }

    private bool IsIgnoredByTag(GameObject go)
    {
        if (string.IsNullOrEmpty(ignoreTag)) return false;
        // CompareTag works on the collider's GO; flying mob's collider should carry the tag too.
        return go.CompareTag(ignoreTag);
    }

    private void ApplySlow(Transform victim)
    {
        if (victim == null)
            return;

        if (!victim.TryGetComponent<MobCore>(out var mob))
            return;

        if (!mob.TryGetComponent<MobSlowEffect>(out var slow))
            slow = mob.gameObject.AddComponent<MobSlowEffect>();

        slow.Apply(slowMultiplier, slowDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, range);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
internal class MobSlowEffect : MonoBehaviour
{
    private NavMeshAgent agent;
    private float baseSpeed;
    private float duration;
    private float timer;
    private float slowMultiplier = 1f;
    private bool active;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed;
    }

    private void OnEnable()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        baseSpeed = agent.speed;
        ResetEffect();
    }

    private void OnDisable()
    {
        ResetEffect();
    }

    private void Update()
    {
        if (!active || agent == null)
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ResetEffect();
            return;
        }

        float normalizedAge = 1f - Mathf.Clamp01(timer / duration);
        float currentMultiplier = Mathf.Lerp(slowMultiplier, 1f, normalizedAge);
        agent.speed = baseSpeed * currentMultiplier;
    }

    public void Apply(float multiplier, float effectDuration)
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (!active)
            baseSpeed = agent.speed;

        slowMultiplier = Mathf.Clamp(multiplier, 0.01f, 1f);
        duration = Mathf.Max(0.01f, effectDuration);
        timer = duration;
        active = true;
        agent.speed = baseSpeed * slowMultiplier;
    }

    private void ResetEffect()
    {
        if (agent != null)
            agent.speed = baseSpeed;

        active = false;
        timer = 0f;
        duration = 0f;
        slowMultiplier = 1f;
    }
}
