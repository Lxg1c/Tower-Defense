using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public sealed class CompanionTurretController : MonoBehaviour
{
    [SerializeField] private CompanionTurret turretPrefab;
    private readonly CompanionTurret[] turrets = new CompanionTurret[2];
    private PlayerHealth health;

    public int ActiveCount
    {
        get
        {
            int count = 0;
            foreach (CompanionTurret turret in turrets)
                if (turret != null) count++;
            return count;
        }
    }

    public float RemainingSeconds => LongestLivedTurret != null
        ? LongestLivedTurret.RemainingSeconds : 0f;
    public float Progress => LongestLivedTurret != null
        ? LongestLivedTurret.Progress : 0f;

    private CompanionTurret LongestLivedTurret
    {
        get
        {
            CompanionTurret longest = null;
            foreach (CompanionTurret turret in turrets)
                if (turret != null && turret.RemainingSeconds > 0f &&
                    (longest == null || turret.RemainingSeconds > longest.RemainingSeconds))
                    longest = turret;
            return longest;
        }
    }

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        if (turretPrefab == null)
        {
            Debug.LogError("[CompanionTurretController] Assign a companion turret prefab.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (health != null) health.onDied.AddListener(Clear);
    }

    private void OnDisable()
    {
        if (health != null) health.onDied.RemoveListener(Clear);
        Clear();
    }

    public bool Grant(float durationSeconds)
    {
        if (!isActiveAndEnabled || health == null || !health.IsAlive || durationSeconds <= 0f)
            return false;

        for (int i = 0; i < turrets.Length; i++)
        {
            if (turrets[i] != null) continue;
            CompanionTurret turret = Instantiate(turretPrefab);
            turret.Initialize(health, i, durationSeconds);
            turret.gameObject.SetActive(true);
            turrets[i] = turret;
            return true;
        }

        int shorter = turrets[0].RemainingSeconds <= turrets[1].RemainingSeconds ? 0 : 1;
        turrets[shorter].Refresh(durationSeconds);
        return true;
    }

    private void Clear()
    {
        for (int i = 0; i < turrets.Length; i++)
        {
            if (turrets[i] != null)
            {
                turrets[i].GetComponent<Shooter>().enabled = false;
                Destroy(turrets[i].gameObject);
            }
            turrets[i] = null;
        }
    }
}
