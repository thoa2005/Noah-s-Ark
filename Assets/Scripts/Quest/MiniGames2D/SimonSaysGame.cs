using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Mini-game NVP3: Simon Says - Bấm nút theo thứ tự 2D.
/// Game hiển thị dãy nút sáng theo thứ tự, người chơi phải bấm lại đúng thứ tự đó.
/// 
/// Kế thừa QuestMinigameUI → tự động mở/đóng panel và báo kết quả về QuestManager.
/// </summary>
public class SimonSaysGame : QuestMinigameUI
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Nút bấm trong game (gán theo thứ tự)")]
    public Button[] buttons;
    public Color    activeColor   = Color.yellow;
    public Color    defaultColor  = Color.white;

    [Header("Cài đặt")]
    public int   startSequenceLength = 3;   // Dãy bắt đầu
    public int   maxSequenceLength   = 6;   // Dãy tối đa để thắng
    public float flashDuration       = 0.5f;
    public float flashInterval       = 0.3f;

    [Header("UI")]
    public Text  statusText;
    public GameObject winPanel;
    public GameObject losePanel;

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private List<int> _sequence     = new List<int>();
    private int       _playerIndex  = 0;
    private bool      _playerTurn   = false;

    // ------------------------------------------------------------------ //
    //  OVERRIDE
    // ------------------------------------------------------------------ //

    protected override void OnOpen()
    {
        _sequence.Clear();
        _playerIndex = 0;
        _playerTurn  = false;

        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        // Gán sự kiện cho từng nút
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i; // capture for lambda
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => OnPlayerPress(idx));
        }

        StartCoroutine(NextRound());
    }

    protected override void OnClose()
    {
        StopAllCoroutines();
    }

    // ------------------------------------------------------------------ //
    //  LOGIC
    // ------------------------------------------------------------------ //

    IEnumerator NextRound()
    {
        _playerTurn = false;

        if (statusText != null) statusText.text = "Quan sát...";

        // Thêm 1 bước ngẫu nhiên vào dãy
        _sequence.Add(Random.Range(0, buttons.Length));

        // Kiểm tra thắng
        if (_sequence.Count > maxSequenceLength)
        {
            winPanel?.SetActive(true);
            Complete();
            yield break;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // Hiển thị dãy
        foreach (int idx in _sequence)
        {
            yield return FlashButton(idx);
            yield return new WaitForSecondsRealtime(flashInterval);
        }

        // Đến lượt người chơi
        _playerIndex = 0;
        _playerTurn  = true;

        if (statusText != null) statusText.text = "Bấm theo thứ tự!";
    }

    IEnumerator FlashButton(int idx)
    {
        SetButtonColor(idx, activeColor);
        yield return new WaitForSecondsRealtime(flashDuration);
        SetButtonColor(idx, defaultColor);
    }

    void OnPlayerPress(int idx)
    {
        if (!isActive || !_playerTurn) return;

        StartCoroutine(FlashButton(idx));

        if (idx != _sequence[_playerIndex])
        {
            // Sai thứ tự
            _playerTurn = false;
            losePanel?.SetActive(true);
            Fail();
            return;
        }

        _playerIndex++;

        if (_playerIndex >= _sequence.Count)
        {
            // Hoàn thành vòng này, sang vòng mới
            StartCoroutine(NextRound());
        }
    }

    void SetButtonColor(int idx, Color color)
    {
        if (idx < 0 || idx >= buttons.Length) return;
        var colors = buttons[idx].colors;
        colors.normalColor = color;
        buttons[idx].colors = colors;
    }
}
