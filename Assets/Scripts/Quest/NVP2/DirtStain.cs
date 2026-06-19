using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DirtStain : MonoBehaviour
{
    [Tooltip("Số lần di chuột QUA (vào rồi ra) để làm sạch hoàn toàn")]
    public int sweepsRequired = 5;

    [Tooltip("Thời gian tối thiểu (giây) giữa 2 lần quét được tính. Tránh quét quá nhanh.")]
    public float sweepCooldown = 0.4f;

    private int   _sweepCount  = 0;
    private bool  _isCleaned   = false;
    private Image _image;
    private Color _originalColor;

    private RectTransform _rectTransform;
    private bool  _mouseWasInside = false;
    private float _lastSweepTime  = -999f; // thời điểm lần quét gần nhất

    void Awake()
    {
        _image         = GetComponent<Image>();
        _originalColor = _image.color;
        _rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (_isCleaned) return;

        Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, mousePosition, null);

        // Đếm khi chuột ĐI RA khỏi vết bẩn + đã qua đủ cooldown
        if (!isInside && _mouseWasInside)
        {
            float now = Time.unscaledTime;
            if (now - _lastSweepTime >= sweepCooldown)
            {
                _lastSweepTime = now;
                OnMouseSweep();
            }
        }

        _mouseWasInside = isInside;
    }

    void OnMouseSweep()
    {
        _sweepCount++;

        // Mờ dần theo tỉ lệ đã quét
        float alpha = 1f - (float)_sweepCount / sweepsRequired;
        _image.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, Mathf.Max(0f, alpha));

        Debug.Log($"[DirtStain] {gameObject.name}: {_sweepCount}/{sweepsRequired} lần quét");

        if (_sweepCount >= sweepsRequired)
        {
            CleanStain();
        }
    }

    void CleanStain()
    {
        _isCleaned = true;
        gameObject.SetActive(false);

        SweepDeckGame game = GetComponentInParent<SweepDeckGame>();
        if (game == null) game = FindFirstObjectByType<SweepDeckGame>();
        game?.OnStainCleaned();
    }

    public void ResetStain()
    {
        _sweepCount     = 0;
        _isCleaned      = false;
        _image.color    = _originalColor;
        gameObject.SetActive(true);
        _mouseWasInside = false;
        _lastSweepTime  = -999f;
    }
}