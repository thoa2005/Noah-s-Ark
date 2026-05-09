using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
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

    [Header("Party Animals Grab Physics")]
    public float grabOffset = 0.1f; // Độ lệch của vòng quét so với bàn tay
    public float grabSpring = 15000f; // Độ mạnh của nam châm hút
    public float grabDamper = 1000f;  // Độ êm (giảm rung lắc)
    public Rigidbody leftPhysicsHand;
    public Rigidbody rightPhysicsHand;
    private List<Joint> activeGrabJoints = new List<Joint>();

    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 15f;
    public float staminaRecoveryDelay = 1.0f;

    float staminaDelayTimer;

    // Xuong tay de punch / grab chinh xac hon
    Transform leftHandBone;
    Transform rightHandBone;

    // --- Runtime state ---
    public Animator anim;
    private ActiveRagdollController ragdoll; 
    private CharacterInput _input;

    Dictionary<GameObject, float> lastHitTime = new Dictionary<GameObject, float>();

    float punchTimer;
    Rigidbody grabbedRb;
    float chargeTimer;
    bool isGrabbing;

    void Start()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        ragdoll = GetComponent<ActiveRagdollController>();
        _input = GetComponent<CharacterInput>();

        currentStamina = maxStamina;
        FindHandBones();
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

    void Update()
    {
        // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Cấm hành động
        if (ragdoll != null && ragdoll.isKnockedOut)
        {
            if (isGrabbing) ReleaseGrab();
            return; // Ngất rồi thì không cho làm gì nữa
        }

        punchTimer -= Time.deltaTime;

        // Xử lý Đấm
        if (_input.isPunching && punchTimer <= 0)
        {
            PerformPunch();
            _input.UsePunchRequest();
        }

        // Xử lý Cầm/Ném
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
            staminaDelayTimer = staminaRecoveryDelay; // Reset thời gian chờ

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                ReleaseGrab(); // Hết thể lực tự buông
            }
        }
        else
        {
            // Nếu không cầm, đếm lùi thời gian chờ rồi mới hồi
            if (staminaDelayTimer > 0)
            {
                staminaDelayTimer -= Time.deltaTime;
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }
    }

    void FixedUpdate()
    {
        // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Cấm đánh đấm
        if (ragdoll != null && ragdoll.isKnockedOut) return;

        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("Punch"))
        {
            Transform[] origins = { leftHandBone, rightHandBone };
            foreach (var hand in origins)
            {
                if (hand == null) continue;

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

    public void ReleaseGrab()
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
        var result = new HashSet<Rigidbody>();

        foreach (var h in Physics.OverlapSphere(origin, radius))
        {
            // Bỏ qua chính mình và các xương của chính mình
            if (h.gameObject == gameObject || h.transform.IsChildOf(transform)) continue;

            var hrb = h.attachedRigidbody;

            if (hrb != null) result.Add(hrb); // Bỏ vào túi
        }

        return result; 
    }

    // HÀM HỖ TRỢ 2: Tạo kết nối lò xo nam châm giữa tay và vật
    void AttachHand(Rigidbody handRb, Rigidbody targetRb)
    {
        ConfigurableJoint joint = handRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = targetRb;
        
        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Locked;
        
        joint.linearLimit = new SoftJointLimit { limit = 0.001f };
        joint.linearLimitSpring = new SoftJointLimitSpring { spring = 5000f, damper = 100f };
        
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.01f;
        
        joint.enableCollision = false;
        
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

        // Vẽ vòng vàng cho Grab
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
