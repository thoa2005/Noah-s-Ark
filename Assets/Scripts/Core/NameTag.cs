using UnityEngine;

public class NameTag : MonoBehaviour
{
    [Header("Display Settings")]
    public string displayName = "Player";
    public Vector3 offset = new Vector3(0, 2.5f, 0); // Position above character pivot
    
    [Header("Color Settings")]
    public Color nameColor = Color.white;
    
    private BattleHUDUI hud;
    private bool isRegistered = false;
    
    void Start()
    {
        // Try to auto-resolve name from object name if default is used
        if (displayName == "Player")
        {
            displayName = gameObject.name;
        }
        
        // Find HUD and register
        hud = FindFirstObjectByType<BattleHUDUI>();
        if (hud != null)
        {
            hud.RegisterNameTag(this);
            isRegistered = true;
        }
    }
    
    void OnEnable()
    {
        if (hud != null && !isRegistered)
        {
            hud.RegisterNameTag(this);
            isRegistered = true;
        }
    }
    
    void OnDisable()
    {
        if (hud != null && isRegistered)
        {
            hud.UnregisterNameTag(this);
            isRegistered = false;
        }
    }
    
    void OnDestroy()
    {
        if (hud != null && isRegistered)
        {
            hud.UnregisterNameTag(this);
            isRegistered = false;
        }
    }
}
