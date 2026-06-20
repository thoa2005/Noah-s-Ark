using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class MainMenuUI : MonoBehaviour
{
    private VisualElement rootElement;
    private VisualElement bubbleContainer;
    private VisualElement starContainer;
    private VisualElement waveBack;
    private VisualElement waveFront;

    // List of bubble elements to animate dynamically
    private List<VisualElement> bubbles = new List<VisualElement>();
    
    // Parallax tracking variables
    private float lastMouseOffsetX = 0f;
    private float lastMouseOffsetY = 0f;
    private float animTime = 0f;

    // Scheduler handle
    private IVisualElementScheduledItem animScheduler;

    private TextField nameInput;

    void Start()
    {
        UIDocument localDoc = GetComponent<UIDocument>();
        if (localDoc != null)
        {
            Initialize(localDoc.rootVisualElement);
        }
        else
        {
            Debug.LogWarning("[MainMenuUI] Missing UIDocument component on MainMenuUI GameObject!");
        }
    }

    /// <summary>
    /// Called by UIManager or local Start when MainMenu screen is instantiated
    /// </summary>
    public void Initialize(VisualElement menuRoot)
    {
        rootElement = menuRoot;

        // Query visual elements
        bubbleContainer = menuRoot.Q<VisualElement>("bubble-container");
        starContainer = menuRoot.Q<VisualElement>("star-container");
        waveBack = menuRoot.Q<VisualElement>("wave-back");
        waveFront = menuRoot.Q<VisualElement>("wave-foam"); // using wave-foam or wave-foam for opposite parallax

        // Programmatic vertical gradient for Season Card to bypass USS compiler limits and achieve perfect Figma looks
        VisualElement seasonCard = menuRoot.Q<VisualElement>("season-card");
        if (seasonCard != null)
        {
            Texture2D gradientTex = new Texture2D(1, 2);
            gradientTex.wrapMode = TextureWrapMode.Clamp;
            gradientTex.filterMode = FilterMode.Bilinear;
            
            // Bottom color: Coral Red (#e11d48)
            Color bottomColor = new Color(0.882f, 0.114f, 0.282f, 1.0f);
            // Top color: Warm Orange (#faa61a)
            Color topColor = new Color(0.980f, 0.651f, 0.102f, 1.0f);
            
            gradientTex.SetPixel(0, 0, bottomColor);
            gradientTex.SetPixel(0, 1, topColor);
            gradientTex.Apply();
            
            seasonCard.style.backgroundImage = gradientTex;
        }

        // Query buttons
        Button btnPlay = menuRoot.Q<Button>("btn-play");
        Button btnCreate = menuRoot.Q<Button>("btn-create");
        Button btnJoin = menuRoot.Q<Button>("btn-join");
        Button btnSettings = menuRoot.Q<Button>("btn-settings");
        Button btnProfile = menuRoot.Q<Button>("btn-profile");

        // Bind button click events
        if (btnPlay != null) btnPlay.clicked += OnPlayClicked;
        if (btnCreate != null) btnCreate.clicked += OnCreateClicked;
        if (btnJoin != null) btnJoin.clicked += OnJoinClicked;
        if (btnSettings != null) btnSettings.clicked += OnSettingsClicked;
        if (btnProfile != null) btnProfile.clicked += OnProfileClicked;

        // Query input field for player name
        nameInput = menuRoot.Q<TextField>("input-player-name");

        // Register pointer move callback for technique 2 (Mouse Parallax)
        menuRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove);

        // Gather bubbles for dynamic float animations (Technique 1)
        bubbles.Clear();
        for (int i = 0; i <= 6; i++)
        {
            VisualElement bubble = menuRoot.Q<VisualElement>($"bubble-{i}");
            if (bubble != null)
            {
                bubbles.Add(bubble);
            }
        }

        // Start hardware-friendly dynamic updates at 60fps
        if (animScheduler != null)
        {
            animScheduler.Pause();
        }
        
        animScheduler = menuRoot.schedule.Execute(UpdateAnimations).Every(16); // ~60fps
        animTime = 0f;


    }

    /// <summary>
    /// Technique 2: Mouse Parallax Effect
    /// Translates background layers slightly based on mouse cursor distance from screen center
    /// </summary>
    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (rootElement == null) return;

        float width = rootElement.layout.width;
        float height = rootElement.layout.height;

        if (width <= 0 || height <= 0) return;

        float centerX = width / 2f;
        float centerY = height / 2f;

        // Calculate normalized offset from screen center (-1.0 to 1.0)
        lastMouseOffsetX = (evt.localPosition.x - centerX) / centerX;
        lastMouseOffsetY = (evt.localPosition.y - centerY) / centerY;
    }

    /// <summary>
    /// Technique 1: Continuous Floating & Twinkling Background Animations
    /// Runs dynamically to simulate sea waves, rising organic bubbles, and starry skies
    /// </summary>
    private void UpdateAnimations()
    {
        animTime += Time.deltaTime;

        // 1. Animate ocean waves nhấp nhô cuộn chảy
        float waveSway = Mathf.Sin(animTime * 1.2f) * 12f;
        if (waveBack != null)
        {
            waveBack.style.translate = new Translate(lastMouseOffsetX * 12f + waveSway, 0);
        }
        if (waveFront != null)
        {
            waveFront.style.translate = new Translate(lastMouseOffsetX * 24f - waveSway, 0);
        }

        // 2. Animate organic rising bubbles (Nhấp nhô bay lên & lắc lư trái phải)
        for (int i = 0; i < bubbles.Count; i++)
        {
            VisualElement b = bubbles[i];
            if (b == null) continue;

            // Differentiate rising speed and sway frequency per bubble index
            float speed = 30f + (i * 12f); // px per second
            float floatDistance = (animTime * speed) % 650f; // Loops every 650px height
            float horizontalSway = Mathf.Sin(animTime * 1.8f + i) * 14f;

            // Apply calculated positions combined with mouse parallax
            b.style.translate = new Translate(lastMouseOffsetX * 18f + horizontalSway, lastMouseOffsetY * 10f - floatDistance);

            // Smooth fade-in near the bottom, and fade-out near the top
            float progress = floatDistance / 650f;
            b.style.opacity = Mathf.Sin(progress * Mathf.PI) * 0.75f;
        }

        // 3. Animate twinkling stars (Bầu trời sao lấp lánh nhẹ nhàng)
        for (int i = 0; i <= 8; i++)
        {
            VisualElement star = rootElement.Q<VisualElement>($"star-{i}");
            if (star != null)
            {
                // Soft sine twinkle wave offset by index
                float opacity = 0.25f + Mathf.Sin(animTime * 2.5f + i) * 0.45f;
                star.style.opacity = opacity;
            }
        }
    }

    private void OnPlayClicked()
    {
        // Save player name if input field exists and is not empty
        string pName = "Player_" + Random.Range(1000, 9999);
        if (nameInput != null && !string.IsNullOrWhiteSpace(nameInput.value))
        {
            pName = nameInput.value;
        }
        PlayerPrefs.SetString("PlayerName", pName);
        PlayerPrefs.Save();

        // Stop animations before switching screen
        if (animScheduler != null)
            animScheduler.Pause();

        // Chuyển sang màn hình chọn nhân vật thay vì load scene ngay
        UIManager.Instance.ShowCharacterSelect();
    }

    private void OnCreateClicked()
    {

        // In the future, this can call UIManager.Instance.ShowScreen(ScreenType.LobbyRoom);
    }

    private void OnJoinClicked()
    {

    }

    private void OnSettingsClicked()
    {

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ToggleSettings();
        }
    }

    private void OnProfileClicked()
    {

    }

    private void OnDestroy()
    {
        if (animScheduler != null)
        {
            animScheduler.Pause();
        }
    }
}
