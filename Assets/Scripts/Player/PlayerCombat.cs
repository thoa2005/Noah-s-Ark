using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// PlayerCombat — NetworkBehaviour (Fusion).
/// Đọc input từ NetworkInputData thay vì CharacterInput trực tiếp.
/// Punch/grab/throw logic chạy locally (physics không sync từng xương).
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    [Header("Detectors")]
    public CombatDetect combatDetect;
    public PlayerStats  stats;

    [Header("Grab/Throw")]
    public Rigidbody leftPhysicsHand;
    public Rigidbody rightPhysicsHand;

    // ── Runtime ──────────────────────────────────────────────────────────
    public  Animator anim;
    private ActiveRagdollController ragdoll;

    private Transform  leftHandBone;
    private Transform  rightHandBone;
    private Rigidbody  spineAnchorRb;

    private List<Joint>                 activeGrabJoints = new List<Joint>();
    private Dictionary<GameObject, float> lastHitTime    = new Dictionary<GameObject, float>();

    private float   punchTimer;
    private float   grabTimer;
    private float   chargeTimer;
    private bool    isGrabbing;
    private Rigidbody                   grabbedRb;
    private ActiveRagdollController     grabbedTargetController;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public override void Spawned()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        ragdoll = GetComponent<ActiveRagdollController>();
        FindHandBones();
    }

    void OnEnable()
    {
        lastHitTime.Clear();
    }

    // ── Fusion tick ───────────────────────────────────────────────────────

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (stats != null && stats.isKnockedOut)
        {
            if (isGrabbing) ReleaseGrab();
            return;
        }

        if (!GetInput(out NetworkInputData data)) return;

        punchTimer -= Runner.DeltaTime;
        grabTimer  -= Runner.DeltaTime;

        // Punch
        if (data.IsPunching && punchTimer <= 0)
            PerformPunch();

        // Grab / Throw
        if (!isGrabbing)
        {
            if (data.IsGrabbing && stats.currentStamina > 10f && grabTimer <= 0)
            {
                PerformGrab();
                grabTimer = 0.2f;
            }
        }
        else
        {
            if (grabbedRb == null)
            {
                ReleaseGrab();
            }
            else if (data.IsGrabbing)
            {
                chargeTimer = Mathf.Clamp(chargeTimer + Runner.DeltaTime, 0f, stats.maxChargeTime);
            }
            else
            {
                PerformThrow();
            }
        }

        // Stamina drain khi đang giữ
        if (isGrabbing)
        {
            bool anyJointAlive = false;
            foreach (var j in activeGrabJoints) if (j != null) anyJointAlive = true;

            if (!anyJointAlive || grabbedRb == null)
                ReleaseGrab();
            else if (!stats.UseStamina(stats.grabStaminaDrainRate * Runner.DeltaTime))
                ReleaseGrab();
        }
    }

    // Punch hit detection vẫn chạy trong Unity FixedUpdate (physics timing)
    void FixedUpdate()
    {
        if (!HasInputAuthority) return;
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
                    if (lastHitTime.TryGetValue(hrb.gameObject, out float t) && Time.time - t < 0.1f)
                        continue;

                    Vector3 dir = (hrb.transform.position - hand.position).normalized + Vector3.up * 0.2f;
                    hrb.AddForce(dir * stats.pushForce, ForceMode.Impulse);

                    var target = hrb.GetComponentInParent<ActiveRagdollController>();
                    if (target != null && target != ragdoll)
                        target.ApplyDamage(stats.punchForce);

                    lastHitTime[hrb.gameObject] = Time.time;
                }
            }
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────

    public void PerformPunch()
    {
        if (stats != null && !stats.UseStamina(stats.punchStaminaCost)) return;
        if (anim != null) anim.SetTrigger("Punch");
        punchTimer = stats.punchCooldown;
        ReleaseGrab();
    }

    public void PerformGrab()
    {
        if (leftPhysicsHand == null || rightPhysicsHand == null || combatDetect == null) return;

        var leftSet  = combatDetect.GetGrabbableTargets(leftPhysicsHand);
        var rightSet = combatDetect.GetGrabbableTargets(rightPhysicsHand);

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

        if (commonTarget != null)
        {
            grabbedTargetController = commonTarget.GetComponentInParent<ActiveRagdollController>();
            if (grabbedTargetController != null)
            {
                if (grabbedTargetController == ragdoll || ragdoll.IsBeingGrabbed) return;
                grabbedTargetController.SetGrabbedState(true, gameObject);
            }

            AttachHand(leftPhysicsHand,  commonTarget);
            AttachHand(rightPhysicsHand, commonTarget);
            if (spineAnchorRb != null) AttachBody(spineAnchorRb, commonTarget);

            isGrabbing  = true;
            grabbedRb   = commonTarget;
            chargeTimer = 0f;
            if (anim != null) anim.SetBool("IsGrabbing", true);
        }
    }

    public void ReleaseGrab()
    {
        if (grabbedTargetController != null && grabbedTargetController.gameObject != null)
        {
            grabbedTargetController.SetGrabbedState(false, gameObject);
            grabbedTargetController = null;
        }

        foreach (var j in activeGrabJoints) if (j != null) Destroy(j);
        activeGrabJoints.Clear();

        isGrabbing  = false;
        chargeTimer = 0f;
        grabbedRb   = null;
        if (anim != null) anim.SetBool("IsGrabbing", false);
    }

    public void PerformThrow()
    {
        if (stats == null) return;
        float pw  = chargeTimer / stats.maxChargeTime;
        float frc = Mathf.Lerp(stats.minThrowForce, stats.maxThrowForce, pw);
        Vector3 td  = (transform.forward + Vector3.up * 0.15f).normalized;
        var tgt     = grabbedRb;
        ReleaseGrab();
        if (tgt != null) tgt.AddForce(td * frc, ForceMode.Impulse);
    }

    public void ResetCombatState()
    {
        lastHitTime.Clear();
        ReleaseGrab();
        punchTimer  = 0f;
        grabTimer   = 0f;
        chargeTimer = 0f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void FindHandBones()
    {
        if (ragdoll == null) ragdoll = GetComponent<ActiveRagdollController>();
        if (ragdoll?.physicRig == null) return;

        foreach (var t in ragdoll.physicRig.GetComponentsInChildren<Transform>())
        {
            string n = t.name.ToLower();
            if (n.Contains("hand.l") || n.Contains("hand_l")) leftHandBone  = t;
            if (n.Contains("hand.r") || n.Contains("hand_r")) rightHandBone = t;
            if (n.Contains("spine.003")) spineAnchorRb = t.GetComponent<Rigidbody>();
        }
    }

    void AttachHand(Rigidbody handRb, Rigidbody targetRb)
    {
        var joint = handRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody  = targetRb;
        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.linearLimit       = new SoftJointLimit { limit = 0.001f };
        joint.linearLimitSpring = new SoftJointLimitSpring { spring = stats.grabSpring, damper = stats.grabDamper };
        joint.projectionMode     = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.01f;
        joint.enableCollision    = false;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero;
        Collider col = targetRb.GetComponentInChildren<Collider>();
        joint.connectedAnchor = col != null
            ? targetRb.transform.InverseTransformPoint(col.ClosestPoint(handRb.position))
            : Vector3.zero;
        joint.breakForce  = stats.grabBreakForce * 100f;
        joint.breakTorque = stats.grabBreakForce * 100f;
        activeGrabJoints.Add(joint);
    }

    void AttachBody(Rigidbody bodyRb, Rigidbody targetRb)
    {
        var joint = bodyRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody  = targetRb;
        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
        joint.linearLimit       = new SoftJointLimit { limit = 0.02f };
        joint.linearLimitSpring = new SoftJointLimitSpring
        {
            spring = stats.grabSpring * 0.8f,
            damper = stats.grabDamper * 2f
        };
        joint.enableCollision = false;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor          = Vector3.zero;
        joint.connectedAnchor = Vector3.zero;
        joint.breakForce  = stats.grabBreakForce * 200f;
        joint.breakTorque = stats.grabBreakForce * 200f;
        activeGrabJoints.Add(joint);
    }

    public bool  IsCharging()    => isGrabbing && grabbedRb != null;
    public float GetChargePct() => stats != null ? chargeTimer / stats.maxChargeTime : 0f;
    public bool  IsPunching     => punchTimer > 0f;

    void OnDrawGizmosSelected()
    {
        if (leftHandBone == null || rightHandBone == null) FindHandBones();
        if (combatDetect != null)
            combatDetect.DrawDetectGizmos(leftHandBone, rightHandBone, leftPhysicsHand, rightPhysicsHand);
    }
}
