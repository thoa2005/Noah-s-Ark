using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 02 - Lobby / Room screen.
/// Quản lý hiển thị player slots, chat, team options và Start button.
/// Được khởi tạo bởi UIManager.ShowLobby() sau khi tạo/join phòng.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  NESTED DATA
    // ------------------------------------------------------------------ //

    public struct PlayerSlotData
    {
        public bool  isActive;
        public bool  isHost;
        public string emoji;
        public string displayName;
        public int   level;
        public string shipName;
        public bool  isReady;
        public string accentClass; // lobby-slot-green, lobby-slot-orange, …
    }

    // ------------------------------------------------------------------ //
    //  UI ELEMENTS
    // ------------------------------------------------------------------ //

    private VisualElement _root;
    private Label         _lblRoomTitle;
    private Label         _lblReadyCount;
    private VisualElement _chatMessages;
    private VisualElement _chatScroll;
    private Button        _btnStart;
    private Button        _btnBack;
    private Button        _btnCopy;

    // Slot elements (max 8)
    private const int MaxSlots = 8;
    private readonly VisualElement[] _slots = new VisualElement[MaxSlots];

    // ------------------------------------------------------------------ //

    private UIDocument _doc;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        // Re-bind mỗi khi được enable lại (UIManager.ShowLobby gọi doc.enabled = true)
        if (_doc != null && _doc.rootVisualElement != null)
            Initialize(_doc.rootVisualElement);
    }

    private void OnDisable()
    {
        _root = null; // reset để Initialize chạy lại lần sau
    }

    // ------------------------------------------------------------------ //
    //  PUBLIC API (called by UIManager)
    // ------------------------------------------------------------------ //

public void Initialize(VisualElement root)
    {
        _root = root;
        BindElements();
        BindButtons(); // luon bind lai, -= truoc += sau nen khong duplicate
    }

    /// <summary>
    /// Cấu hình Lobby theo vai trò.
    /// isHost=true  → hiện Start button, ẩn "Waiting for host".
    /// isHost=false → ẩn Start button, hiện "Waiting for host".
    /// </summary>
    public void SetHostMode(bool isHost)
    {
        if (_btnStart != null)
            _btnStart.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;

        var waitingLbl = _root?.Q<Label>("lbl-waiting-host");
        if (waitingLbl != null)
            waitingLbl.style.display = isHost ? DisplayStyle.None : DisplayStyle.Flex;
    }

    /// <summary>Đánh dấu slot của local player là READY sau khi chọn captain.</summary>
    public void SetLocalPlayerReady(bool ready)
    {
        // Slot 0 = local player (có YOU tag)
        var readyLbl = _root?.Q<Label>("player-ready-0");
        if (readyLbl != null)
        {
            readyLbl.text = ready ? "✅ READY" : "⏳ NOT READY";
            readyLbl.EnableInClassList("lobby-ready-yes", ready);
            readyLbl.EnableInClassList("lobby-ready-no", !ready);
        }
    }

    /// <summary>Update all 8 player slots at once.</summary>
    public void SetPlayerSlots(PlayerSlotData[] data)
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (_slots[i] == null) continue;
            var d = i < data.Length ? data[i] : default;
            ApplySlotData(i, d);
        }
        RefreshReadyCount(data);
    }

    public void SetRoomCode(string code)
    {
        if (_lblRoomTitle != null) _lblRoomTitle.text = $"🚢 Room: {code}";
    }

    /// <summary>Append a chat message row.</summary>
    public void AddChatMessage(string senderName, string senderColorHex, string message)
    {
        if (_chatMessages == null) return;

        var row = new VisualElement();
        row.AddToClassList("lobby-chat-msg");

        var sender = new Label($"{senderName}: ");
        sender.AddToClassList("lobby-chat-sender");
        sender.style.color = new Color(
            Convert.HexToFloat(senderColorHex, 0),
            Convert.HexToFloat(senderColorHex, 1),
            Convert.HexToFloat(senderColorHex, 2));

        var text = new Label(message);
        text.AddToClassList("lobby-chat-text");

        row.Add(sender);
        row.Add(text);
        _chatMessages.Add(row);

        // Scroll to bottom next frame
        _root?.schedule.Execute(() =>
            (_chatScroll as ScrollView)?.ScrollTo(_chatMessages.ElementAt(_chatMessages.childCount - 1))
        ).ExecuteLater(50);
    }

    // ------------------------------------------------------------------ //
    //  PRIVATE SETUP
    // ------------------------------------------------------------------ //

    private void BindElements()
    {
        _lblRoomTitle  = _root.Q<Label>("lbl-room-title");
        _lblReadyCount = _root.Q<Label>("lbl-ready-count");
        _chatMessages  = _root.Q<VisualElement>("lobby-chat-messages");
        _chatScroll    = _root.Q<VisualElement>("lobby-chat-scroll");
        _btnStart      = _root.Q<Button>("btn-start");
        _btnBack       = _root.Q<Button>("btn-back");
        _btnCopy       = _root.Q<Button>("btn-copy-code");

        for (int i = 0; i < MaxSlots; i++)
            _slots[i] = _root.Q<VisualElement>($"player-slot-{i}");

        // Team size buttons
        BindToggleGroup(
            new[] { "btn-team-1", "btn-team-2", "btn-team-4" },
            "lobby-option-selected");

        // Team assignment buttons
        BindToggleGroup(
            new[] { "btn-assign-random", "btn-assign-pick" },
            "lobby-option-selected");

        // Demo chat messages
        AddChatMessage("OceanPanda", "#ffd94d", "Let's gooo! 🐼");
        AddChatMessage("WaveRider", "#cc80ff", "gg ez 🦊");
        AddChatMessage("StormKing", "#ff7373", "I will sink all of you 😈");
    }

