using UnityEngine;

namespace NoahsArk.VFX.FootTrail
{
    /// <summary>
    /// ScriptableObject describing a single foot trail effect preset.
    /// A preset with a null trailPrefab acts as the "None" preset — no effect spawned.
    /// </summary>
    [CreateAssetMenu(fileName = "TrailPreset", menuName = "Noah's Ark/Trail Preset")]
    public class TrailPreset : ScriptableObject
    {
        [SerializeField] private string presetName;
        [SerializeField] private Sprite thumbnail;
        [SerializeField] private GameObject trailPrefab;

        /// <summary>Display name shown in the Cosmetic Menu and used as the PlayerPrefs key value.</summary>
        public string PresetName => presetName;

        /// <summary>128×128 thumbnail sprite shown in the Cosmetic Menu grid cell.</summary>
        public Sprite Thumbnail => thumbnail;

        /// <summary>
        /// Prefab containing the ParticleSystem(s) for this trail effect.
        /// Null indicates a "None" preset — no trail is spawned.
        /// </summary>
        public GameObject TrailPrefab => trailPrefab;
    }
}
