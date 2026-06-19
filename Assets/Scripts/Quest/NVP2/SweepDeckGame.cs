using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// NVP2: Quét dọn sàn tàu. Di chuột qua các vết bẩn để làm sạch.
/// </summary>
public class SweepDeckGame : QuestMinigameUI
{
    [Header("Vết bẩn")]
    public List<DirtStain> stains = new List<DirtStain>();

    [Header("Chổi theo chuột")]
    public RectTransform brushCursor;

    [Header("Background")]
    public Image backgroundImage;
    public Sprite backgroundSprite;

    [Header("UI")]
    public Text  remainingText;
    public Text  timerText;
    public float timeLimit = 45f;
    public GameObject winPanel;
    public GameObject losePanel;

    [Tooltip("Bật game ngay khi scene load")]
    public bool startActive = true;

    [Tooltip("Thời gian hiển thị WinPanel trước khi thoát (giây)")]
    public float winDisplayDuration = 2.5f;

    private int   _totalStains;
    private int   _cleanedCount;
    private float _timeRemaining;

    // ------------------------------------------------------------------ //

    void Start()
    {
        if (startActive) Open(QuestID.SweepDeck);
    }

    protected override void OnOpen()
    {
        // Set background
        if (backgroundImage != null && backgroundSprite != null)
            backgroundImage.sprite = backgroundSprite;

        _cleanedCount  = 0;
        _timeRemaining = timeLimit;

        foreach (var s in stains)
            if (s != null) s.ResetStain();   // ← tên mới tránh conflict

        _totalStains = stains.Count;
        UpdateUI();

        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;

        MoveBrushCursor();

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

  void MoveBrushCursor()
    {
        if (brushCursor == null) return;
        
        // Lấy vị trí chuột theo hệ thống Input System mới
        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            brushCursor.parent as RectTransform,
            mousePos, null, 
            out Vector2 localPoint);
            
        brushCursor.localPosition = localPoint;
    }

    public void OnStainCleaned()
    {
        _cleanedCount++;
        UpdateUI();
        Debug.Log($"[SweepDeck] Sạch: {_cleanedCount}/{_totalStains}");

        if (_cleanedCount >= _totalStains)
        {
            isActive = false;
            if (winPanel != null) winPanel.SetActive(true);
            StartCoroutine(CompleteAfterDelay());
        }
    }

    IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSecondsRealtime(winDisplayDuration);
        Complete(); // đóng panel và trả quyền điều khiển sau khi WinPanel đã hiện đủ lâu
    }

    void UpdateUI()
    {
        if (remainingText != null)
            remainingText.text = $"Còn lại: {_totalStains - _cleanedCount} vết bẩn";
    }
}
