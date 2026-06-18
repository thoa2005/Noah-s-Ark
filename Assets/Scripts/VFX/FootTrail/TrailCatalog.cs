using System.Collections.Generic;
using NoahsArk.VFX.FootTrail;
using UnityEngine;

[CreateAssetMenu(fileName = "TrailCatalog", menuName = "Noah's Ark/Trail Catalog")]
public class TrailCatalog : ScriptableObject
{
    [SerializeField] private TrailPreset nonePreset;
    [SerializeField] private List<TrailPreset> presets;

    /// <summary>
    /// The special "None" preset that represents no trail effect.
    /// </summary>
    public TrailPreset NonePreset => nonePreset;

    /// <summary>
    /// Returns a filtered list beginning with nonePreset, skipping any null entries.
    /// Logs a warning for each null entry encountered (Req 3.5).
    /// </summary>
    public IReadOnlyList<TrailPreset> GetValidPresets()
    {
        var result = new List<TrailPreset> { nonePreset };

        if (presets == null)
            return result;

        for (int i = 0; i < presets.Count; i++)
        {
            if (presets[i] == null)
            {
                Debug.LogWarning($"[TrailCatalog] Null entry at index {i} — skipped.");
                continue;
            }

            result.Add(presets[i]);
        }

        return result;
    }
}
