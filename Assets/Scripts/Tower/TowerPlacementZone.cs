using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TowerPlacementZone : MonoBehaviour
{
    [Header("Tower")]
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private int cost = 50;
    [SerializeField] private float buildTime = 3f;
    [SerializeField] private Transform spawnPoint;

    [Header("Zone")]
    [SerializeField] private float zoneRadius = 2f;
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color highlightColor = new Color(0f, 1f, 0f, 0.6f);
    [SerializeField] private Color cantAffordColor = new Color(1f, 0f, 0f, 0.6f);
    [SerializeField] private Color buildingColor = new Color(1f, 0.7f, 0f, 0.6f);

    [Header("UI")]
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private GameObject progressBarRoot;
    [SerializeField] private Image progressFill;

    public int Cost => cost;
    public bool IsBuilt { get; private set; }
    public bool IsBuilding { get; private set; }

    public UnityEvent onTowerBuilt;

    private bool playerInZone;
    private PlayerWallet wallet;
    private Color currentColor;
    private float buildTimer;
    private float enterTimer;

    private void Start()
    {
        if (costLabel != null)
            costLabel.text = cost.ToString();

        if (progressBarRoot != null)
            progressBarRoot.SetActive(false);

        currentColor = idleColor;
    }

    private void Update()
    {
        DetectPlayer();

        // Reset enter timer when player leaves
        if (!playerInZone)
        {
            if (IsBuilding)
                CancelBuild();

            enterTimer = 0f;
            currentColor = idleColor;
            return;
        }

        if (wallet == null)
            return;

        if (wallet.Coins < cost)
        {
            currentColor = cantAffordColor;
            enterTimer = 0f;
            return;
        }

        // Player is in zone with enough coins — count up enter delay
        currentColor = highlightColor;
        enterTimer += Time.deltaTime;

        // Show progress during enter delay
        if (progressBarRoot != null)
            progressBarRoot.SetActive(true);
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(enterTimer / buildTime);

        if (enterTimer >= buildTime)
        {
            if (wallet.TrySpend(cost))
                CompleteBuild();
        }
    }

    private void DetectPlayer()
    {
        // Sphere check for player inside zone
        Collider[] hits = Physics.OverlapSphere(transform.position, zoneRadius);
        PlayerWallet foundWallet = null;
        foreach (Collider c in hits)
        {
            var w = c.GetComponent<PlayerWallet>();
            if (w != null)
            {
                foundWallet = w;
                break;
            }
        }

        wallet = foundWallet;
        playerInZone = foundWallet != null;
    }

    private void CancelBuild()
    {
        IsBuilding = false;
        enterTimer = 0f;
        currentColor = idleColor;

        if (progressBarRoot != null)
            progressBarRoot.SetActive(false);
        if (progressFill != null)
            progressFill.fillAmount = 0f;
    }

    private void CompleteBuild()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        Instantiate(towerPrefab, pos, rot);

        IsBuilt = true;
        IsBuilding = false;
        onTowerBuilt?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Color c = Application.isPlaying ? currentColor : idleColor;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, zoneRadius);

        Gizmos.color = new Color(c.r, c.g, c.b, 1f);
        Gizmos.DrawWireSphere(transform.position, zoneRadius);
    }
}
