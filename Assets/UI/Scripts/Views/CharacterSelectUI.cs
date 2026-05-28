using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Màn hình chọn nhân vật - UI Toolkit.
/// Hiển thị 3D preview qua Render Texture, swap mesh/material qua CharacterSkinManager.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField] private CharacterData[] characters;

    [Header("3D Preview")]
    [SerializeField] private RenderTexture previewRenderTexture;
    [SerializeField] private CharacterSkinManager previewSkinManager;
    [SerializeField] private Transform previewTransform;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float autoRotateSpeed = 20f;

    // UI Elements
    private VisualElement root;
    private VisualElement previewContainer;
    private Label lblCharacterName;
    private VisualElement dragOverlay;

    // Character card buttons & checks
    private Button[] charButtons;
    private VisualElement[] charChecks;

    // State
    private int selectedIndex = 0;
    private bool isDragging = false;
    private float lastPointerX = 0f;
    private float currentRotation = 0f;
    private IVisualElementScheduledItem autoRotateScheduler;

    // ===== LIFECYCLE =====

    
    // ===== INITIALIZE =====

    /// <summary>
    /// Khởi tạo UI - gọi từ UIManager hoặc Start()
    /// </summary>
    public void Initialize(VisualElement uiRoot)
    {
        root = uiRoot;

        // Query UI elements
        previewContainer = root.Q<VisualElement>("cs-preview-container");
        lblCharacterName = root.Q<Label>("lbl-character-name");
        dragOverlay = root.Q<VisualElement>("cs-drag-overlay");

        // Query character cards (8 cards)
        charButtons = new Button[8];
        charChecks = new VisualElement[8];
        for (int i = 0; i < 8; i++)
        {
            int index = i; // closure capture
            charButtons[i] = root.Q<Button>($"char-btn-{i}");
            charChecks[i] = root.Q<VisualElement>($"char-check-{i}");

            if (charButtons[i] != null)
                charButtons[i].clicked += () => SelectCharacter(index);
        }

        // Query buttons
        Button btnLockIn = root.Q<Button>("btn-lock-in");

        if (btnLockIn != null) btnLockIn.clicked += OnLockInClicked;

        // Gán Render Texture vào preview container
        SetupRenderTexture();

        // Setup drag input trên overlay
        SetupDragInput();

        // Load saved character hoặc default
        int savedIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, characters.Length - 1);
        
        // Preview trống lúc đầu - không chọn character nào
        selectedIndex = -1;
        if (lblCharacterName != null)
            lblCharacterName.text = "";
        
        // Reset card highlights
        for (int i = 0; i < charButtons.Length; i++)
        {
            if (charButtons[i] != null)
                charButtons[i].RemoveFromClassList("cs-char-selected");
            if (charChecks[i] != null)
                charChecks[i].AddToClassList("cs-hidden");
        }

        // Auto rotate nhẹ khi không drag
        autoRotateScheduler = root.schedule.Execute(AutoRotate).Every(16);
    }

    // ===== RENDER TEXTURE =====

    private void SetupRenderTexture()
    {
        if (previewContainer == null)
        {
            Debug.LogError("[CharacterSelectUI] Không tìm thấy cs-preview-container!");
            return;
        }

        if (previewRenderTexture == null)
        {
            Debug.LogError("[CharacterSelectUI] previewRenderTexture chưa được gán!");
            return;
        }

        // Gán Render Texture vào VisualElement background
        previewContainer.style.backgroundImage =
            new StyleBackground(Background.FromRenderTexture(previewRenderTexture));

        Debug.Log("[CharacterSelectUI] Render Texture đã được gán vào preview container.");
    }

    // ===== CHARACTER SELECTION =====

    /// <summary>
    /// Chọn nhân vật theo index - swap mesh/material qua CharacterSkinManager
    /// </summary>
    public void SelectCharacter(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length)
        {
            Debug.LogError($"[CharacterSelectUI] Invalid character index: {index}");
            return;
        }

        selectedIndex = index;
        CharacterData charData = characters[index];

        // Swap mesh + material trên PreviewObject
        if (previewSkinManager != null)
            previewSkinManager.ApplyCharacter(charData);
        else
            Debug.LogWarning("[CharacterSelectUI] previewSkinManager chưa được gán!");

        // Cập nhật tên nhân vật
        if (lblCharacterName != null)
            lblCharacterName.text = charData.CharacterName;

        // Cập nhật highlight cards
        UpdateCardHighlights(index);

        // Reset rotation về 0 khi đổi nhân vật
        currentRotation = 0f;

        Debug.Log($"[CharacterSelectUI] Selected: {charData.CharacterName}");
    }

    private void UpdateCardHighlights(int selectedIdx)
    {
        for (int i = 0; i < charButtons.Length; i++)
        {
            if (charButtons[i] == null) continue;

            if (i == selectedIdx)
            {
                // Highlight card được chọn
                charButtons[i].AddToClassList("cs-char-selected");
                if (charChecks[i] != null)
                    charChecks[i].RemoveFromClassList("cs-hidden");
            }
            else
            {
                // Bỏ highlight các card khác
                charButtons[i].RemoveFromClassList("cs-char-selected");
                if (charChecks[i] != null)
                    charChecks[i].AddToClassList("cs-hidden");
            }
        }
    }

    // ===== DRAG INPUT (xoay model) =====

    private void SetupDragInput()
    {
        if (dragOverlay == null)
        {
            Debug.LogWarning("[CharacterSelectUI] Không tìm thấy cs-drag-overlay!");
            return;
        }

        dragOverlay.RegisterCallback<PointerDownEvent>(OnPointerDown);
        dragOverlay.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        dragOverlay.RegisterCallback<PointerUpEvent>(OnPointerUp);
        dragOverlay.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        isDragging = true;
        lastPointerX = evt.localPosition.x;
        dragOverlay.CapturePointer(evt.pointerId);
        autoRotateScheduler?.Pause(); // Dừng auto rotate khi drag
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isDragging) return;

        float deltaX = evt.localPosition.x - lastPointerX;
        lastPointerX = evt.localPosition.x;

        currentRotation -= deltaX * rotationSpeed * Time.deltaTime;
        ApplyRotation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        isDragging = false;
        dragOverlay.ReleasePointer(evt.pointerId);
        autoRotateScheduler = root.schedule.Execute(AutoRotate).Every(16); // Resume auto rotate
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (isDragging)
        {
            isDragging = false;
            autoRotateScheduler = root.schedule.Execute(AutoRotate).Every(16);
        }
    }

    private void AutoRotate()
    {
        if (isDragging) return;
        currentRotation += autoRotateSpeed * Time.deltaTime;
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (previewTransform != null)
            previewTransform.rotation = Quaternion.Euler(0, currentRotation, 0);
    }

    // ===== BUTTONS =====

    private void OnLockInClicked()
    {
        if (characters == null || selectedIndex >= characters.Length) return;

        // Lưu lựa chọn vào PlayerPrefs
        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedIndex);
        PlayerPrefs.Save();

        Debug.Log($"[CharacterSelectUI] Locked in: {characters[selectedIndex].CharacterName} (index {selectedIndex})");

        // Load game
        UIManager.Instance.StartGameplay();
    }

    // ===== HELPERS =====

    private void DisablePreviewCamera()
    {
        // Tắt Preview Camera để tiết kiệm GPU khi không dùng
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (cam.targetTexture == previewRenderTexture)
            {
                cam.enabled = false;
                break;
            }
        }
    }

    public void EnablePreviewCamera()
    {
        // Bật lại Preview Camera khi vào màn hình
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (cam.targetTexture == previewRenderTexture)
            {
                cam.enabled = true;
                break;
            }
        }
    }
}
