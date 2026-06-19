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

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    /// <summary>True khi có nhân vật đang giữ một nhiệm vụ — các NPC/nhân vật khác dùng để kiểm tra.</summary>
    public static bool isQuestTaken = false;

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
