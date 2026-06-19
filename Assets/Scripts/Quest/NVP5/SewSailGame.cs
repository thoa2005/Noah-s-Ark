using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SewSailGame : MonoBehaviour
{
    public static SewSailGame Instance;

    [Header("Các lỗ theo đúng thứ tự khâu")]
    public List<SewHole> Holes = new List<SewHole>();

    [Header("Số lỗ đã khâu sẵn từ đầu")]
    public int prefilledCount = 2;

    [Header("Đường chỉ")]
    public UILineRenderer threadLine;

    [Header("UI")]
    public Text       progressText;
    public Text       timerText;
    public float      timeLimit = 60f;
    public GameObject winPanel;
    public GameObject losePanel;

    private int   _nextIndex;
    private float _timeLeft;
    private bool  _running;

    // ------------------------------------------------------------------ //

    void Awake() { Instance = this; }

    void Start() { StartGame(); }

    void Update()
    {
        if (!_running || timeLimit <= 0f) return;
        _timeLeft -= Time.deltaTime;
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(Mathf.Max(0, _timeLeft)).ToString();
        if (_timeLeft <= 0f)
        {
            _running = false;
            if (losePanel != null) losePanel.SetActive(true);
            Debug.Log("[SewSail] ⏰ Hết giờ!");
        }
    }

    // ------------------------------------------------------------------ //

    public void StartGame()
    {
        _timeLeft = timeLimit;
        _running  = true;

        threadLine?.ClearPoints();

        foreach (var h in Holes)
            if (h != null) h.ResetHole();

        // Điền sẵn các lỗ đã khâu trước
        int pre = Mathf.Min(prefilledCount, Holes.Count);
        for (int i = 0; i < pre; i++)
        {
            Holes[i].MarkSewn();
            AddThreadPoint(Holes[i]);
        }

        _nextIndex = 0;
        // Không highlight theo thứ tự — người chơi click tự do

        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    // ------------------------------------------------------------------ //

    public void OnHoleClicked(SewHole hole)
    {
        if (!_running) return;

        // Không cần thứ tự — click lỗ nào khâu lỗ đó
        if (hole.IsSewn) return;

        hole.MarkSewn();
        AddThreadPoint(hole);
        _nextIndex++;
        UpdateUI();

        Debug.Log($"[SewSail] ✅ {hole.name} — {_nextIndex}/{Holes.Count}");

        if (_nextIndex >= Holes.Count)
        {
            _running = false;
            if (winPanel != null) winPanel.SetActive(true);
            Debug.Log("[SewSail] 🎉 Hoàn thành!");
        }
    }

    // ------------------------------------------------------------------ //

    /// <summary>
    /// Lấy vị trí của lỗ trong local space của ThreadLine để vẽ đúng chỗ.
    /// ThreadLine phải stretch full Canvas (Left=Right=Top=Bottom=0).
    /// </summary>
    void AddThreadPoint(SewHole hole)
    {
        if (threadLine == null) return;

        // Lấy screen position của lỗ — dùng null camera cho Screen Space Overlay
        Canvas canvas = threadLine.GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, hole.transform.position);

        // Convert sang local point của ThreadLine
        RectTransform lineRT = threadLine.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineRT, screenPos, cam, out Vector2 localPoint);

        threadLine.AddPoint(localPoint);
        Debug.Log($"[SewSail] Vẽ điểm tại {localPoint}");
    }

    void UpdateUI()
    {
        if (progressText != null)
            progressText.text = $"{_nextIndex}/{Holes.Count} mũi khâu";
    }
}
