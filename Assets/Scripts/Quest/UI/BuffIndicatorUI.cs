using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hiện icon trạng thái khi nhân vật được tăng tốc / nhận buff sau khi hoàn thành nhiệm vụ.
/// Mỗi buff hiện 1 icon với thanh thời gian đếm ngược.
/// 
/// Setup:
///   1. Gắn script này vào một HorizontalLayoutGroup trong Canvas.
///   2. Tạo prefab "BuffIcon" (Image + Text) và gán vào buffIconPrefab.
/// </summary>
public class BuffIndicatorUI : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  SINGLETON
    // ------------------------------------------------------------------ //

    public static BuffIndicatorUI Instance { get; private set; }

    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Prefab icon buff (Image + Text)")]
    public GameObject buffIconPrefab;

    [Header("Container chứa các icon (HorizontalLayoutGroup)")]
    public Transform  iconContainer;

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private Dictionary<QuestID, GameObject> _activeIcons = new Dictionary<QuestID, GameObject>();

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ------------------------------------------------------------------ //
    //  PUBLIC API
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Hiện icon buff cho một nhiệm vụ vừa hoàn thành.
    /// </summary>
    /// <param name="id">ID nhiệm vụ</param>
    /// <param name="duration">Thời gian buff tồn tại (giây). 0 = vĩnh viễn.</param>
    /// <param name="icon">Sprite icon (tuỳ chọn)</param>
    public void ShowBuff(QuestID id, float duration = 10f, Sprite icon = null)
    {
        // Nếu buff đã có thì remove cũ trước
        RemoveBuff(id);

        if (buffIconPrefab == null || iconContainer == null) return;

        GameObject go = Instantiate(buffIconPrefab, iconContainer);
        _activeIcons[id] = go;

        // Gán icon nếu có
        Image img = go.GetComponentInChildren<Image>();
        if (img != null && icon != null) img.sprite = icon;

        // Gán text tên
        Text label = go.GetComponentInChildren<Text>();
        if (label != null) label.text = id.ToString();

        // Tự xoá sau duration giây
        if (duration > 0f)
            StartCoroutine(RemoveAfter(id, duration));

        Debug.Log($"[BuffIndicatorUI] Hiện buff: {id} trong {duration}s");
    }

    /// <summary>Xoá icon buff thủ công.</summary>
    public void RemoveBuff(QuestID id)
    {
        if (_activeIcons.TryGetValue(id, out GameObject go))
        {
            Destroy(go);
            _activeIcons.Remove(id);
        }
    }

    // ------------------------------------------------------------------ //
    //  COROUTINE
    // ------------------------------------------------------------------ //

    IEnumerator RemoveAfter(QuestID id, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveBuff(id);
    }
}
