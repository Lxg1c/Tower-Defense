using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BarracksBuilding : MonoBehaviour
{
    [Header("Squad")]
    [SerializeField] private AllySoldier allyPrefab;
    [Min(1)] [SerializeField] private int squadSize = 3;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform formationCenter;
    [SerializeField] private float formationSpacing = 1.2f;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float spawnInterval = 0.15f;

    private readonly List<AllySoldier> soldiers = new();
    private WaveSpawner spawner;
    private Transform player;
    private bool combatActive;
    private bool spawning;

    private void OnEnable()
    {
        ResolvePlayer();
        AllyCommand.OnFollowPlayerChanged += OnFollowPlayerChanged;
        BindSpawner();
        if (spawner == null)
            Invoke(nameof(BindSpawner), 0.1f);

        EnsureSquad();
    }

    private void OnDisable()
    {
        AllyCommand.OnFollowPlayerChanged -= OnFollowPlayerChanged;

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

    private void EnsureSquad()
    {
        if (spawning || allyPrefab == null)
            return;

        soldiers.RemoveAll(s => s == null || !s.isActiveAndEnabled);

        int missing = squadSize - soldiers.Count;
        if (missing > 0)
            StartCoroutine(SpawnMissing(missing));
    }

    private IEnumerator SpawnMissing(int count)
    {
        spawning = true;

        for (int i = 0; i < count; i++)
        {
            SpawnOne();

            if (spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
        }

        spawning = false;
    }

    private void SpawnOne()
    {
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        AllySoldier soldier = Instantiate(allyPrefab, position, rotation);

        soldier.Died += OnSoldierDied;
        soldiers.Add(soldier);
        InitSoldier(soldier, soldiers.Count - 1);
        ApplyMode(soldier);
    }

    private void InitSoldier(AllySoldier soldier, int index)
    {
        if (soldier == null)
            return;

        soldier.Init(GetFormationCenter(), GetSlotOffset(index), ResolvePlayer());
    }

    private void OnSoldierDied(AllySoldier soldier)
    {
        if (soldier != null)
            soldier.Died -= OnSoldierDied;

        soldiers.Remove(soldier);
        StartCoroutine(RespawnOne());
    }

    private IEnumerator RespawnOne()
    {
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        SpawnOne();
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
        int centered = index - (squadSize - 1) / 2;
        return transform.right * (centered * formationSpacing);
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
