using UnityEngine;
using Fusion;

/// <summary>
/// PlayerMovement — NetworkBehaviour (Fusion).
/// - HasInputAuthority: player này là local → apply movement.
/// - Remote players: Fusion sync position/rotation qua NetworkRigidbody.
/// - Ragdoll physics chạy locally trên từng máy (không sync từng xương).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    public GroundDetect groundDetect;
    public PlayerStats  stats;
    public Animator     anim;

    [Header("Movement Smoothing")]
    public float accelerationTime = 0.12f;
    public float decelerationTime = 0.08f;
    [Range(1f, 20f)]
    public float rotationSpeed    = 8f;

    // ── Runtime ──────────────────────────────────────────────────────────
    private ActiveRagdollController ragdoll;
    private Rigidbody               rb;
    private Camera                  mainCam;

    private Vector3    currentVelocityXZ;
    private Quaternion targetRotation;
    private Vector3    lastMoveDir;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public override void Spawned()
    {
        ragdoll        = GetComponent<ActiveRagdollController>();
        rb             = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam        = Camera.main;
        targetRotation = transform.rotation;
    }

    // ── State để Render() đọc ────────────────────────────────────────────
    private float  _animSpeed;
    private float  _animMotionDir;
    private bool   _hasAnimData;

    // ── Fusion tick (replaces FixedUpdate) ───────────────────────────────

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (stats != null && stats.isKnockedOut) return;
        if (!GetInput(out NetworkInputData data)) return;

        if (data.IsJumping) HandleJump();

        if (mainCam == null || !mainCam.isActiveAndEnabled)
            mainCam = Camera.main;

        Vector3 camF = mainCam != null ? mainCam.transform.forward : Vector3.forward;
        camF.y = 0f; camF.Normalize();

        Vector3 camR = mainCam != null ? mainCam.transform.right : Vector3.right;
        camR.y = 0f; camR.Normalize();

        Vector3 dir = (camF * data.MoveInput.y + camR * data.MoveInput.x).normalized;

        bool isMovingBackwards = dir.magnitude > 0.1f &&
                                 Vector3.Dot(transform.forward, dir) < -0.7f;

        // Lưu animation data để Render() apply mỗi frame
        _animSpeed     = dir.magnitude;
        _animMotionDir = isMovingBackwards ? -1f : 1f;
        _hasAnimData   = true;

        if (dir.magnitude > 0.1f)
        {
            float finalSpeed = stats != null ? stats.moveSpeed : 5f;
            if (ragdoll != null && ragdoll.IsBeingGrabbed) finalSpeed *= 0.1f;

            if (lastMoveDir != Vector3.zero && Vector3.Dot(lastMoveDir, dir) < 0f)
                lastMoveDir = dir;
            else
                lastMoveDir = Vector3.Slerp(lastMoveDir, dir, rotationSpeed * Runner.DeltaTime);

            if (lastMoveDir.magnitude < 0.01f) lastMoveDir = dir;

            Vector3 targetVelXZ = lastMoveDir.normalized * finalSpeed * dir.magnitude;
            currentVelocityXZ   = Vector3.Lerp(currentVelocityXZ, targetVelXZ,
                                      (1f / accelerationTime) * Runner.DeltaTime);

            Vector3 vel = currentVelocityXZ;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;

            if (!isMovingBackwards)
            {
                targetRotation = Quaternion.LookRotation(lastMoveDir);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation,
                    rotationSpeed * Runner.DeltaTime));
            }
        }
        else
        {
            lastMoveDir       = Vector3.zero;
            currentVelocityXZ = Vector3.Lerp(currentVelocityXZ, Vector3.zero,
                                    (1f / decelerationTime) * Runner.DeltaTime);

            Vector3 vel = currentVelocityXZ;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
        }
    }

    // ── Render: chạy mỗi frame, update animation mượt như offline ────────
    public override void Render()
    {
        if (!_hasAnimData || anim == null) return;

        // Dùng Time.deltaTime thay Runner.DeltaTime để blend đúng theo frame rate
        anim.SetFloat("Speed", _animSpeed, 0.15f, Time.deltaTime);
        if (_animSpeed > 0.1f)
            anim.SetFloat("MotionDirection", _animMotionDir, 0.1f, Time.deltaTime);
    }

    // ── Jump ─────────────────────────────────────────────────────────────

    private void HandleJump()
    {
        if (stats != null && stats.isKnockedOut) return;
        if (ragdoll != null && ragdoll.IsBeingGrabbed) return;
        if (groundDetect == null || !groundDetect.isGrounded) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float jumpForce = stats != null ? stats.jumpForce : 10f;
        var allRbs = GetComponentsInChildren<Rigidbody>();
        foreach (var r in allRbs)
            r.AddForce(Vector3.up * jumpForce * (r.mass * 0.8f), ForceMode.Impulse);
    }
}