private void BindButtons()
    {
        if (_btnBack != null)  { _btnBack.clicked  -= OnBackClicked;  _btnBack.clicked  += OnBackClicked; }
        if (_btnStart != null) { _btnStart.clicked -= OnStartClicked; _btnStart.clicked += OnStartClicked; }
        if (_btnCopy != null)  { _btnCopy.clicked  -= OnCopyCode;     _btnCopy.clicked  += OnCopyCode; }

        var btnChooseCaptain = _root?.Q<Button>("btn-choose-captain");
        if (btnChooseCaptain != null)
            btnChooseCaptain.clicked += () =>
                UIManager.Instance?.ShowCharacterSelect(UIManager.CharacterSelectContext.FromLobby);
    }

    private void OnBackClicked()  => UIManager.Instance?.ShowMainMenu();
    private void OnStartClicked() => UIManager.Instance?.StartGameplay();

    private void BindToggleGroup(string[] buttonNames, string selectedClass)
    {
        foreach (var name in buttonNames)
        {
            var btn = _root.Q<Button>(name);
            if (btn == null) continue;
            string capturedName = name;
            btn.RegisterCallback<ClickEvent>(_ =>
            {
                foreach (var n in buttonNames)
                {
                    var b = _root.Q<Button>(n);
                    if (b == null) continue;
                    if (n == capturedName) b.AddToClassList(selectedClass);
                    else                  b.RemoveFromClassList(selectedClass);

                    // Sync icon/text colors via child labels
                    foreach (var lbl in b.Query<Label>().Build())
                    {
                        lbl.style.color = n == capturedName
                            ? new Color(0.18f, 0.85f, 0.45f)   // green
                            : new Color(1f, 1f, 1f, 0.80f);
                    }
                }
            });
        }
    }

    private void ApplySlotData(int index, PlayerSlotData d)
    {
        var slot = _slots[index];
        if (slot == null) return;

        bool isEmpty = !d.isActive;

        // Toggle empty / filled classes
        slot.EnableInClassList("lobby-slot-empty",  isEmpty);
        slot.EnableInClassList("lobby-slot-filled", !isEmpty);

        // Clear old accent colour
        foreach (var accent in new[] { "lobby-slot-green","lobby-slot-orange","lobby-slot-purple",
                                        "lobby-slot-cyan","lobby-slot-red","lobby-slot-blue" })
            slot.RemoveFromClassList(accent);

        if (!isEmpty && !string.IsNullOrEmpty(d.accentClass))
            slot.AddToClassList(d.accentClass);

        // Update child labels
        slot.Q<Label>($"player-emoji-{index}")?.SetText(isEmpty ? "➕" : d.emoji);
        slot.Q<Label>($"player-name-{index}")?.SetText(d.displayName);
        slot.Q<Label>($"player-level-{index}")?.SetText($"⭐ Lv.{d.level}");
        slot.Q<Label>($"player-ship-{index}")?.SetText(d.shipName);

        var hostTag = slot.Q<Label>($"player-host-tag-{index}");
        if (hostTag != null) hostTag.style.display = d.isHost ? DisplayStyle.Flex : DisplayStyle.None;

        var readyLbl = slot.Q<Label>($"player-ready-{index}");
        if (readyLbl != null)
        {
            readyLbl.text = d.isReady ? "✅ READY" : "⏳ NOT READY";
            readyLbl.EnableInClassList("lobby-ready-yes", d.isReady);
            readyLbl.EnableInClassList("lobby-ready-no", !d.isReady);
            readyLbl.style.display = isEmpty ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    private void RefreshReadyCount(PlayerSlotData[] data)
    {
        int total = 0, ready = 0;
        foreach (var d in data)
        {
            if (!d.isActive) continue;
            total++;
            if (d.isReady) ready++;
        }
        if (_lblReadyCount != null) _lblReadyCount.text = $"{ready}/{total} players ready";
    }

    private void OnCopyCode()
    {
        if (_lblRoomTitle == null) return;
        // Extract code after last space
        var parts = _lblRoomTitle.text.Split(' ');
        string code = parts.Length > 0 ? parts[^1] : "";
        GUIUtility.systemCopyBuffer = code;
        Debug.Log($"[LobbyUI] Room code copied: {code}");
    }

    // ------------------------------------------------------------------ //
    //  HELPERS
    // ------------------------------------------------------------------ //

    private static class Convert
    {
        public static float HexToFloat(string hex, int channel)
        {
            if (hex == null || hex.Length < 7) return 1f;
            string h = hex.TrimStart('#');
            try
            {
                int idx = channel * 2;
                return System.Convert.ToInt32(h.Substring(idx, 2), 16) / 255f;
            }
            catch { return 1f; }
        }
    }
}

// Extension helper to set Label text safely
internal static class LabelExt
{
    public static void SetText(this Label lbl, string text)
    {
        if (lbl != null) lbl.text = text;
    }
}
