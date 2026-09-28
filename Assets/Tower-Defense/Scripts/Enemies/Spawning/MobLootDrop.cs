using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MobHealth))]
public sealed class MobLootDrop : MonoBehaviour
{
    private MobHealth health;
    private EnemyLoadout.SlowAmmoDrop drop;
    private SlowAmmoPickup pickupPrefab;

    private void Awake() => health = GetComponent<MobHealth>();

    private void OnEnable() => health.OnDeathHandled += Drop;

    private void OnDisable()
    {
        health.OnDeathHandled -= Drop;
        drop = default;
        pickupPrefab = null;
    }

    public void Configure(EnemyLoadout.SlowAmmoDrop settings, SlowAmmoPickup prefab)
    {
        drop = settings;
        pickupPrefab = prefab;
        if (drop.chance > 0f && (pickupPrefab == null || drop.durationSeconds <= 0f))
            Debug.LogError($"[MobLootDrop] {drop.enemy.name}: assign a pickup prefab and positive bonus duration.", this);
    }

    private void Drop(MobHealth _)
    {
        if (drop.enemy == null || pickupPrefab == null ||
            drop.chance <= 0f ||
            (drop.chance < 1f && Random.value >= drop.chance)) return;

        SlowAmmoPickup pickup = Instantiate(pickupPrefab,
            transform.position + Vector3.up * 0.7f, Quaternion.identity);
        pickup.Configure(drop.durationSeconds);
    }
}
