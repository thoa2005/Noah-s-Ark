using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using Fusion;

public class BattleHUDUI : MonoBehaviour
{
    public static BattleHUDUI Instance { get; private set; }

    [Header("Player Reference")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerCombat playerCombat;

    [Header("HUD Information")]
    [SerializeField] private string mapName = "Typhoon Bay";
    [SerializeField] private string playerName = "CaptainCute";

    // UI Elements
    private VisualElement root;
    private VisualElement hpBarFill;
    private VisualElement shieldBarFill;
    private VisualElement koOverlay;
    private VisualElement hotbarContainer;
    private VisualElement skillR;
    private VisualElement lockOverlayR;
    private VisualElement killFeedContainer;
    private Label mapTextLabel;
    private Label aliveTextLabel;
    private Label playerNameLabel;
    private VisualElement nameTagsContainer;

    // Game stats tracking
    private int aliveCount = 4;
    private int totalPlayers = 6;

    // Track active UI components
    private List<VisualElement> activeKillEntries = new List<VisualElement>();
    private List<NameTag> activeNameTags = new List<NameTag>();
    private Dictionary<NameTag, VisualElement> nameTagElements = new Dictionary<NameTag, VisualElement>();

    void Awake()
    {
        // Setup Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ẩn HUD cho đến khi LoadingScreen unload xong
        // Tránh UI chèn lên LoadingScene khi load additive
        var doc = GetComponent<UIDocument>();
        if (doc != null) doc.enabled = false;
        StartCoroutine(ShowHudWhenLoadingDone());
    }

    private IEnumerator ShowHudWhenLoadingDone()
    {
        // Chờ cho đến khi LoadingScene bị unload
        while (UnityEngine.SceneManagement.SceneManager.GetSceneByName("LoadingScene").isLoaded)
            yield return null;

        // LoadingScene đã unload, giờ mới show HUD
        var doc = GetComponent<UIDocument>();
        if (doc != null)
        {
            doc.enabled = true;

            // Đợi 1 frame để UIDocument khởi tạo rootVisualElement
            yield return null;

            // Re-initialize HUD với root mới
            if (doc.rootVisualElement != null)
                Initialize(doc.rootVisualElement);

            // Re-bind local player nếu đã spawn rồi
            TryBindLocalPlayer();
        }

        Debug.Log("[BattleHUDUI] LoadingScene unloaded, HUD initialized.");
    }

    void Start()
    {
        // Backward-compatibility fallback for standalone testing without UIManager
        if (root == null)
        {
            UIDocument localDoc = GetComponent<UIDocument>();
            if (localDoc != null && localDoc.rootVisualElement != null)
            {
                Initialize(localDoc.rootVisualElement);
            }
        }
    }

    /// <summary>
    /// Dynamically initializes the Battle HUD using a spawned UXML root visual element
    /// </summary>
    public void Initialize(VisualElement hudRoot)
    {
        root = hudRoot;
        InitializeUI();
        SubscribeToStatsEvents();

        // Add a welcome kill feed entry just for fun and visual impact!
        AddKillEntry("⚓ Battle started in Typhoon Bay!", "warning");
    }

    void OnDestroy()
    {
        UnsubscribeFromStatsEvents();
    }

    void Update()
    {
        if (!IsPanelReady()) return;

        if (!IsBoundToLocalPlayer())
        {
            TryBindLocalPlayer();
        }

        UpdateStatsBars();
        UpdateSkillSlots();
    }

    private void InitializeUI()
    {
        if (root == null)
        {
            Debug.LogError("[BattleHUDUI] rootVisualElement is null! Cannot initialize UI.", this);
            return;
        }

        // Query all Elements
        hpBarFill = root.Q<VisualElement>("hp-bar-fill");
        shieldBarFill = root.Q<VisualElement>("shield-bar-fill");
        koOverlay = root.Q<VisualElement>("ko-overlay");
        hotbarContainer = root.Q<VisualElement>("hotbar-container");
        skillR = root.Q<VisualElement>("skill-r");
        lockOverlayR = root.Q<VisualElement>("lock-overlay");
        killFeedContainer = root.Q<VisualElement>("kill-feed");
        nameTagsContainer = root.Q<VisualElement>("name-tags-container");

        mapTextLabel = root.Q<Label>("map-text");
        aliveTextLabel = root.Q<Label>("alive-text");
        playerNameLabel = root.Q<Label>("player-name");

        // Clear mock items from UXML preview in actual game start
        if (killFeedContainer != null)
        {
            killFeedContainer.Clear();
        }

        // Register any pre-existing name tags in the scene
        if (nameTagsContainer != null)
        {
            nameTagsContainer.Clear();
            NameTag[] preExistingTags = FindObjectsByType<NameTag>(FindObjectsSortMode.None);
            foreach (var tag in preExistingTags)
            {
                RegisterNameTag(tag);
            }
        }

        // Set static texts
        if (playerNameLabel != null) playerNameLabel.text = playerName;
        if (mapTextLabel != null) mapTextLabel.text = $"🌊 {mapName}";

        UpdateAliveCount(aliveCount, totalPlayers);

        // Reset KO screen overlay
        if (koOverlay != null)
        {
            koOverlay.AddToClassList("visual-hidden");
        }

        // Register Reset Camera Button Listener
        Button btnResetCamera = root.Q<Button>("btn-reset-camera");
        if (btnResetCamera != null)
        {
            btnResetCamera.focusable = false; // Ngăn chặn tuyệt đối việc nút cướp tiêu điểm bàn phím của nhân vật!
            btnResetCamera.clicked += OnResetCameraClicked;
        }
    }

    private void OnResetCameraClicked()
    {
        CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            camFollow.ResetDistance();
            AddKillEntry("🎥 Camera distance reset to default", "normal");
        }
        else
        {
            Debug.LogWarning("[BattleHUDUI] CameraFollow script not found in scene!");
        }
    }

