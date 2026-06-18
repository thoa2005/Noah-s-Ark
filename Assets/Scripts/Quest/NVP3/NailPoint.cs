using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn lên mỗi đinh / tấm ván bị vênh.
/// Người chơi phải đưa búa vào vùng này rồi click chuột trái 2-3 lần.
/// Mỗi lần click, tấm ván "chìm" dần (scale giảm). Đủ số lần → cố định xong.
/// </summary>
[RequireComponent(typeof(Image))]
public class NailPoint : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Tooltip("Số lần click cần để đóng đinh này")]
    public int clicksRequired = 3;

    [Tooltip("Sprite tấm ván đã cố định (tuỳ chọn)")]
    public Sprite fixedSprite;

    [Tooltip("Hiệu ứng búa đập (Animator trên đối tượng này)")]
    public Animator hammerAnim;
    public string   hammerHitTrigger = "Hit";

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private int   _clickCount = 0;
    private bool  _isFixed    = false;
    private Image _image;
    private Color _hoverColor   = new Color(1f, 1f, 0.5f, 1f);
    private Color _defaultColor = Color.white;

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

    void Awake()
    {
        _image = GetComponent<Image>();
    }

    // ------------------------------------------------------------------ //
    //  EVENTS
    // ------------------------------------------------------------------ //

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isFixed)
            _image.color = _hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _image.color = _defaultColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isFixed) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _clickCount++;

        // Hiệu ứng búa đập
        hammerAnim?.SetTrigger(hammerHitTrigger);

        // Tấm ván "chìm" dần — scale Y giảm để mô phỏng ván đang được đóng
        float progress = (float)_clickCount / clicksRequired;
        transform.localScale = new Vector3(1f, 1f - progress * 0.3f, 1f);

        Debug.Log($"[NailPoint] {gameObject.name} click {_clickCount}/{clicksRequired}");

        if (_clickCount >= clicksRequired)
            FixNail();
    }

    // ------------------------------------------------------------------ //
    //  LOGIC
    // ------------------------------------------------------------------ //

    void FixNail()
    {
        _isFixed = true;
        transform.localScale = Vector3.one;
        _image.color = _defaultColor;

        if (fixedSprite != null)
            _image.sprite = fixedSprite;

        // Báo lên NailBoardsGame
        NailBoardsGame game = GetComponentInParent<NailBoardsGame>();
        if (game == null) game = FindFirstObjectByType<NailBoardsGame>();
        game?.OnNailFixed();

        Debug.Log($"[NailPoint] {gameObject.name} đã được đóng cố định!");
    }

    public void ResetNail()
    {
        _clickCount = 0;
        _isFixed    = false;
        _image.color = _defaultColor;
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);
    }
}
