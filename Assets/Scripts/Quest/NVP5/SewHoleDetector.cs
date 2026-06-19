using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn lên SewingManager (hoặc bất kỳ GameObject nào active trong scene).
/// Dùng Raycast từ camera để detect click vào lỗ — hoàn toàn độc lập với UI EventSystem.
/// </summary>
public class SewHoleDetector : MonoBehaviour
{
    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;

        // Bắn ray từ camera qua vị trí chuột
        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = _cam.ScreenPointToRay(mousePos);
        
        Debug.Log($"[Detector] Click tại screen {mousePos}, bắn ray...");

        // Thử tất cả hit
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);
        foreach (var hit in hits)
        {
            Debug.Log($"[Detector] Trúng: {hit.collider.gameObject.name}");
            SewHole hole = hit.collider.GetComponent<SewHole>();
            if (hole != null)
            {
                SewSailGame.Instance?.OnHoleClicked(hole);
                return;
            }
        }

        // Fallback: thử overlap circle tại vị trí world
        Vector3 worldPos = _cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
        Debug.Log($"[Detector] World pos: {worldPos}");
        
        // Tìm tất cả SewHole, check bằng RectTransform
        SewHole[] allHoles = FindObjectsByType<SewHole>(FindObjectsSortMode.None);
        foreach (var hole in allHoles)
        {
            RectTransform rt = hole.GetComponent<RectTransform>();
            Camera uiCam = hole.GetComponentInParent<Canvas>()?.renderMode == 
                          RenderMode.ScreenSpaceOverlay ? null : _cam;
            
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, uiCam))
            {
                Debug.Log($"[Detector] ✅ Click trúng lỗ: {hole.gameObject.name}");
                SewSailGame.Instance?.OnHoleClicked(hole);
                return;
            }
        }
        
        Debug.Log("[Detector] Không trúng lỗ nào");
    }
}
