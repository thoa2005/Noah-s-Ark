// File này đã được thay thế bởi hệ thống mới:
//   FruitDraggable.cs  →  gắn lên mỗi hoa quả
//   FruitBin.cs        →  gắn lên mỗi thùng gỗ
//   SortingGameManager.cs → quản lý trạng thái mini-game
//
// Giữ lại file này để tránh lỗi tham chiếu cũ trong scene.
// Nếu không có component nào dùng DropSlot thì có thể xoá file này.

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [DEPRECATED] Dùng FruitBin.cs thay thế.
/// </summary>
public class DropSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Debug.LogWarning("[DropSlot] Script này đã deprecated. Hãy dùng FruitBin.cs thay thế.");
    }
}
