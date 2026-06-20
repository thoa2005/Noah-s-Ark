using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    public TMP_Text scoreText;

    void Update()
    {
        if (ScoreManager.Instance == null)
            return;

        PlayerStats[] allPlayers = FindObjectsByType<PlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allPlayers.Length == 0)
        {
            scoreText.text = "Waiting for players...";
            return;
        }

        List<(string name, int score)> scoreList = new List<(string name, int score)>();

        foreach (var p in allPlayers)
        {
            string displayName = p.gameObject.name;
            var np = p.GetComponent<NetworkPlayer>();

            // Kiểm tra NetworkObject đã được Spawn chưa trước khi đọc networked property
            if (np != null && np.Object != null && np.Object.IsValid)
            {
                try
                {
                    string netName = np.PlayerName;
                    if (!string.IsNullOrEmpty(netName))
                        displayName = netName;
                }
                catch { /* Chưa Spawned xong, bỏ qua */ }
            }

            // Fallback dùng NameTag nếu chưa có tên từ mạng
            if (displayName == p.gameObject.name)
            {
                var nt = p.GetComponent<NameTag>();
                if (nt != null && !string.IsNullOrEmpty(nt.displayName))
                {
                    displayName = nt.displayName;
                }
            }

            // Chỉ đọc score khi NetworkObject đã Spawn xong
            if (!p.NetworkReady) continue;

            int playerScore = 0;
            try { playerScore = p.score; } catch { continue; }

            scoreList.Add((displayName, playerScore));
        }

        scoreList.Sort((a, b) => b.score.CompareTo(a.score));

        string boardText = "<color=yellow><b>BẢNG ĐIỂM</b></color>\n";
        for (int i = 0; i < scoreList.Count; i++)
        {
            boardText += $"{i + 1}. {scoreList[i].name}: {scoreList[i].score}đ\n";
        }

        scoreText.text = boardText;
    }
}