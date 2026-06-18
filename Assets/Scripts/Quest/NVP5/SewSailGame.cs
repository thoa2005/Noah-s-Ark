using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// NVP5: Khâu vá cánh buồm rách.
/// Cận cảnh mảnh vải có các lỗ luồn chỉ theo 2 hàng (trái/phải).
/// Người chơi click lần lượt từng lỗ để luồn chỉ zigzag qua lại.
/// Đường chỉ được vẽ bằng LineRenderer hoặc UI Line.
/// </summary>
public class SewSailGame : QuestMinigameUI
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Các lỗ luồn chỉ (theo thứ tự từ trên xuống)")]
    [Tooltip("Gán theo thứ tự khâu: lỗ 0 → lỗ 1 → lỗ 2 → ... zigzag trái phải")]
    public List<SewHole> holes = new List<SewHole>();

    [Header("Đường chỉ UI")]
    public UILineRenderer threadLine;  // Tuỳ chọn: script vẽ đường thẳng giữa các điểm

    [Header("UI")]
    public Text  progressText;
    public Text  timerText;
    public float timeLimit = 50f;
    public GameObject winPanel;
    public GameObject losePanel;

    [Tooltip("Bật game ngay khi scene load")]
    public bool startActive = true;

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private int   _nextHoleIndex = 0;
    private float _timeRemaining;

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

    void Start()
    {
        if (startActive) Open(QuestID.SewSail);
    }

    // ------------------------------------------------------------------ //
    //  OVERRIDE
    // ------------------------------------------------------------------ //

    protected override void OnOpen()
    {
        _timeRemaining = timeLimit;
        _nextHoleIndex = 0;

        // Xoá đường chỉ cũ khi chơi lại
        threadLine?.ClearPoints();

        foreach (var h in holes)
            if (h != null) h.ResetHole();

        // Đánh dấu lỗ đầu tiên cần click
        HighlightNext();
        UpdateUI();

        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    protected override void OnClose()
    {
        StopAllCoroutines();
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
    //  PUBLIC (gọi từ SewHole khi click)
    // ------------------------------------------------------------------ //

    public void OnHoleClicked(SewHole hole)
    {
        if (!isActive) return;
        if (_nextHoleIndex >= holes.Count) return;

        SewHole expected = holes[_nextHoleIndex];

        if (hole != expected)
        {
            // Click sai lỗ → nhấp nháy đỏ để báo
            expected.FlashWrong();
            return;
        }

        // Đúng lỗ → mark sewn
        hole.MarkSewn();

        // Thêm điểm vào đường chỉ
        threadLine?.AddPoint(hole.GetComponent<RectTransform>().anchoredPosition);

        _nextHoleIndex++;
        UpdateUI();

        if (_nextHoleIndex >= holes.Count)
        {
            // Hoàn thành tất cả lỗ
            isActive = false;
            winPanel?.SetActive(true);
            Complete();
        }
        else
        {
            HighlightNext();
        }
    }

    // ------------------------------------------------------------------ //
    //  HELPER
    // ------------------------------------------------------------------ //

    void HighlightNext()
    {
        if (_nextHoleIndex < holes.Count)
            holes[_nextHoleIndex]?.SetHighlight(true);
    }

    void UpdateUI()
    {
        if (progressText != null)
            progressText.text = $"{_nextHoleIndex}/{holes.Count} mũi khâu";
    }
}
