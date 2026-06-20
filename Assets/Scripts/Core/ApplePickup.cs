using UnityEngine;
using Fusion;

public class ApplePickup : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.HasStateAuthority) return;

        if (other.CompareTag("Player") || other.CompareTag("Bot"))
        {
            ScoreManager.Instance.AddScore(other.gameObject, 1);
            Runner.Despawn(Object);
        }
    }
}