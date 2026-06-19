using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RepairShipGame : QuestMinigameUI
{
    [Header("Panel chứa toàn bộ UI mini-game")]
    public GameObject Panel;

    [Header("Tất cả mảnh vỡ trong mini-game")]
    public List<ShipPiece> Pieces = new List<ShipPiece>();

    [Header("Canvas Camera (để null nếu dùng Screen Space Overlay)")]
    public Camera uiCamera;

    [Header("UI")]
    public Text RemainingText;
    public Text TimerText;
    public float TimeLimit = 60f;
    public GameObject WinPanel;
    public GameObject LosePanel;
    public bool StartActive = true;

    [Tooltip("Thời gian hiển thị WinPanel trước khi thoát (giây)")]
    public float winDisplayDuration = 2.5f;

    private ShipPiece _draggingPiece = null;
    private Vector2   _dragStartPos;
    private float     _countdownTimer;

    // ------------------------------------------------------------------ //

    void Start()
    {
        if (StartActive) Open(QuestID.RepairShip);
    }

    protected override void OnOpen()
    {
        if (WinPanel  != null) WinPanel.SetActive(false);
        if (LosePanel != null) LosePanel.SetActive(false);
        _countdownTimer = TimeLimit;

        // Lưu vị trí đúng của từng mảnh (vị trí bạn đã sắp xếp trong Editor)
        for (int i = 0; i < Pieces.Count; i++)
        {
            if (Pieces[i] == null) continue;
            RectTransform rect = Pieces[i].GetComponent<RectTransform>();
            Pieces[i].SetupPiece(this, rect.anchoredPosition);
            Debug.Log($"[RepairShip] Piece {i} correctPos = {rect.anchoredPosition}");
        }

        ShufflePieces();
        UpdateUI();
    }

    void Update()
    {
        if (!isActive) return;
        HandleTimer();
        HandlePuzzleInput();
    }

    // ------------------------------------------------------------------ //

    void HandleTimer()
    {
        if (_countdownTimer <= 0) return;

        _countdownTimer -= Time.deltaTime;
        if (TimerText != null)
            TimerText.text = Mathf.CeilToInt(Mathf.Max(0, _countdownTimer)).ToString();

        if (_countdownTimer <= 0)
        {
            isActive = false;
            if (LosePanel != null) LosePanel.SetActive(true);
            Fail();
        }
    }

    void HandlePuzzleInput()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        // --- Nhấn chuột: tìm mảnh chưa khóa ---
        if (mouse.leftButton.wasPressedThisFrame)
        {
            foreach (var piece in Pieces)
            {
                if (piece == null || piece.IsLocked) continue;

                RectTransform rect = piece.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, uiCamera))
                {
                    _draggingPiece = piece;
                    _dragStartPos  = rect.anchoredPosition;
                    piece.transform.SetAsLastSibling();
                    Debug.Log($"[RepairShip] Đang kéo: {piece.name}");
                    break;
                }
            }
        }

        // --- Giữ chuột: di chuyển mảnh ---
        if (mouse.leftButton.isPressed && _draggingPiece != null)
        {
            RectTransform rect = _draggingPiece.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect.parent as RectTransform, mousePos, uiCamera, out Vector2 localPoint))
            {
                rect.anchoredPosition = localPoint;
            }
        }

        // --- Thả chuột: thử snap, không được thì hoàn về ---
        if (mouse.leftButton.wasReleasedThisFrame && _draggingPiece != null)
        {
            bool snapped = _draggingPiece.TrySnap();
            Debug.Log($"[RepairShip] Thả {_draggingPiece.name} → snap = {snapped}");

            if (!snapped)
                _draggingPiece.GetComponent<RectTransform>().anchoredPosition = _dragStartPos;

            _draggingPiece = null;
            UpdateUI();
            CheckWinCondition();
        }
    }

    // ------------------------------------------------------------------ //

    void ShufflePieces()
    {
        if (Pieces.Count < 2) return;

        // Fisher-Yates shuffle
        List<Vector2> positions = new List<Vector2>();
        foreach (var p in Pieces)
            positions.Add(p.GetComponent<RectTransform>().anchoredPosition);

        // Đảm bảo kết quả khác với trạng thái ban đầu (thử lại nếu giống)
        List<Vector2> shuffled = new List<Vector2>(positions);
        int attempts = 0;
        do
        {
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Vector2 tmp  = shuffled[i];
                shuffled[i]  = shuffled[j];
                shuffled[j]  = tmp;
            }
            attempts++;
        }
        while (IsSameOrder(positions, shuffled) && attempts < 10);

        for (int i = 0; i < Pieces.Count; i++)
        {
            Pieces[i].GetComponent<RectTransform>().anchoredPosition = shuffled[i];
            Debug.Log($"[RepairShip] Sau shuffle: Piece {i} → {shuffled[i]}");
        }
    }

    bool IsSameOrder(List<Vector2> a, List<Vector2> b)
    {
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    void UpdateUI()
    {
        if (RemainingText == null) return;
        int wrong = 0;
        foreach (var p in Pieces)
            if (p != null && !p.IsInCorrectPosition()) wrong++;
        RemainingText.text = $"Mảnh chưa khớp: {wrong}";
    }

    void CheckWinCondition()
    {
        int remaining = 0;
        foreach (var p in Pieces)
            if (p != null && !p.IsInCorrectPosition()) remaining++;

        Debug.Log($"[RepairShip] CheckWin — còn {remaining} mảnh chưa khớp, WinPanel = {(WinPanel != null ? WinPanel.name : "NULL")}");

        if (remaining > 0) return;

        isActive = false;
        if (WinPanel != null)
            WinPanel.SetActive(true);
        else
            Debug.LogError("[RepairShip] WinPanel chưa được gán trong Inspector!");

        StartCoroutine(CompleteAfterDelay());
    }

    IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSecondsRealtime(winDisplayDuration);
        Complete();
    }
}
