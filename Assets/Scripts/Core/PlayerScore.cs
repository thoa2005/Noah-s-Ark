using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public string playerName;

    public int score = 0;

    private void Start()
    {
        if (string.IsNullOrEmpty(playerName))
            playerName = gameObject.name;
    }

    public void AddScore(int amount)
    {
        score += amount;

        Debug.Log(
            playerName +
            " nhận "
            + amount +
            " điểm. Tổng: "
            + score
        );
    }
}