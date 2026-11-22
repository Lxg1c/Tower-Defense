using UnityEngine;

/// <summary>
/// Drives the PulseTower's attack animation.
/// Winds up the hammer and draws the ring when the blow lands.
/// </summary>
[DisallowMultipleComponent]
public class PulseTowerAnimator : MonoBehaviour
{
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private Material pulseMaterial;

    private const int Segments = 48;
    private const float PulseDuration = .45f;

    private Animator   animator;
    private PulseTower tower;
    private int        attackHash;
    private bool       hasAttack;
    private LineRenderer pulseRing;
    private float pulseAge;
    private float pulseRange;

    private void Awake()
    {
        animator   = GetComponentInChildren<Animator>(true);
        tower      = GetComponent<PulseTower>();
        attackHash = Animator.StringToHash(attackTrigger);

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
        {
            foreach (var p in animator.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == attackTrigger)
                { hasAttack = true; break; }
        }
    }

    private void OnEnable()
    {
        if (tower == null) return;
        tower.onAttackWindup.AddListener(OnAttackWindup);
        tower.onAttack.AddListener(OnAttack);
    }

    private void OnDisable()
    {
        if (tower != null)
        {
            tower.onAttackWindup.RemoveListener(OnAttackWindup);
            tower.onAttack.RemoveListener(OnAttack);
        }
        if (pulseRing != null) pulseRing.enabled = false;
    }

    private void OnAttackWindup()
    {
        if (animator != null && hasAttack)
            animator.SetTrigger(attackHash);
    }

    private void OnAttack()
    {
        if (pulseMaterial == null || tower.Range <= 0f) return;

        if (pulseRing == null)
        {
            var effect = new GameObject("Pulse attack ring");
            effect.transform.SetParent(transform, false);
            pulseRing = effect.AddComponent<LineRenderer>();
            pulseRing.sharedMaterial = pulseMaterial;
            pulseRing.loop = true;
            pulseRing.positionCount = Segments;
            pulseRing.alignment = LineAlignment.TransformZ;
            effect.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pulseRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pulseRing.receiveShadows = false;
        }

        pulseAge = 0f;
        pulseRange = tower.Range;
        DrawPulse(0f);
        pulseRing.enabled = true;
    }

    private void LateUpdate()
    {
        if (pulseRing == null || !pulseRing.enabled) return;
        pulseAge += Time.deltaTime;
        if (pulseAge >= PulseDuration)
        {
            pulseRing.enabled = false;
            return;
        }
        DrawPulse(pulseAge / PulseDuration);
    }

    private void DrawPulse(float progress)
    {
        float radius = pulseRange * Mathf.Lerp(.16f, 1f, progress);
        Vector3 center = transform.position + Vector3.up * .22f;
        pulseRing.widthMultiplier = Mathf.Lerp(.25f, .025f, progress);
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * (2f * Mathf.PI / Segments);
            pulseRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
        }
    }
}
