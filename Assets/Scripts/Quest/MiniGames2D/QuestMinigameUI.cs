using UnityEngine;

/// <summary>
/// Lớp cha chung cho tất cả mini-game UI 2D.
/// Các mini-game cụ thể (WiresGame, SimonSaysGame, KeypadGame, SortingGame)
/// kế thừa từ class này.
/// 
/// Chức năng:
///   - Mở/đóng panel UI
///   - Gọi QuestManager khi hoàn thành hoặc thất bại
///   - Quản lý đếm ngược thời gian (nếu có)
/// </summary>
public abstract class QuestMinigameUI : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Panel chứa toàn bộ UI mini-game")]
    public GameObject panel;

    [Tooltip("Dừng thời gian game 3D khi mini-game đang chạy?")]
    public bool pauseGameWhileActive = false;   // Mặc định FALSE — drag/drop UI cần timeScale = 1

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    protected QuestID currentQuestID;
    protected bool    isActive = false;

    // ------------------------------------------------------------------ //
    //  PUBLIC API
    // ------------------------------------------------------------------ //

    /// <summary>Mở mini-game, gọi bởi QuestManager.</summary>
    public virtual void Open(QuestID id)
    {
        currentQuestID = id;
        isActive       = true;

        if (panel != null) panel.SetActive(true);
        if (pauseGameWhileActive) Time.timeScale = 0f;

        OnOpen();
        Debug.Log($"[{GetType().Name}] Mở mini-game: {id}");
    }

    /// <summary>Đóng mini-game.</summary>
    public virtual void Close()
    {
        isActive = false;

        if (panel != null) panel.SetActive(false);
        if (pauseGameWhileActive) Time.timeScale = 1f;

        // Bật lại điều khiển nhân vật sau khi đóng bảng
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterInput input = player.GetComponent<CharacterInput>();
            if (input != null) input.DisableUIMode();
        }

        // Khóa lại chuột về trạng thái gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        OnClose();
        Debug.Log($"[{GetType().Name}] Đóng mini-game: {currentQuestID}");
    }

    // ------------------------------------------------------------------ //
    //  HOÀN THÀNH / THẤT BẠI
    // ------------------------------------------------------------------ //

    protected void Complete()
    {
        QuestManager.Instance?.CompleteQuest(currentQuestID);
    }

    protected void Fail()
    {
        QuestManager.Instance?.FailQuest(currentQuestID);
    }

    // ------------------------------------------------------------------ //
    //  OVERRIDE Ở LỚP CON
    // ------------------------------------------------------------------ //

    /// <summary>Khởi tạo trạng thái khi mở (reset bài, shuffle quả, v.v.)</summary>
    protected virtual void OnOpen()  { }

    /// <summary>Dọn dẹp khi đóng (huỷ coroutine, reset UI, v.v.)</summary>
    protected virtual void OnClose() { }
}
