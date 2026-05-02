using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance      = 10f;
    public float fixedPitch    = 40f;   // Goc nhin tu tren xuong co dinh (nhu Party Animals)
    public float rotateSpeed   = 120f;
    public float smoothSpeed   = 6f;

    [Header("Wall Collision")]
    public float minDistance        = 3f;   // Khoang cach toi thieu toi muc tieu
    public float collisionRadius    = 0.3f; // Ban kinh sphere cast tranh tuong
    public LayerMask collisionMask  = ~0;   // Mac dinh va cham tat ca layer

    private float currentYaw   = 0f;
    private float currentDist;             // Khoang cach thuc te sau khi tranh tuong

    void Start()
    {
        currentDist = distance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Xoay ngang bang Middle Mouse
        var mouse = Mouse.current;
        if (mouse != null && mouse.middleButton.isPressed)
            currentYaw += mouse.delta.x.ReadValue() * rotateSpeed * Time.deltaTime;

        // Tinh huong camera
        Quaternion rotation   = Quaternion.Euler(fixedPitch, currentYaw, 0f);
        Vector3    targetPos  = target.position + Vector3.up * 1f;
        Vector3    camDir     = rotation * Vector3.back; // Huong tu muc tieu den camera

        // --- Wall Collision Avoidance ---
        float targetDist = distance;
        RaycastHit hit;
        if (Physics.SphereCast(targetPos, collisionRadius, camDir, out hit, distance, collisionMask))
        {
            // Dat camera truoc vat can voi mot chut offset
            targetDist = Mathf.Max(hit.distance - 0.3f, minDistance);
        }

        // Smooth khoang cach de khong giat
        currentDist = Mathf.Lerp(currentDist, targetDist, smoothSpeed * 2f * Time.deltaTime);

        Vector3 desiredPos = targetPos + camDir * currentDist;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(targetPos);
    }
}
