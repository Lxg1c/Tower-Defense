using UnityEngine;
using UnityEngine.AI;

/// <summary>Opens the path when a wall is destroyed and closes it on wave-end revival.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TowerHealth), typeof(BoxCollider), typeof(NavMeshObstacle))]
public sealed class WallCollision : MonoBehaviour
{
    private TowerHealth health;
    private BoxCollider body;
    private NavMeshObstacle obstacle;

    private void Awake()
    {
        health = GetComponent<TowerHealth>();
        body = GetComponent<BoxCollider>();
        obstacle = GetComponent<NavMeshObstacle>();
    }

    private void OnEnable()
    {
        health.onDied.AddListener(Sync);
        health.onRespawned.AddListener(Sync);
        Sync();
    }

    private void OnDisable()
    {
        health.onDied.RemoveListener(Sync);
        health.onRespawned.RemoveListener(Sync);
    }

    private void Sync()
    {
        bool standing = health.IsAlive;
        body.enabled = standing;
        obstacle.enabled = standing;
    }
}
