using UnityEngine;

/// <summary>
/// Đảm bảo scene chỉ có đúng 1 AudioListener active.
/// Gắn script này vào Camera chính (Main Camera) trong scene.
/// Khi Fusion Multi-Peer tạo thêm camera/peer, các AudioListener thừa sẽ bị disable.
/// </summary>
public class SingletonAudioListener : MonoBehaviour
{
    private void Awake()
    {
        EnforceOne();
    }

    private void OnEnable()
    {
        EnforceOne();
    }

    private void EnforceOne()
    {
        var allListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (allListeners.Length <= 1) return;

        foreach (var listener in allListeners)
        {
            if (listener.gameObject == this.gameObject) continue;

            Debug.Log($"[SingletonAudioListener] Disable AudioListener thừa: {listener.gameObject.name}");
            listener.enabled = false;
        }
    }
}
