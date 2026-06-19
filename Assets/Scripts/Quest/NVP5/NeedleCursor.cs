using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn lên Image kim khâu — kim sẽ di chuyển theo con trỏ chuột.
///
/// Setup:
///   1. Tạo Image kim trong Canvas, gắn script này.
///   2. Không cần gán gì trong Inspector (tự tìm Canvas).
///   3. Tắt Raycast Target trên Image kim để không chặn click vào lỗ.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class NeedleCursor : MonoBehaviour
{
    [Tooltip("Offset vị trí kim so với con trỏ (px). " +
             "Điều chỉnh để mũi kim trùng với con trỏ.")]
    public Vector2 offset = new Vector2(10f, -10f);

    [Tooltip("Ẩn con trỏ chuột hệ thống khi đang chơi?")]
    public bool hideCursor = true;

    private RectTransform _rt;
    private Canvas        _canvas;
    private Camera        _cam;

    // ------------------------------------------------------------------ //

    void Start()
    {
        _rt     = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>().rootCanvas;
        _cam    = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;

        // Tắt Raycast Target để không chặn click vào lỗ
        GetComponent<Image>().raycastTarget = false;

        if (hideCursor)
            Cursor.visible = false;
    }

    void OnDestroy()
    {
        // Bật lại cursor khi thoát
        Cursor.visible = true;
    }

    void OnDisable()
    {
        Cursor.visible = true;
    }

    void OnEnable()
    {
        if (hideCursor)
            Cursor.visible = false;
    }

    // ------------------------------------------------------------------ //

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        // Chuyển vị trí chuột sang toạ độ Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            mousePos, _cam,
            out Vector2 localPoint);

        _rt.anchoredPosition = localPoint + offset;
    }
}
