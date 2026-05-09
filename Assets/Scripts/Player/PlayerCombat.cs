using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Detectors")]
    public CombatDetect combatDetect;

    [Header("Punch (Chuot phai)")]
    public float punchForce = 15f;
    public float pushForce = 50f; // Lực đẩy vật lý (Chỉnh số này to để đối thủ bay xa)
    public float punchCooldown = 0.5f;

    [Header("Grab+Throw (Chuot trai): Nhan=cam, Giu=charge, Nha=nem")]
    public float minThrowForce = 5f;
    public float maxThrowForce = 20f;
    public float maxChargeTime = 1.0f;
    [Tooltip("Luc pha vo FixedJoint khi bi va cham manh")]
    public float grabBreakForce = 800f;

    [Header("Party Animals Grab Physics")]
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
            if (combatDetect == null) return;
            Transform[] origins = { leftHandBone, rightHandBone };
            foreach (var hand in origins)
            {
                if (hand == null) continue;

                var targets = combatDetect.GetPunchTargets(hand);
                foreach (var hrb in targets)
                {
                    // KIỂM TRA: Nếu vừa đấm người này cách đây chưa đầy 0.1s thì bỏ qua
                    if (lastHitTime.ContainsKey(hrb.gameObject))
                    {
                        if (Time.time - lastHitTime[hrb.gameObject] < 0.1f) continue;
                    }
                    
                    // Thực hiện đẩy và gây sát thương
                    Vector3 punchDir = (hrb.transform.position - hand.position).normalized + Vector3.up * 0.2f;
                    hrb.AddForce(punchDir * pushForce, ForceMode.Impulse);
                    var targetController = hrb.GetComponentInParent<ActiveRagdollController>();
                    if (targetController != null) targetController.ApplyDamage(punchForce);
                    // Ghi nhớ thời gian vừa đấm trúng người này
                    lastHitTime[hrb.gameObject] = Time.time;
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
        
        if (leftPhysicsHand == null || rightPhysicsHand == null || combatDetect == null) return;
        
        // Lấy TẤT CẢ Rb trong từng vùng quét
        var leftSet = combatDetect.GetGrabbableTargets(leftPhysicsHand);
        var rightSet = combatDetect.GetGrabbableTargets(rightPhysicsHand);

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

        if (combatDetect != null)
        {
            combatDetect.DrawDetectGizmos(leftHandBone, rightHandBone, leftPhysicsHand, rightPhysicsHand);
        }
    }
}
