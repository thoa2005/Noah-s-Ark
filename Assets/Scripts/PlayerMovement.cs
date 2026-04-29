using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed           = 8f;
    public float jumpForce           = 10f;
    public float groundCheckDistance = 1.3f;
    public Transform leftFoot;
    public Transform rightFoot;

    [Header("Punch (Chuot phai)")]
    public float punchForce    = 15f;
    public float punchRadius   = 2f;
    public float punchCooldown = 0.5f;

    [Header("Grab+Throw (Chuot trai): Nhan=cam, Giu=charge, Nha=nem")]
    public float grabRadius    = 2.5f;
    public float grabDistance  = 1.5f;
    public float minThrowForce = 5f;
    public float maxThrowForce = 20f;
    public float maxChargeTime = 1.0f;
    [Tooltip("Luc pha vo FixedJoint khi bi va cham manh")]
    public float grabBreakForce = 800f;

    // --- Runtime state ---
    private ActiveRagdollController ragdoll; // <-- THÊM DÒNG NÀY
    Rigidbody  rb;
    bool       isGrounded;
    float      punchTimer;
    Camera     mainCam;
    Vector2    moveInput;

    Rigidbody  grabbedRb;
    FixedJoint grabJoint;
    float      chargeTimer;
    bool       isGrabbing;

    // Xuong tay de punch / grab chinh xac hon
    Transform  leftHandBone;
    Transform  rightHandBone;

    // ------------------------------------------------------------------ //

    void Start()
    {
        ragdoll = GetComponent<ActiveRagdollController>(); // <-- THÊM DÒNG NÀY
        rb             = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam        = Camera.main;

        // Cache xuong tay tu physicRig (neu co ActiveRagdollController)
        var controller = GetComponent<ActiveRagdollController>();
        if (controller != null && controller.physicRig != null)
        {
            Transform[] allBones = controller.physicRig.GetComponentsInChildren<Transform>();
            foreach (var t in allBones)
            {
                string n = t.name.ToLower();
                if (n.Contains("hand.l") || n.Contains("hand_l") || n == "handl")
                    leftHandBone  = t;
                if (n.Contains("hand.r") || n.Contains("hand_r") || n == "handr")
                    rightHandBone = t;
            }
        }
    }

    void OnMove(InputValue v) => moveInput = v.Get<Vector2>();

    void OnJump(InputValue v)
    {
         // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Cấm nhảy
        if (ragdoll != null && (ragdoll.isKnockedOut )) return;
        if (!v.isPressed || !isGrounded) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Tinh tong khoi luong ragdoll de jump cho dung
        float totalMass = rb.mass;
        foreach (var r in GetComponentsInChildren<Rigidbody>())
            if (r != rb) totalMass += r.mass;

        float finalJumpForce = jumpForce * (totalMass * 0.8f);
        rb.AddForce(Vector3.up * finalJumpForce, ForceMode.Impulse);
        Debug.Log($"[Jump] Mass={totalMass:F1}  Force={finalJumpForce:F1}");
    }

    // ------------------------------------------------------------------ //

    void Update()
    {
         // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Cấm hành động
        if (ragdoll != null && (ragdoll.isKnockedOut)) return;
        CheckGround();
        punchTimer -= Time.deltaTime;

        var mouse    = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null || keyboard == null) return;

        // CHUOT PHAI: DAM
        if (mouse.rightButton.wasPressedThisFrame && punchTimer <= 0f)
            PerformPunch();

        // CHUOT TRAI: GRAB / CHARGE / THROW
        if (!isGrabbing)
        {
            if (mouse.leftButton.wasPressedThisFrame) PerformGrab();
        }
        else
        {
            if (grabbedRb == null)                          { ReleaseGrab(); }
            else if (mouse.leftButton.isPressed)            { chargeTimer = Mathf.Clamp(chargeTimer + Time.deltaTime, 0f, maxChargeTime); }
            else if (mouse.leftButton.wasReleasedThisFrame) { PerformThrow(); }
        }
    }

    void CheckGround()
    {
        RaycastHit hit;
        bool leftG   = leftFoot  != null && Physics.SphereCast(leftFoot.position,  0.1f, Vector3.down, out hit, 0.15f);
        bool rightG  = rightFoot != null && Physics.SphereCast(rightFoot.position, 0.1f, Vector3.down, out hit, 0.15f);
        bool centerG = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance);
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
        Vector3 dir = (camF * moveInput.y + camR * moveInput.x).normalized;

        if (dir.magnitude > 0.1f)
        {
            Vector3 vel = dir * moveSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), 12f * Time.fixedDeltaTime);
        }
        else
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = Mathf.Lerp(vel.x, 0f, 10f * Time.fixedDeltaTime);
            vel.z = Mathf.Lerp(vel.z, 0f, 10f * Time.fixedDeltaTime);
            rb.linearVelocity = vel;
        }
    }

    // ------------------------------------------------------------------ //
    //  ACTIONS
    // ------------------------------------------------------------------ //

    public void PerformPunch()
    {
        punchTimer = punchCooldown;
        ReleaseGrab();

        // Dung vi tri tay phai de punch chinh xac hon
        Vector3 origin = rightHandBone != null
            ? rightHandBone.position
            : transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;

        foreach (var h in Physics.OverlapSphere(origin, punchRadius))
        {
            if (h.gameObject == gameObject)       continue;
            if (h.transform.IsChildOf(transform)) continue;
            var hrb = h.GetComponent<Rigidbody>();
            if (hrb == null) continue;
            Vector3 punchDir = (h.transform.position - origin).normalized + Vector3.up * 0.3f;
            hrb.AddForce(punchDir * punchForce, ForceMode.Impulse);
            Debug.Log("[Punch] Hit: " + h.gameObject.name);
        }
    }

    public void PerformGrab()
    {
        Vector3 origin = rightHandBone != null
            ? rightHandBone.position
            : transform.position + transform.forward * grabDistance;

        float best     = Mathf.Infinity;
        Rigidbody pick = null;
        foreach (var h in Physics.OverlapSphere(origin, grabRadius))
        {
            if (h.gameObject == gameObject)       continue;
            if (h.transform.IsChildOf(transform)) continue;
            var hrb = h.GetComponent<Rigidbody>();
            if (hrb == null) continue;
            float d = Vector3.Distance(origin, h.transform.position);
            if (d < best) { best = d; pick = hrb; }
        }

        if (pick != null)
        {
            ReleaseGrab();

            grabbedRb   = pick;
            isGrabbing  = true;
            chargeTimer = 0f;

            // FixedJoint: vat bi giu chac, breakForce cho phep bi bung khi va manh
            GameObject anchor = rightHandBone != null ? rightHandBone.gameObject : gameObject;
            grabJoint = anchor.AddComponent<FixedJoint>();
            grabJoint.connectedBody   = pick;
            grabJoint.breakForce      = grabBreakForce;
            grabJoint.breakTorque     = grabBreakForce;
            grabJoint.enableCollision = false;

            Debug.Log("[Grab] Caught: " + pick.name + " via FixedJoint");
        }
    }

    void ReleaseGrab()
    {
        if (grabJoint != null) { Destroy(grabJoint); grabJoint = null; }
        isGrabbing  = false;
        chargeTimer = 0f;
        grabbedRb   = null;
    }

    // Goi khi FixedJoint bi break do luc manh
    void OnJointBreak(float breakForce)
    {
        Debug.Log($"[Grab] Joint broke at force {breakForce:F1}");
        grabJoint  = null;
        isGrabbing = false;
        grabbedRb  = null;
    }

    public bool IsCharging() { return isGrabbing && grabbedRb != null; }
    public float GetChargePct() { return chargeTimer / maxChargeTime; }

    public void PerformThrow()
    {
        float pw  = chargeTimer / maxChargeTime;
        float frc = Mathf.Lerp(minThrowForce, maxThrowForce, pw);
        Vector3 td = (transform.forward + Vector3.up * 0.15f).normalized;
        var tgt    = grabbedRb;

        ReleaseGrab();

        if (tgt != null)
        {
            tgt.AddForce(td * frc, ForceMode.Impulse);
            Debug.Log($"[Throw] Force={frc:F1} Dir={td}");
        }
    }

    // ------------------------------------------------------------------ //

    void OnDrawGizmosSelected()
    {
        Vector3 punchOrigin = rightHandBone != null
            ? rightHandBone.position
            : transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(punchOrigin, punchRadius);

        Vector3 grabOrigin = rightHandBone != null
            ? rightHandBone.position
            : transform.position + transform.forward * grabDistance;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(grabOrigin, grabRadius);

        Gizmos.color = isGrounded ? Color.green : Color.gray;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}
