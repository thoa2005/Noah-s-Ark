using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Mini-game NVP2: Nối dây điện 2D.
/// Người chơi kéo dây từ đầu nối trái sang đầu nối phải cho đúng màu/ký hiệu.
/// 
/// Kế thừa QuestMinigameUI → tự động mở/đóng panel và báo kết quả về QuestManager.
/// </summary>
public class WiresGame : QuestMinigameUI
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Cài đặt")]
    [Tooltip("Số cặp dây cần nối")]
    public int wireCount = 4;

    public float timeLimit = 30f;

    [Header("UI References")]
    public Text timerText;
    public GameObject winPanel;
    public GameObject losePanel;

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private int   _connectedCount = 0;
    private float _timeRemaining;

    // ------------------------------------------------------------------ //
    //  OVERRIDE
    // ------------------------------------------------------------------ //

    protected override void OnOpen()
    {
        _connectedCount = 0;
        _timeRemaining  = timeLimit;

        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    protected override void OnClose()
    {
        // TODO: Huỷ các coroutine nếu có
    }

    // ------------------------------------------------------------------ //
    //  UPDATE (chạy trong Time.timeScale = 0 nên dùng unscaled)
    // ------------------------------------------------------------------ //

    void Update()
    {
        if (!isActive || timeLimit <= 0f) return;

        _timeRemaining -= Time.unscaledDeltaTime;

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, _timeRemaining)).ToString();

        if (_timeRemaining <= 0f)
        {
            isActive = false;
            losePanel?.SetActive(true);
            Fail();
        }
    }

    // ------------------------------------------------------------------ //
    //  PUBLIC (gọi từ wire connector UI)
    // ------------------------------------------------------------------ //

    /// <summary>Gọi mỗi khi 1 cặp dây được nối đúng.</summary>
    public void OnWireConnected()
    {
        _connectedCount++;

        if (_connectedCount >= wireCount)
        {
            isActive = false;
            winPanel?.SetActive(true);
            Complete();
        }
    }
}
