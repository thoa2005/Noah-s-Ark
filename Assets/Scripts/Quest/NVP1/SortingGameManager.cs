using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SortingGameManager : MonoBehaviour
{
    public static SortingGameManager Instance { get; private set; }

    [Header("Kéo tất cả quả vào đây")]
    public List<GameObject> fruits = new List<GameObject>();

    [Header("Panel tổng của mini-game (kéo GameObject cha chứa toàn bộ UI vào đây)")]
    public GameObject miniGamePanel;

    [Header("UI")]
    public Text remainingText;
    public Text timerText;
    public float timeLimit = 60f;
    public GameObject winPanel;
    public GameObject losePanel;

    private int   _total;
    private int   _sorted;
    private float _timeLeft;
    private bool  _running;

    // ------------------------------------------------------------------ //

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
        _running = true;

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
            Invoke(nameof(CloseMiniGame), 2f);
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
            Invoke(nameof(CloseMiniGame), 2f); // Đợi 2 giây để người chơi thấy WinPanel rồi đóng
        }
    }

    void UpdateUI()
    {
        if (remainingText != null)
            remainingText.text = $"Còn lại: {_total - _sorted} quả";
    }

    // ------------------------------------------------------------------ //

    public void CloseMiniGame()
    {
        // 1. Ẩn toàn bộ panel mini-game
        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);

        // 2. Trả quyền điều khiển về cho player
        RestorePlayerControl();

        Debug.Log("[SortingGame] Đã đóng mini-game.");
    }

    void RestorePlayerControl()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("[SortingGame] Không tìm thấy GameObject tag 'Player'.");
            return;
        }

        // Enable lại PlayerMovement
        PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = true;

        // Reset input tránh "giữ phím" ảo
        CharacterInput input = playerObj.GetComponent<CharacterInput>();
        if (input != null)
        {
            input.ClearAllInputs();
            input.enabled = true;
        }

        // Enable lại PlayerInput (Unity Input System)
        var playerInput = playerObj.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null) playerInput.enabled = true;

        // Đảm bảo timeScale bình thường
        Time.timeScale = 1f;

        // Lock cursor lại cho game 3D
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        Debug.Log("[SortingGame] Đã trả quyền điều khiển về cho player.");
    }
}
