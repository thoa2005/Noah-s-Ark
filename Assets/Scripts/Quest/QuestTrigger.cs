using UnityEngine;

/// <summary>
/// Gắn vào vùng 3D ngoài Map (collider trigger) để mở UI 2D mini-game
/// khi nhân vật bước vào.
/// 
/// Setup:
///   1. Tạo GameObject (ví dụ: "QuestZone_Storage"), gắn Collider (Is Trigger = true).
///   2. Gắn script này, chọn questID tương ứng trong Inspector.
///   3. Đảm bảo nhân vật có tag "Player".
/// </summary>
public class QuestTrigger : MonoBehaviour
{
    [Header("Nhiệm vụ cần mở")]
    public QuestID questID = QuestID.None;

    [Tooltip("Chỉ kích hoạt 1 lần rồi tắt trigger?")]
    public bool triggerOnce = true;

    [Tooltip("Hiển thị gợi ý tương tác (UI nhỏ) khi đứng gần")]
    public GameObject interactHint;

    private bool _triggered = false;

    // ------------------------------------------------------------------ //
    //  TRIGGER 3D
    // ------------------------------------------------------------------ //

    void OnTriggerEnter(Collider other)
    {
        if (_triggered && triggerOnce) return;
        if (!other.CompareTag("Player")) return;

        // Hiện gợi ý tương tác
        if (interactHint != null)
            interactHint.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (interactHint != null)
            interactHint.SetActive(false);
    }

    void Update()
    {
        // TODO: Thay bằng Input System nếu cần
        if (interactHint != null && interactHint.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
                ActivateQuest();
        }
    }

    // ------------------------------------------------------------------ //
    //  KÍCH HOẠT
    // ------------------------------------------------------------------ //

    void ActivateQuest()
    {
        if (_triggered && triggerOnce) return;

        _triggered = true;

        if (interactHint != null)
            interactHint.SetActive(false);

        QuestManager.Instance?.OpenQuest(questID);

        Debug.Log($"[QuestTrigger] Mở nhiệm vụ: {questID}");
    }
}
