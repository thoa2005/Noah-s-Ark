using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Loading Screen — Hiện trong lúc mạng load SampleScene ở background ngầm.
/// Đã được tối ưu hóa loại bỏ hàm LoadRoutine cũ gây sập luồng mạng Photon.
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Thời gian tối thiểu ở loading screen (giây)")]
    public float minLoadTime = 10f;
    [Tooltip("Tên scene sẽ load")]
    public string targetScene = "SampleScene";

    // Static flags: communicate between systems
    public static bool LoadingComplete = false;      // Progress bar reached 100%
    public static bool FusionSceneReady = false;     // Fusion loaded SampleScene

    // ------------------------------------------------------------------ //
    // UI REFERENCES
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
    // TIPS
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
    // RUNTIME
    // ------------------------------------------------------------------ //
    private float animTime = 0f;
    private IVisualElementScheduledItem animScheduler;

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
        
        // Start loading progress animation
        StartCoroutine(LoadRoutine());
    }

    void BindElements(VisualElement root)
    {
        progressFill = root.Q<VisualElement>("progress-bar-fill");
        progressDot = root.Q<VisualElement>("progress-dot");
        progressPercent = root.Q<Label>("progress-percent");
        tipText = root.Q<Label>("tip-text");
        mapName = root.Q<Label>("map-name");
        modeBadge = root.Q<Label>("mode-badge");
        spinnerDot0 = root.Q<VisualElement>("spinner-dot-0");
        spinnerDot1 = root.Q<VisualElement>("spinner-dot-1");
        spinnerDot2 = root.Q<VisualElement>("spinner-dot-2");

        // Cập nhật thông tin màn chơi từ GameManager nếu có
        // (Nếu bạn đã xóa GameManager offline cũ thì có thể comment đoạn if này lại)
        /*
        if (GameManager.Instance != null)
        {
            int round = GameManager.Instance.GetCurrentRound();
            int max = GameManager.Instance.GetMaxRounds();
            if (mapName != null) mapName.text = $"Round {round} of {max}";
            if (modeBadge != null) modeBadge.text = $"⚔ TEAM BATTLE · {max} ROUNDS";
        }
        */

        // Bắt đầu chạy các hiệu ứng đồ họa vòng quay + lắc lư tàu
        animScheduler = root.schedule.Execute(() => UpdateAnimations(root)).Every(16);
    }

    void SetRandomTip()
    {
        if (tipText == null) return;
        tipText.text = tips[Random.Range(0, tips.Length)];
    }

    /// <summary>
    /// HÀM NÂNG CẤP CHUẨN STUDIO:
    /// Cho phép hệ thống GameNetworkManager truyền phần trăm tiến trình mạng thực tế (0.0 -> 1.0) vào đây.
    /// </summary>
    public void SetLoadingProgress(float progressValue)
    {
        float clampedProgress = Mathf.Clamp01(progressValue);
        UpdateProgressBar(clampedProgress);
    }

    public float GetCurrentProgress()
    {
        if (progressPercent != null)
        {
            string text = progressPercent.text.Replace("%", "");
            if (int.TryParse(text, out int percent))
            {
                return percent / 100f;
            }
        }
        return 0f;
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
    // LOAD ROUTINE - Animate progress bar from 0 to 100% while loading
    // ------------------------------------------------------------------ //
    private IEnumerator LoadRoutine()
    {
        LoadingComplete = false;
        FusionSceneReady = false;
        float elapsed = 0f;
        
        // Phase 1: Animate progress from 0 to 100% over minLoadTime
        while (elapsed < minLoadTime)
        {
            elapsed += Time.deltaTime;
            float timeProgress = Mathf.Clamp01(elapsed / minLoadTime);
            UpdateProgressBar(timeProgress);
            yield return null;
        }

        // Progress reached 100%
        UpdateProgressBar(1f);
        LoadingComplete = true;
        Debug.Log("[LoadingScreenUI] Progress bar reached 100%");

        // Phase 2: Wait for Fusion to finish loading SampleScene
        float waitElapsed = 0f;
        float waitTimeout = 30f;
        while (!FusionSceneReady && waitElapsed < waitTimeout)
        {
            waitElapsed += Time.deltaTime;
            yield return null;
        }

        if (FusionSceneReady)
        {
            Debug.Log("[LoadingScreenUI] Fusion scene ready, unloading LoadingScene...");
        }
        else
        {
            Debug.LogWarning("[LoadingScreenUI] Timeout waiting for Fusion scene ready!");
        }

        // Phase 3: Unload LoadingScene now that gameplay scene is ready
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync("LoadingScene");
        while (!unloadOp.isDone)
            yield return null;

        Debug.Log("[LoadingScreenUI] LoadingScene unloaded, gameplay ready");
    }

    // ------------------------------------------------------------------ //
    // ANIMATIONS (Giữ nguyên toàn bộ hiệu ứng chuyển động đẹp đẽ của bạn)
    // ------------------------------------------------------------------ //
    void UpdateAnimations(VisualElement root)
    {
        animTime += Time.deltaTime;

        // Spinner dots — lần lượt sáng lên theo vòng tròn
        AnimateSpinnerDot(spinnerDot0, 0);
        AnimateSpinnerDot(spinnerDot1, 1);
        AnimateSpinnerDot(spinnerDot2, 2);

        // Stars twinkle (Sao nhấp nháy)
        for (int i = 0; i < 8; i++)
        {
            var star = root.Q<VisualElement>($"ls-star-{i}");
            if (star != null)
                star.style.opacity = 0.3f + Mathf.Sin(animTime * 2f + i * 0.8f) * 0.5f;
        }

        // Ship silhouette nhẹ nhàng lắc lư trên sóng biển
        var ship = root.Q<Label>("ship-silhouette");
        if (ship != null)
        {
            float sway = Mathf.Sin(animTime * 0.8f) * 8f;
            ship.style.translate = new Translate(0, sway);
        }

        // Waves (Sóng biển cuộn nhẹ)
        var waveA = root.Q<VisualElement>("wave-a");
        var waveB = root.Q<VisualElement>("wave-b");
        if (waveA != null) waveA.style.translate = new Translate(Mathf.Sin(animTime * 0.5f) * 20f, 0);
        if (waveB != null) waveB.style.translate = new Translate(Mathf.Sin(animTime * 0.4f + 1f) * -15f, 0);
    }

    void AnimateSpinnerDot(VisualElement dot, int index)
    {
        if (dot == null) return;
        float phase = (animTime * 2.5f - index * 0.4f) % (Mathf.PI * 2f);
        dot.style.opacity = 0.2f + Mathf.Max(0, Mathf.Sin(phase)) * 0.8f;
    }

    void OnDestroy()
    {
        // Tắt bộ lập lịch hoạt ảnh một cách an toàn khi đóng scene để tránh rác bộ nhớ
        animScheduler?.Pause();
    }
}
