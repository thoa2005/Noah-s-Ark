using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn lên mỗi quả. Tên GameObject: fruit_banana / fruit_apple / fruit_pear
/// Dùng New Input System (InputSystem package).
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class FruitDraggable : MonoBehaviour
{
    [HideInInspector] public string fruitType;

    private RectTransform _rt;
    private Canvas        _canvas;
    private Camera        _cam;

    private bool      _dragging;
    private Vector2   _dragOffset;
    private Vector2   _originPos;
    private Transform _originParent;

    // ------------------------------------------------------------------ //

    void Start()
    {
        _rt     = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null) _canvas = _canvas.rootCanvas;
        _cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
               ? Camera.main : null;

        GetComponent<Image>().raycastTarget = true;

        // Lấy loại quả — chỉ giữ chữ cái sau dấu _
        // fruit_banana → banana, fruit_apple1 → apple, fruit_pear (2) → pear
        string raw  = gameObject.name.ToLower();
        string part = raw.Contains("_") ? raw.Split('_')[1] : raw;
        var sb = new System.Text.StringBuilder();
        foreach (char c in part)
            if (c >= 'a' && c <= 'z') sb.Append(c);
        fruitType = sb.ToString();

        SaveOrigin();
        Debug.Log($"[Fruit] {gameObject.name} → loại: '{fruitType}'");
    }

    // ------------------------------------------------------------------ //

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        // Nhấn chuột xuống
        if (mouse.leftButton.wasPressedThisFrame && !_dragging)
        {
            if (IsMouseOver(mousePos))
                BeginDrag(mousePos);
        }

        // Đang kéo
        if (_dragging)
        {
            if (mouse.leftButton.isPressed)
                DoDrag(mousePos);
            else
                EndDrag(mousePos);
        }
    }

    // ------------------------------------------------------------------ //

    void BeginDrag(Vector2 mousePos)
    {
        SaveOrigin();
        _dragging = true;

        transform.SetParent(_canvas.transform, true);
        transform.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            mousePos, _cam, out Vector2 mp);
        _dragOffset = _rt.anchoredPosition - mp;

        Debug.Log($"[Fruit] Bắt đầu kéo: {fruitType}");
    }

    void DoDrag(Vector2 mousePos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            mousePos, _cam, out Vector2 mp);
        _rt.anchoredPosition = mp + _dragOffset;
    }

    void EndDrag(Vector2 mousePos)
    {
        _dragging = false;
        Debug.Log($"[Fruit] Thả: {fruitType}");

        FruitBin bin = FindBinUnderMouse(mousePos);
        if (bin != null)
            bin.TryAccept(this);
        else
            ReturnToOrigin();
    }

    // ------------------------------------------------------------------ //

    bool IsMouseOver(Vector2 mousePos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(_rt, mousePos, _cam);
    }

    FruitBin FindBinUnderMouse(Vector2 mousePos)
    {
        foreach (var bin in FindObjectsByType<FruitBin>(FindObjectsSortMode.None))
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    bin.GetComponent<RectTransform>(), mousePos, _cam))
                return bin;
        }
        return null;
    }

    void SaveOrigin()
    {
        _originParent = transform.parent;
        _originPos    = _rt.anchoredPosition;
    }

    public void ReturnToOrigin()
    {
        transform.SetParent(_originParent, false);
        _rt.anchoredPosition = _originPos;
        Debug.Log($"[Fruit] Trả về chỗ cũ: {fruitType}");
    }
}
