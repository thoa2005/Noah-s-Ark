using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

/// <summary>
/// Quản lý toàn bộ screen navigation trong MainMenuScene.
///
/// Luồng đúng:
///   [Play]   → CharacterSelect (quick) → Lock In → LoadingScene
///   [Create] → Lobby (host=true)  → CharacterSelect (lobby) → Lock In → StartGame
///   [Join]   → Lobby (host=false) → CharacterSelect (lobby) → Lock In → StartGame
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ------------------------------------------------------------------ //
    //  INSPECTOR FIELDS
    // ------------------------------------------------------------------ //

    [Header("Global Overlay (Settings / ESC)")]
    [SerializeField] private UIDocument      globalDocument;
    [SerializeField] private VisualTreeAsset settingsTemplate;

    [Header("Main Menu")]
    [SerializeField] private UIDocument mainMenuDocument;

    [Header("Character Select")]
    [SerializeField] private UIDocument        characterSelectDocument;
    [SerializeField] private CharacterSelectUI characterSelectUI;

    [Header("Lobby")]
    [SerializeField] private UIDocument lobbyDocument;
    [SerializeField] private LobbyUI    lobbyUI;

    // ------------------------------------------------------------------ //
    //  RUNTIME STATE
    // ------------------------------------------------------------------ //

    public enum Screen { None, MainMenu, CharacterSelect, Lobby }

    /// <summary>Context khi mở CharacterSelect: từ Quick Play hay từ Lobby.</summary>
    public enum CharacterSelectContext { QuickPlay, FromLobby }

    public Screen                 CurrentScreen  { get; private set; } = Screen.None;
    public CharacterSelectContext SelectContext  { get; private set; } = CharacterSelectContext.QuickPlay;

    private bool _isSettingsOpen;

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

