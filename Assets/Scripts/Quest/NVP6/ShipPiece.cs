using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn lên mỗi mảnh vỡ tàu. Kéo thả vào đúng vị trí để snap khóa.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class ShipPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("anchoredPosition đích cần ghép vào")]
    public Vector2 targetPosition;

    [Tooltip("Phạm vi snap (px)")]
    public float snapRange = 40f;

    public Color placedColor = new Color(0.6f, 1f, 0.6f);

    private Vector2       _startPosition;
    private CanvasGroup   _canvasGroup;
    private RectTransform _rectTransform;
    private Canvas        _rootCanvas;
    private bool          _isPlaced = false;

    // ------------------------------------------------------------------ //

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup   = GetComponent<CanvasGroup>();
        GetComponent<Image>().raycastTarget = true;
    }

    void Start()
    {
        // Lấy Canvas ở Start — đảm bảo hierarchy đã active
        Canvas c = GetComponentInParent<Canvas>();
        _rootCanvas    = c != null ? c.rootCanvas : FindFirstObjectByType<Canvas>();
        _startPosition = _rectTransform.anchoredPosition;
    }

    // ------------------------------------------------------------------ //

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isPlaced) return;
        if (_rootCanvas == null) return;

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha          = 0.8f;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isPlaced || _rootCanvas == null) return;
        _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isPlaced) return;

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha          = 1f;

        float dist = Vector2.Distance(_rectTransform.anchoredPosition, targetPosition);
        if (dist <= snapRange)
            PlacePiece();
        else
            _rectTransform.anchoredPosition = _startPosition;
    }

    void PlacePiece()
    {
        _isPlaced                        = true;
        _rectTransform.anchoredPosition  = targetPosition;
        _canvasGroup.blocksRaycasts      = false;
        GetComponent<Image>().color      = placedColor;

        RepairShipGame game = GetComponentInParent<RepairShipGame>();
        if (game == null) game = FindFirstObjectByType<RepairShipGame>();
        game?.OnPiecePlaced();

        Debug.Log($"[ShipPiece] {gameObject.name} snap thành công!");
    }

    public void ResetPiece()
    {
        _isPlaced                        = false;
        _rectTransform.anchoredPosition  = _startPosition;
        _canvasGroup.alpha               = 1f;
        _canvasGroup.blocksRaycasts      = true;
        GetComponent<Image>().color      = Color.white;
    }
}
