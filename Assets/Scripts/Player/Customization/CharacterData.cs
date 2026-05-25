using UnityEngine;

/// <summary>
/// ScriptableObject để lưu thông tin nhân vật (mesh + material).
/// Mỗi character (Bunny, Duck, Lion, Monkey) sẽ có 1 file .asset riêng.
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Character System/Character Data")]
public class CharacterData : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    public string CharacterName => characterName;
    public Mesh Mesh => mesh;
    public Material Material => material;
}
