using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : Damageable
{
    [Header("Respawn")]
    [SerializeField] private float respawnTime = 5f;

    /// <summary>True while the player is dead and regenerating.</summary>
    public bool  IsGhost         { get; private set; }

    /// <summary>0 → 1 regen progress. Use this to drive a UI timer.</summary>
    public float RespawnProgress { get; private set; }

    public UnityEvent onGhostEntered;   // wire up: make transparent, disable shooter…
    public UnityEvent onRespawned;      // wire up: restore visuals, re-enable shooter…

    // Block all damage while ghost (health is 0 but we don't want double-death).
    protected override bool CanTakeDamage => IsAlive && !IsGhost;

    // Player respawns — keep the health bar so it acts as a respawn timer.
    public override bool DespawnBarOnDeath => false;

    protected override void OnDeath()
    {
        IsGhost         = true;
        RespawnProgress = 0f;
        onGhostEntered?.Invoke();
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        float elapsed       = 0f;
        float healPerSecond = MaxHealth / respawnTime;

        while (elapsed < respawnTime)
        {
            elapsed         += Time.deltaTime;
            RespawnProgress  = elapsed / respawnTime;
            RestoreHealth(healPerSecond * Time.deltaTime);
            yield return null;
        }

        // Guarantee full health
        RestoreHealth(MaxHealth);
        IsGhost         = false;
        RespawnProgress = 1f;
        onRespawned?.Invoke();
    }
}
