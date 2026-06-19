using UnityEngine;
using Fusion;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Detectors")]
    public GroundDetect groundDetect;
    public PlayerStats stats;

    public Animator anim;
    private ActiveRagdollController ragdoll;
    private Rigidbody rb;
    private CharacterInput _input;
    private Camera mainCam;

    [Header("Movement Smoothing")]
    public float accelerationTime = 0.12f;
    public float decelerationTime = 0.08f;
    [Range(1f, 20f)]
    public float rotationSpeed = 8f;

    private Vector3 currentVelocityXZ;
    private Quaternion targetRotation;
    private Vector3 lastMoveDir;

    private void Awake()
    {
        ragdoll = GetComponent<ActiveRagdollController>();
        rb = GetComponent<Rigidbody>();
        _input = GetComponent<CharacterInput>();
    }

    private void Start()
    {
        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam = Camera.main;
        targetRotation = transform.rotation;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return;

        if (!GetInput(out NetworkInputData inputData))
        {
            ApplyMovement(Vector2.zero, false);
            return;
        }

        if (stats != null && stats.NetworkReady && stats.isKnockedOut)
        {
            ApplyMovement(Vector2.zero, false);
            return;
        }

        ApplyMovement(inputData.moveInput, inputData.isJumpPressed);
    }

    private void ApplyMovement(Vector2 moveInput, bool jumpPressed)
    {
        if (rb == null)
            return;

        float deltaTime = Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime;

        if (jumpPressed)
        {
            Jump();
            if (Object != null && Object.HasInputAuthority && _input != null)
                _input.UseJumpRequest();
        }

        Vector3 camF = mainCam != null ? mainCam.transform.forward : Vector3.forward;
        camF.y = 0f;
        camF.Normalize();

        Vector3 camR = mainCam != null ? mainCam.transform.right : Vector3.right;
        camR.y = 0f;
        camR.Normalize();

        Vector3 dir = (camF * moveInput.y + camR * moveInput.x).normalized;

        bool isMovingBackwards = false;
        if (dir.magnitude > 0.1f)
        {
            float dot = Vector3.Dot(transform.forward, dir);
            isMovingBackwards = dot < -0.7f;
        }

        if (dir.magnitude > 0.1f)
        {
            float finalMoveSpeed = stats != null ? stats.moveSpeed : 0f;
            if (ragdoll != null && ragdoll.IsBeingGrabbed)
                finalMoveSpeed *= 0.1f;

            lastMoveDir = Vector3.Slerp(lastMoveDir, dir, rotationSpeed * deltaTime);
            if (lastMoveDir.magnitude < 0.01f)
                lastMoveDir = dir;

            Vector3 targetVelXZ = lastMoveDir.normalized * finalMoveSpeed * dir.magnitude;
            currentVelocityXZ = Vector3.Lerp(
                currentVelocityXZ,
                targetVelXZ,
                (1f / accelerationTime) * deltaTime
            );

            Vector3 vel = currentVelocityXZ;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;

            if (!isMovingBackwards)
            {
                targetRotation = Quaternion.LookRotation(lastMoveDir);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * deltaTime));
            }
        }
        else
        {
            lastMoveDir = Vector3.zero;
            currentVelocityXZ = Vector3.Lerp(
                currentVelocityXZ,
                Vector3.zero,
                (1f / decelerationTime) * deltaTime
            );

            Vector3 vel = currentVelocityXZ;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
        }

        UpdateAnimator(dir, isMovingBackwards, deltaTime);
    }

    private void UpdateAnimator(Vector3 dir, bool isMovingBackwards, float deltaTime)
    {
        if (anim == null)
            return;

        anim.SetFloat("Speed", dir.magnitude, 0.25f, deltaTime);
        if (dir.magnitude > 0.1f)
        {
            float motionDir = isMovingBackwards ? -1f : 1f;
            anim.SetFloat("MotionDirection", motionDir, 0.1f, deltaTime);
        }
    }

    private void Jump()
    {
        if (stats != null && stats.NetworkReady && stats.isKnockedOut)
            return;
        if (ragdoll != null && ragdoll.IsBeingGrabbed)
            return;
        if (groundDetect == null || !groundDetect.isGrounded)
            return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        var allRbs = GetComponentsInChildren<Rigidbody>();
        foreach (var r in allRbs)
        {
            float force = stats.jumpForce * (r.mass * 0.8f);
            r.AddForce(Vector3.up * force, ForceMode.Impulse);
        }
    }
}
