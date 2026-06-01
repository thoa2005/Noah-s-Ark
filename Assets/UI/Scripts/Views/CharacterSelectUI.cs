using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Character Selection Screen.
/// - Player preview ẩn cho đến khi chọn nhân vật lần đầu.
/// - Kéo chuột trái trên preview để xoay model.
/// - Camera position/rotation/FOV chỉnh thẳng trên Transform của camera GameObject trong scene.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterData[] characters;

    [Header("Preview")]
    [SerializeField] private GameObject       previewPlayerRoot;   // Player GameObject (inactive lúc đầu)
    [SerializeField] private Transform        previewRotateTarget; // Transform để xoay (Player root)
    [SerializeField] private Camera           previewCamera;
    [SerializeField] private RenderTexture    previewRT;
    [SerializeField] private CharacterSkinManager previewSkin;

    [Header("Rotation")]
    [SerializeField] private float dragSensitivity = 0.4f;
    [SerializeField] private float autoRotateSpeed = 20f;

    // ── UI refs ───────────────────────────────────────────────────────────
    private VisualElement    previewContainer;
    private Label            lblName;
    private VisualElement    dragOverlay;
    private Button[]         cardButtons = new Button[8];
    private VisualElement[]  cardChecks  = new VisualElement[8];

    // ── State ─────────────────────────────────────────────────────────────
    private int   selectedIndex = -1;
    private float yaw           = 0f;
    private bool  isDragging    = false;
    private float lastX         = 0f;
    private IVisualElementScheduledItem autoRotateTick;

    // ═════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Gọi từ UIManager khi hiện màn hình này.</summary>
    public void Initialize(VisualElement uiRoot)
    {
        previewContainer = uiRoot.Q<VisualElement>("cs-preview-container");
        lblName          = uiRoot.Q<Label>("lbl-character-name");
        dragOverlay      = uiRoot.Q<VisualElement>("cs-drag-overlay");

        for (int i = 0; i < 8; i++)
        {
            int idx = i;
            cardButtons[i] = uiRoot.Q<Button>($"char-btn-{i}");
            cardChecks[i]  = uiRoot.Q<VisualElement>($"char-check-{i}");
            cardButtons[i]?.RegisterCallback<ClickEvent>(_ => SelectCharacter(idx));
        }

        uiRoot.Q<Button>("btn-lock-in")?.RegisterCallback<ClickEvent>(_ => OnLockIn());

        // Gán RT — resize pixel-perfect theo kích thước container
        if (previewContainer != null && previewRT != null)
        {
            previewContainer.RegisterCallback<GeometryChangedEvent>(OnPreviewContainerResized);
            previewContainer.style.backgroundImage =
                new StyleBackground(Background.FromRenderTexture(previewRT));
        }

        ResetSelection();
        RegisterDragCallbacks();
        autoRotateTick = uiRoot.schedule.Execute(Tick).Every(16);
    }

    /// <summary>Gọi từ UIManager khi vào màn hình.</summary>
    public void EnablePreviewCamera()
    {
        if (previewCamera != null) previewCamera.enabled = true;
    }

    /// <summary>Gọi từ UIManager khi thoát màn hình.</summary>
    public void DisablePreviewCamera()
    {
        if (previewCamera != null) previewCamera.enabled = false;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  RENDER TEXTURE — resize để tránh blur
    // ═════════════════════════════════════════════════════════════════════

    private void OnPreviewContainerResized(GeometryChangedEvent e)
    {
        if (previewRT == null || previewContainer == null) return;

        float panelScale = previewContainer.panel?.scaledPixelsPerPoint ?? 1f;
        int w = Mathf.Max(64, Mathf.RoundToInt(e.newRect.width  * panelScale));
        int h = Mathf.Max(64, Mathf.RoundToInt(e.newRect.height * panelScale));

        if (previewRT.width == w && previewRT.height == h) return;

        previewRT.Release();
        previewRT.width  = w;
        previewRT.height = h;
        previewRT.Create();

        previewContainer.style.backgroundImage =
            new StyleBackground(Background.FromRenderTexture(previewRT));

        if (previewCamera != null)
            previewCamera.targetTexture = previewRT;

        Debug.Log($"[CharacterSelectUI] RT resized {w}×{h}");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  SELECTION
    // ═════════════════════════════════════════════════════════════════════

    private void SelectCharacter(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length) return;

        selectedIndex = index;
        ActivatePreviewPlayer();
        previewSkin?.ApplyCharacter(characters[index]);

        if (lblName != null) lblName.text = characters[index].CharacterName;
        UpdateCardHighlights(index);

        yaw = 0f;
        Debug.Log($"[CharacterSelectUI] Selected: {characters[index].CharacterName}");
    }

    private void OnLockIn()
    {
        if (selectedIndex < 0) return;
        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedIndex);
        PlayerPrefs.Save();
        UIManager.Instance.StartGameplay();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PREVIEW PLAYER
    // ═════════════════════════════════════════════════════════════════════

    private void ActivatePreviewPlayer()
    {
        if (previewPlayerRoot == null || previewPlayerRoot.activeSelf) return;

        previewPlayerRoot.SetActive(true);

        // Freeze physics
        foreach (var rb in previewPlayerRoot.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic     = true;
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Disable gameplay scripts
        foreach (var mb in previewPlayerRoot.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb is PlayerMovement or ActiveRagdollController or ActiveRagdollBalancer
                   or PlayerCombat   or CharacterInput          or GroundDetect
                   or CombatDetect)
                mb.enabled = false;
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  DRAG TO ROTATE
    // ═════════════════════════════════════════════════════════════════════

    private void RegisterDragCallbacks()
    {
        if (dragOverlay == null) return;
        dragOverlay.RegisterCallback<PointerDownEvent>(OnPointerDown);
        dragOverlay.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        dragOverlay.RegisterCallback<PointerUpEvent>(OnPointerUp);
        dragOverlay.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    private void OnPointerDown(PointerDownEvent e)
    {
        isDragging = true;
        lastX      = e.localPosition.x;
        dragOverlay.CapturePointer(e.pointerId);
        autoRotateTick?.Pause();
    }

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (!isDragging) return;
        yaw  -= (e.localPosition.x - lastX) * dragSensitivity;
        lastX = e.localPosition.x;
        ApplyYaw();
    }

    private void OnPointerUp(PointerUpEvent e)
    {
        isDragging = false;
        dragOverlay.ReleasePointer(e.pointerId);
        autoRotateTick?.Resume();
    }

    private void OnPointerLeave(PointerLeaveEvent e)
    {
        if (!isDragging) return;
        isDragging = false;
        autoRotateTick?.Resume();
    }

    private void Tick()
    {
        if (isDragging || previewRotateTarget == null) return;
        yaw += autoRotateSpeed * Time.deltaTime;
        ApplyYaw();
    }

    private void ApplyYaw()
    {
        if (previewRotateTarget != null)
            previewRotateTarget.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════

    private void ResetSelection()
    {
        selectedIndex = -1;
        if (lblName != null) lblName.text = "";
        for (int i = 0; i < 8; i++)
        {
            cardButtons[i]?.RemoveFromClassList("cs-cap-selected");
            cardChecks[i]?.AddToClassList("cs-hidden");
        }
    }

    private void UpdateCardHighlights(int idx)
    {
        for (int i = 0; i < 8; i++)
        {
            bool selected = i == idx;
            if (selected) cardButtons[i]?.AddToClassList("cs-cap-selected");
            else          cardButtons[i]?.RemoveFromClassList("cs-cap-selected");

            if (selected) cardChecks[i]?.RemoveFromClassList("cs-hidden");
            else          cardChecks[i]?.AddToClassList("cs-hidden");
        }
    }
}
