using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh chữ hiển thị ở góc màn hình (HUD) khi đang có nhiệm vụ active.
/// Hiện tên nhiệm vụ, mô tả ngắn, và thanh tiến độ nếu cần.
/// 
/// Setup: Gắn vào một Panel UI cố định ở góc màn hình.
/// </summary>
public class QuestHUDDisplay : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("UI Elements")]
    public GameObject hudPanel;
    public Text       questNameText;
    public Text       questDescText;
    public Slider     progressBar;      // Tuỳ chọn

    [Header("Animation")]
    public Animator   animator;         // Tuỳ chọn — slide-in/out animation
    public string     showTrigger  = "Show";
    public string     hideTrigger  = "Hide";

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

    void Awake()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    // ------------------------------------------------------------------ //
    //  PUBLIC API
    // ------------------------------------------------------------------ //

    /// <summary>Hiện HUD với thông tin nhiệm vụ.</summary>
    public void ShowQuest(QuestData data)
    {
        if (data == null) return;

        if (questNameText != null) questNameText.text = data.displayName;
        if (questDescText != null) questDescText.text = data.description;
        if (progressBar   != null) progressBar.value  = 0f;

        if (hudPanel != null) hudPanel.SetActive(true);

        if (animator != null) animator.SetTrigger(showTrigger);
    }

    /// <summary>Cập nhật thanh tiến độ (0.0 → 1.0).</summary>
    public void SetProgress(float normalizedValue)
    {
        if (progressBar != null)
            progressBar.value = Mathf.Clamp01(normalizedValue);
    }

    /// <summary>Ẩn HUD khi nhiệm vụ kết thúc.</summary>
    public void HideQuest()
    {
        if (animator != null)
        {
            animator.SetTrigger(hideTrigger);
            // Panel sẽ tắt qua animation event hoặc dùng coroutine
        }
        else
        {
            if (hudPanel != null) hudPanel.SetActive(false);
        }
    }
}
