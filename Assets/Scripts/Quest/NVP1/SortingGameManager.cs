using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Tạo một GameObject tên "SortingGameManager" trong scene.
// Gắn script này vào. Kéo tất cả quả vào danh sách Fruits trong Inspector.
public class SortingGameManager : MonoBehaviour
{
    public static SortingGameManager Instance { get; private set; }

    [Header("Kéo tất cả quả vào đây")]
    public List<GameObject> fruits = new List<GameObject>();

    [Header("UI (tuỳ chọn)")]
    public Text  remainingText;
    public Text  timerText;
    public float timeLimit    = 60f;
    public GameObject winPanel;
    public GameObject losePanel;

    private int   _total;
    private int   _sorted;
    private float _timeLeft;
    private bool  _running;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        _total   = fruits.Count;
        _sorted  = 0;
        _timeLeft = timeLimit;
        _running  = true;

        foreach (var f in fruits)
            if (f != null) f.SetActive(true);

        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        UpdateUI();
        Debug.Log($"[SortingGame] Bắt đầu — {_total} quả");
    }

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
            Debug.Log("[SortingGame] ⏰ Hết giờ!");
        }
    }

    // Gọi từ FruitBin mỗi khi có quả vào đúng thùng
    public void OnFruitSorted()
    {
        _sorted++;
        UpdateUI();
        Debug.Log($"[SortingGame] {_sorted}/{_total}");

        if (_sorted >= _total)
        {
            _running = false;
            if (winPanel != null) winPanel.SetActive(true);
            Debug.Log("[SortingGame] 🎉 Hoàn thành!");
        }
    }

    void UpdateUI()
    {
        if (remainingText != null)
            remainingText.text = $"Còn lại: {_total - _sorted} quả";
    }
}
