using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NoahsArk.VFX.FootTrail;

/// <summary>
/// UI cell representing a single TrailPreset in the Cosmetic Menu grid.
/// Displays a thumbnail image and preset name, and notifies CosmeticMenuController
/// when clicked. Highlights itself when its preset is the active selection.
/// </summary>
public class TrailPresetCell : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private GameObject highlightBorder;
    [SerializeField] private Button button;

    /// <summary>The TrailPreset this cell represents.</summary>
    public TrailPreset Preset { get; private set; }

    /// <summary>
    /// Configures the cell with preset data and wires up the click callback.
    /// </summary>
    /// <param name="preset">The TrailPreset to display.</param>
    /// <param name="controller">The CosmeticMenuController that owns this cell.</param>
    public void Initialize(TrailPreset preset, CosmeticMenuController controller)
    {
        Preset = preset;

        // Set thumbnail — disable the image component if no sprite is assigned
        if (thumbnailImage != null)
        {
            if (preset.Thumbnail != null)
            {
                thumbnailImage.sprite = preset.Thumbnail;
                thumbnailImage.enabled = true;
            }
            else
            {
                thumbnailImage.enabled = false;
            }
        }

        // Set the display name
        if (nameLabel != null)
        {
            nameLabel.text = preset.PresetName;
        }

        // Wire up button — clear old listeners first to avoid duplicate registrations
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => controller.OnCellClicked(preset));
        }
    }

    /// <summary>
    /// Shows or hides the highlight border to indicate the selected state.
    /// </summary>
    /// <param name="active">True to show the highlight; false to hide it.</param>
    public void SetHighlight(bool active)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(active);
        }
    }
}
