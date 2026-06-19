using UnityEngine;
using UnityEngine.UI;

public class ShipPiece : MonoBehaviour
{
    // Vị trí đúng (được lưu lúc setup)
    private Vector2 _correctPos;
    private RepairShipGame _gameManager;

    // Đã khớp đúng vị trí → khóa lại, không kéo được nữa
    public bool IsLocked { get; private set; } = false;

    [Tooltip("Khoảng cách tối đa để snap vào đúng vị trí (pixel)")]
    public float snapThreshold = 40f;

    public void SetupPiece(RepairShipGame manager, Vector2 correctPos)
    {
        _gameManager  = manager;
        _correctPos   = correctPos;
        IsLocked      = false;
        GetComponent<Image>().raycastTarget = true;
    }

    /// <summary>
    /// Gọi khi thả mảnh. Nếu gần đúng vị trí thì snap vào và khóa.
    /// Trả về true nếu snap thành công.
    /// </summary>
    public bool TrySnap()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (Vector2.Distance(rect.anchoredPosition, _correctPos) <= snapThreshold)
        {
            rect.anchoredPosition = _correctPos;   // snap chính xác
            IsLocked = true;
            GetComponent<Image>().raycastTarget = false; // không thể click/kéo nữa
            return true;
        }
        return false;
    }

    public bool IsInCorrectPosition()
    {
        return IsLocked;
    }
}
