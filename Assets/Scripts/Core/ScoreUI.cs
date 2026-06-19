using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    public TMP_Text scoreText;

    void Update()
    {
        if (ScoreManager.Instance == null)
            return;

        int playerScore =
            ScoreManager.Instance.GetScoreByName("Player");

        int botScore =
            ScoreManager.Instance.GetScoreByName("Player (1)");

        scoreText.text =
            "Player: " + playerScore +
            "\nBot: " + botScore;
    }
}