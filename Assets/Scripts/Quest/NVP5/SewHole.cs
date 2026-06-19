using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Gắn lên mỗi lỗ luồn chỉ trên cánh buồm.
/// Người chơi click vào đúng thứ tự.
/// </summary>
[RequireComponent(typeof(Image))]
public class SewHole : MonoBehaviour, IPointerClickHandler
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    public Color defaultColor   = Color.white;
    public Color highlightColor = Color.yellow;   // Lỗ đang cần click
    public Color sewnColor      = new Color(0.2f, 0.8f, 0.2f); // Đã khâu xong
    public Color wrongColor     = Color.red;      // Flash khi click sai

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private Image _image;
    private bool  _isSewn = false;

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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isSewn) return;

        SewSailGame game = GetComponentInParent<SewSailGame>();
        if (game == null) game = FindFirstObjectByType<SewSailGame>();
        game?.OnHoleClicked(this);
    }

    // ------------------------------------------------------------------ //
    //  PUBLIC API
    // ------------------------------------------------------------------ //

    public void SetHighlight(bool on)
    {
        if (_isSewn) return;
        _image.color = on ? highlightColor : defaultColor;
    }

    public void MarkSewn()
    {
        _isSewn      = true;
        _image.color = sewnColor;
    }

    public void FlashWrong()
    {
        StartCoroutine(FlashRoutine());
    }

    public void ResetHole()
    {
        _isSewn      = false;
        _image.color = defaultColor;
    }

    // ------------------------------------------------------------------ //
    //  COROUTINE
    // ------------------------------------------------------------------ //

    IEnumerator FlashRoutine()
    {
        _image.color = wrongColor;
        yield return new WaitForSecondsRealtime(0.3f);
        _image.color = _isSewn ? sewnColor : defaultColor;
    }
}
