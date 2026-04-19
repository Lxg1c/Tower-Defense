using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMover : MonoBehaviour
{
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float rotateSpeed    = 10f;

    private NavMeshAgent  _agent;
    private EnemyBehavior _behavior;
    private Base          _targetBase;
    private float         _repathTimer;

    private void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _behavior = GetComponent<EnemyBehavior>();

        // Stop just inside attack range — behavior handles the actual hit.
        if (_behavior != null)
            _agent.stoppingDistance = _behavior.AttackRange * 0.9f;
    }

    public void Init(Vector3 spawnPosition, Base targetBase)
    {
        _targetBase = targetBase;

        if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            _agent.Warp(hit.position);
        else
            _agent.Warp(spawnPosition);

        _behavior?.SetFinalTarget(targetBase);
        UpdateDestination();
    }

    private void Update()
    {
        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            UpdateDestination();
            _repathTimer = repathInterval;
        }

        FaceDestination();
    }

    private void UpdateDestination()
    {
        Transform overrideTarget = _behavior?.GetOverrideTarget();

        Vector3 dest = overrideTarget != null
                     ? overrideTarget.position
                     : _targetBase != null
                       ? _targetBase.transform.position
                       : transform.position;

        _agent.isStopped = false;
        _agent.SetDestination(dest);
    }

    private void FaceDestination()
    {
        Vector3 dir = _agent.velocity;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            rotateSpeed * Time.deltaTime);
    }
}
