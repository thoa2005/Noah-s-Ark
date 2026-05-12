using UnityEngine;
using UnityEngine.InputSystem;
public class CharacterInput : MonoBehaviour
{
    // Chỉ chứa 4 thứ này thôi:
    public Vector2 moveInput { get; set; }
    public bool isPunching { get; set; }
    public bool isJumpPressed { get; set; }
    public bool isGrabPressed { get; set; }
    public Vector2 lookInput { get; set; }
    public bool isRestartRequest { get; set; }
    public bool isCameraRotatePressed { get; set; }
    public Vector2 zoomInput { get; set; }


    // --- Các hàm nhận tín hiệu từ Input System ---
    public void OnMove(InputValue v)
    {
        moveInput = v.Get<Vector2>();
    }
    public void OnAttack(InputValue v)
    {
        if (v.isPressed)
        {
            isPunching = true;
        }
    }
    public void OnJump(InputValue v)
    {
        // Khi bấm nút nhảy, ta bật cái đèn hiệu JumpPressed lên
        if (v.isPressed) isJumpPressed = true;
    }
    public void OnInteract(InputValue v)
    {
        Debug.Log($"[INPUT CHECK] Object: {gameObject.name} | InputValue: {v.isPressed} | Frame: {Time.frameCount}");
        isGrabPressed = v.isPressed;
    }
    // Hàm để tầng Logic reset lại lệnh nhảy sau khi nhảy xong
    public void UseJumpRequest() => isJumpPressed = false;
    public void UsePunchRequest() => isPunching = false;
    public void OnLook(InputValue v)
    {
        lookInput = v.Get<Vector2>();
    }
    public void OnRestart(InputValue v)
    {
        if (v.isPressed) isRestartRequest = true;
    }
    public void OnCameraRotate(InputValue v)
    {
        isCameraRotatePressed = v.isPressed;
    }
    public void OnZoom(InputValue v)
    {
        zoomInput = v.Get<Vector2>();
    }
    public void UseRestartRequest() => isRestartRequest = false;

    // --- CẦU CHÌ AN TOÀN: Reset toàn bộ phím khi mất tập trung hoặc lag ---
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) ClearAllInputs();
    }

    public void ClearAllInputs()
    {
        moveInput = Vector2.zero;
        isPunching = false;
        isJumpPressed = false;
        isGrabPressed = false;
        isRestartRequest = false;
        isCameraRotatePressed = false;
        zoomInput = Vector2.zero;
        Debug.Log($"[INPUT SAFETY] All inputs cleared for {gameObject.name}");
    }
}
