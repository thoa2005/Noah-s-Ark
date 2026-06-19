using UnityEngine;
using System.Collections;

/// <summary>
/// Gắn lên WinPanel. Tắt GameObject thủ công trong Editor (bỏ tick).
/// Gọi Show() để hiện 2 giây rồi tự tắt.
/// </summary>
public class Finish : MonoBehaviour
{
    public float displayTime = 2f;

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayTime);
        gameObject.SetActive(false);
    }
}
