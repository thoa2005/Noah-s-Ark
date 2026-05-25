using UnityEngine;

/// <summary>
/// Quản lý việc swap mesh + material của nhân vật.
/// Attach vào Player GameObject, sẽ tự động tìm SkinnedMeshRenderer và apply character data.
/// </summary>
public class CharacterSkinManager : MonoBehaviour
{
    private SkinnedMeshRenderer skinMeshRenderer;

    void Start()
    {
        // Tìm SkinnedMeshRenderer trên chính object này hoặc con của nó
        skinMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        if (skinMeshRenderer == null)
            skinMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        if (skinMeshRenderer == null)
            Debug.LogError($"[CharacterSkinManager] Không tìm thấy SkinnedMeshRenderer trên {gameObject.name}!");
    }

    /// <summary>
    /// Apply character data (mesh + material) vào nhân vật.
    /// </summary>
    public void ApplyCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            Debug.LogError("[CharacterSkinManager] CharacterData là null!");
            return;
        }

        if (skinMeshRenderer == null)
        {
            Debug.LogError("[CharacterSkinManager] SkinnedMeshRenderer chưa được khởi tạo!");
            return;
        }

        // Swap mesh
        if (characterData.Mesh != null)
            skinMeshRenderer.sharedMesh = characterData.Mesh;
        else
            Debug.LogWarning($"[CharacterSkinManager] {characterData.CharacterName} không có mesh!");

        // Swap material
        if (characterData.Material != null)
            skinMeshRenderer.material = characterData.Material;
        else
            Debug.LogWarning($"[CharacterSkinManager] {characterData.CharacterName} không có material!");

        Debug.Log($"[CharacterSkinManager] Applied character: {characterData.CharacterName}");
    }
}
