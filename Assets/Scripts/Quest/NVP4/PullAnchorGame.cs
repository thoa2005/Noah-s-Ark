using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// NVP4: Kéo mỏ neo. Click vào tay quay mỗi lần = neo lên 1 bậc.
/// </summary>
public class AnchorGame : QuestMinigameUI
{
    [Header("Tay quay - chỉ dùng để phát hiện click")]
    public RectTransform crankCenter;

    [Header("Mỏ neo")]
    public RectTransform anchorImage;
    public Vector2 anchorStartPos;   // vị trí neo lúc đầu (dưới nước)
    public Vector2 anchorEndPos;     // vị trí neo khi kéo lên xong

    [Header("Số lần click cần để kéo neo lên hoàn toàn")]
    public int clicksRequired = 10;

    [Header("UI")]
    public Slider progressSlider;
    public Text   timerText;
    public float  timeLimit   = 50f;
    public GameObject winPanel;
    public GameObject losePanel;

    [Tooltip("Bật game ngay khi scene load")]
    public bool startActive = true;

    [Tooltip("Thời gian hiển thị WinPanel trước khi thoát (giây)")]
    public float winDisplayDuration = 2.5f;

    private int   _clickCount      = 0;
    private float _timeRemaining   = -1f; // -1 = chưa khởi tạo, tránh Fail ngay frame đầu
    private bool  _initialized     = false;

    // ------------------------------------------------------------------ //

    void Start()
    {
        if (startActive) Open(QuestID.PullAnchor);
    }

    protected override void OnOpen()
    {
        _clickCount    = 0;
        _timeRemaining = timeLimit;  // set đúng giá trị trước khi Update chạy
        _initialized   = true;

        if (anchorImage    != null) anchorImage.anchoredPosition = anchorStartPos;
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = clicksRequired;
            progressSlider.value    = 0;
        }
        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;

        // --- Đếm giờ ---
        if (timeLimit > 0f && _initialized)
        {
            _timeRemaining -= Time.unscaledDeltaTime;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(Mathf.Max(0f, _timeRemaining)).ToString();

            if (_timeRemaining <= 0f)
            {
                isActive = false;
                if (losePanel != null) losePanel.SetActive(true);
                Fail();
                return;
            }
        }

        // --- Phát hiện click ---
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = mouse.position.ReadValue();

            if (crankCenter != null &&
                RectTransformUtility.RectangleContainsScreenPoint(crankCenter, mousePos, null))
            {
                OnCrankClicked();
            }
        }
    }

    void OnCrankClicked()
    {
        _clickCount++;

        // Tính % hoàn thành
        float percent = Mathf.Clamp01((float)_clickCount / clicksRequired);

        // Kéo neo lên theo %
        if (anchorImage != null)
            anchorImage.anchoredPosition = Vector2.Lerp(anchorStartPos, anchorEndPos, percent);

        // Cập nhật thanh tiến độ
        if (progressSlider != null)
            progressSlider.value = _clickCount;

        Debug.Log($"[AnchorGame] Click {_clickCount}/{clicksRequired}");

        // Kiểm tra thắng
        if (_clickCount >= clicksRequired)
        {
            isActive = false;
            if (winPanel != null) winPanel.SetActive(true);
            StartCoroutine(CompleteAfterDelay());
        }
    }

    IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSecondsRealtime(winDisplayDuration);
        Complete();
    }
}
