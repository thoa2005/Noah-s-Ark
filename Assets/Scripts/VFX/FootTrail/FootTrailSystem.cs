using NoahsArk.VFX.FootTrail;
using UnityEngine;

/// <summary>
/// Core MonoBehaviour that manages the lifecycle of foot trail VFX for a player.
/// Attach to a child GameObject of the Player root (which holds Rigidbody and GroundDetect).
/// </summary>
public class FootTrailSystem : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Catalog")]
    [SerializeField] private TrailCatalog catalog;

    [Header("Foot Anchors")]
    [SerializeField] private Transform leftFootAnchor;
    [SerializeField] private Transform rightFootAnchor;

    [Header("Activation Thresholds")]
    [SerializeField] private float minSpeedThreshold = 0.5f;   // m/s — below this, emission is disabled
    [SerializeField] private float runSpeedThreshold = 4.0f;   // m/s — above this, run emission rate is used
    [SerializeField] private float walkEmissionRate  = 1.0f;   // rateOverDistanceMultiplier at walk speed
    [SerializeField] private float runEmissionRate   = 2.0f;   // rateOverDistanceMultiplier at run speed

    // ── Runtime state ────────────────────────────────────────────────────────

    private TrailPreset _currentPreset;
    private GameObject  _leftInstance;
    private GameObject  _rightInstance;
    private ParticleSystem.EmissionModule[] _emissionsLeft;
    private ParticleSystem.EmissionModule[] _emissionsRight;

    // ── Dependency instances (auto-resolved via GetComponentInParent) ────────

    private Rigidbody    _rb;
    private GroundDetect _groundDetect;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>The Trail Preset currently applied to this character.</summary>
    public TrailPreset CurrentPreset => _currentPreset;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _rb           = GetComponentInParent<Rigidbody>();
        _groundDetect = GetComponentInParent<GroundDetect>();

        if (catalog == null)
        {
            Debug.LogError("[FootTrailSystem] TrailCatalog is not assigned. Component disabled.", this);
            enabled = false;
            return;
        }

        if (_rb == null)
        {
            Debug.LogError("[FootTrailSystem] Rigidbody not found in parent hierarchy. Component disabled.", this);
            enabled = false;
            return;
        }

        if (_groundDetect == null)
        {
            Debug.LogWarning("[FootTrailSystem] GroundDetect not found in parent hierarchy. Will treat character as always grounded.", this);
        }
    }

    private void Start()
    {
        LoadSavedPreset();
    }

    private void Update()
    {
        // Req 7.3 / design early-exit: no-op when no preset is active or no instances exist
        if (_currentPreset == null || _currentPreset == catalog.NonePreset)
            return;

        if (_leftInstance == null && _rightInstance == null)
            return;

        // Req 2.1–2.3: compute horizontal speed (ignore vertical component)
        float horizontalSpeed = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z).magnitude;

        // Req 7.3 / design: fall back to always-grounded when GroundDetect is unavailable
        bool isGrounded = _groundDetect != null ? _groundDetect.isGrounded : true;

        // Req 2.1–2.3: emission enabled iff grounded and above minimum speed threshold
        bool shouldEmit = isGrounded && horizontalSpeed >= minSpeedThreshold;

        // Req 2.5: double the emission rate when running
        float rate = horizontalSpeed >= runSpeedThreshold ? runEmissionRate : walkEmissionRate;

        SetEmission(shouldEmit, rate);
    }

    // ── Public methods (implementations in later tasks) ──────────────────────

    /// <summary>
    /// Replaces the currently active trail prefab with the given preset.
    /// Req 1.1, 1.2, 1.3, 1.4
    /// </summary>
    public void ApplyPreset(TrailPreset preset)
    {
        // Req 1.4 — null prefab on a non-None preset: warn and keep current state
        if (preset.TrailPrefab == null && preset != catalog.NonePreset)
        {
            Debug.LogWarning($"[FootTrailSystem] TrailPrefab is null on preset: {preset.PresetName}. Keeping current state.", this);
            return;
        }

        // Req 1.3 — None preset: destroy instances and record state, then return
        if (preset == catalog.NonePreset)
        {
            DestroyTrailInstances();
            _currentPreset = catalog.NonePreset;
            return;
        }

        // Req 1.2 — valid preset: destroy old, spawn new, cache emissions
        DestroyTrailInstances();
        SpawnTrailInstances(preset);
        CacheEmissions(_leftInstance,  out _emissionsLeft);
        CacheEmissions(_rightInstance, out _emissionsRight);
        _currentPreset = preset;
    }

    /// <summary>
    /// Calls ApplyPreset and persists the selection to PlayerPrefs.
    /// Req 5.1, 5.4
    /// </summary>
    public void SaveAndApplyPreset(TrailPreset preset)
    {
        ApplyPreset(preset);
        string savedValue = (preset == catalog.NonePreset) ? "None" : preset.PresetName;
        PlayerPrefs.SetString("FootTrailPreset", savedValue);
    }

    // ── Private helpers (implementations in later tasks) ─────────────────────

    /// <summary>
    /// Instantiates trail prefab instances into left and right foot anchors,
    /// logging a warning and skipping any null anchor.
    /// </summary>
    private void SpawnTrailInstances(TrailPreset preset)
    {
        if (leftFootAnchor == null)
        {
            Debug.LogWarning("[FootTrailSystem] leftFootAnchor is null — skipping left trail instance.", this);
        }
        else
        {
            _leftInstance = Instantiate(preset.TrailPrefab, leftFootAnchor);
            _leftInstance.transform.localPosition = Vector3.zero;
            _leftInstance.transform.localRotation = Quaternion.identity;
        }

        if (rightFootAnchor == null)
        {
            Debug.LogWarning("[FootTrailSystem] rightFootAnchor is null — skipping right trail instance.", this);
        }
        else
        {
            _rightInstance = Instantiate(preset.TrailPrefab, rightFootAnchor);
            _rightInstance.transform.localPosition = Vector3.zero;
            _rightInstance.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Destroys active left and right trail prefab instances if they exist.
    /// </summary>
    private void DestroyTrailInstances()
    {
        if (_leftInstance != null)
        {
            Destroy(_leftInstance);
            _leftInstance = null;
        }

        if (_rightInstance != null)
        {
            Destroy(_rightInstance);
            _rightInstance = null;
        }
    }

    /// <summary>
    /// Retrieves all ParticleSystem EmissionModule references from a trail instance.
    /// </summary>
    private void CacheEmissions(GameObject instance, out ParticleSystem.EmissionModule[] modules)
    {
        if (instance == null)
        {
            modules = System.Array.Empty<ParticleSystem.EmissionModule>();
            return;
        }

        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>();
        modules = new ParticleSystem.EmissionModule[systems.Length];
        for (int i = 0; i < systems.Length; i++)
        {
            modules[i] = systems[i].emission;
        }
    }

    /// <summary>
    /// Enables or disables emission and sets rateOverDistanceMultiplier on all cached modules.
    /// </summary>
    private void SetEmission(bool emissionEnabled, float rate)
    {
        if (_emissionsLeft != null)
        {
            for (int i = 0; i < _emissionsLeft.Length; i++)
            {
                _emissionsLeft[i].enabled = emissionEnabled;
                _emissionsLeft[i].rateOverDistanceMultiplier = rate;
            }
        }

        if (_emissionsRight != null)
        {
            for (int i = 0; i < _emissionsRight.Length; i++)
            {
                _emissionsRight[i].enabled = emissionEnabled;
                _emissionsRight[i].rateOverDistanceMultiplier = rate;
            }
        }
    }

    /// <summary>
    /// Reads PlayerPrefs and applies the saved preset, or falls back to nonePreset.
    /// Req 5.2, 5.3
    /// </summary>
    private void LoadSavedPreset()
    {
        string savedName = PlayerPrefs.GetString("FootTrailPreset", "None");

        TrailPreset found = null;
        foreach (TrailPreset preset in catalog.GetValidPresets())
        {
            if (preset != null && preset.PresetName == savedName)
            {
                found = preset;
                break;
            }
        }

        if (found == null)
        {
            Debug.LogWarning($"[FootTrailSystem] Saved preset '{savedName}' not found in catalog. Applying None.");
            ApplyPreset(catalog.NonePreset);
        }
        else
        {
            ApplyPreset(found);
        }
    }
}
