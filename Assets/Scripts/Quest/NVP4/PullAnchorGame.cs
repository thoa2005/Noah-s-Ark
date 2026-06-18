using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NVP4: Kéo mỏ neo.
/// Màn hình chia 2:
///   - Trái: Tay quay cận cảnh — người chơi xoay chuột theo vòng tròn CW.
///   - Phải: Góc nhìn ngang con thuyền — mỏ neo di chuyển từ đáy biển lên.
///
/// Cơ chế:
///   Mỗi frame tính góc chuột so với tâm tay quay.
///   Nếu chuột đang xoay CW → tích luỹ progress.
///   Khi progress = 1.0 → hoàn thành.
/// </summary>
public class PullAnchorGame : QuestMinigameUI
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Tay quay")]
    public RectTransform crankCenter;       // Tâm của tay quay (gán trong Inspector)
    public RectTransform crankHandle;       // Hình tay quay xoay theo góc chuột
    public float         crankRadius = 80f; // Bán kính vùng xoay hợp lệ (px)

    [Header("Tiến độ")]
    public Slider progressSlider;           // 0 → 1
    public float  rotationNeeded = 720f;    // Tổng độ cần xoay (2 vòng = 720°)
    public float  rotationDecay  = 30f;     // Độ giảm/giây nếu không xoay

    [Header("Mỏ neo (phần góc nhìn ngang)")]
    public RectTransform anchorImage;       // Hình mỏ neo
    public Vector2       anchorStartPos;    // Vị trí dưới biển
    public Vector2       anchorEndPos;      // Vị trí trên thuyền

    [Header("UI")]
    public Text timerText;
    public float timeLimit = 50f;
    public GameObject winPanel;
    public GameObject losePanel;

    [Tooltip("Bật game ngay khi scene load")]
    public bool startActive = true;

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private float _totalRotation  = 0f;
    private float _prevAngle      = 0f;
    private bool  _mouseInRange   = false;
    private float _timeRemaining;

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

    void Start()
    {
        if (startActive) Open(QuestID.PullAnchor);
    }

    // ------------------------------------------------------------------ //
    //  OVERRIDE
    // ------------------------------------------------------------------ //

    protected override void OnOpen()
    {
        _totalRotation = 0f;
        _timeRemaining = timeLimit;

        if (progressSlider != null) progressSlider.value = 0f;
        if (anchorImage    != null) anchorImage.anchoredPosition = anchorStartPos;
        if (winPanel       != null) winPanel.SetActive(false);
        if (losePanel      != null) losePanel.SetActive(false);

        // Lấy góc ban đầu của chuột
        _prevAngle = GetMouseAngle();
    }

    // ------------------------------------------------------------------ //
    //  UPDATE
    // ------------------------------------------------------------------ //

    void Update()
    {
        if (!isActive) return;

        HandleCrank();

        // Đếm ngược
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

    void HandleCrank()
    {
        float currentAngle = GetMouseAngle();
        float delta        = Mathf.DeltaAngle(_prevAngle, currentAngle);

        // Kiểm tra chuột có trong vùng bán kính tay quay không
        if (crankCenter != null)
        {
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                crankCenter.parent as RectTransform,
                Input.mousePosition, null, out mousePos);
            float dist = Vector2.Distance(mousePos, crankCenter.anchoredPosition);
            _mouseInRange = dist <= crankRadius;
        }

        if (_mouseInRange)
        {
            // Xoay CW → delta âm trong Unity (góc giảm khi CW)
            if (delta < 0f)
                _totalRotation += Mathf.Abs(delta);
            else
                _totalRotation = Mathf.Max(0f, _totalRotation - rotationDecay * Time.unscaledDeltaTime);
        }
        else
        {
            // Chuột ra ngoài → chậm dần
            _totalRotation = Mathf.Max(0f, _totalRotation - rotationDecay * Time.unscaledDeltaTime);
        }

        // Xoay hình tay quay
        if (crankHandle != null)
            crankHandle.localEulerAngles = new Vector3(0f, 0f, -currentAngle);

        // Cập nhật progress và vị trí mỏ neo
        float progress = Mathf.Clamp01(_totalRotation / rotationNeeded);
        UpdateProgress(progress);

        _prevAngle = currentAngle;

        if (progress >= 1f)
        {
            isActive = false;
            winPanel?.SetActive(true);
            Complete();
        }
    }

    void UpdateProgress(float t)
    {
        if (progressSlider != null)
            progressSlider.value = t;

        if (anchorImage != null)
            anchorImage.anchoredPosition = Vector2.Lerp(anchorStartPos, anchorEndPos, t);
    }

    float GetMouseAngle()
    {
        if (crankCenter == null) return 0f;

        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            crankCenter.parent as RectTransform,
            Input.mousePosition, null, out mousePos);

        Vector2 dir = mousePos - crankCenter.anchoredPosition;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }
}
