using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Gắn lên mỗi lỗ luồn chỉ.
/// Yêu cầu: có Button component — Unity tự xử lý click, không cần code phức tạp.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class SewHole : MonoBehaviour
{
    [Header("Màu sắc")]
    public Color defaultColor   = Color.white;
    public Color highlightColor = Color.yellow;
    public Color sewnColor      = new Color(0.2f, 0.8f, 0.2f);
    public Color wrongColor     = Color.red;

    private Image  _image;
    private Button _button;
    private bool   _isSewn = false;

    public bool IsSewn => _isSewn;

    // ------------------------------------------------------------------ //

    void Awake()
    {
        _image  = GetComponent<Image>();
        _button = GetComponent<Button>();

        // Gán sự kiện click vào Button
        _button.onClick.AddListener(OnClick);

        // Tắt transition màu của Button để mình tự quản lý màu
        _button.transition = Selectable.Transition.None;
    }

    void OnClick()
    {
        if (_isSewn) return;
        Debug.Log($"[SewHole] Click: {gameObject.name}");

        SewSailGame game = SewSailGame.Instance;
        if (game == null)
        {
            Debug.LogWarning("[SewHole] Không tìm thấy SewSailGame!");
            return;
        }
        game.OnHoleClicked(this);
    }

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
        _button.interactable = false;
    }

    public void FlashWrong()
    {
        StartCoroutine(FlashRoutine());
    }

    public void ResetHole()
    {
        _isSewn              = false;
        _image.color         = defaultColor;
        _button.interactable = true;
    }

    IEnumerator FlashRoutine()
    {
        Color prev   = _image.color;
        _image.color = wrongColor;
        yield return new WaitForSecondsRealtime(0.3f);
        _image.color = prev;
    }
}
