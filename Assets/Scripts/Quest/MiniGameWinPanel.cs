using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào GameObject WinPanel của bất kỳ mini-game nào.
/// Khi được bật (SetActive true), tự động:
///   1. Hiện thông báo hoàn thành
///   2. Đợi displayDuration giây
///   3. Đóng mini-game, trả quyền điều khiển về cho player
/// </summary>
public class MiniGameWinPanel : MonoBehaviour
{
    [Header("Thời gian hiển thị thông báo (giây)")]
    public float displayDuration = 2.5f;

    [Header("Text thông báo (tuỳ chỉnh nội dung)")]
    public Text messageText;
    public string message = "Hoàn thành nhiệm vụ!";

    // Mini-game cha cần đóng khi xong
    private QuestMinigameUI _parentGame;

    // ------------------------------------------------------------------ //

    void Awake()
    {
        // Tự tìm QuestMinigameUI trên cha hoặc trên cùng cấp
        _parentGame = GetComponentInParent<QuestMinigameUI>();
    }

    void OnEnable()
    {
        // Set nội dung text nếu có
        if (messageText != null)
            messageText.text = message;

        // Bắt đầu đếm ngược rồi đóng
        StartCoroutine(CloseAfterDelay());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    // ------------------------------------------------------------------ //

    IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDuration);
        CloseMinigame();
    }

    /// <summary>
    /// Có thể gọi từ nút bấm "OK" trong WinPanel nếu muốn đóng thủ công.
    /// </summary>
    public void OnClickClose()
    {
        StopAllCoroutines();
        CloseMinigame();
    }

    void CloseMinigame()
    {
        // 1. Đóng panel mini-game (ẩn UI, trả timeScale về 1)
        if (_parentGame != null)
            _parentGame.Close();
        else
            gameObject.SetActive(false); // fallback nếu không tìm được

        // 2. Tìm player và trả quyền điều khiển
        RestorePlayerControl();
    }

    void RestorePlayerControl()
    {
        // Tìm CharacterInput của local player (tag "Player")
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("[MiniGameWinPanel] Không tìm thấy GameObject tag 'Player'.");
            return;
        }

        // Enable lại PlayerMovement nếu bị tắt
        PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = true;

        // Reset input để tránh "giữ phím" ảo sau khi thoát
        CharacterInput input = playerObj.GetComponent<CharacterInput>();
        if (input != null) input.ClearAllInputs();

        // Enable lại PlayerInput (Unity Input System)
        var playerInput = playerObj.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null) playerInput.enabled = true;

        Debug.Log("[MiniGameWinPanel] Đã trả quyền điều khiển về cho player.");
    }
}
