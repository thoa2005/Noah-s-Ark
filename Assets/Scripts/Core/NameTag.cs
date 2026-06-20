using UnityEngine;

/// <summary>
/// Attach to any GameObject that should display a floating name tag above its head.
/// BattleHUDUI reads the displayName and nameColor each frame to render it.
/// </summary>
public class NameTag : MonoBehaviour
{
    [Header("Display Settings")]
    public string displayName;
    public Vector3 offset = new Vector3(0, 2.5f, 0);

    [Header("Color Settings")]
    public Color nameColor = Color.white;

    // Whether this NameTag has been registered with the HUD
    private BattleHUDUI _hud;
    private bool _isRegistered;

    private void Start()
    {
        // Only fall back to the GameObject name if no name was explicitly set
        if (string.IsNullOrEmpty(displayName))
            displayName = gameObject.name;

        _hud = FindFirstObjectByType<BattleHUDUI>();
        if (_hud != null)
        {
            _hud.RegisterNameTag(this);
            _isRegistered = true;
        }
    }

    private void OnEnable()
    {
        if (_hud != null && !_isRegistered)
        {
            _hud.RegisterNameTag(this);
            _isRegistered = true;
        }
    }

    private void OnDisable()
    {
        if (_hud != null && _isRegistered)
        {
            _hud.UnregisterNameTag(this);
            _isRegistered = false;
        }
    }

    private void OnDestroy()
    {
        if (_hud != null && _isRegistered)
        {
            _hud.UnregisterNameTag(this);
            _isRegistered = false;
        }
    }

    public void SetDisplayName(string newName)
    {
        displayName = newName;
        // The HUD usually updates every frame or we can notify it here if it's event-driven.
        // For BattleHUDUI reading it every frame, just changing the string is enough!
    }
}
