using UnityEngine;

/// <summary>
/// Quản lý việc chọn character cho player.
/// Gọi ApplyCharacter() trên tất cả player khi game start.
/// </summary>
public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField] private CharacterData[] characterDataArray;

    [Header("Default Character")]
    [SerializeField] private int defaultCharacterIndex = 0;

    void Start()
    {
        // Đọc character đã chọn từ PlayerPrefs
        int savedIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", defaultCharacterIndex);
        savedIndex = Mathf.Clamp(savedIndex, 0, characterDataArray != null ? characterDataArray.Length - 1 : 0);
        defaultCharacterIndex = savedIndex;

        ApplyCharacterToAllPlayers();
    }

    /// <summary>
    /// Apply character data cho tất cả player trong scene.
    /// </summary>
    void ApplyCharacterToAllPlayers()
    {
        // Tìm tất cả player trong scene
        var players = FindObjectsByType<CharacterSkinManager>(FindObjectsSortMode.None);

        if (players.Length == 0)
        {
            Debug.LogWarning("[CharacterSelectionManager] Không tìm thấy CharacterSkinManager nào trong scene!");
            return;
        }

        // Lấy character data mặc định
        if (characterDataArray == null || characterDataArray.Length == 0)
        {
            Debug.LogError("[CharacterSelectionManager] CharacterDataArray trống!");
            return;
        }

        CharacterData selectedCharacter = characterDataArray[defaultCharacterIndex];

        // Apply character cho tất cả player
        foreach (var player in players)
        {
            player.ApplyCharacter(selectedCharacter);
            Debug.Log($"[CharacterSelectionManager] Applied {selectedCharacter.CharacterName} to {player.gameObject.name}");
        }
    }

    public void ApplySelectedCharacterTo(CharacterSkinManager playerSkin)
    {
        if (playerSkin == null)
        {
            Debug.LogWarning("[CharacterSelectionManager] PlayerSkin null, không apply character.");
            return;
        }

        if (characterDataArray == null || characterDataArray.Length == 0)
        {
            Debug.LogError("[CharacterSelectionManager] CharacterDataArray trống!");
            return;
        }

        int savedIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", defaultCharacterIndex);
        savedIndex = Mathf.Clamp(savedIndex, 0, characterDataArray.Length - 1);

        CharacterData selectedCharacter = characterDataArray[savedIndex];
        playerSkin.ApplyCharacter(selectedCharacter);

        Debug.Log($"[CharacterSelectionManager] Applied selected {selectedCharacter.CharacterName} to {playerSkin.gameObject.name}");
    }

    /// <summary>
    /// Apply character theo index.
    /// </summary>
    public void SelectCharacterByIndex(int index)
    {
        if (characterDataArray == null || index < 0 || index >= characterDataArray.Length)
        {
            Debug.LogError($"[CharacterSelectionManager] Invalid character index: {index}");
            return;
        }

        var players = FindObjectsByType<CharacterSkinManager>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.ApplyCharacter(characterDataArray[index]);
        }
    }

    /// <summary>
    /// Apply character theo tên.
    /// </summary>
    public void SelectCharacterByName(string characterName)
    {
        foreach (var charData in characterDataArray)
        {
            if (charData.CharacterName == characterName)
            {
                var players = FindObjectsByType<CharacterSkinManager>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    player.ApplyCharacter(charData);
                }
                return;
            }
        }

        Debug.LogError($"[CharacterSelectionManager] Character '{characterName}' not found!");
    }
}
