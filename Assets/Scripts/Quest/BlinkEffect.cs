using UnityEngine;

/// <summary>
/// Làm cho Renderer của object nhấp nháy màu liên tục.
/// Gắn script này lên Cube, không cần cấu hình thêm.
/// </summary>
public class BlinkEffect : MonoBehaviour
{
    [Header("Màu nhấp nháy")]
    public Color colorA = Color.yellow;
    public Color colorB = Color.white;

    [Header("Tốc độ nhấp nháy")]
    public float speed = 3f;

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (_renderer == null) return;

        // Lerp qua lại giữa colorA và colorB theo sin
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        Color blinkColor = Color.Lerp(colorA, colorB, t);

        // Dùng MaterialPropertyBlock để không tạo material instance mới
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", blinkColor); // URP
        _mpb.SetColor("_Color", blinkColor);     // Built-in / HDRP
        _renderer.SetPropertyBlock(_mpb);
    }

    void OnDestroy()
    {
        // Reset màu khi Cube bị xóa
        if (_renderer != null)
        {
            _renderer.GetPropertyBlock(_mpb);
            _mpb.Clear();
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
