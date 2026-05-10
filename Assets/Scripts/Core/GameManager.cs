using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Settings")]
    public float fallLimit = -5f;
    public int   maxLives  = 3;

        List<GameObject> cachedPlayers = new List<GameObject>();
    List<GameObject> cachedBots    = new List<GameObject>();

int  playerLives;
    bool gameOver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

void Start()
    {
        playerLives = maxLives;
        gameOver    = false;
        RefreshEntityLists();
    }

    void Update()
    {
        if (gameOver) return;

        // Kiểm tra Player bị rơi
        for (int i = cachedPlayers.Count - 1; i >= 0; i--)
        {
            var p = cachedPlayers[i];
            if (p == null) { cachedPlayers.RemoveAt(i); continue; }
            if (p.transform.position.y < fallLimit)
            {
                HandlePlayerFall(p);
            }
        }

        // Kiểm tra Bot bị rơi
        for (int i = cachedBots.Count - 1; i >= 0; i--)
        {
            var b = cachedBots[i];
            if (b == null) { cachedBots.RemoveAt(i); continue; }
            if (b.transform.position.y < fallLimit)
            {
                HandleBotFall(b);
            }
        }
    }

    void HandlePlayerFall(GameObject p)
    {
        var stats = p.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.lives--;
            Debug.Log($"[GameManager] Player {p.name} fell! Lives remaining: {stats.lives}");

            if (stats.lives <= 0)
            {
                gameOver = true;
                Debug.Log("GAME OVER");
                return;
            }
        }
        else
        {
            // Fallback nếu không có stats
            playerLives--;
            if (playerLives <= 0) { gameOver = true; return; }
        }

        Respawn(p, new Vector3(Random.Range(-3f, 3f), 5f, Random.Range(-3f, 3f)));
    }

    void HandleBotFall(GameObject b)
    {
        Debug.Log($"[GameManager] Bot {b.name} fell!");
        Respawn(b, new Vector3(Random.Range(-8f, 8f), 5f, Random.Range(-8f, 8f)));
    }

    void Respawn(GameObject go, Vector3 pos)
    {
        // Reset velocity for all rigidbodies in the hierarchy
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>())
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        go.transform.position = pos;
    }

        public void RefreshEntityLists()
    {
        cachedPlayers = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
        cachedBots    = new List<GameObject>(GameObject.FindGameObjectsWithTag("Bot"));
    }

    public bool IsGameOver() { return gameOver; }
    public int GetLives() 
    { 
        if (cachedPlayers.Count > 0 && cachedPlayers[0] != null)
        {
            var stats = cachedPlayers[0].GetComponent<PlayerStats>();
            if (stats != null) return stats.lives;
        }
        return playerLives; 
    }
}
