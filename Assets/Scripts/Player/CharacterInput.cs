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
    public void UseRestartRequest() => isRestartRequest = false;
}
