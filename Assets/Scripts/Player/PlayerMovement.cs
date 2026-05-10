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
    private ActiveRagdollController ragdoll; // <-- THÊM DÒNG NÀY
    Rigidbody rb;
    private CharacterInput _input;
    Camera mainCam;



    // ------------------------------------------------------------------ //

    void Start()
    {
        ragdoll = GetComponent<ActiveRagdollController>(); // <-- THÊM DÒNG NÀY
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam = Camera.main;

        _input = GetComponent<CharacterInput>();
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
            anim.SetFloat("Speed", dir.magnitude);
            if (dir.magnitude > 0.1f)
            {
                anim.SetFloat("MotionDirection", isMovingBackwards ? -1f : 1f);
            }
        }

        if (dir.magnitude > 0.1f)
        {
            float finalMoveSpeed = stats.moveSpeed;
            if (ragdoll != null && ragdoll.IsBeingGrabbed) finalMoveSpeed *= 0.1f; // Giảm 90% tốc độ khi bị tóm

            Vector3 vel = dir * finalMoveSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;

            if (!isMovingBackwards)
            {
                rb.MoveRotation(Quaternion.Slerp(rb.rotation,
                    Quaternion.LookRotation(dir), 4f * Time.fixedDeltaTime));
            }
        }
        else
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = Mathf.Lerp(vel.x, 0f, 10f * Time.fixedDeltaTime);
            vel.z = Mathf.Lerp(vel.z, 0f, 10f * Time.fixedDeltaTime);
            rb.linearVelocity = vel;
            // rb.AddForce(dir * moveSpeed, ForceMode.Acceleration);

        }

    }



}
