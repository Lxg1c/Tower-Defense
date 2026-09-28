using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class MobSlow : MonoBehaviour
{
    private NavMeshAgent agent;
    private FlyingNav flyingNav;
    private float normalAgentSpeed;
    private float expiresAt;

    public float Multiplier { get; private set; } = 1f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        flyingNav = GetComponent<FlyingNav>();
        if (agent != null) normalAgentSpeed = agent.speed;
    }

    public void Apply(float multiplier, float duration)
    {
        if (duration <= 0f || multiplier >= 1f) return;
        Multiplier = Mathf.Min(Multiplier, Mathf.Clamp(multiplier, 0.1f, 1f));
        expiresAt = Mathf.Max(expiresAt, Time.time + duration);
        if (agent != null && agent.enabled) agent.speed = normalAgentSpeed * Multiplier;
        if (flyingNav != null) flyingNav.SpeedMultiplier = Multiplier;
    }

    private void Update()
    {
        if (Multiplier < 1f && Time.time >= expiresAt) Restore();
    }

    private void OnDisable() => Restore();

    private void Restore()
    {
        Multiplier = 1f;
        expiresAt = 0f;
        if (agent != null) agent.speed = normalAgentSpeed;
        if (flyingNav != null) flyingNav.SpeedMultiplier = 1f;
    }
}
