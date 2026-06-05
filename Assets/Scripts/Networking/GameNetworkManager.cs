using UnityEngine;
using Fusion;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Fusion.Sockets;

public class GameNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static GameNetworkManager Instance { get; private set; }

    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 2f, 0f);

    private NetworkRunner  _currentRunner;
    private NetworkObject  _localPlayerObject;
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
    /// Kết nối Fusion và load gameplay scene additive (không unload LoadingScene).
    /// LoadingScene sẽ tự unload sau khi thanh progress đạt 100%.
    /// </summary>
    public async Task StartGameMatch(GameMode mode, string roomName, string sceneName)
    {
        if (_currentRunner == null)
        {
            _currentRunner = Instantiate(runnerPrefab);
            _currentRunner.AddCallbacks(this);
            DontDestroyOnLoad(_currentRunner.gameObject);
        }

        Debug.Log("[Mạng] Bắt đầu kết nối Fusion (không để Fusion tự load scene)...");

        // QUAN TRỌNG: Không set Scene và SceneManager
        // → Fusion chỉ lo network, không tự load/unload scene
        var result = await _currentRunner.StartGame(new StartGameArgs()
        {
            GameMode    = mode,
            SessionName = roomName,
        });

        if (!result.Ok)
        {
            Debug.LogError($"[Mạng] Kết nối thất bại: {result.ShutdownReason}");
            return;
        }

        // Sau khi kết nối xong, load gameplay scene ADDITIVE
        // LoadingScene vẫn còn hiển thị trong lúc này
        Debug.Log("[Mạng] Kết nối thành công, bắt đầu load gameplay scene...");
        StartCoroutine(LoadGameplaySceneAdditive(sceneName));
    }

    private IEnumerator LoadGameplaySceneAdditive(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = false;

        // Chờ Unity load xong (0.9 = Unity threshold)
        while (op.progress < 0.9f)
            yield return null;

        // Activate scene
        op.allowSceneActivation = true;
        yield return null;

        // Set active scene
        Scene gameplayScene = SceneManager.GetSceneByName(sceneName);
        if (gameplayScene.IsValid() && gameplayScene.isLoaded)
            SceneManager.SetActiveScene(gameplayScene);

        Debug.Log($"[Mạng] Gameplay scene '{sceneName}' loaded và active.");

        // Báo LoadingScreen — thanh sẽ chạy đến 100% rồi tự unload
        LoadingScreenUI.FusionSceneReady = true;
    }

    // ── Player lifecycle ─────────────────────────────────────────────────

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player != runner.LocalPlayer) return;
        SpawnLocalPlayer(runner, player);
    }

    private void SpawnLocalPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_localPlayerObject != null) return;

        if (playerPrefab == null)
        {
            Debug.LogError("[Mạng] Chưa gán Player Prefab trong GameNetworkManager.");
            return;
        }

        _localPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        Debug.Log($"[Mạng] Spawn Player cho {player}");

        _localCharacterInput = _localPlayerObject.GetComponentInChildren<CharacterInput>();
        SetupLocalPlayerAfterSpawn(_localPlayerObject);
    }

    private void SetupLocalPlayerAfterSpawn(NetworkObject playerObject)
    {
        if (playerObject == null) return;

        CharacterSkinManager skinManager = playerObject.GetComponentInChildren<CharacterSkinManager>();
        CharacterSelectionManager selectionManager = FindFirstObjectByType<CharacterSelectionManager>();

        if (selectionManager != null && skinManager != null)
            selectionManager.ApplySelectedCharacterTo(skinManager);
        else
            StartCoroutine(ApplySelectedCharacterWhenReady(playerObject));

        PlayerStats  stats  = playerObject.GetComponentInChildren<PlayerStats>();
        PlayerCombat combat = playerObject.GetComponentInChildren<PlayerCombat>();

        if (BattleHUDUI.Instance != null && stats != null)
        {
            BattleHUDUI.Instance.BindPlayer(stats, combat);
            Debug.Log($"[Mạng] Bind HUD vào local player: {playerObject.name}");
        }
        else
        {
            StartCoroutine(BindHudWhenReady(playerObject));
        }
    }

    private IEnumerator BindHudWhenReady(NetworkObject playerObject)
    {
        for (int attempt = 0; attempt < 180; attempt++)
        {
            if (playerObject == null) yield break;

            PlayerStats  stats  = playerObject.GetComponentInChildren<PlayerStats>();
            PlayerCombat combat = playerObject.GetComponentInChildren<PlayerCombat>();

            if (BattleHUDUI.Instance != null && stats != null)
            {
                BattleHUDUI.Instance.BindPlayer(stats, combat);
                Debug.Log($"[Mạng] Bind HUD sau khi chờ: {playerObject.name}");
                yield break;
            }
            yield return null;
        }
        Debug.LogWarning("[Mạng] Không bind được HUD.");
    }

    private IEnumerator ApplySelectedCharacterWhenReady(NetworkObject playerObject)
    {
        for (int attempt = 0; attempt < 120; attempt++)
        {
            if (playerObject == null) yield break;

            CharacterSkinManager skinManager = playerObject.GetComponentInChildren<CharacterSkinManager>();
            CharacterSelectionManager selectionManager = FindFirstObjectByType<CharacterSelectionManager>();

            if (selectionManager != null && skinManager != null)
            {
                selectionManager.ApplySelectedCharacterTo(skinManager);
                yield break;
            }
            yield return null;
        }
        Debug.LogWarning("[Mạng] Không apply được character sau khi chờ.");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer && _localPlayerObject != null)
        {
            runner.Despawn(_localPlayerObject);
            _localPlayerObject   = null;
            _localCharacterInput = null;
        }
    }

    // ── Input ────────────────────────────────────────────────────────────

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (_localCharacterInput == null) return;

        var data = new NetworkInputData
        {
            MoveInput  = _localCharacterInput.moveInput,
            IsJumping  = _localCharacterInput.isJumpPressed,
            IsPunching = _localCharacterInput.isPunching,
            IsGrabbing = _localCharacterInput.isGrabPressed
        };

        if (_localCharacterInput.isJumpPressed) _localCharacterInput.UseJumpRequest();
        if (_localCharacterInput.isPunching)    _localCharacterInput.UsePunchRequest();

        input.Set(data);
    }

    // ── Required empty callbacks ─────────────────────────────────────────

    public void OnSceneLoadDone(NetworkRunner runner)   { }
    public void OnSceneLoadStart(NetworkRunner runner)  { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { _localPlayerObject = null; }
    public void OnConnectedToServer(NetworkRunner runner)    { }
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
