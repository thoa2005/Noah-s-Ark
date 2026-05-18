using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour
{
    // Chỉ chứa các thuộc tính trạng thái:
    public Vector2 moveInput { get; set; }
    public bool isPunching { get; set; }
    public bool isJumpPressed { get; set; }
    public bool isGrabPressed { get; set; }
    public Vector2 lookInput { get; set; }
    public bool isRestartRequest { get; set; }
    public bool isCameraRotatePressed { get; set; }
    public Vector2 zoomInput { get; set; }

    // --- Bộ đệm ghi nhớ thời gian nhấn nút (Anti-Spam 0.08s) ---
    private float lastAttackTime;
    private float lastJumpTime;
    private float lastInteractTime;
    private float lastRestartTime;
    private const float INPUT_COOLDOWN = 0.08f; // Giới hạn 0.08 giây (tối đa ~12.5 click/giây)

    // --- Các hàm nhận tín hiệu từ Input System ---
    public void OnMove(InputValue v)
    {
        moveInput = v.Get<Vector2>();
    }

    public void OnAttack(InputValue v)
    {
        if (v.isPressed)
        {
            // Kiểm tra và chặn đứng spam click nếu nhanh hơn 0.08 giây
            if (Time.time - lastAttackTime < INPUT_COOLDOWN) return;
            lastAttackTime = Time.time;
            
            isPunching = true;
        }
    }

    public void OnJump(InputValue v)
    {
        if (v.isPressed)
        {
            // Kiểm tra và chặn đứng spam nhảy nếu nhanh hơn 0.08 giây
            if (Time.time - lastJumpTime < INPUT_COOLDOWN) return;
            lastJumpTime = Time.time;
            
            isJumpPressed = true;
        }
    }

    public void OnInteract(InputValue v)
    {
        if (v.isPressed)
        {
            // Kiểm tra và chặn đứng spam tóm/thả nếu nhanh hơn 0.08 giây
            if (Time.time - lastInteractTime < INPUT_COOLDOWN) return;
            lastInteractTime = Time.time;
        }

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
        if (v.isPressed)
        {
            // Kiểm tra và chặn đứng spam restart nếu nhanh hơn 0.08 giây
            if (Time.time - lastRestartTime < INPUT_COOLDOWN) return;
            lastRestartTime = Time.time;
            
            isRestartRequest = true;
        }
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
