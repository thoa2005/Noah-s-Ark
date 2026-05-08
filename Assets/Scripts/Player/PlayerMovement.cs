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

    [Header("Punch (Chuot phai)")]
    public float punchForce = 15f;
    public float pushForce = 50f; // Lực đẩy vật lý (Chỉnh số này to để đối thủ bay xa)
    public float punchRadius = 2f;
    public float punchCooldown = 0.5f;
    public Vector3 punchOffset = new Vector3(0, 0.0002f, 0f); // Độ lệch của vòng đấm so với bàn tay


    [Header("Grab+Throw (Chuot trai): Nhan=cam, Giu=charge, Nha=nem")]
    public float grabRadius = 2.5f;
    public float minThrowForce = 5f;
    public float maxThrowForce = 20f;
    public float maxChargeTime = 1.0f;
    [Tooltip("Luc pha vo FixedJoint khi bi va cham manh")]
    public float grabBreakForce = 800f;
    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 25f; // Mất 25 thể lực mỗi giây khi cầm
    public float staminaRegenRate = 20f; // Hồi 20 thể lực mỗi giây khi nghỉ

    // --- Runtime state ---
    public Animator anim;
    private ActiveRagdollController ragdoll; // <-- THÊM DÒNG NÀY
    Rigidbody rb;
    private CharacterInput _input;
    bool isGrounded;
    Dictionary<GameObject, float> lastHitTime = new Dictionary<GameObject, float>();

    float punchTimer;
    Camera mainCam;
    Rigidbody grabbedRb;
    float chargeTimer;
    bool isGrabbing;

    [Header("Party Animals Grab Physics")]
    public float grabOffset = 0.1f; // Độ lệch của vòng quét so với bàn tay
    public float grabSpring = 15000f; // Độ mạnh của nam châm hút
    public float grabDamper = 1000f;  // Độ êm (giảm rung lắc)
    public Rigidbody leftPhysicsHand;
    public Rigidbody rightPhysicsHand;
    private List<Joint> activeGrabJoints = new List<Joint>();

    // Xuong tay de punch / grab chinh xac hon
    Transform leftHandBone;
    Transform rightHandBone;

    // ------------------------------------------------------------------ //

    void Start()
    {
        ragdoll = GetComponent<ActiveRagdollController>(); // <-- THÊM DÒNG NÀY
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam = Camera.main;

        FindHandBones();
        _input = GetComponent<CharacterInput>();
        currentStamina = maxStamina;
    }

    void FindHandBones()
    {
        if (ragdoll == null) ragdoll = GetComponent<ActiveRagdollController>();
        if (ragdoll != null && ragdoll.physicRig != null)
        {
            Transform[] allBones = ragdoll.physicRig.GetComponentsInChildren<Transform>();
            foreach (var t in allBones)
            {
                string n = t.name.ToLower();
                if (n.Contains("hand.l") || n.Contains("hand_l"))
                    leftHandBone = t;
                if (n.Contains("hand.r") || n.Contains("hand_r"))
                    rightHandBone = t;
            }
        }
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

            if (isGrabbing) ReleaseGrab();
            return; // Ngất rồi thì không cho làm gì nữa
        }

        CheckGround();
        punchTimer -= Time.deltaTime;
        // Thêm đoạn này để xử lý Nhảy
        if (_input.isJumpPressed)
        {
            Jump(); // Gọi hàm nhảy chúng ta vừa sửa ở trên
            _input.UseJumpRequest(); // Nhảy xong thì reset lệnh về false
        }

        // 2. Xử lý Đấm
        if (_input.isPunching && punchTimer <= 0)
        {
            PerformPunch();
            _input.UsePunchRequest();
        }
        // 3. Xử lý Cầm/Ném
        if (!isGrabbing)
        {
            // Cho phép "hút" đồ liên tục khi giữ chuột, miễn là còn thể lực
            if (_input.isGrabPressed && currentStamina > (maxStamina * 0.1f))
            {
                PerformGrab();
            }
        }
        else
        {
            if (grabbedRb == null) ReleaseGrab();
            // Nếu vẫn đang giữ nút Grab thì sạc lực ném
            else if (_input.isGrabPressed)
            {
                chargeTimer = Mathf.Clamp(chargeTimer + Time.deltaTime, 0f, maxChargeTime);
            }
            // Nếu nhả nút Grab ra thì thực hiện ném
            else
            {
                PerformThrow();
            }
        }
        // 1. Logic xử lý Thể lực
        if (isGrabbing)
        {
            // Đang cầm thì trừ thể lực
            currentStamina -= staminaDrainRate * Time.deltaTime;

            // Nếu hết thể lực thì tự động nhả đồ
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                ReleaseGrab();
                Debug.Log("[Stamina] Mệt quá! Đã tự buông tay.");
            }
        }
        else
        {
            // Không cầm thì hồi thể lực
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
            }
        }
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);


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
        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("Punch"))
        {
            Transform[] origins = { leftHandBone, rightHandBone };
            foreach (var hand in origins)
            {
                // Tạo vị trí mới dựa trên Offset (xa tay hơn)
                Vector3 pPos = hand.TransformPoint(punchOffset);

                // Quét tại pPos thay vì hand.position
                foreach (var h in Physics.OverlapSphere(pPos, punchRadius))
                {
                    if (h.gameObject == gameObject || h.transform.IsChildOf(transform)) continue;
                    // KIỂM TRA: Nếu vừa đấm người này cách đây chưa đầy 0.1s thì bỏ qua
                    if (lastHitTime.ContainsKey(h.gameObject))
                    {
                        if (Time.time - lastHitTime[h.gameObject] < 0.1f) continue;
                    }
                    var hrb = h.GetComponent<Rigidbody>();
                    if (hrb == null) continue;
                    // Thực hiện đẩy và gây sát thương
                    Vector3 punchDir = (h.transform.position - hand.position).normalized + Vector3.up * 0.2f;
                    hrb.AddForce(punchDir * pushForce, ForceMode.Impulse);
                    var targetController = h.GetComponentInParent<ActiveRagdollController>();
                    if (targetController != null) targetController.ApplyDamage(punchForce);
                    // Ghi nhớ thời gian vừa đấm trúng người này
                    lastHitTime[h.gameObject] = Time.time;
                }
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  ACTIONS
    // ------------------------------------------------------------------ //

    public void PerformPunch()
    {
        // THÊM DÒNG NÀY ĐỂ KÍCH HOẠT ANIMATION ĐẤM:
        if (anim != null) anim.SetTrigger("Punch");

        punchTimer = punchCooldown;
        ReleaseGrab();

    }
    public void PerformGrab()
    {
        if (currentStamina < 10f) return; // Thể lực dưới 10 thì không cho cầm
        if (leftPhysicsHand == null || rightPhysicsHand == null) return;
        Vector3 leftPos = leftPhysicsHand.position + leftPhysicsHand.transform.forward * grabOffset;
        Vector3 rightPos = rightPhysicsHand.position + rightPhysicsHand.transform.forward * grabOffset;

        // Lấy TẤT CẢ Rb trong từng vùng quét
        var leftSet = GetClosestRb(leftPos, grabRadius);
        var rightSet = GetClosestRb(rightPos, grabRadius);

        // Tìm phần giao: Rb nào nằm trong CẢ 2 vòng?
        Rigidbody commonTarget = null;
        float bestDist = Mathf.Infinity;
        foreach (var rb in leftSet)
        {
            if (rightSet.Contains(rb))
            {
                float d = Vector3.Distance(transform.position, rb.position);
                if (d < bestDist) { bestDist = d; commonTarget = rb; }
            }
        }
        // Nếu tìm được vật chung thì cầm
        if (commonTarget != null)
        {
            AttachHand(leftPhysicsHand, commonTarget);
            AttachHand(rightPhysicsHand, commonTarget);
            isGrabbing = true;
            grabbedRb = commonTarget;
            chargeTimer = 0f;
            if (anim != null) anim.SetBool("IsGrabbing", true);
            Debug.Log("[Grab] Đã ôm vật bằng cả 2 tay: " + commonTarget.name);
        }
        else
        {
            Debug.Log("[Grab] Không có vật nào nằm trong cả 2 vòng quét!");
        }

    }

    void ReleaseGrab()
    {
        // Chặt đứt tất cả lò xo nam châm ở 2 tay khi buông chuột
        foreach (var j in activeGrabJoints)
        {
            if (j != null) Destroy(j);
        }
        activeGrabJoints.Clear();

        isGrabbing = false;
        chargeTimer = 0f;
        grabbedRb = null;

        // Tắt Anim, thả tay xuống
        if (anim != null) anim.SetBool("IsGrabbing", false);
    }
    // HÀM HỖ TRỢ 1: Tìm vật có Rigidbody gần tay nhất
    HashSet<Rigidbody> GetClosestRb(Vector3 origin, float radius)
    {
        // HashSet là một túi đựng, mỗi thứ chỉ được bỏ vào 1 lần (không trùng)
        var result = new HashSet<Rigidbody>();

        foreach (var h in Physics.OverlapSphere(origin, radius))
        {
            // Bỏ qua chính mình và các xương của chính mình
            if (h.gameObject == gameObject || h.transform.IsChildOf(transform)) continue;

            // attachedRigidbody tự leo lên tìm Rigidbody ở parent nếu cần
            // (khác với GetComponent chỉ tìm đúng trên GameObject đó thôi)
            var hrb = h.attachedRigidbody;

            if (hrb != null) result.Add(hrb); // Bỏ vào túi
        }

        return result; // Trả về cả túi, không chỉ 1 cái
    }
    // HÀM HỖ TRỢ 2: Tạo kết nối lò xo nam châm giữa tay và vật
    void AttachHand(Rigidbody handRb, Rigidbody targetRb)
    {
        // 1. Đổi sang ConfigurableJoint
        ConfigurableJoint joint = handRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = targetRb;
        // 2. Thêm các dòng khóa Motion (Vị trí và Xoay)
        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Locked;
        // 3. Thiết lập giới hạn vị trí (thay cho spring/damper cũ)
        joint.linearLimit = new SoftJointLimit { limit = 0.001f };
        // 1. Thêm độ nhún cho giới hạn (giúp bớt giật)
        joint.linearLimitSpring = new SoftJointLimitSpring { spring = 5000f, damper = 100f };
        // 2. Kích hoạt chế độ Projection (giúp vật lý ổn định hơn khi bị nén)
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.01f;
        // 3. Tắt va chạm trực tiếp giữa tay và vật (CỰC KỲ QUAN TRỌNG để bớt giật)
        joint.enableCollision = false;
        // 4. Giữ nguyên phần thiết lập Anchor (Điểm neo)
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero;
        Collider targetCol = targetRb.GetComponentInChildren<Collider>();
        if (targetCol != null)
        {
            Vector3 worldClosestPoint = targetCol.ClosestPoint(handRb.position);
            joint.connectedAnchor = targetRb.transform.InverseTransformPoint(worldClosestPoint);
        }
        else
        {
            joint.connectedAnchor = Vector3.zero;
        }
        // 5. Giữ nguyên phần độ bền
        joint.breakForce = grabBreakForce * 100f;
        joint.breakTorque = grabBreakForce * 100f;
        activeGrabJoints.Add(joint);
    }

    public bool IsCharging() { return isGrabbing && grabbedRb != null; }
    public float GetChargePct() { return chargeTimer / maxChargeTime; }

    public void PerformThrow()
    {
        float pw = chargeTimer / maxChargeTime;
        float frc = Mathf.Lerp(minThrowForce, maxThrowForce, pw);
        Vector3 td = (transform.forward + Vector3.up * 0.15f).normalized;
        var tgt = grabbedRb;

        ReleaseGrab();

        if (tgt != null)
        {
            tgt.AddForce(td * frc, ForceMode.Impulse);
            Debug.Log($"[Throw] Force={frc:F1} Dir={td}");
        }
    }


    void OnDrawGizmosSelected()
    {
        // Nếu chưa Play, tự đi tìm xương tay để hiển thị vòng đỏ trong Scene
        if (leftHandBone == null || rightHandBone == null)
        {
            FindHandBones();
        }


        // Vẽ vòng đỏ cho đấm (Punch)
        Gizmos.color = Color.red;
        if (leftHandBone != null) Gizmos.DrawWireSphere(leftHandBone.TransformPoint(punchOffset), punchRadius);
        if (rightHandBone != null) Gizmos.DrawWireSphere(rightHandBone.TransformPoint(punchOffset), punchRadius);

        // Vẽ vòng vàng cho Grab (nếu bạn muốn xem luôn cả vòng Grab)
        Gizmos.color = Color.yellow;
        if (leftPhysicsHand != null)
        {
            Vector3 lPos = leftPhysicsHand.position + leftPhysicsHand.transform.forward * grabOffset;
            Gizmos.DrawWireSphere(lPos, grabRadius);
        }
        if (rightPhysicsHand != null)
        {
            Vector3 rPos = rightPhysicsHand.position + rightPhysicsHand.transform.forward * grabOffset;
            Gizmos.DrawWireSphere(rPos, grabRadius);
        }
    }

}
