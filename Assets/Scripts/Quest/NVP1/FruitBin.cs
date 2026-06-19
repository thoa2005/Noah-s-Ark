using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên mỗi thùng gỗ.
/// Tên GameObject phải là: "banana", "apple", "pear"
/// (đúng tên, không có số, không có khoảng trắng)
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class FruitBin : MonoBehaviour
{
    private string _binType;

    void Start()
    {
        // Lấy tên thùng — chỉ giữ chữ cái
        var sb = new System.Text.StringBuilder();
        foreach (char c in gameObject.name.ToLower())
            if (c >= 'a' && c <= 'z') sb.Append(c);
        _binType = sb.ToString();

        // Raycast target bắt buộc phải bật
        GetComponent<Image>().raycastTarget = true;

        Debug.Log($"[Bin] {gameObject.name} → nhận loại: '{_binType}'");
    }

    /// <summary>Gọi từ FruitDraggable khi thả vào vùng thùng này.</summary>
    public void TryAccept(FruitDraggable fruit)
    {
        Debug.Log($"[Bin] Thử nhận '{fruit.fruitType}' vào thùng '{_binType}'");

        if (fruit.fruitType == _binType)
        {
            // ✅ Đúng thùng → ẩn quả
            Debug.Log($"[Bin] ✅ Đúng! {fruit.gameObject.name} vào thùng {_binType}");
            fruit.gameObject.SetActive(false);
            SortingGameManager.Instance?.OnFruitSorted();
        }
        else
        {
            // ❌ Sai thùng → trả về
            Debug.Log($"[Bin] ❌ Sai! Thùng [{_binType}] không nhận [{fruit.fruitType}]");
            fruit.ReturnToOrigin();
        }
    }
}
