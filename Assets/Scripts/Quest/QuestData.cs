using UnityEngine;

/// <summary>
/// Chứa ID (Enum) định nghĩa tất cả các nhiệm vụ trong game.
/// Thêm nhiệm vụ mới vào enum QuestID bên dưới.
/// </summary>

// ------------------------------------------------------------------ //
//  ENUM ĐỊNH NGHĨA ID NHIỆM VỤ
// ------------------------------------------------------------------ //

public enum QuestID
{
    None = 0,

    // --- Nhiệm vụ phụ ---
    SortStorage    = 1,   // NVP1: Sắp xếp kho lương (kéo thả hoa quả vào thùng)
    SweepDeck      = 2,   // NVP2: Quét dọn sàn tàu (di chuột qua vết bẩn)
    NailBoards     = 3,   // NVP3: Đóng đinh tấm ván (click búa vào đinh)
    PullAnchor     = 4,   // NVP4: Kéo mỏ neo (xoay tay quay theo vòng tròn)
    SewSail        = 5,   // NVP5: Khâu vá cánh buồm (luồn chỉ qua lỗ)
    RepairShip     = 6,   // NVP6: Ghép mảnh tàu vỡ (drag & snap puzzle)

    // --- Nhiệm vụ chính (thêm sau) ---
    // MainQuest1  = 100,
}

// ------------------------------------------------------------------ //
//  TRẠNG THÁI NHIỆM VỤ
// ------------------------------------------------------------------ //

public enum QuestState
{
    Inactive,       // Chưa kích hoạt
    Active,         // Đang thực hiện
    Completed,      // Hoàn thành
    Failed,         // Thất bại
}

// ------------------------------------------------------------------ //
//  DATA CLASS
// ------------------------------------------------------------------ //

[System.Serializable]
public class QuestData
{
    public QuestID   id;
    public string    displayName;
    [TextArea] 
    public string    description;
    public QuestState state = QuestState.Inactive;
    public float      timeLimit = 0f;   // 0 = không giới hạn thời gian
    public int        rewardScore = 100;
}
