using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Đảm bảo scene chỉ có đúng 1 EventSystem.
/// Gắn script này vào EventSystem chính trong scene.
/// Nếu Photon Fusion tự tạo thêm cái khác, script này sẽ tự xóa nó.
/// </summary>
public class SingletonEventSystem : MonoBehaviour
{
    private void Awake()
    {
        var allEventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allEventSystems.Length <= 1) return;

        // Giữ lại cái này, xóa tất cả cái còn lại
        foreach (var es in allEventSystems)
        {
            if (es.gameObject == this.gameObject) continue;

            Debug.Log($"[SingletonEventSystem] Xóa EventSystem thừa: {es.gameObject.name}");
            Destroy(es.gameObject);
        }
    }
}
