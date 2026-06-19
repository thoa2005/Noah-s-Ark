using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý tất cả nhiệm vụ: mở/đóng UI mini-game, cấp buff/điểm khi hoàn thành.
/// Singleton — truy cập qua QuestManager.Instance.
/// </summary>
public class QuestManager : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  SINGLETON
    // ------------------------------------------------------------------ //

    public static QuestManager Instance { get; private set; }

    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Danh sách tất cả nhiệm vụ")]
    public List<QuestData> allQuests = new List<QuestData>();

    [Header("Tham chiếu tới các mini-game UI (gán trong Inspector)")]
    public QuestMinigameUI sortingGameUI;   // NVP1 - Sắp xếp kho lương
    public QuestMinigameUI sweepDeckUI;     // NVP2 - Quét dọn sàn tàu
    public QuestMinigameUI nailBoardsUI;    // NVP3 - Đóng đinh
    public QuestMinigameUI pullAnchorUI;    // NVP4 - Kéo mỏ neo
    public QuestMinigameUI sewSailUI;       // NVP5 - Khâu vá cánh buồm
    public QuestMinigameUI repairShipUI;    // NVP6 - Ghép mảnh tàu vỡ

    [Header("HUD")]
    public QuestHUDDisplay hudDisplay;

    [Header("Player (để trả quyền điều khiển sau khi hoàn thành)")]
    public PlayerMovement playerMovement;
    public CharacterInput playerInput;

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private Dictionary<QuestID, QuestData> _questMap = new Dictionary<QuestID, QuestData>();

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Build lookup map
        foreach (var q in allQuests)
            _questMap[q.id] = q;
    }

    // ------------------------------------------------------------------ //
    //  PUBLIC API
    // ------------------------------------------------------------------ //

    /// <summary>Mở UI mini-game tương ứng với questID.</summary>
    public void OpenQuest(QuestID id)
    {
        QuestMinigameUI ui = GetUIFor(id);
        if (ui == null)
        {
            Debug.LogWarning($"[QuestManager] Không tìm thấy UI cho quest: {id}");
            return;
        }

        SetQuestState(id, QuestState.Active);
        ui.Open(id);

        hudDisplay?.ShowQuest(GetQuestData(id));
    }

    /// <summary>Gọi khi hoàn thành nhiệm vụ (từ mini-game).</summary>
    public void CompleteQuest(QuestID id)
    {
        SetQuestState(id, QuestState.Completed);

        GetUIFor(id)?.Close();
        RestorePlayerControl();

        QuestData data = GetQuestData(id);
        if (data != null)
        {
            // TODO: Cộng điểm vào GameManager chính
            // GameManager.Instance?.AddScore(data.rewardScore);

            // TODO: Phát buff cho nhân vật
            // BuffIndicatorUI.Instance?.ShowBuff(id);

            Debug.Log($"[QuestManager] ✅ Hoàn thành: {id} | Điểm thưởng: {data.rewardScore}");
        }

        hudDisplay?.HideQuest();
    }

    /// <summary>Gọi khi thất bại (hết giờ, sai...).</summary>
    public void FailQuest(QuestID id)
    {
        SetQuestState(id, QuestState.Failed);
        GetUIFor(id)?.Close();
        RestorePlayerControl();
        hudDisplay?.HideQuest();

        Debug.Log($"[QuestManager] ❌ Thất bại: {id}");
    }

    public QuestData GetQuestData(QuestID id)
    {
        _questMap.TryGetValue(id, out QuestData data);
        return data;
    }

    public QuestState GetQuestState(QuestID id)
    {
        QuestData data = GetQuestData(id);
        return data?.state ?? QuestState.Inactive;
    }

    // ------------------------------------------------------------------ //
    //  HELPER
    // ------------------------------------------------------------------ //

    /// <summary>Trả quyền điều khiển về cho player sau khi quest kết thúc.</summary>
    void RestorePlayerControl()
    {
        // Ưu tiên dùng reference đã gán trong Inspector
        // Nếu chưa gán thì tự tìm qua tag
        if (playerMovement == null || playerInput == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                if (playerMovement == null) playerMovement = playerObj.GetComponent<PlayerMovement>();
                if (playerInput    == null) playerInput    = playerObj.GetComponent<CharacterInput>();
            }
        }

        if (playerMovement != null) playerMovement.enabled = true;

        if (playerInput != null)
        {
            playerInput.ClearAllInputs(); // xóa input ảo tích lũy khi đang chơi mini-game
            playerInput.enabled = true;
        }

        // Đảm bảo timeScale bình thường
        Time.timeScale = 1f;

        // Unlock cursor nếu game dùng cursor lock
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        Debug.Log("[QuestManager] ✅ Đã trả quyền điều khiển về cho player.");
    }

    void SetQuestState(QuestID id, QuestState newState)
    {
        if (_questMap.TryGetValue(id, out QuestData data))
            data.state = newState;
    }

    QuestMinigameUI GetUIFor(QuestID id)
    {
        switch (id)
        {
            case QuestID.SortStorage:  return sortingGameUI;
            case QuestID.SweepDeck:    return sweepDeckUI;
            case QuestID.NailBoards:   return nailBoardsUI;
            case QuestID.PullAnchor:   return pullAnchorUI;
            case QuestID.SewSail:      return sewSailUI;
            case QuestID.RepairShip:   return repairShipUI;
            default: return null;
        }
    }
}