void Awake()
    {
        Debug.Log("[UIManager] Awake() called");
        // Khong dung DontDestroyOnLoad vi UIManager song trong MainMenuScene
        // Neu co instance cu (tu scene truoc) thi destroy no, dung instance moi
        if (Instance != null && Instance != this)
        {
            Debug.Log("[UIManager] Previous Instance found, destroying it");
            Destroy(Instance.gameObject);
        }
        Instance = this;
        Debug.Log("[UIManager] Instance set to this");

        if (globalDocument == null) globalDocument = GetComponent<UIDocument>();
        if (globalDocument != null) globalDocument.enabled = false;
    }

    void Start()
    {
        Debug.Log("[UIManager] Start() called");
        SetAllScreensOff();
        ShowMainMenu();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ToggleSettings();
    }

    // ------------------------------------------------------------------ //
    //  SETTINGS OVERLAY
    // ------------------------------------------------------------------ //

    public void ToggleSettings()
    {
        if (globalDocument == null) globalDocument = GetComponent<UIDocument>();
        if (globalDocument == null || settingsTemplate == null)
        {
            Debug.LogWarning("[UIManager] Settings template hoặc Global UIDocument chưa gán!");
            return;
        }

        _isSettingsOpen = !_isSettingsOpen;

        if (_isSettingsOpen)
        {
            globalDocument.enabled = true;
            globalDocument.visualTreeAsset = settingsTemplate;
            FindFirstObjectByType<CharacterInput>()?.ClearAllInputs();
            var pi = FindFirstObjectByType<PlayerInput>();
            if (pi != null) pi.enabled = false;
        }
        else
        {
            globalDocument.visualTreeAsset = null;
            globalDocument.enabled = false;
            globalDocument.rootVisualElement?.panel?.focusController?.focusedElement?.Blur();
            var pi = FindFirstObjectByType<PlayerInput>();
            if (pi != null) pi.enabled = true;
        }
    }

    // ------------------------------------------------------------------ //
    //  SCREEN NAVIGATION
    // ------------------------------------------------------------------ //

    /// <summary>01 - Main Menu</summary>
    public void ShowMainMenu()
    {
        Debug.Log("[UIManager] ShowMainMenu() called");
        characterSelectUI?.DisablePreviewCamera();
        SetAllScreensOff();
        SetScreen(mainMenuDocument, true);
        
        // IMPORTANT: Re-bind buttons khi MainMenu hiển thị lại
        if (mainMenuDocument != null && mainMenuDocument.rootVisualElement != null)
        {
            Debug.Log("[UIManager] Re-initializing MainMenuUI");
            var mainMenuUI = mainMenuDocument.GetComponent<MainMenuUI>();
            if (mainMenuUI != null)
            {
                mainMenuUI.Initialize(mainMenuDocument.rootVisualElement);
            }
        }
        
        CurrentScreen = Screen.MainMenu;
        Debug.Log("[UIManager] → MainMenu");
    }

    /// <summary>
    /// 02 - Lobby / Room.
    /// isHost=true  : player vừa nhấn Create → tạo phòng mới.
    /// isHost=false : player vừa nhấn Join   → tìm phòng có sẵn.
    /// CharacterSelect được mở từ nút trong Lobby, không phải bước trước Lobby.
    /// </summary>
    public void ShowLobby(bool isHost = true)
    {
        Debug.Log($"[UIManager] ShowLobby(isHost={isHost}) called");
        characterSelectUI?.DisablePreviewCamera();
        SetAllScreensOff();
        SetScreen(lobbyDocument, true);

        if (lobbyUI == null && lobbyDocument != null)
            lobbyUI = lobbyDocument.GetComponent<LobbyUI>();

        if (lobbyUI == null)
        {
            Debug.LogWarning("[UIManager] LobbyUI chưa gán!");
            return;
        }

        // LobbyUI.OnEnable() tự Initialize nếu rootVE đã sẵn sàng
        // Fallback: delay 1 frame rồi gọi SetHostMode
        bool capturedIsHost = isHost;
        StartCoroutine(DelayedLobbySetup(capturedIsHost));

        CurrentScreen = Screen.Lobby;
        Debug.Log($"[UIManager] → Lobby (host={isHost})");
    }

    private System.Collections.IEnumerator DelayedLobbySetup(bool isHost)
    {
        yield return null;
        if (lobbyDocument?.rootVisualElement != null && lobbyUI != null)
        {
            lobbyUI.Initialize(lobbyDocument.rootVisualElement);
            lobbyUI.SetHostMode(isHost);
        }
    }

    /// <summary>
    /// 03 - Character Select.
    /// context = QuickPlay  : Play button → Lock In → LoadingScene trực tiếp.
    /// context = FromLobby  : nút Captain Select trong Lobby → Lock In → quay về Lobby (ready).
    /// </summary>
    public void ShowCharacterSelect(CharacterSelectContext context = CharacterSelectContext.QuickPlay)
    {
        SelectContext = context;
        SetAllScreensOff();

        if (characterSelectDocument == null && characterSelectUI != null)
            characterSelectDocument = characterSelectUI.GetComponent<UIDocument>();

        // Enable trước để rootVisualElement được khởi tạo
        SetScreen(characterSelectDocument, true);

        if (characterSelectUI != null && characterSelectDocument?.rootVisualElement != null)
        {
            characterSelectUI.EnablePreviewCamera();
            characterSelectUI.Initialize(characterSelectDocument.rootVisualElement);
        }
        else if (characterSelectUI != null)
        {
            // rootVisualElement chưa sẵn sàng — delay 1 frame
            characterSelectUI.EnablePreviewCamera();
            StartCoroutine(InitCharSelectNextFrame());
        }
        else
        {
            Debug.LogWarning("[UIManager] CharacterSelectUI chưa gán!");
        }

        CurrentScreen = Screen.CharacterSelect;
        Debug.Log($"[UIManager] → CharacterSelect (context={context})");
    }

    private System.Collections.IEnumerator InitCharSelectNextFrame()
    {
        yield return null;
        if (characterSelectDocument?.rootVisualElement != null)
            characterSelectUI.Initialize(characterSelectDocument.rootVisualElement);
    }

    /// <summary>
    /// Gọi từ CharacterSelectUI.OnLockIn().
    /// - QuickPlay  → thẳng vào LoadingScene
    /// - FromLobby  → quay về Lobby, đánh dấu player READY
    /// </summary>
/// <summary>
    /// Gọi từ CharacterSelectUI.OnLockIn().
    /// - QuickPlay  → load LoadingScene trực tiếp
    /// - FromLobby  → load LoadingScene (đã chọn xong, host đã nhấn START)
    /// </summary>
    public void OnCharacterLockIn(int selectedIndex)
    {
        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedIndex);
        PlayerPrefs.Save();
        StartGameplayDirect();
    }

    /// <summary>Host nhấn START GAME trong Lobby → load game.</summary>
/// <summary>
    /// Lobby: host bấm START GAME → vào CharacterSelect (FromLobby) trước.
    /// Quick Play: OnCharacterLockIn sẽ gọi StartGameplayDirect() sau khi lock in.
    /// </summary>
    public void StartGameplay()
    {
        ShowCharacterSelect(CharacterSelectContext.FromLobby);
    }

