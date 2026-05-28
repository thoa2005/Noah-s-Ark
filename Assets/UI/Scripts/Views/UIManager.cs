using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Core UI Document (Global Overlay)")]
    [SerializeField] private UIDocument globalDocument;

    [Header("Global UI Templates")]
    [SerializeField] private VisualTreeAsset settingsTemplate;

    [Header("Screen Templates")]
    [SerializeField] private VisualTreeAsset characterSelectTemplate;

    [Header("Screen References")]
    [SerializeField] private UIDocument mainMenuDocument;
    [SerializeField] private CharacterSelectUI characterSelectUI;
    private UIDocument characterSelectDocument;

    private bool isSettingsOpen = false;

    void Awake()
    {
        // Setup Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Standard Singleton: Destroy duplicate GameObject cleanly!
            Destroy(gameObject);
            return;
        }

        if (globalDocument == null)
        {
            globalDocument = GetComponent<UIDocument>();
        }

        // Disable global overlay UIDocument by default so it doesn't block gameplay input/clicks!
        if (globalDocument != null)
        {
            globalDocument.enabled = false;
        }
    }

    void Update()
    {
        // Toggle Settings Overlay with Escape key (Modern Input System compatible)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleSettings();
        }
    }

    /// <summary>
    /// Toggles the global Settings UI overlay on top of any active scene
    /// </summary>
    public void ToggleSettings()
    {
        if (globalDocument == null)
        {
            globalDocument = GetComponent<UIDocument>();
        }

        if (globalDocument == null || settingsTemplate == null)
        {
            Debug.LogWarning("[UIManager] Settings template or Global UIDocument is not assigned!");
            return;
        }

        isSettingsOpen = !isSettingsOpen;

        if (isSettingsOpen)
        {
            globalDocument.enabled = true; // Enable overlay to display settings
            globalDocument.visualTreeAsset = settingsTemplate;


            // Safe Freeze Input: Reset movement values and disable input listeners
            CharacterInput charInput = FindFirstObjectByType<CharacterInput>();
            if (charInput != null)
            {
                charInput.ClearAllInputs();
            }
            PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }
        else
        {
            globalDocument.visualTreeAsset = null;
            globalDocument.enabled = false; // Disable overlay completely so it releases input capturing
            
            // Blur focus to return keyboard control to the active gameplay scene
            globalDocument.rootVisualElement?.panel?.focusController?.focusedElement?.Blur();


            // Thaw Input: Re-enable inputs for gameplay
            PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
            }
        }
    }

    /// <summary>
    /// Hiển thị màn hình chọn nhân vật (CharacterSelect)
    /// </summary>
    public void ShowCharacterSelect()
    {
        // Lấy UIDocument của CharacterSelectUI nếu chưa có
        if (characterSelectDocument == null && characterSelectUI != null)
            characterSelectDocument = characterSelectUI.GetComponent<UIDocument>();

        // Ẩn MainMenu
        if (mainMenuDocument != null)
            mainMenuDocument.enabled = false;

        // Hiện CharacterSelect
        if (characterSelectDocument != null)
        {
            characterSelectDocument.enabled = true;
            characterSelectUI.EnablePreviewCamera();
            characterSelectUI.Initialize(characterSelectDocument.rootVisualElement);
        }
        else
        {
            Debug.LogWarning("[UIManager] CharacterSelectUI UIDocument chưa được gán!");
        }
    }

    /// <summary>
    /// Quay lại màn hình MainMenu từ CharacterSelect
    /// </summary>
    public void ShowMainMenu()
    {
        // Ẩn CharacterSelect
        if (characterSelectDocument != null)
            characterSelectDocument.enabled = false;

        // Hiện lại MainMenu
        if (mainMenuDocument != null)
            mainMenuDocument.enabled = true;
    }

    /// <summary>
    /// Loads the loading scene which will then async-load SampleScene
    /// </summary>
    public void StartGameplay()
    {
        SceneManager.LoadScene("LoadingScene");
    }

    /// <summary>
    /// Returns to the main menu scene
    /// </summary>
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
