using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// NVP6: Ghép các mảnh tàu bị vỡ.
/// Góc nhìn chính diện. Người chơi kéo thả (drag & drop) từng mảnh vào đúng vị trí.
/// Khi mảnh được thả gần đúng vị trí (snap range), nó tự khóa vào chỗ.
/// </summary>
public class RepairShipGame : QuestMinigameUI
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Tất cả mảnh vỡ trong mini-game")]
    public List<ShipPiece> pieces = new List<ShipPiece>();

    [Header("UI")]
    public Text  remainingText;
    public Text  timerText;
    public float timeLimit = 60f;
    public GameObject winPanel;
    public GameObject losePanel;

    [Tooltip("Bật game ngay khi scene load")]
    public bool startActive = true;

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private int   _placedCount;
    private float _timeRemaining;

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

    void Start()
    {
        if (startActive) Open(QuestID.RepairShip);
    }

    // ------------------------------------------------------------------ //
    //  OVERRIDE
    // ------------------------------------------------------------------ //

    protected override void OnOpen()
    {
        _placedCount   = 0;
        _timeRemaining = timeLimit;

        foreach (var p in pieces)
            if (p != null) p.ResetPiece();

        UpdateUI();

        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    // ------------------------------------------------------------------ //
    //  UPDATE
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
    //  PUBLIC (gọi từ ShipPiece khi snap thành công)
    // ------------------------------------------------------------------ //

    public void OnPiecePlaced()
    {
        _placedCount++;
        UpdateUI();

        Debug.Log($"[RepairShip] Đã ghép: {_placedCount}/{pieces.Count}");

        if (_placedCount >= pieces.Count)
        {
            isActive = false;
            winPanel?.SetActive(true);
            Complete();
        }
    }

    void UpdateUI()
    {
        if (remainingText != null)
            remainingText.text = $"Còn lại: {pieces.Count - _placedCount} mảnh";
    }
}
