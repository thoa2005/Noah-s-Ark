using System.Collections.Generic;
using NoahsArk.VFX.FootTrail;
using UnityEngine;

/// <summary>
/// Manages the Cosmetic Menu UI for selecting foot trail effects.
/// Builds a grid of TrailPresetCell components from a TrailCatalog and keeps
/// the highlight state in sync with FootTrailSystem.CurrentPreset.
/// Requirements: 4.1, 4.2, 4.3, 4.4, 4.5
/// </summary>
public class CosmeticMenuController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Data")]
    [SerializeField] private TrailCatalog catalog;
    [SerializeField] private FootTrailSystem footTrailSystem;

    [Header("UI")]
    [SerializeField] private Transform gridContainer;
    [SerializeField] private TrailPresetCell cellPrefab;

    // ── Runtime state ────────────────────────────────────────────────────────

    private List<TrailPresetCell> _cells = new List<TrailPresetCell>();

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    /// <summary>
    /// Validates required dependencies and builds the preset grid.
    /// </summary>
    private void Start()
    {
        if (catalog == null)
        {
            Debug.LogError("[CosmeticMenuController] TrailCatalog is not assigned.", this);
        }

        if (footTrailSystem == null)
        {
            Debug.LogError("[CosmeticMenuController] FootTrailSystem is not assigned.", this);
        }

        if (cellPrefab == null)
        {
            Debug.LogError("[CosmeticMenuController] cellPrefab is not assigned.", this);
        }

        BuildGrid();
    }

    /// <summary>
    /// Refreshes the highlight whenever the menu becomes visible,
    /// so the selection stays in sync if the preset changed while the panel was hidden.
    /// </summary>
    private void OnEnable()
    {
        // Null-guard: only refresh when dependencies are ready (Req 4.4)
        if (footTrailSystem != null && _cells.Count > 0)
        {
            RefreshHighlight();
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by a TrailPresetCell when the player clicks it.
    /// Applies the selected preset and refreshes the highlight. (Req 4.2)
    /// </summary>
    /// <param name="preset">The preset that was clicked.</param>
    public void OnCellClicked(TrailPreset preset)
    {
        footTrailSystem.SaveAndApplyPreset(preset);
        RefreshHighlight();
    }

    /// <summary>
    /// Syncs the highlight border on all cells with FootTrailSystem.CurrentPreset. (Req 4.4)
    /// </summary>
    public void RefreshHighlight()
    {
        foreach (TrailPresetCell cell in _cells)
        {
            cell.SetHighlight(cell.Preset == footTrailSystem.CurrentPreset);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Instantiates one TrailPresetCell per valid preset from the catalog. (Req 4.1, 4.3)
    /// </summary>
    private void BuildGrid()
    {
        // Req 4.5: early-return if required dependencies are missing
        if (catalog == null)
        {
            Debug.LogError("[CosmeticMenuController] Cannot build grid — TrailCatalog is not assigned.", this);
            return;
        }

        if (footTrailSystem == null)
        {
            Debug.LogError("[CosmeticMenuController] Cannot build grid — FootTrailSystem is not assigned.", this);
            return;
        }

        if (cellPrefab == null)
        {
            Debug.LogError("[CosmeticMenuController] Cannot build grid — cellPrefab is not assigned.", this);
            return;
        }

        _cells.Clear();

        // Req 4.1, 4.3: GetValidPresets() returns nonePreset first, then the rest (nulls filtered)
        foreach (TrailPreset preset in catalog.GetValidPresets())
        {
            TrailPresetCell cell = Instantiate(cellPrefab, gridContainer);
            cell.Initialize(preset, this);
            _cells.Add(cell);
        }

        RefreshHighlight();
    }
}
