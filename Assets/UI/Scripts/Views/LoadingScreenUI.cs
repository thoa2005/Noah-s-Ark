using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Loading Screen — hiện trong lúc load SampleScene ở background.
/// Tối thiểu 10 giây trước khi chuyển scene.
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Thời gian tối thiểu ở loading screen (giây)")]
    public float minLoadTime = 10f;

    [Tooltip("Tên scene sẽ load")]
    public string targetScene = "SampleScene";

    // ------------------------------------------------------------------ //
    //  UI REFERENCES
    // ------------------------------------------------------------------ //

    private VisualElement progressFill;
    private VisualElement progressDot;
    private Label progressPercent;
    private Label tipText;
    private Label mapName;
    private Label modeBadge;

    private VisualElement spinnerDot0;
    private VisualElement spinnerDot1;
    private VisualElement spinnerDot2;

    // ------------------------------------------------------------------ //
    //  TIPS
    // ------------------------------------------------------------------ //

    private static readonly string[] tips = new string[]
    {
        "Grab enemies and throw them overboard for instant KO!",
        "Jumping while being grabbed can break free faster.",
        "Punching while charging a throw deals bonus damage.",
        "Stay near the center of the ship — the edges are dangerous!",
        "Team up to grab and throw the same enemy at once.",
        "Your stamina recovers faster when you stop attacking.",
        "A fully charged throw sends enemies flying much farther.",
        "Watch out for waves — they can push you off the edge!",
    };

    // ------------------------------------------------------------------ //
    //  RUNTIME
    // ------------------------------------------------------------------ //

    private float animTime = 0f;
    private IVisualElementScheduledItem animScheduler;

    // ------------------------------------------------------------------ //

    void Start()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("[LoadingScreenUI] Missing UIDocument!");
            return;
        }

        var root = doc.rootVisualElement;
        BindElements(root);
        SetRandomTip();
        StartCoroutine(LoadRoutine());
    }

    void BindElements(VisualElement root)
    {
        progressFill    = root.Q<VisualElement>("progress-bar-fill");
        progressDot     = root.Q<VisualElement>("progress-dot");
        progressPercent = root.Q<Label>("progress-percent");
        tipText         = root.Q<Label>("tip-text");
        mapName         = root.Q<Label>("map-name");
        modeBadge       = root.Q<Label>("mode-badge");

        spinnerDot0 = root.Q<VisualElement>("spinner-dot-0");
        spinnerDot1 = root.Q<VisualElement>("spinner-dot-1");
        spinnerDot2 = root.Q<VisualElement>("spinner-dot-2");

        // Cập nhật thông tin màn từ GameManager nếu có
        if (GameManager.Instance != null)
        {
            int round = GameManager.Instance.GetCurrentRound();
            int max   = GameManager.Instance.GetMaxRounds();
            if (mapName  != null) mapName.text  = $"Round {round} of {max}";
            if (modeBadge != null) modeBadge.text = $"⚔ TEAM BATTLE · {max} ROUNDS";
        }

        // Bắt đầu animation spinner + stars
        animScheduler = root.schedule.Execute(() => UpdateAnimations(root)).Every(16);
    }

    void SetRandomTip()
    {
        if (tipText == null) return;
        tipText.text = tips[Random.Range(0, tips.Length)];
    }

    // ------------------------------------------------------------------ //
    //  LOAD ROUTINE
    // ------------------------------------------------------------------ //

    IEnumerator LoadRoutine()
    {
        float elapsed = 0f;

        // Bắt đầu load scene ở background
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        op.allowSceneActivation = false; // Chưa chuyển scene ngay

        while (true)
        {
            elapsed += Time.deltaTime;

            // Progress thực từ Unity (0 → 0.9)
            float loadProgress = Mathf.Clamp01(op.progress / 0.9f);

            // Progress hiển thị = max(loadProgress, elapsed/minLoadTime)
            // Đảm bảo thanh không chạy nhanh hơn thời gian tối thiểu
            float displayProgress = Mathf.Max(loadProgress, elapsed / minLoadTime);
            displayProgress = Mathf.Clamp01(displayProgress);

            UpdateProgressBar(displayProgress);

            // Điều kiện chuyển scene: load xong VÀ đã đủ thời gian tối thiểu
            bool loadDone = op.progress >= 0.9f;
            bool timeDone = elapsed >= minLoadTime;

            if (loadDone && timeDone)
            {
                // Hiện 100% rồi chờ 0.5s cho người chơi thấy
                UpdateProgressBar(1f);
                yield return new WaitForSeconds(0.5f);

                // Dừng animation
                animScheduler?.Pause();

                // Chuyển sang SampleScene
                op.allowSceneActivation = true;
                yield break;
            }

            yield return null;
        }
    }

    void UpdateProgressBar(float t)
    {
        if (progressFill != null)
            progressFill.style.width = Length.Percent(t * 100f);

        if (progressDot != null)
            progressDot.style.left = Length.Percent(t * 100f);

        if (progressPercent != null)
            progressPercent.text = $"{Mathf.RoundToInt(t * 100f)}%";
    }

    // ------------------------------------------------------------------ //
    //  ANIMATIONS
    // ------------------------------------------------------------------ //

    void UpdateAnimations(VisualElement root)
    {
        animTime += Time.deltaTime;

        // Spinner dots — lần lượt sáng lên
        AnimateSpinnerDot(spinnerDot0, 0);
        AnimateSpinnerDot(spinnerDot1, 1);
        AnimateSpinnerDot(spinnerDot2, 2);

        // Stars twinkle
        for (int i = 0; i < 8; i++)
        {
            var star = root.Q<VisualElement>($"ls-star-{i}");
            if (star != null)
                star.style.opacity = 0.3f + Mathf.Sin(animTime * 2f + i * 0.8f) * 0.5f;
        }

        // Ship silhouette nhẹ nhàng lắc lư
        var ship = root.Q<Label>("ship-silhouette");
        if (ship != null)
        {
            float sway = Mathf.Sin(animTime * 0.8f) * 8f;
            ship.style.translate = new Translate(0, sway);
        }

        // Waves cuộn nhẹ
        var waveA = root.Q<VisualElement>("wave-a");
        var waveB = root.Q<VisualElement>("wave-b");
        if (waveA != null) waveA.style.translate = new Translate(Mathf.Sin(animTime * 0.5f) * 20f, 0);
        if (waveB != null) waveB.style.translate = new Translate(Mathf.Sin(animTime * 0.4f + 1f) * -15f, 0);
    }

    void AnimateSpinnerDot(VisualElement dot, int index)
    {
        if (dot == null) return;
        // Mỗi dot sáng lên theo thứ tự, chu kỳ 1.2 giây
        float phase = (animTime * 2.5f - index * 0.4f) % (Mathf.PI * 2f);
        dot.style.opacity = 0.2f + Mathf.Max(0, Mathf.Sin(phase)) * 0.8f;
    }

    void OnDestroy()
    {
        animScheduler?.Pause();
    }
}
