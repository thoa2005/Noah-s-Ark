using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Script dùng chung cho WinPanel của tất cả mini-game.
/// Hiện thông báo → đợi vài giây → tắt WinPanel → tắt panel mini-game → trở về màn chơi.
///
/// Setup:
///   1. Gắn script này lên WinPanel
///   2. Bỏ tick WinPanel trong Hierarchy (tắt active)
///   3. Kéo NVP5 (hoặc panel mini-game) vào field Mini Game Panel
///   4. Kéo WinPanel vào field Win Panel của mini-game script
/// </summary>
public class MiniGameWinPanel : MonoBehaviour
{
    public static MiniGameWinPanel Instance { get; private set; }

    [Header("Thời gian hiển thị (giây)")]
    public float displayTime = 2f;

    [Header("Text thông báo (tuỳ chọn)")]
    public Text messageText;

    [Header("Thông báo mặc định")]
    public string defaultMessage = "Hoàn thành!";

    [Header("Panel mini-game cần tắt sau khi hoàn thành")]
    public GameObject miniGamePanel;

    // ------------------------------------------------------------------ //

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ------------------------------------------------------------------ //

    public void Show()
    {
        Show(defaultMessage);
    }

    public void Show(string message)
    {
        StopAllCoroutines();

        if (messageText != null)
            messageText.text = message;

        gameObject.SetActive(true);
        StartCoroutine(HideAfterDelay());

        Debug.Log($"[WinPanel] Hiện \"{message}\" trong {displayTime}s");
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayTime);
        Hide();
    }

    public void Hide()
    {
        // Tắt WinPanel
        gameObject.SetActive(false);

        // Tắt panel mini-game → trở về màn chơi
        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);

        // Bật lại điều khiển nhân vật
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterInput input = player.GetComponent<CharacterInput>();
            if (input != null) input.DisableUIMode();
        }

        // Khóa chuột về trạng thái gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[WinPanel] Đóng xong, trở về màn chơi.");
    }
}