/// <summary>Load thẳng LoadingScene — gọi sau khi đã chọn nhân vật xong.</summary>
   public void StartGameplayDirect()
{
    Debug.Log("[UI] StartGameplayDirect called");
    SetAllScreensOff();
    StartCoroutine(TransitionToGameplayCoroutine());
}

private System.Collections.IEnumerator TransitionToGameplayCoroutine()
{
    // CRITICAL: Move UIManager outside MainMenuScene FIRST
    Debug.Log("[UI] Moving UIManager to DontDestroyOnLoad...");
    gameObject.transform.SetParent(null);
    DontDestroyOnLoad(gameObject);
    
    yield return null;

    // IMPORTANT: Load LoadingScene FIRST (additive)
    // This ensures there's always at least one scene loaded when we unload MainMenuScene
    Debug.Log("[UI] Step 1: Loading LoadingScene (additive) FIRST...");
    AsyncOperation loadingOp = SceneManager.LoadSceneAsync("LoadingScene", LoadSceneMode.Additive);
    if (loadingOp != null)
    {
        yield return loadingOp;
        Debug.Log("[UI] LoadingScene loaded");
    }
    else
    {
        Debug.LogError("[UI] Failed to load LoadingScene");
        yield break;
    }

    yield return new WaitForSeconds(0.1f);

    // NOW we can safely unload MainMenuScene
    Debug.Log("[UI] Step 2: Unloading MainMenuScene...");
    Scene mainMenuScene = SceneManager.GetSceneByName("MainMenuScene");
    Debug.Log($"[UI] MainMenuScene - IsValid: {mainMenuScene.IsValid()}, isLoaded: {mainMenuScene.isLoaded}");
    
    if (mainMenuScene.IsValid() && mainMenuScene.isLoaded)
    {
        Debug.Log("[UI] Attempting UnloadSceneAsync...");
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync("MainMenuScene", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        
        if (unloadOp != null)
        {
            Debug.Log("[UI] Waiting for unload to complete...");
            yield return unloadOp;
            Debug.Log("[UI] MainMenuScene unload completed");
            
            // Verify it's gone
            Scene check = SceneManager.GetSceneByName("MainMenuScene");
            Debug.Log($"[UI] After unload - IsValid: {check.IsValid()}, isLoaded: {check.isLoaded}");
        }
        else
        {
            Debug.LogError("[UI] UnloadSceneAsync returned null - trying force unload");
            // Force unload by loading another scene (not ideal but works)
            SceneManager.LoadScene("LoadingScene");
        }
    }
    else
    {
        Debug.Log("[UI] MainMenuScene not found or already unloaded");
    }

    yield return new WaitForSeconds(0.1f);

    // Step 3: Set LoadingScene as active
    Debug.Log("[UI] Step 3: Setting LoadingScene as active...");
    Scene loadingScene = SceneManager.GetSceneByName("LoadingScene");
    if (loadingScene.IsValid())
    {
        SceneManager.SetActiveScene(loadingScene);
        Debug.Log("[UI] LoadingScene is now the active scene");
    }
    else
    {
        Debug.LogError("[UI] LoadingScene not valid");
        yield break;
    }

    yield return new WaitForSeconds(0.1f);

    // Step 4: Call GameNetworkManager
    Debug.Log("[UI] Step 4: Starting game via GameNetworkManager...");
    if (GameNetworkManager.Instance != null)
    {
        GameNetworkManager.Instance.StartGameMatchAsync(
            Fusion.GameMode.AutoHostOrClient,
            "Phong_Ragdoll_Direct",
            "SampleScene"
        );
        Debug.Log("[UI] GameNetworkManager.StartGameMatchAsync called");
    }
    else
    {
        Debug.LogError("[UI] GameNetworkManager.Instance is null!");
    }
}

    /// <summary>Quay về MainMenuScene từ gameplay.</summary>
    public void ReturnToMainMenu() => SceneManager.LoadScene("MainMenuScene");

    // ------------------------------------------------------------------ //
    //  HELPERS
    // ------------------------------------------------------------------ //

    private void SetAllScreensOff()
    {
        SetScreen(mainMenuDocument,        false);
        SetScreen(characterSelectDocument, false);
        SetScreen(lobbyDocument,           false);
    }

    private static void SetScreen(UIDocument doc, bool active)
    {
        if (doc != null) doc.enabled = active;
    }
}
