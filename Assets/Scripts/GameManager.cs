using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Settings")]
    public float fallLimit = -5f;
    public int   maxLives  = 3;

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
    }

    void Update()
    {
        if (gameOver) return;

        // Kiem tra Player bi roi
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            if (p.transform.position.y >= fallLimit) continue;

            playerLives--;
            var prb = p.GetComponent<Rigidbody>();
            if (prb != null) { prb.linearVelocity = Vector3.zero; prb.angularVelocity = Vector3.zero; }

            if (playerLives <= 0)
            {
                gameOver = true;
                Debug.Log("GAME OVER");
                return;
            }
            p.transform.position = new Vector3(Random.Range(-3f, 3f), 2f, Random.Range(-3f, 3f));
        }

        // Kiem tra Bot bi roi
        var bots = GameObject.FindGameObjectsWithTag("Bot");
        foreach (var b in bots)
        {
            if (b.transform.position.y >= fallLimit) continue;
            var brb = b.GetComponent<Rigidbody>();
            if (brb != null) { brb.linearVelocity = Vector3.zero; brb.angularVelocity = Vector3.zero; }
            b.transform.position = new Vector3(Random.Range(-5f, 5f), 2f, Random.Range(-5f, 5f));
        }
    }

    public bool IsGameOver() { return gameOver; }
}
