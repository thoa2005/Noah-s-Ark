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
    public float rotationSmoothSpeed = 5f; // Tốc độ xoay camera mượt

    [Header("Wall Collision")]
    public float minDistance        = 3f;   // Khoang cach toi thieu toi muc tieu
    public float collisionRadius    = 0.3f; // Ban kinh sphere cast tranh tuong
    public LayerMask collisionMask  = ~0;   // Mac dinh va cham tat ca layer

    [Header("Input")]
    public CharacterInput _input;

    private float currentYaw    = 0f;
    private Vector3 currentTargetPos;      // Vị trí mục tiêu ảo (đã làm mượt)
    private Vector3 smoothVelocity;        // Biến phụ cho SmoothDamp

    void Start()
    {
        if (target != null) currentTargetPos = target.position;
        // Nếu chưa gán input, tự tìm trên player
        if (_input == null) _input = FindFirstObjectByType<CharacterInput>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. LÀM MƯỢT VỊ TRÍ MỤC TIÊU: Triệt tiêu rung lắc từ Ragdoll
        currentTargetPos = Vector3.SmoothDamp(currentTargetPos, target.position, ref smoothVelocity, 0.2f);

        // Xoay ngang bằng Input System mới
        if (_input != null && _input.isCameraRotatePressed)
        {
            currentYaw += _input.lookInput.x * rotateSpeed * Time.deltaTime;
        }

        // Tinh huong camera
        Quaternion rotation     = Quaternion.Euler(fixedPitch, currentYaw, 0f);
        Vector3    targetViewPos = currentTargetPos + Vector3.up * 0.5f; // Điểm nhìn cao hơn chân một chút
        Vector3    camDir        = rotation * Vector3.back;

        // 2. DI CHUYỂN CAMERA MƯỢT MÀ
        Vector3 desiredPos = targetViewPos + camDir * distance;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // 3. XOAY CAMERA MƯỢT MÀ
        Quaternion targetRotation = Quaternion.LookRotation(targetViewPos - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}
