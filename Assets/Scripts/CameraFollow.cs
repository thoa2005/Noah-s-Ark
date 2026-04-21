using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 12f;
    public float fixedPitch = 40f;      // Góc nhìn từ trên xuống cố định (như Party Animals)
    public float rotateSpeed = 120f;
    public float smoothSpeed = 6f;

    private float currentYaw = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        // Chỉ xoay ngang bằng chuột phải (không cho xoay lên/xuống)
        var mouse = Mouse.current;
        if (mouse != null && mouse.middleButton.isPressed)
            currentYaw += mouse.delta.x.ReadValue() * rotateSpeed * Time.deltaTime;

        // Tính vị trí camera với góc pitch cố định
        Quaternion rotation = Quaternion.Euler(fixedPitch, currentYaw, 0f);
        Vector3 desiredPos = target.position + rotation * new Vector3(0f, 0f, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}
