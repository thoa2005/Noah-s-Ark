using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// NVP3: Đóng đinh tấm ván.
/// Góc nhìn từ trên xuống. 3 tấm ván bị vênh, mỗi tấm có 1 đinh.
/// Người chơi di chuột đưa búa đến vị trí đinh, click 2-3 lần để đóng.
/// </summary>
public class NailBoardsGame : QuestMinigameUI
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Danh sách đinh cần đóng")]
    public List<NailPoint> nails = new List<NailPoint>();

    [Header("Búa (theo con trỏ chuột)")]
    public RectTransform hammerCursor;

    [Header("UI")]
    public Text  remainingText;
    public Text  timerText;
    public float timeLimit = 40f;
    public GameObject winPanel;
    public GameObject losePanel;

    [Tooltip("Bật game ngay khi scene load")]
    public bool startActive = true;

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private int   _fixedCount;
    private float _timeRemaining;

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

    void Start()
    {
        if (startActive) Open(QuestID.NailBoards);
    }

    // ------------------------------------------------------------------ //
    //  OVERRIDE
    // ------------------------------------------------------------------ //

    protected override void OnOpen()
    {
        _fixedCount    = 0;
        _timeRemaining = timeLimit;

        foreach (var n in nails)
            if (n != null) n.ResetNail();

        UpdateUI();

        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    // ------------------------------------------------------------------ //
    //  UPDATE
    // ------------------------------------------------------------------ //

    void Update()
    {
        if (!isActive) return;

        MoveHammerCursor();

        if (timeLimit > 0f)
        {
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
    }

    void MoveHammerCursor()
    {
        if (hammerCursor == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hammerCursor.parent as RectTransform,
            Input.mousePosition,
            null,
            out Vector2 localPoint);

        hammerCursor.localPosition = localPoint;
    }

    // ------------------------------------------------------------------ //
    //  PUBLIC (gọi từ NailPoint)
    // ------------------------------------------------------------------ //

    public void OnNailFixed()
    {
        _fixedCount++;
        UpdateUI();

        Debug.Log($"[NailBoards] Đã đóng: {_fixedCount}/{nails.Count}");

        if (_fixedCount >= nails.Count)
        {
            isActive = false;
            winPanel?.SetActive(true);
            Complete();
        }
    }

    void UpdateUI()
    {
        if (remainingText != null)
            remainingText.text = $"Còn lại: {nails.Count - _fixedCount} tấm ván";
    }
}
