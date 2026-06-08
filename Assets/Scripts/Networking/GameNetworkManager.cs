using UnityEngine;
using Fusion;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Fusion.Sockets;
using UnityEngine.UIElements;

/// <summary>
/// Bộ quản lý mạng cốt lõi chuẩn Studio.
/// Quản lý scene loading, kết nối Photon Fusion, và spawn network players.
/// </summary>
public class GameNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static GameNetworkManager Instance { get; private set; }

    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 2f, 0f);

    private NetworkRunner _currentRunner;
    private NetworkObject _localPlayerObject;
    private CharacterInput _localCharacterInput;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Kết nối Fusion và load SampleScene additive.
    /// LoadingScene hiển thị trong lúc này.
    /// Version async dùng cho UIManager.
    /// </summary>
    public async Task StartGameMatch(GameMode mode, string roomName, string sceneName)
    {
        Debug.Log("[Network] ═══ StartGameMatch (async) START ═══");

        try
        {
            // Step 1: Initialize Fusion runner if needed
            if (_currentRunner == null)
            {
                if (runnerPrefab == null)
                {
                    Debug.LogError("[Network] FATAL: runnerPrefab not assigned!");
                    return;
                }
                _currentRunner = Instantiate(runnerPrefab);
                if (_currentRunner == null)
                {
                    Debug.LogError("[Network] FATAL: Failed to instantiate runner!");
                    return;
                }
                Debug.Log("[Network] NetworkRunner instantiated");
                Debug.Log($"[Network] Runner reference: {_currentRunner.GetHashCode()}");
                DontDestroyOnLoad(_currentRunner.gameObject);
            }

            // Small delay to let runner initialize
            await System.Threading.Tasks.Task.Delay(10);

            // Step 2: Register callbacks IMMEDIATELY (CRITICAL - before StartGame)
            try
            {
                Debug.Log("[Network] ═══ CALLBACK REGISTRATION START ═══");
                Debug.Log($"[Network] _currentRunner reference: {_currentRunner.GetHashCode()}");
                
                // Try to remove any existing callbacks first
                try { _currentRunner.RemoveCallbacks(this); }
                catch { }
                
                _currentRunner.AddCallbacks(this);
                Debug.Log("[Network] ═══ CALLBACK REGISTRATION SUCCESS ═══");
                Debug.Log($"[Network][FORCE] Callback registration details: _currentRunner={( _currentRunner==null?"null":_currentRunner.GetHashCode().ToString())}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Network] ❌ Exception adding callbacks: {ex.Message}\n{ex.StackTrace}");
                return;
            }

            // Step 3: Start Fusion network
            Debug.Log("[Network] ═══ FUSION STARTUP START ═══");
            var result = await _currentRunner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = roomName,
            });

            if (!result.Ok)
            {
                Debug.LogError($"[Network] ❌ Failed to start game: {result.ShutdownReason}");
                return;
            }

            Debug.Log("[Network] ═══ FUSION STARTUP SUCCESS ═══");
            Debug.Log($"[Network] LocalPlayer: {_currentRunner.LocalPlayer}");
            Debug.Log($"[Network][FORCE] StartGameSuccess details: _currentRunner={( _currentRunner==null?"null":_currentRunner.GetHashCode().ToString())}, LocalPlayer={_currentRunner.LocalPlayer}, _localPlayerObject={( _localPlayerObject==null?"null":_localPlayerObject.gameObject.name)}");
            
            // TEMP_TEST: Attempt manual spawn immediately after Fusion startup to
            // verify prefab/registration issues. This is a diagnostic helper and
            // can be removed after debugging.
            try
            {
                var manual = await _currentRunner.SpawnAsync(playerPrefab, spawnPosition, Quaternion.identity, _currentRunner.LocalPlayer);
                Debug.Log($"[Network][TEST] Manual async spawn returned: {manual}");
                if (manual != null && _localPlayerObject == null)
                {
                    _localPlayerObject = manual;
                    SetupLocalPlayerAfterSpawn(_localPlayerObject);
                    Debug.Log("[Network][TEST] Manual async spawn assigned as _localPlayerObject and setup executed.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Network][TEST] Exception during manual async spawn: {ex.Message}\n{ex.StackTrace}");
            }

            // Step 4: AFTER network ready, load gameplay scene additive
            Debug.Log($"[Network] Loading {sceneName} scene...");
            StartCoroutine(LoadGameplaySceneAdditive(sceneName));
            
            Debug.Log("[Network] ═══ StartGameMatch (async) END ═══");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Network] ❌ Exception in StartGameMatch: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Same as StartGameMatch but can be called from coroutines
    /// (returns void instead of async Task)
    /// </summary>
    public void StartGameMatchAsync(GameMode mode, string roomName, string sceneName)
    {
        Debug.Log("[Network] Starting game match (from coroutine)...");
        StartCoroutine(StartGameMatchCoroutine(mode, roomName, sceneName));
    }

    private IEnumerator StartGameMatchCoroutine(GameMode mode, string roomName, string sceneName)
    {
        Debug.Log("[Network] ═══ StartGameMatchCoroutine START ═══");
        
        // Step 1: Initialize Fusion runner if needed
        if (_currentRunner == null)
        {
            if (runnerPrefab == null)
            {
                Debug.LogError("[Network] FATAL: runnerPrefab not assigned!");
                yield break;
            }
            _currentRunner = Instantiate(runnerPrefab);
            if (_currentRunner == null)
            {
                Debug.LogError("[Network] FATAL: Failed to instantiate runner!");
                yield break;
            }
            Debug.Log("[Network] NetworkRunner instantiated");
            Debug.Log($"[Network] Runner reference: {_currentRunner.GetHashCode()}");
            DontDestroyOnLoad(_currentRunner.gameObject);
        }

        yield return null; // IMPORTANT: Wait one frame to let runner initialize

        // Step 2: Register callbacks IMMEDIATELY (CRITICAL - before StartGame)
        try
        {
            Debug.Log("[Network] ═══ CALLBACK REGISTRATION START ═══");
            Debug.Log($"[Network] _currentRunner is null? {_currentRunner == null}");
            Debug.Log($"[Network] _currentRunner reference: {_currentRunner.GetHashCode()}");
            Debug.Log($"[Network] this reference: {this.GetHashCode()}");
            Debug.Log($"[Network] this is INetworkRunnerCallbacks? {this is INetworkRunnerCallbacks}");
            
            // Try to remove any existing callbacks first
            try
            {
                _currentRunner.RemoveCallbacks(this);
                Debug.Log("[Network] (Removed previous callbacks if any)");
            }
            catch { } // Ignore if not found
            
            _currentRunner.AddCallbacks(this);
            
            Debug.Log("[Network] ═══ CALLBACK REGISTRATION SUCCESS ═══");
            Debug.Log("[Network] Callbacks added successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Network] ❌ Exception adding callbacks: {ex.Message}\n{ex.StackTrace}");
            yield break;
        }

        // Step 3: Start Fusion network
        Debug.Log("[Network] ═══ FUSION STARTUP START ═══");
        Task<StartGameResult> startTask = null;
        try
        {
            Debug.Log($"[Network] Starting Fusion with GameMode={mode}, RoomName={roomName}");
            startTask = _currentRunner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = roomName,
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Network] ❌ Exception calling StartGame: {ex.Message}\n{ex.StackTrace}");
            yield break;
        }

        while (!startTask.IsCompleted)
            yield return null;

        if (!startTask.Result.Ok)
        {
            Debug.LogError($"[Network] ❌ Failed to start game: {startTask.Result.ShutdownReason}");
            yield break;
        }

        Debug.Log("[Network] ═══ FUSION STARTUP SUCCESS ═══");
        Debug.Log("[Network] Fusion network started successfully");
        Debug.Log($"[Network] LocalPlayer: {_currentRunner.LocalPlayer}");

        // Note: coroutine diagnostic manual spawn removed to avoid incorrect handling
        // of Fusion's NetworkSpawnOp type. Spawning is handled in OnPlayerJoined
        // (SpawnLocalPlayerAsync) which uses await/async.

        // Step 4: AFTER network ready, load gameplay scene additive
        Debug.Log($"[Network] Loading {sceneName} scene...");
        yield return StartCoroutine(LoadGameplaySceneAdditive(sceneName));
        
        Debug.Log("[Network] ═══ StartGameMatchCoroutine END ═══");
    }

    private IEnumerator LoadGameplaySceneAdditive(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = false;

        // Wait for Unity load (0.9 = threshold)
        while (op.progress < 0.9f)
            yield return null;

        // Activate scene
        op.allowSceneActivation = true;
        yield return null;

        // Set as active scene
        Scene gameplayScene = SceneManager.GetSceneByName(sceneName);
        if (gameplayScene.IsValid() && gameplayScene.isLoaded)
            SceneManager.SetActiveScene(gameplayScene);

        Debug.Log($"[Network] {sceneName} loaded and activated");

        // Disable gameplay UI temporarily (LoadingScene showing on top)
        DisableGameplayUI(gameplayScene);

        // Signal LoadingScreen that Fusion is ready
        // LoadingScreen will finish its 100% animation, then unload itself
        LoadingScreenUI.FusionSceneReady = true;
    }

    private void DisableGameplayUI(Scene scene)
    {
        var uiDocs = FindObjectsOfType<UIDocument>();
        foreach (var uiDoc in uiDocs)
        {
            if (uiDoc.gameObject.scene == scene)
            {
                uiDoc.enabled = false;
                Debug.Log($"[Network] Disabled UIDocument: {uiDoc.name}");
            }
        }
    }

    private void EnableGameplayUI()
    {
        var uiDocs = FindObjectsOfType<UIDocument>();
        Scene sampleScene = SceneManager.GetSceneByName("SampleScene");

        foreach (var uiDoc in uiDocs)
        {
            if (uiDoc.gameObject.scene == sampleScene)
            {
                uiDoc.enabled = true;
                Debug.Log($"[Network] Enabled UIDocument: {uiDoc.name}");
            }
        }
    }

    public async void LeaveMatchAndReturnToMenu()
    {
        if (_currentRunner != null)
        {
            await _currentRunner.Shutdown();
            _currentRunner = null;
        }

        SceneManager.UnloadSceneAsync("SampleScene");
        SceneManager.LoadScene("MainMenuScene");
    }

    // ================================================================ //
    //  INetworkRunnerCallbacks Implementation
    // ================================================================ //

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("[Network] ╔════════════════════════════════════════╗");
        Debug.Log("[Network] ║ OnPlayerJoined CALLBACK CALLED!        ║");
        Debug.Log("[Network] ╚════════════════════════════════════════╝");
        Debug.Log($"[Network][FORCE] OnPlayerJoined hit: runnerHash={(runner==null?"null":runner.GetHashCode().ToString())}, player={player}");
        Debug.Log($"[Network] Player joined: {player}");
        Debug.Log($"[Network] Callback Runner HashCode: {runner.GetHashCode()}");
        Debug.Log($"[Network] Stored _currentRunner HashCode: {_currentRunner.GetHashCode()}");
        Debug.Log($"[Network] Are they same instance? {runner == _currentRunner}");
        Debug.Log($"[Network] LocalPlayer: {runner.LocalPlayer}");
        Debug.Log($"[Network] Is local player? {player == runner.LocalPlayer}");
        
        // Only spawn local player
        if (player != runner.LocalPlayer)
        {
            Debug.Log($"[Network] ⚠️  Ignoring OnPlayerJoined for non-local player {player}");
            return;
        }
        
        Debug.Log("[Network] ✓ Player is local player, spawning...");
        // Use async spawn to avoid synchronous prefab load failures
        SpawnLocalPlayerAsync(runner, player);
    }

    private async void SpawnLocalPlayerAsync(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Network] ═══ SpawnLocalPlayerAsync START ═══");
        Debug.Log($"[Network] Player: {player}");
        Debug.Log($"[Network] _localPlayerObject already exists? {_localPlayerObject != null}");

        if (_localPlayerObject != null)
        {
            Debug.LogWarning("[Network] ⚠️  _localPlayerObject already exists, returning early");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[Network] ❌ Player prefab not assigned!");
            return;
        }

        Debug.Log($"[Network] Spawning player at position: {spawnPosition} (async)");
        Debug.Log($"[Network] Runner: {runner.GetHashCode()}");
        Debug.Log($"[Network] PlayerPrefab: {playerPrefab.gameObject.name}");

        try
        {
            var spawned = await runner.SpawnAsync(playerPrefab, spawnPosition, Quaternion.identity, player);

            if (spawned == null)
            {
                Debug.LogError("[Network] ❌ Failed to spawn player (async) - returned null!");
                return;
            }

            _localPlayerObject = spawned;
            Debug.Log($"[Network] ✓ Player spawned successfully (async): {_localPlayerObject.gameObject.name}");
            Debug.Log($"[Network] Spawned object has NetworkObject? {_localPlayerObject.GetComponent<NetworkObject>() != null}");

            _localCharacterInput = _localPlayerObject.GetComponentInChildren<CharacterInput>();
            Debug.Log($"[Network] CharacterInput found? {_localCharacterInput != null}");

            SetupLocalPlayerAfterSpawn(_localPlayerObject);
            Debug.Log($"[Network] ═══ SpawnLocalPlayerAsync END ═══");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Network] ❌ Exception during async spawn: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void SetupLocalPlayerAfterSpawn(NetworkObject playerObject)
    {
        Debug.Log($"[Network] ═══ SetupLocalPlayerAfterSpawn START ═══");
        
        if (playerObject == null)
        {
            Debug.LogError("[Network] ❌ SetupLocalPlayerAfterSpawn received null playerObject!");
            return;
        }

        try
        {
            // Apply selected character skin
            int selectedIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
            Debug.Log($"[Network] Selected character index: {selectedIndex}");
            
            CharacterSelectionManager selectionManager = FindFirstObjectByType<CharacterSelectionManager>();

            if (selectionManager != null)
            {
                selectionManager.SelectCharacterByIndex(selectedIndex);
                Debug.Log("[Network] ✓ Applied selected character");
            }
            else
            {
                Debug.LogWarning("[Network] ⚠️  CharacterSelectionManager not found");
            }

            // Bind HUD
            PlayerStats stats = playerObject.GetComponentInChildren<PlayerStats>();
            PlayerCombat combat = playerObject.GetComponentInChildren<PlayerCombat>();

            Debug.Log($"[Network] PlayerStats found? {stats != null}");
            Debug.Log($"[Network] PlayerCombat found? {combat != null}");
            Debug.Log($"[Network] BattleHUDUI.Instance exists? {BattleHUDUI.Instance != null}");

            if (BattleHUDUI.Instance != null && stats != null)
            {
                BattleHUDUI.Instance.BindPlayer(stats, combat);
                Debug.Log("[Network] ✓ Bound player to HUD");
            }
            else
            {
                Debug.LogWarning("[Network] ⚠️  Could not bind HUD immediately, waiting for setup...");
                StartCoroutine(BindHudWhenReady(playerObject));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Network] ❌ Exception in SetupLocalPlayerAfterSpawn: {ex.Message}\n{ex.StackTrace}");
        }
        
        // Enable gameplay UI now
        EnableGameplayUI();
        
        Debug.Log($"[Network] ═══ SetupLocalPlayerAfterSpawn END ═══");
    }

    private IEnumerator BindHudWhenReady(NetworkObject playerObject)
    {
        for (int attempt = 0; attempt < 180; attempt++)
        {
            if (playerObject == null) yield break;

            PlayerStats stats = playerObject.GetComponentInChildren<PlayerStats>();
            PlayerCombat combat = playerObject.GetComponentInChildren<PlayerCombat>();

            if (BattleHUDUI.Instance != null && stats != null)
            {
                BattleHUDUI.Instance.BindPlayer(stats, combat);
                Debug.Log("[Network] Bound player to HUD (after wait)");
                yield break;
            }
            yield return null;
        }
        Debug.LogWarning("[Network] Failed to bind HUD");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer && _localPlayerObject != null)
        {
            runner.Despawn(_localPlayerObject);
            _localPlayerObject = null;
            _localCharacterInput = null;
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (_localCharacterInput == null) return;

        var data = new NetworkInputData
        {
            moveInput = _localCharacterInput.moveInput,
            isJumpPressed = _localCharacterInput.isJumpPressed,
            isPunching = _localCharacterInput.isPunching,
            isGrabPressed = _localCharacterInput.isGrabPressed
        };

        if (_localCharacterInput.isJumpPressed) _localCharacterInput.UseJumpRequest();
        if (_localCharacterInput.isPunching) _localCharacterInput.UsePunchRequest();

        input.Set(data);
    }

    // ── Required empty callbacks ─────────────────────────────────────

    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _localPlayerObject = null;
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}

public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;
    public NetworkBool isJumpPressed;
    public NetworkBool isPunching;
    public NetworkBool isGrabPressed;
}
