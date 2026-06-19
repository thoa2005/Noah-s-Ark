using UnityEngine;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private Dictionary<GameObject, int> playerScores =
        new Dictionary<GameObject, int>();

    void Awake()
    {
        Instance = this;
    }

    public void AddScore(GameObject player, int amount)
    {
        if (!playerScores.ContainsKey(player))
            playerScores[player] = 0;

        playerScores[player] += amount;

        Debug.Log(
            player.name +
            " +" +
            amount +
            " điểm. Tổng: " +
            playerScores[player]
        );
    }

    public int GetScore(GameObject player)
    {
        if (!playerScores.ContainsKey(player))
            return 0;

        return playerScores[player];
    }
    public int GetScoreByName(string playerName)
{
    foreach (var entry in playerScores)
    {
        if (entry.Key.name == playerName)
            return entry.Value;
    }

    return 0;
}
}