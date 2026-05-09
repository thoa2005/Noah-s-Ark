using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 10f;
    public float groundCheckDistance = 1.3f;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;



    public Transform leftFoot;
    public Transform rightFoot;


    // --- Runtime state ---
    public Animator anim;
    private ActiveRagdollController ragdoll; // <-- THÊM DÒNG NÀY
    Rigidbody rb;
    private CharacterInput _input;
    bool isGrounded;
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
        if (ragdoll != null && (ragdoll.isKnockedOut)) return;
        if (!isGrounded) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // // Tinh tong khoi luong ragdoll de jump cho dung
        // float totalMass = rb.mass;

        // 1. Lấy tất cả Rigidbody (vỏ capsule + các khớp xương)
        var allRbs = GetComponentsInChildren<Rigidbody>();
        foreach (var r in allRbs)
        {
            // if (r != rb) totalMass += r.mass;

            float force = jumpForce * (r.mass * 0.8f);
            r.AddForce(Vector3.up * force, ForceMode.Impulse);
        }

    }

    // ------------------------------------------------------------------ //

    void Update()
    {
        // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Cấm hành động
        if (ragdoll != null && ragdoll.isKnockedOut)
        {
            return; // Ngất rồi thì không cho làm gì nữa
        }

        CheckGround();
        // Thêm đoạn này để xử lý Nhảy
        if (_input.isJumpPressed)
        {
            Jump(); // Gọi hàm nhảy chúng ta vừa sửa ở trên
            _input.UseJumpRequest(); // Nhảy xong thì reset lệnh về false
        }
    }

    void CheckGround()
    {
        RaycastHit hit;
        // Bắn lade từ chân trái, chân phải và bụng xuống dưới, CHỈ chạm vào groundLayer
        bool leftG = leftFoot != null && Physics.SphereCast(leftFoot.position, groundCheckRadius, Vector3.down, out hit, groundCheckRadius * 1.5f, groundLayer);
        bool rightG = rightFoot != null && Physics.SphereCast(rightFoot.position, groundCheckRadius, Vector3.down, out hit, groundCheckRadius * 1.5f, groundLayer);
        bool centerG = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance, groundLayer);

        isGrounded = leftG || rightG || centerG;

    }

    // ------------------------------------------------------------------ //

    void FixedUpdate()
    {
        // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Liệt chân, cấm chạy
        if (ragdoll != null && (ragdoll.isKnockedOut)) return;
        Vector3 camF = mainCam != null ? mainCam.transform.forward : Vector3.forward;
        camF.y = 0f; camF.Normalize();
        Vector3 camR = mainCam != null ? mainCam.transform.right : Vector3.right;
        camR.y = 0f; camR.Normalize();
        Vector3 dir = (camF * _input.moveInput.y + camR * _input.moveInput.x).normalized;

        if (anim != null)
        {
            anim.SetFloat("Speed", dir.magnitude);
        }

        if (dir.magnitude > 0.1f)
        {
            Vector3 vel = dir * moveSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;

            rb.MoveRotation(Quaternion.Slerp(rb.rotation,
                Quaternion.LookRotation(dir), 4f * Time.fixedDeltaTime));
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
