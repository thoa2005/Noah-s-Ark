using UnityEngine;
using UnityEngine.EventSystems;

public class FruitDraggable : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Vector3 startPosition;
    private bool droppedInBin = false;

    void Awake() {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        startPosition = rectTransform.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData) {
        Debug.Log("OnPointerDown: " + this.name);
    }

    public void OnBeginDrag(PointerEventData eventData) {
        Debug.Log("Bắt đầu kéo: " + this.name);
        droppedInBin = false;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData) {
        rectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData) {
        canvasGroup.blocksRaycasts = true;
        if (!droppedInBin) {
            rectTransform.anchoredPosition = startPosition;
        }
    }

    // Gọi bởi FruitBin khi nhận quả thành công
    public void SetDroppedInBin() {
        droppedInBin = true;
    }
}
