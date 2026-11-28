using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class GroundTrapPlacement : MonoBehaviour
{
    [SerializeField] private GroundTrap prefab;
    [SerializeField, Min(1)] private int cost = 2;

    private readonly List<GroundTrap> placedTraps = new();
    private bool placing;

    public int Cost => cost;

    public bool TryPlace(PlayerHealth hero, PlayerWallet wallet, WaveSpawner spawner)
    {
        if (placing || prefab == null || !spawner.IsBuildPhase || !hero.IsAlive || hero.IsGhost ||
            wallet.Coins < cost)
            return false;

        Vector3 origin = hero.transform.position + Vector3.up * 0.2f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit ground, 2.6f,
                LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore) ||
            ground.normal.y < 0.8f || !NavMesh.SamplePosition(ground.point, out _, 0.45f, NavMesh.AllAreas))
            return false;

        placedTraps.RemoveAll(trap => trap == null);
        foreach (GroundTrap trap in placedTraps)
            if ((trap.transform.position - ground.point).sqrMagnitude < 0.9f * 0.9f)
                return false;

        placing = true;
        try
        {
            if (!wallet.TrySpend(cost))
                return false;

            placedTraps.Add(Instantiate(prefab, ground.point, Quaternion.identity, transform));
            return true;
        }
        finally
        {
            placing = false;
        }
    }
}
