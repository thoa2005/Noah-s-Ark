using UnityEngine;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void AddScore(GameObject player, int amount)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            // Chỉ cập nhật điểm nếu có quyền kiểm soát (để tránh 2 client cùng cộng điểm)
            if (stats.Object != null && stats.HasStateAuthority)
            {
                stats.score += amount;
                Debug.Log(player.name + " +" + amount + " điểm. Tổng: " + stats.score);
            }
        }
    }

    public int GetScore(GameObject player)
    {
        if (player == null) return 0;
        var stats = player.GetComponent<PlayerStats>();
        return stats != null ? stats.score : 0;
    }

    public int GetScoreByName(string playerName)
    {
        GameObject player = GameObject.Find(playerName);
        if (player != null)
        {
            var stats = player.GetComponent<PlayerStats>();
            return stats != null ? stats.score : 0;
        }
        return 0;
    }
}