using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class BarracksBuilding : MonoBehaviour
{
    [Header("Squad")]
    [SerializeField] private AllySoldier.CombatType squadType = AllySoldier.CombatType.Melee;
    [SerializeField] private AllySoldier allyPrefab;
    [SerializeField] private AllySoldier rangedAllyPrefab;
    [Min(1)] [SerializeField] private int squadSize = 3;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform formationCenter;
    [SerializeField] private float formationSpacing = 1.2f;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float spawnInterval = 0.15f;
    [SerializeField] private GameObject orderMarkerPrefab;

    private readonly List<AllySoldier> soldiers = new();
    private readonly HashSet<int> pendingRespawns = new();
    private WaveSpawner spawner;
    private Transform player;
    private bool combatActive;
    private bool spawning;
    private bool hasGuardPosition;
    private Vector3 guardPosition;
    private GameObject orderMarker;

    public bool HasGuardPosition => hasGuardPosition;
    public Vector3 GuardPosition => guardPosition;
    public GameObject OrderMarkerPrefab => orderMarkerPrefab;
    public Vector3 SelectionMarkerPosition => spawnPoint.position + Vector3.up * 0.08f;

    private void OnEnable()
    {
        ResolvePlayer();
        AllyCommand.OnFollowPlayerChanged += OnFollowPlayerChanged;
        BindSpawner();
        if (spawner == null)
            Invoke(nameof(BindSpawner), 0.1f);

        EnsureSquad();
        if (hasGuardPosition)
            ShowOrderMarker();
        ApplyModeToSquad();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        spawning = false;
        pendingRespawns.Clear();
        AllyCommand.OnFollowPlayerChanged -= OnFollowPlayerChanged;
        if (orderMarker != null)
        {
            Destroy(orderMarker);
            orderMarker = null;
        }

        if (spawner != null)
        {
            spawner.onBuildPhaseStarted.RemoveListener(OnBuildPhaseStarted);
            spawner.onCombatPhaseStarted.RemoveListener(OnCombatPhaseStarted);
        }
    }

    private void Start()
    {
        ResolvePlayer();
        EnsureSquad();
        ApplyModeToSquad();
    }

    private void BindSpawner()
    {
        if (spawner != null)
        {
            spawner.onBuildPhaseStarted.RemoveListener(OnBuildPhaseStarted);
            spawner.onCombatPhaseStarted.RemoveListener(OnCombatPhaseStarted);
        }

        spawner = WaveSpawner.Instance;
        if (spawner == null)
            return;

        spawner.onBuildPhaseStarted.AddListener(OnBuildPhaseStarted);
        spawner.onCombatPhaseStarted.AddListener(OnCombatPhaseStarted);
        combatActive = spawner.IsCombatPhase;
    }

    private void OnBuildPhaseStarted(int nextWaveIndex, int totalWaves, int reward)
    {
        combatActive = false;
        EnsureSquad();
        ApplyModeToSquad();
    }

    private void OnCombatPhaseStarted(int waveIndex, int totalWaves, int reward)
    {
        combatActive = true;
        EnsureSquad();
        ApplyModeToSquad();
    }

    private void OnFollowPlayerChanged(bool followPlayer)
    {
        ApplyModeToSquad();
    }

    public bool TryAssignGuardPosition(Vector3 requestedPosition)
    {
        if (!NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            return false;

        NavMeshPath path = new();
        Vector3 origin = GetFormationCenter();
        foreach (AllySoldier soldier in soldiers)
        {
            if (soldier == null || !soldier.isActiveAndEnabled)
                continue;
            origin = soldier.transform.position;
            break;
        }
        if (!NavMesh.SamplePosition(origin, out NavMeshHit start, 2f, NavMesh.AllAreas)
            || !NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path)
            || path.status != NavMeshPathStatus.PathComplete)
            return false;

        guardPosition = hit.position;
        hasGuardPosition = true;
        AllyCommand.SetFollowPlayer(false);
        ShowOrderMarker();
        ApplyModeToSquad();
        return true;
    }

    public void ClearGuardPosition()
    {
        hasGuardPosition = false;
        if (orderMarker != null)
        {
            Destroy(orderMarker);
            orderMarker = null;
        }
        ApplyModeToSquad();
    }

    private void ShowOrderMarker()
    {
        if (orderMarkerPrefab == null)
            return;
        if (orderMarker == null)
            orderMarker = Instantiate(orderMarkerPrefab);
        orderMarker.transform.position = guardPosition + Vector3.up * 0.08f;
    }

    private void EnsureSquad()
    {
        if (spawning)
            return;

        if (GetSelectedPrefab() == null)
        {
            Debug.LogError($"[BarracksBuilding] Assign an ally prefab for {squadType} squads.", this);
            return;
        }

        while (soldiers.Count < squadSize)
            soldiers.Add(null);

        var missing = new List<int>();
        for (int slot = 0; slot < squadSize; slot++)
            if ((soldiers[slot] == null || !soldiers[slot].isActiveAndEnabled)
                && !pendingRespawns.Contains(slot))
                missing.Add(slot);

        if (missing.Count > 0)
            StartCoroutine(SpawnMissing(missing));
    }

    private IEnumerator SpawnMissing(List<int> missing)
    {
        spawning = true;

        foreach (int slot in missing)
        {
            if (soldiers[slot] == null || !soldiers[slot].isActiveAndEnabled)
                SpawnOne(slot);

            if (spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
        }

        spawning = false;
        EnsureSquad();
    }

    private void SpawnOne(int slot)
    {
        AllySoldier prefab = GetSelectedPrefab();
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        AllySoldier soldier = Instantiate(prefab, position, rotation);

        soldier.Died += OnSoldierDied;
        soldiers[slot] = soldier;
        InitSoldier(soldier, slot);
        ApplyMode(soldier);
    }

    private void InitSoldier(AllySoldier soldier, int index)
    {
        if (soldier == null)
            return;

        soldier.Init(GetFormationCenter(), GetSlotOffset(index), ResolvePlayer());
        soldier.AssignBarracks(this);
    }

    private void OnSoldierDied(AllySoldier soldier)
    {
        int slot = soldiers.IndexOf(soldier);
        if (slot < 0)
            return;

        soldier.Died -= OnSoldierDied;
        soldiers[slot] = null;
        if (!isActiveAndEnabled)
            return;

        pendingRespawns.Add(slot);
        StartCoroutine(RespawnOne(slot));
    }

    private IEnumerator RespawnOne(int slot)
    {
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        pendingRespawns.Remove(slot);
        EnsureSquad();
    }

    private void ApplyModeToSquad()
    {
        for (int i = 0; i < soldiers.Count; i++)
            ApplyMode(soldiers[i]);
    }

    private void ApplyMode(AllySoldier soldier)
    {
        if (soldier == null)
            return;

        if (AllyCommand.FollowPlayer)
            soldier.SetFollowPlayerMode();
        else if (hasGuardPosition)
            soldier.SetGuardPositionMode(guardPosition + GetSlotOffset(soldiers.IndexOf(soldier)), combatActive);
        else if (combatActive)
            soldier.SetAttackMode();
        else
            soldier.SetFormationMode();
    }

    private Vector3 GetFormationCenter()
    {
        return formationCenter != null ? formationCenter.position : transform.position + transform.forward * 2f;
    }

    private Vector3 GetSlotOffset(int index)
    {
        float centered = index - (squadSize - 1) * 0.5f;
        return transform.right * (centered * formationSpacing);
    }

    private AllySoldier GetSelectedPrefab()
    {
        return squadType switch
        {
            AllySoldier.CombatType.Melee => allyPrefab,
            AllySoldier.CombatType.Ranged => rangedAllyPrefab,
            _ => null
        };
    }

    private Transform ResolvePlayer()
    {
        if (player != null)
            return player;

        PlayerWallet wallet = PlayerWallet.Instance;
        if (wallet == null)
            wallet = FindFirstObjectByType<PlayerWallet>();

        player = wallet != null ? wallet.transform : null;
        return player;
    }
}
