using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Fusion.Sockets;
using System.Linq;
using System;

/// <summary>
/// Handles NetworkRunner initialization and management.
/// Responsible for creating, configuring, and managing the network runner instance.
/// </summary>
public class NetworkRunnerHandler : MonoBehaviour
{
    [SerializeField]
    private NetworkRunner networkRunnerPrefab;

    public static NetworkRunnerHandler Instance { get; private set; }
    private NetworkRunner networkRunner;

    void Awake()
    {
        // Đảm bảo Handler này là duy nhất và sống sót qua mọi cảnh
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        networkRunner = FindObjectOfType<NetworkRunner>();
    }

    void Start()
    {
        // Xóa Instantiate ở đây để tránh bị xóa mất khi BootLoader nhảy cảnh
    }

    /// <summary>
    /// Get or create NetworkSceneManager from the runner.
    /// </summary>
    public INetworkSceneManager GetSceneManager(NetworkRunner runner)
    {
        INetworkSceneManager sceneManager = runner.GetComponents<MonoBehaviour>().OfType<INetworkSceneManager>().FirstOrDefault();

        if (sceneManager == null)
        {
            // Handle networked objects that already exist in the scene
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        return sceneManager;
    }

    /// <summary>
    /// Initialize NetworkRunner with game settings and scene manager.
    /// </summary>
    protected virtual Task InitializeNetworkRunner(NetworkRunner networkRunner, GameMode gameMode, string sessionName, NetAddress address, SceneRef scene, ActionNetworkRunner initialized)
    {
        INetworkSceneManager sceneManager = GetSceneManager(networkRunner);

        var startGameArgs = new StartGameArgs()
        {
            GameMode = gameMode,
            Address = address,
            Scene = scene,
            SessionName = sessionName,
            CustomLobbyName = "QuickDebug",
            SceneManager = sceneManager
        };

        return networkRunner.StartGame(startGameArgs);
    }

    /// <summary>
    /// Provide input for network simulation.
    /// </summary>
    public void ProvideInput(bool isInput)
    {
        // Input will be provided by the game logic
    }

    /// <summary>
    /// Start game with specified parameters.
    /// </summary>
    public async Task StartGame(GameMode gameMode, string sessionName, NetAddress address, SceneRef scene, ActionNetworkRunner initialized)
    {
        if (networkRunner == null)
        {
            if (networkRunnerPrefab == null)
            {
                Debug.LogError("[NetworkRunnerHandler] Chưa gán NetworkRunnerPrefab trong Inspector!");
                return;
            }

            networkRunner = Instantiate(networkRunnerPrefab);
            networkRunner.name = "Network runner";
            
            // QUAN TRỌNG: Bảo vệ cục Runner khỏi bị xóa khi chuyển cảnh
            DontDestroyOnLoad(networkRunner.gameObject);
        }

        await InitializeNetworkRunner(networkRunner, gameMode, sessionName, address, scene, initialized);
    }
}

/// <summary>
/// Delegate for network runner initialization callback.
/// </summary>
public delegate void ActionNetworkRunner(NetworkRunner runner);

