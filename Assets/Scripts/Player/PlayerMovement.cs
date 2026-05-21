using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Detectors")]
    public GroundDetect groundDetect;
    public PlayerStats stats;


    // --- Runtime state ---
    public Animator anim;
    private ActiveRagdollController ragdoll;
    Rigidbody rb;
    private CharacterInput _input;
    Camera mainCam;

    [Header("Movement Smoothing")]
    public float accelerationTime = 0.12f;
    public float decelerationTime = 0.08f;
    [Range(1f, 20f)]
    public float rotationSpeed = 8f;
    private Vector3 currentVelocityXZ;
    private Quaternion targetRotation;
    private Vector3 lastMoveDir; // Hướng di chuyển frame trước để smooth



    // ------------------------------------------------------------------ //

    void Start()
    {
        ragdoll = GetComponent<ActiveRagdollController>();
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam = Camera.main;
        _input = GetComponent<CharacterInput>();
        targetRotation = transform.rotation; // Khởi tạo rotation đích
    }






    void Jump()
    {
        // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Cấm nhảy
        if (stats != null && stats.isKnockedOut) return;
        if (ragdoll != null && ragdoll.IsBeingGrabbed) return; // KHÓA: Đang bị tóm thì không cho nhảy
        if (groundDetect == null || !groundDetect.isGrounded) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // // Tinh tong khoi luong ragdoll de jump cho dung
        // float totalMass = rb.mass;

        // 1. Lấy tất cả Rigidbody (vỏ capsule + các khớp xương)
        var allRbs = GetComponentsInChildren<Rigidbody>();
        foreach (var r in allRbs)
        {
            // if (r != rb) totalMass += r.mass;

            float force = stats.jumpForce * (r.mass * 0.8f);
            r.AddForce(Vector3.up * force, ForceMode.Impulse);
        }

    }

    // ------------------------------------------------------------------ //

    void Update()
    {
        // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Cấm hành động
        if (stats != null && stats.isKnockedOut)
        {
            return; // Ngất rồi thì không cho làm gì nữa
        }

        // Thêm đoạn này để xử lý Nhảy
        if (_input.isJumpPressed)
        {
            Jump(); // Gọi hàm nhảy chúng ta vừa sửa ở trên
            _input.UseJumpRequest(); // Nhảy xong thì reset lệnh về false
        }
    }

    // ------------------------------------------------------------------ //

    void FixedUpdate()
    {
        // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Liệt chân, cấm chạy
        if (stats != null && stats.isKnockedOut) return;
        Vector3 camF = mainCam != null ? mainCam.transform.forward : Vector3.forward;
        camF.y = 0f; camF.Normalize();
        Vector3 camR = mainCam != null ? mainCam.transform.right : Vector3.right;
        camR.y = 0f; camR.Normalize();
        Vector3 dir = (camF * _input.moveInput.y + camR * _input.moveInput.x).normalized;

        bool isMovingBackwards = false;
        if (dir.magnitude > 0.1f)
        {
            float dot = Vector3.Dot(transform.forward, dir);
            isMovingBackwards = dot < -0.7f;
        }

        if (anim != null)
        {
            // Dùng dampTime để animation blend mượt thay vì chuyển đột ngột
            anim.SetFloat("Speed", dir.magnitude, 0.25f, Time.fixedDeltaTime);
            if (dir.magnitude > 0.1f)
            {
                float motionDir = isMovingBackwards ? -1f : 1f;
                anim.SetFloat("MotionDirection", motionDir, 0.1f, Time.fixedDeltaTime);
            }
        }

        if (dir.magnitude > 0.1f)
        {
            float finalMoveSpeed = stats.moveSpeed;
            if (ragdoll != null && ragdoll.IsBeingGrabbed) finalMoveSpeed *= 0.1f;

            // Smooth hướng di chuyển trước khi tính velocity
            // Đây là key fix: không đổi hướng đột ngột mà lerp dần
            lastMoveDir = Vector3.Slerp(lastMoveDir, dir, rotationSpeed * Time.fixedDeltaTime);
            if (lastMoveDir.magnitude < 0.01f) lastMoveDir = dir;

            Vector3 targetVelXZ = lastMoveDir.normalized * finalMoveSpeed * dir.magnitude;
            currentVelocityXZ = Vector3.Lerp(currentVelocityXZ, targetVelXZ,
                (1f / accelerationTime) * Time.fixedDeltaTime);

            Vector3 vel = currentVelocityXZ;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;

            if (!isMovingBackwards)
            {
                // Xoay theo hướng đã smooth (lastMoveDir) thay vì dir thô
                targetRotation = Quaternion.LookRotation(lastMoveDir);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation,
                    rotationSpeed * Time.fixedDeltaTime));
            }
        }
        else
        {
            lastMoveDir = Vector3.zero;
            currentVelocityXZ = Vector3.Lerp(currentVelocityXZ, Vector3.zero,
                (1f / decelerationTime) * Time.fixedDeltaTime);

            Vector3 vel = currentVelocityXZ;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
        }

    }



}
