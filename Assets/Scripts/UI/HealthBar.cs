using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pure visual component — knows nothing about game logic.
/// HealthBarManager drives it via SetFill().
///
/// Prefab structure:
///   HealthBar (RectTransform + HealthBar script)
///     └─ Background  (Image — dark, stretch to fill)
///     └─ GhostFill   (Image — white, Filled/Horizontal — sits BELOW Fill)
///     └─ Fill        (Image — coloured, Filled/Horizontal — sits ON TOP)
/// </summary>
[DisallowMultipleComponent]
public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image ghostFillImage;

    [Header("Size (canvas units)")]
    [SerializeField] private Vector2 barSize = new Vector2(60f, 10f);

    [Header("Colours")]
    [SerializeField] private Color colorFull  = new Color(0.18f, 0.80f, 0.24f);
    [SerializeField] private Color ghostColor = Color.white;
    [Header("Fill Lerp (0 = instant)")]
    [SerializeField] private float lerpSpeed = 12f;

    [Header("Ghost Bar")]
    [Tooltip("Seconds to wait before the ghost bar starts draining.")]
    [SerializeField] private float ghostDelay     = 0.45f;
    [Tooltip("How fast the ghost bar drains to match real HP.")]
    [SerializeField] private float ghostLerpSpeed = 3f;

    // ── State ─────────────────────────────────────────────────────────────────

    private float _targetFill  = 1f;
    private float _currentFill = 1f;   // coloured bar
    private float _ghostFill   = 1f;   // white bar
    private float _ghostTimer  = 0f;   // countdown before ghost starts draining
    private RectTransform _rt;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        ApplySize();

        if (ghostFillImage != null)
            ghostFillImage.color = ghostColor;
    }

    private void Update()
    {
        // ── Coloured fill ─────────────────────────────────────────────────
        if (!Mathf.Approximately(_currentFill, _targetFill))
        {
            _currentFill = lerpSpeed > 0f
                ? Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * lerpSpeed)
                : _targetFill;

            if (Mathf.Abs(_currentFill - _targetFill) < 0.001f)
                _currentFill = _targetFill;

            ApplyFill(_currentFill);
        }

        // ── Ghost bar ─────────────────────────────────────────────────────
        if (ghostFillImage == null) return;

        if (_ghostFill <= _currentFill + 0.001f)
        {
            // Ghost already caught up — keep it flush.
            _ghostFill = _currentFill;
            ghostFillImage.fillAmount = _ghostFill;
            return;
        }

        if (_ghostTimer > 0f)
        {
            _ghostTimer -= Time.deltaTime;
            return; // still in the hold period
        }

        // Drain ghost toward real fill.
        _ghostFill = Mathf.Lerp(_ghostFill, _currentFill, Time.deltaTime * ghostLerpSpeed);
        if (Mathf.Abs(_ghostFill - _currentFill) < 0.001f)
            _ghostFill = _currentFill;

        ghostFillImage.fillAmount = _ghostFill;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetFill(float normalised)
    {
        float newTarget = Mathf.Clamp01(normalised);

        bool isDamage = newTarget < _targetFill;

        if (isDamage)
        {
            // Snap ghost to current visual position and start hold timer.
            _ghostFill  = _currentFill;
            _ghostTimer = ghostDelay;
        }

        _targetFill = newTarget;

        // Instant mode — update everything now.
        if (lerpSpeed <= 0f)
        {
            _currentFill = _targetFill;
            ApplyFill(_currentFill);
        }
    }

    public void SetFillInstant(float normalised)
    {
        _targetFill = Mathf.Clamp01(normalised);
        _currentFill = _targetFill;
        _ghostFill = _targetFill;
        _ghostTimer = 0f;

        ApplyFill(_currentFill);
        if (ghostFillImage != null)
            ghostFillImage.fillAmount = _ghostFill;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyFill(float value)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = value;
    }

    private void ApplySize()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        _rt.sizeDelta = barSize;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (fillImage != null)      ApplyFill(_targetFill);
        if (ghostFillImage != null) ghostFillImage.color = ghostColor;

        // sizeDelta cannot be set during OnValidate — defer it.
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) ApplySize();
        };
    }
#endif
}
