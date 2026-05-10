using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Detectors")]
    public CombatDetect combatDetect;

    public PlayerStats stats;

    [Header("Grab+Throw (Chuot trai): Nhan=cam, Giu=charge, Nha=nem")]

    public Rigidbody leftPhysicsHand;
    public Rigidbody rightPhysicsHand;
    private List<Joint> activeGrabJoints = new List<Joint>();


    // Xuong tay de punch / grab chinh xac hon
    Transform leftHandBone;
    Transform rightHandBone;
    private Rigidbody spineAnchorRb; // Xuong spine_3 lam diem neo nguc

    // --- Runtime state ---
    public Animator anim;
    private ActiveRagdollController ragdoll;
    private CharacterInput _input;

    Dictionary<GameObject, float> lastHitTime = new Dictionary<GameObject, float>();

    float punchTimer;
    Rigidbody grabbedRb;
    float chargeTimer;
    bool isGrabbing;
    private ActiveRagdollController grabbedTargetController;

    void Start()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        ragdoll = GetComponent<ActiveRagdollController>();
        _input = GetComponent<CharacterInput>();

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
                // Tu dong tim xuong spine_3 lam diem neo nguc
                if (n.Contains("spine.003"))
                    spineAnchorRb = t.GetComponent<Rigidbody>();
            }
        }
    }

    void Update()
    {
        // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Cấm hành động
        if (stats != null && stats.isKnockedOut)
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
            if (_input.isGrabPressed && stats.currentStamina > 10f)
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
                chargeTimer = Mathf.Clamp(chargeTimer + Time.deltaTime, 0f, stats.maxChargeTime);
            }
            // Nếu nhả nút Grab ra thì thực hiện ném
            else
            {
                PerformThrow();
            }
        }

        // Logic tiêu tốn thể lực khi đang ôm đồ
        if (isGrabbing)
        {
            // TỰ ĐỘNG THẢ NẾU ĐỨT JOINT VẬT LÝ
            bool anyJointAlive = false;
            foreach (var j in activeGrabJoints) { if (j != null) anyJointAlive = true; }

            if (!anyJointAlive || grabbedRb == null)
            {
                ReleaseGrab();
            }
            else if (!stats.UseStamina(stats.grabStaminaDrainRate * Time.deltaTime))
            {
                ReleaseGrab(); // Hết thể lực tự buông
            }
        }
    }

    void FixedUpdate()
    {
        // KHÓA: Nếu đang xỉu hoặc đang gượng dậy -> Cấm đánh đấm
        if (stats != null && stats.isKnockedOut) return;

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
                    hrb.AddForce(punchDir * stats.pushForce, ForceMode.Impulse);
                    var targetController = hrb.GetComponentInParent<ActiveRagdollController>();
                    if (targetController != null) targetController.ApplyDamage(stats.punchForce);
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
        if (stats != null && !stats.UseStamina(stats.punchStaminaCost)) return;

        if (anim != null) anim.SetTrigger("Punch");

        punchTimer = stats.punchCooldown;
        ReleaseGrab();
    }

    public void PerformGrab()
    {
        if (stats != null && stats.currentStamina < stats.grabStaminaCost) return;

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
            // Tìm Controller của nạn nhân để báo hiệu
            grabbedTargetController = commonTarget.GetComponentInParent<ActiveRagdollController>();
            if (grabbedTargetController != null)
            {
                // KHÔNG CHO TÓM NẾU MÌNH ĐANG BỊ TÓM (Chống đệ quy vật lý)
                if (grabbedTargetController == ragdoll || ragdoll.IsBeingGrabbed) return;

                grabbedTargetController.SetGrabbedState(true);
            }

            AttachHand(leftPhysicsHand, commonTarget);
            AttachHand(rightPhysicsHand, commonTarget);
            // Joint thu 3: spine_3 keo doi phuong ap sat vao nguc
            if (spineAnchorRb != null)
                AttachBody(spineAnchorRb, commonTarget);
            isGrabbing = true;
            grabbedRb = commonTarget;
            chargeTimer = 0f;
            if (anim != null) anim.SetBool("IsGrabbing", true);
        }
    }

    public void ReleaseGrab()
    {
        // Báo cho nạn nhân biết mình đã thả
        if (grabbedTargetController != null)
        {
            grabbedTargetController.SetGrabbedState(false);
            grabbedTargetController = null;
        }

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
        joint.linearLimitSpring = new SoftJointLimitSpring { spring = stats.grabSpring, damper = stats.grabDamper };

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

        joint.breakForce = stats.grabBreakForce * 200f;
        joint.breakTorque = stats.grabBreakForce * 200f;
        activeGrabJoints.Add(joint);
    }

    // Ham ho tro 3: Joint nguc - keo doi phuong ap sat vao nguoi (nhe hon tay)
    void AttachBody(Rigidbody bodyRb, Rigidbody targetRb)
    {
        ConfigurableJoint joint = bodyRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = targetRb;

        // Chi gioi han vi tri, de goc xoay tu do (doi phuong van xoay tu nhien)
        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;

        // Limit lon hon tay: cho phep doi phuong lay la mot chut, khong ap chat
        joint.linearLimit = new SoftJointLimit { limit = 0.03f };
        // Spring yeu hon (40%), damper manh hon (200%) de dan vao nguoi khong giat
        joint.linearLimitSpring = new SoftJointLimitSpring
        {
            spring = stats.grabSpring * 0.8f,
            damper = stats.grabDamper * 2f
        };

        joint.enableCollision = false;
        joint.autoConfigureConnectedAnchor = false;

        // Neo vao chinh tam xuong (khong offset - tranh bi day ra do truc xuong sai huong)
        joint.anchor = Vector3.zero;
        joint.connectedAnchor = Vector3.zero;

        joint.breakForce = stats.grabBreakForce * 200f;
        joint.breakTorque = stats.grabBreakForce * 200f;
        activeGrabJoints.Add(joint);
    }

    public bool IsCharging() { return isGrabbing && grabbedRb != null; }
    public float GetChargePct() { return stats != null ? chargeTimer / stats.maxChargeTime : 0; }

    public void PerformThrow()
    {
        if (stats == null) return;
        float pw = chargeTimer / stats.maxChargeTime;
        float frc = Mathf.Lerp(stats.minThrowForce, stats.maxThrowForce, pw);
        Vector3 td = (transform.forward + Vector3.up * 0.15f).normalized;
        var tgt = grabbedRb;

        ReleaseGrab();

        if (tgt != null)
        {
            tgt.AddForce(td * frc, ForceMode.Impulse);
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