    /// <summary>
    /// Binds the HUD dynamically to a spawned player. Ideal for online multiplayer!
    /// </summary>
    /// <param name="stats">The spawned player's stats</param>
    /// <param name="combat">The spawned player's combat system</param>
    public void BindPlayer(PlayerStats stats, PlayerCombat combat)
    {
        // Unsubscribe from old player if any
        UnsubscribeFromStatsEvents();

        playerStats = stats;
        playerCombat = combat;

        // Subscribe to new player events
        SubscribeToStatsEvents();

        // Immediate visual update
        UpdateStatsBars();
        UpdateSkillSlots();


    }

    private void SubscribeToStatsEvents()
    {
        if (playerStats == null)
        {
            TryBindLocalPlayer();
        }

        if (playerStats != null)
        {
            playerStats.OnKnockout += OnPlayerKnockedOut;
            playerStats.OnWakeUp += OnPlayerWokeUp;
        }
    }

    private void UnsubscribeFromStatsEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnKnockout -= OnPlayerKnockedOut;
            playerStats.OnWakeUp -= OnPlayerWokeUp;
        }
    }

    private void UpdateStatsBars()
    {
        if (playerStats == null) return;

        // 1. HP (currentStability / maxStability)
        if (hpBarFill != null)
        {
            float hpPercent = Mathf.Clamp01(playerStats.currentStability / playerStats.maxStability) * 100f;
            hpBarFill.style.width = Length.Percent(hpPercent);
        }

        // 2. Shield / Stamina (currentStamina / maxStamina)
        if (shieldBarFill != null)
        {
            float shieldPercent = Mathf.Clamp01(playerStats.currentStamina / playerStats.maxStamina) * 100f;
            shieldBarFill.style.width = Length.Percent(shieldPercent);
        }
    }

    private void UpdateSkillSlots()
    {
        if (playerCombat == null || skillR == null) return;

        // Skill [R] Special unlocks only when player is grabbing something!
        bool isGrabbing = playerCombat.IsCharging();

        if (isGrabbing)
        {
            if (skillR.ClassListContains("locked"))
            {
                skillR.RemoveFromClassList("locked");
                skillR.AddToClassList("ready");
                if (lockOverlayR != null)
                {
                    lockOverlayR.style.display = DisplayStyle.None;
                }

                // Add a notification when weapon is grabbed!
                AddKillEntry("✨ Special Skill unlocked! Weapon grabbed!", "warning");
            }
        }
        else
        {
            if (!skillR.ClassListContains("locked"))
            {
                skillR.RemoveFromClassList("ready");
                skillR.AddToClassList("locked");
                if (lockOverlayR != null)
                {
                    lockOverlayR.style.display = DisplayStyle.Flex;
                }
            }
        }
    }

    private void OnPlayerKnockedOut()
    {


        // Show KO Screen overlay (red flash)
        if (koOverlay != null)
        {
            koOverlay.RemoveFromClassList("visual-hidden");
        }

        // Dim the hotbar to show disabled state
        if (hotbarContainer != null)
        {
            hotbarContainer.style.opacity = 0.45f;
        }

        AddKillEntry("❌ You were knocked out!", "danger");
    }

    private void OnPlayerWokeUp()
    {


        // Hide KO Screen overlay
        if (koOverlay != null)
        {
            koOverlay.AddToClassList("visual-hidden");
        }

        // Restore hotbar opacity
        if (hotbarContainer != null)
        {
            hotbarContainer.style.opacity = 1.0f;
        }

        AddKillEntry("💪 You recovered and stood up!", "normal");
    }

    /// <summary>
    /// Adds a dynamic kill feed entry to the right side of the screen.
    /// </summary>
    /// <param name="text">The message to display</param>
    /// <param name="type">The type of display: "normal", "danger", or "warning"</param>
    public void AddKillEntry(string text, string type = "normal")
    {
        if (killFeedContainer == null) return;

        // Create Container VisualElement
        VisualElement entry = new VisualElement();
        entry.AddToClassList("kill-entry");

        // Apply background/border class based on type
        switch (type.ToLower())
        {
            case "danger":
                entry.AddToClassList("kill-entry-danger");
                break;
            case "warning":
                entry.AddToClassList("kill-entry-warning");
                break;
            default:
                entry.AddToClassList("kill-entry-normal");
                break;
        }

        // Create Label
        Label label = new Label(text);
        label.AddToClassList("kill-text");
        entry.Add(label);

        // Add to Feed
        killFeedContainer.Add(entry);
        activeKillEntries.Add(entry);

        // Limit feed size to max 5 items to avoid cluttering the screen
        if (activeKillEntries.Count > 5)
        {
            VisualElement oldest = activeKillEntries[0];
            killFeedContainer.Remove(oldest);
            activeKillEntries.RemoveAt(0);
        }

        // Automatically fade out and remove after 4 seconds
        entry.schedule.Execute(() =>
        {
            if (killFeedContainer != null && activeKillEntries.Contains(entry))
            {
                // Smooth fade out using UI Toolkit transitions
                entry.style.opacity = 0f;

                // Remove from DOM shortly after fade animation
                entry.schedule.Execute(() =>
                {
                    if (killFeedContainer != null && entry.parent == killFeedContainer)
                    {
                        killFeedContainer.Remove(entry);
                    }
                    activeKillEntries.Remove(entry);
                }).ExecuteLater(300);
            }
        }).ExecuteLater(4000);
    }

    /// <summary>
    /// Updates the alive players counter at the top bar.
    /// </summary>
    public void UpdateAliveCount(int remaining, int total)
    {
        aliveCount = remaining;
        totalPlayers = total;
        if (aliveTextLabel != null)
        {
            aliveTextLabel.text = $"🚢 {aliveCount} / {totalPlayers} Left";
        }
    }

    /// <summary>
    /// Registers a floating text name tag above a player or enemy bot.
    /// </summary>
    public void RegisterNameTag(NameTag tag)
    {
        if (tag == null) return;
        if (activeNameTags.Contains(tag)) return;

        activeNameTags.Add(tag);

        if (nameTagsContainer != null)
        {
            // Spawn dynamic text-only container and label
            VisualElement container = new VisualElement();
            container.AddToClassList("floating-name-tag");

            Label nameLabel = new Label(tag.displayName);
            nameLabel.AddToClassList("name-tag-text");
            nameLabel.style.color = tag.nameColor;
            container.Add(nameLabel);

            nameTagsContainer.Add(container);
            nameTagElements[tag] = container;

            // Hide initially until LateUpdate positions it correctly
            container.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// Unregisters a floating name tag when an entity dies or is disabled.
    /// </summary>
    public void UnregisterNameTag(NameTag tag)
    {
        if (tag == null) return;
        if (activeNameTags.Contains(tag))
        {
            activeNameTags.Remove(tag);
        }

        if (nameTagElements.TryGetValue(tag, out VisualElement element))
        {
            if (nameTagsContainer != null && element.parent == nameTagsContainer)
            {
                nameTagsContainer.Remove(element);
            }
            nameTagElements.Remove(tag);
        }
    }

    private float autoAttachTimer = 0f;

    void LateUpdate()
    {
        if (!IsPanelReady()) return;

        // Periodically scan and auto-attach NameTag components to entities that don't have them
        autoAttachTimer += Time.deltaTime;
        if (autoAttachTimer >= 0.5f)
        {
            autoAttachTimer = 0f;
            AutoAttachNameTags();
        }

        UpdateNameTags();
    }

    private void AutoAttachNameTags()
    {
        GameObject playerObj = GetLocalPlayerObject();
        if (playerObj != null && playerObj.GetComponent<NameTag>() == null)
        {
            AttachNameTag(
                playerObj,
                string.IsNullOrEmpty(playerName) ? "Player" : playerName,
                new Color(0.2f, 0.8f, 1f));
        }

        // Auto-attach to all AI Bots (identified by AIBot component, never by name)
        foreach (var bot in FindObjectsByType<AIBot>(FindObjectsSortMode.None))
        {
            if (bot.GetComponent<NameTag>() != null) continue;

            string botName = bot.gameObject.name
                .Replace("(Clone)", "")
                .Replace("Player", "Bot")   // guard: rename if GameObject was named "Player"
                .Trim();

            AttachNameTag(bot.gameObject, botName, new Color(1f, 0.35f, 0.35f));
        }
    }

    /// <summary>
    /// Adds a NameTag component with all fields pre-set before Start() runs,
    /// so NameTag.Start() never overwrites the assigned displayName.
    /// </summary>
    private void AttachNameTag(GameObject target, string name, Color color)
    {
        // Set values on the component immediately after adding it.
        // Unity guarantees Start() runs at least one frame later,
        // so our values are stable before NameTag.Start() checks them.
        NameTag tag = target.AddComponent<NameTag>();
        tag.displayName = name;
        tag.nameColor = color;
        tag.offset = new Vector3(0, 2.0f, 0);
    }

    private bool IsBoundToLocalPlayer()
    {
        if (playerStats == null) return false;

        NetworkObject netObj = playerStats.GetComponent<NetworkObject>();
        if (netObj == null) netObj = playerStats.GetComponentInParent<NetworkObject>();

        return netObj == null || netObj.HasInputAuthority;
    }

    private GameObject GetLocalPlayerObject()
    {
        NetworkObject[] networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);

        foreach (NetworkObject netObj in networkObjects)
        {
            if (netObj == null || !netObj.HasInputAuthority) continue;

            PlayerStats stats = netObj.GetComponent<PlayerStats>();
            if (stats == null) stats = netObj.GetComponentInChildren<PlayerStats>();
            if (stats == null) continue;

            return stats.gameObject;
        }

        return null;
    }

    private void TryBindLocalPlayer()
    {
        NetworkObject[] networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);

        foreach (NetworkObject netObj in networkObjects)
        {
            if (netObj == null || !netObj.HasInputAuthority) continue;

            PlayerStats stats = netObj.GetComponent<PlayerStats>();
            if (stats == null) stats = netObj.GetComponentInChildren<PlayerStats>();
            if (stats == null) continue;

            PlayerCombat combat = netObj.GetComponent<PlayerCombat>();
            if (combat == null) combat = netObj.GetComponentInChildren<PlayerCombat>();

            if (playerStats == stats && playerCombat == combat) return;

            UnsubscribeFromStatsEvents();

            playerStats = stats;
            playerCombat = combat;

            playerStats.OnKnockout += OnPlayerKnockedOut;
            playerStats.OnWakeUp += OnPlayerWokeUp;

            UpdateStatsBars();
            UpdateSkillSlots();

            Debug.Log($"[BattleHUDUI] Bind local player: {netObj.name}");
            return;
        }
    }

    private bool IsPanelReady()
    {
        return isActiveAndEnabled
            && root != null
            && root.panel != null;
    }

    private void UpdateNameTags()
    {
        if (!IsPanelReady()) return;
        if (nameTagsContainer == null || nameTagsContainer.panel == null) return;

        Camera mainCam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (mainCam == null) return;

        for (int i = activeNameTags.Count - 1; i >= 0; i--)
        {
            NameTag tag = activeNameTags[i];
            if (tag == null) { activeNameTags.RemoveAt(i); continue; }

            if (!nameTagElements.TryGetValue(tag, out VisualElement element) || element == null) continue;
            if (element.panel == null) { element.style.display = DisplayStyle.None; continue; }

            if (!tag.gameObject.activeInHierarchy || !tag.enabled)
            {
                element.style.display = DisplayStyle.None;
                continue;
            }

            Vector3 worldPos  = tag.transform.position + tag.offset;
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0) { element.style.display = DisplayStyle.None; continue; }

            element.style.display = DisplayStyle.Flex;

            // Cập nhật text và màu
            Label label = element.Q<Label>(className: "name-tag-text");
            if (label != null)
            {
                label.text       = tag.displayName;
                label.style.color = tag.nameColor;
            }

            // Chuyển screen → panel coordinates
            // Unity screen: (0,0) = bottom-left; UI Toolkit panel: (0,0) = top-left
            float panelScale = element.panel.scaledPixelsPerPoint;
            float panelX     = screenPos.x / panelScale;
            float panelY     = (Screen.height - screenPos.y) / panelScale;

            element.style.left = panelX;
            element.style.top  = panelY;
        }
    }
}
