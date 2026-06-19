using UnityEngine;

public class ApplePickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") ||
            other.CompareTag("Bot"))
        {
            ScoreManager.Instance.AddScore(
                other.gameObject,
                20
            );

            Destroy(gameObject);
        }
    }
}