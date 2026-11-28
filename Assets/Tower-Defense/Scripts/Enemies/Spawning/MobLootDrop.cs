using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MobHealth))]
public sealed class MobLootDrop : MonoBehaviour
{
    private MobHealth health;
    private EnemyLoadout.EnemyEntry entry;
    private SlowAmmoPickup slowPrefab;
    private ShieldPickup shieldPrefab;
    private CompanionTurretPickup companionPrefab;

    private void Awake() => health = GetComponent<MobHealth>();

    private void OnEnable() => health.OnDeathHandled += Drop;

    private void OnDisable()
    {
        health.OnDeathHandled -= Drop;
        entry = default;
        slowPrefab = null;
        shieldPrefab = null;
        companionPrefab = null;
    }

    public void Configure(EnemyLoadout.EnemyEntry settings, SlowAmmoPickup slowPickup,
        ShieldPickup shieldPickup, CompanionTurretPickup companionPickup = null)
    {
        entry = settings;
        slowPrefab = slowPickup;
        shieldPrefab = shieldPickup;
        companionPrefab = companionPickup;
        if (entry.slowAmmoChance > 0f && (slowPrefab == null || entry.slowAmmoDurationSeconds <= 0f))
            Debug.LogError($"[MobLootDrop] {entry.enemy.name}: assign a slow pickup and positive duration.", this);
        if (entry.shieldChance > 0f && (shieldPrefab == null || entry.shieldDurationSeconds <= 0f))
            Debug.LogError($"[MobLootDrop] {entry.enemy.name}: assign a shield pickup and positive duration.", this);
        if (entry.companionChance > 0f && (companionPrefab == null || entry.companionDurationSeconds <= 0f))
            Debug.LogError($"[MobLootDrop] {entry.enemy.name}: assign a companion pickup and positive duration.", this);
    }

    private void Drop(MobHealth _)
    {
        if (entry.enemy == null) return;
        Vector3 position = transform.position + Vector3.up * 0.7f;
        switch (entry.PickDrop(Random.value))
        {
            case EnemyLoadout.PickupKind.SlowAmmo when slowPrefab != null:
                Instantiate(slowPrefab, position, Quaternion.identity)
                    .Configure(entry.slowAmmoDurationSeconds);
                break;
            case EnemyLoadout.PickupKind.Shield when shieldPrefab != null:
                Instantiate(shieldPrefab, position, Quaternion.identity)
                    .Configure(entry.shieldDurationSeconds);
                break;
            case EnemyLoadout.PickupKind.Companion when companionPrefab != null:
                Instantiate(companionPrefab, position, Quaternion.identity)
                    .Configure(entry.companionDurationSeconds);
                break;
        }
    }
}
