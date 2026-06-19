using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn lên mỗi vết bẩn. Khi con trỏ di qua (PointerEnter) → đếm số lần quét.
/// Đủ số lần → vết bẩn biến mất và báo lên SweepDeckGame.
/// </summary>
[RequireComponent(typeof(Image))]
public class DirtStain : MonoBehaviour, IPointerEnterHandler
{
    [Tooltip("Số lần di chuột qua để làm sạch")]
    public int sweepsRequired = 1;

    private int   _sweepCount  = 0;
    private bool  _isCleaned   = false;
    private Image _image;
    private Color _originalColor;

    void Awake()
    {
        _image         = GetComponent<Image>();
        _image.raycastTarget = true;   // bắt buộc để nhận PointerEnter
        _originalColor = _image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isCleaned) return;

        _sweepCount++;
        float alpha  = 1f - (float)_sweepCount / sweepsRequired;
        _image.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b,
                                 Mathf.Max(0f, alpha));

        if (_sweepCount >= sweepsRequired)
            CleanStain();
    }

    void CleanStain()
    {
        _isCleaned = true;
        gameObject.SetActive(false);

        SweepDeckGame game = GetComponentInParent<SweepDeckGame>();
        if (game == null) game = FindFirstObjectByType<SweepDeckGame>();
        game?.OnStainCleaned();
    }

    /// <summary>Reset về bẩn ban đầu — đổi tên tránh trùng MonoBehaviour.Reset()</summary>
    public void ResetStain()
    {
        _sweepCount  = 0;
        _isCleaned   = false;
        _image.color = _originalColor;
        gameObject.SetActive(true);
    }
}
