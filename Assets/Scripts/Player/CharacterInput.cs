using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(PlayerInput))]
public class CharacterInput : MonoBehaviour
{
    // --- Input state ---
    public Vector2 moveInput          { get; set; }
    public bool    isPunching         { get; set; }
    public bool    isJumpPressed      { get; set; }
    public bool    isGrabPressed      { get; set; }
    public Vector2 lookInput          { get; set; }
    public bool    isRestartRequest   { get; set; }
    public bool    isCameraRotatePressed { get; set; }
    public Vector2 zoomInput          { get; set; }

    // --- Anti-spam cooldown (0.08s = max ~12 inputs/sec) ---
    private float lastAttackTime;
    private float lastJumpTime;
    private float lastInteractTime;
    private float lastRestartTime;
    private const float INPUT_COOLDOWN = 0.08f;

    // --- Unity lifecycle ---
    private void Start()
    {
        ReleaseUIFocus();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ClearAllInputs();
    }

    // --- Input System callbacks (PlayerInput → SendMessages mode) ---

    public void OnMove(InputValue v)         => moveInput           = v.Get<Vector2>();
    public void OnLook(InputValue v)         => lookInput           = v.Get<Vector2>();
    public void OnZoom(InputValue v)         => zoomInput           = v.Get<Vector2>();
    public void OnCameraRotate(InputValue v) => isCameraRotatePressed = v.isPressed;

    public void OnAttack(InputValue v)
    {
        if (!v.isPressed) return;
        if (Time.time - lastAttackTime < INPUT_COOLDOWN) return;
        lastAttackTime = Time.time;
        isPunching = true;
    }

    public void OnJump(InputValue v)
    {
        if (!v.isPressed) return;
        if (Time.time - lastJumpTime < INPUT_COOLDOWN) return;
        lastJumpTime = Time.time;
        isJumpPressed = true;
    }

    public void OnInteract(InputValue v)
    {
        if (v.isPressed && Time.time - lastInteractTime < INPUT_COOLDOWN) return;
        if (v.isPressed) lastInteractTime = Time.time;
        isGrabPressed = v.isPressed;
    }

    public void OnRestart(InputValue v)
    {
        if (!v.isPressed) return;
        if (Time.time - lastRestartTime < INPUT_COOLDOWN) return;
        lastRestartTime = Time.time;
        isRestartRequest = true;
    }

    // --- Consume helpers (called by PlayerMovement / PlayerCombat after processing) ---
    public void UseJumpRequest()    => isJumpPressed    = false;
    public void UsePunchRequest()   => isPunching       = false;
    public void UseRestartRequest() => isRestartRequest = false;

    // --- Safety reset: clears all inputs on focus loss or manual call ---
    public void ClearAllInputs()
    {
        moveInput            = Vector2.zero;
        isPunching           = false;
        isJumpPressed        = false;
        isGrabPressed        = false;
        isRestartRequest     = false;
        isCameraRotatePressed = false;
        zoomInput            = Vector2.zero;
    }

    // --- Chuyển sang chế độ UI: tắt PlayerInput để không tranh input với UI ---
    public void EnableUIMode()
    {
        ClearAllInputs();
        GetComponent<PlayerInput>().enabled = false;
    }

    // --- Trở lại chế độ gameplay: bật lại PlayerInput ---
    public void DisableUIMode()
    {
        GetComponent<PlayerInput>().enabled = true;
    }

    // --- Releases any UI focus that may have stolen keyboard input (e.g. after scene transition) ---
    private void ReleaseUIFocus()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null)
        {
            es.SetSelectedGameObject(null);
            es.sendNavigationEvents = false;
        }

        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            doc.rootVisualElement?.panel?.focusController?.focusedElement?.Blur();
        }
    }
}
