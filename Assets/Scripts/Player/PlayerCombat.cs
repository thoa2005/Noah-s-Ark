using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Detectors")]
    public CombatDetect combatDetect;
    public PlayerStats stats;

    [Header("Grab+Throw (Chuot trai): Nhan=cam, Giu=charge, Nha=nem")]
    public Rigidbody leftPhysicsHand;
    public Rigidbody rightPhysicsHand;

    public Animator anim;

    private readonly List<Joint> activeGrabJoints = new List<Joint>();
    private readonly Dictionary<GameObject, float> lastHitTime = new Dictionary<GameObject, float>();

    private Transform leftHandBone;
    private Transform rightHandBone;
    private Rigidbody spineAnchorRb;
    private ActiveRagdollController ragdoll;
    private CharacterInput _input;

    [Networked] public float punchTimer { get; set; }
    [Networked] public float grabTimer { get; set; }
    [Networked] public float chargeTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnIsGrabbingChanged))]
    public NetworkBool isGrabbing { get; set; }
    [Networked] public NetworkId grabbedObjectId { get; set; }

    // Đồng bộ dáng cầm tới mọi máy khi biến mạng isGrabbing đổi
    private void OnIsGrabbingChanged()
    {
        if (anim != null)
            anim.SetBool("IsGrabbing", isGrabbing);
    }

    private Rigidbody grabbedRb
    {
        get
        {
            if (Runner != null && grabbedObjectId.IsValid && Runner.TryFindObject(grabbedObjectId, out NetworkObject netObj))
                return netObj.GetComponentInChildren<Rigidbody>();
            return null;
        }
    }

    private ActiveRagdollController grabbedTargetController
    {
        get
        {
            if (Runner != null && grabbedObjectId.IsValid && Runner.TryFindObject(grabbedObjectId, out NetworkObject netObj))
                return netObj.GetComponentInChildren<ActiveRagdollController>();
            return null;
        }
    }

    private void Start()
    {
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        ragdoll = GetComponent<ActiveRagdollController>();
        _input = GetComponent<CharacterInput>();

        if (_input != null)
            _input.isGrabPressed = false;

        FindHandBones();
    }

    private void OnEnable()
    {
        lastHitTime.Clear();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return;

        bool hasInput = GetInput(out NetworkInputData inputData);
        float deltaTime = Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime;

        if (stats != null && stats.isKnockedOut)
        {
            if (isGrabbing)
                ReleaseGrab();
            return;
        }

        punchTimer -= deltaTime;
        grabTimer -= deltaTime;

        if (hasInput)
            ProcessInput(inputData, deltaTime);

        UpdateGrabStamina(deltaTime);
        ProcessPunchHits();
    }

    private void ProcessInput(NetworkInputData inputData, float deltaTime)
    {
        if (inputData.isPunching && punchTimer <= 0)
            PerformPunch();

        if (!isGrabbing)
        {
            if (inputData.isGrabPressed && stats.currentStamina > 10f && grabTimer <= 0)
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
            else if (inputData.isGrabPressed)
            {
                chargeTimer = Mathf.Clamp(chargeTimer + deltaTime, 0f, stats.maxChargeTime);
            }
            else
            {
                PerformThrow();
            }
        }
    }

    private void UpdateGrabStamina(float deltaTime)
    {
        if (!isGrabbing)
            return;

        bool anyJointAlive = false;
        foreach (var joint in activeGrabJoints)
        {
            if (joint != null)
                anyJointAlive = true;
        }

        if (!anyJointAlive || grabbedRb == null)
        {
            ReleaseGrab();
        }
        else if (!stats.UseStamina(stats.grabStaminaDrainRate * deltaTime))
        {
            ReleaseGrab();
        }
    }

    private void ProcessPunchHits()
    {
        if (anim == null || !anim.GetCurrentAnimatorStateInfo(0).IsName("Punch"))
            return;
        if (combatDetect == null)
            return;

        Transform[] origins = { leftHandBone, rightHandBone };
        foreach (var hand in origins)
        {
            if (hand == null)
                continue;

            var targets = combatDetect.GetPunchTargets(hand);
            foreach (var hrb in targets)
            {
                if (lastHitTime.TryGetValue(hrb.gameObject, out float lastHit) && Time.time - lastHit < 0.1f)
                    continue;

                Vector3 punchDir = (hrb.transform.position - hand.position).normalized + Vector3.up * 0.2f;
                Vector3 impulse = punchDir * stats.pushForce;

                var targetController = hrb.GetComponentInParent<ActiveRagdollController>();
                var targetNetObj = hrb.GetComponentInParent<NetworkObject>();

                if (targetNetObj != null && targetNetObj != Object && targetController != null)
                    targetController.Rpc_PlayHitReaction(hrb.name, impulse, (int)ForceMode.Impulse);
                else if (targetController != null)
                    targetController.PlayHitReaction(hrb.name, impulse, ForceMode.Impulse);
                else
                    hrb.AddForce(impulse, ForceMode.Impulse);

                if (targetController != null && targetController != ragdoll)
                    targetController.ApplyDamage(stats.punchForce);

                lastHitTime[hrb.gameObject] = Time.time;
            }
        }
    }

    private void FindHandBones()
    {
        if (ragdoll == null)
            ragdoll = GetComponent<ActiveRagdollController>();
        if (ragdoll == null || ragdoll.physicRig == null)
            return;

        Transform[] allBones = ragdoll.physicRig.GetComponentsInChildren<Transform>();
        foreach (var t in allBones)
        {
            string n = t.name.ToLower();
            if (n.Contains("hand.l") || n.Contains("hand_l"))
                leftHandBone = t;
            if (n.Contains("hand.r") || n.Contains("hand_r"))
                rightHandBone = t;
            if (n.Contains("spine.003"))
                spineAnchorRb = t.GetComponent<Rigidbody>();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastPunch()
    {
        if (anim != null)
            anim.SetTrigger("Punch");
        if (ragdoll != null)
            ragdoll.TriggerPunchMuscle();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_SetGrabbedState(NetworkBool state, NetworkId victimId)
    {
        if (Runner != null && Runner.TryFindObject(victimId, out NetworkObject victimNetObj))
        {
            var victimController = victimNetObj.GetComponent<ActiveRagdollController>();
            if (victimController != null)
                victimController.SetGrabbedState(state, gameObject);
        }
    }

    public void PerformPunch()
    {
        if (stats != null && !stats.UseStamina(stats.punchStaminaCost))
            return;

        punchTimer = stats.punchCooldown;

        if (_input != null)
            _input.isGrabPressed = false;

        ReleaseGrab();

        if (Object.HasInputAuthority && anim != null)
            anim.SetTrigger("Punch");

        if (Object.HasStateAuthority)
            Rpc_BroadcastPunch();

        if (CharacterInput.Local != null)
            CharacterInput.Local.UsePunchRequest();
    }

    public void PerformGrab()
    {
        if (stats != null && stats.currentStamina < stats.grabStaminaCost)
            return;
        if (leftPhysicsHand == null || rightPhysicsHand == null || combatDetect == null)
            return;

        var leftSet = combatDetect.GetGrabbableTargets(leftPhysicsHand);
        var rightSet = combatDetect.GetGrabbableTargets(rightPhysicsHand);

        Rigidbody commonTarget = null;
        float bestDist = Mathf.Infinity;
        foreach (var targetRb in leftSet)
        {
            if (!rightSet.Contains(targetRb))
                continue;

            float d = Vector3.Distance(transform.position, targetRb.position);
            if (d < bestDist)
            {
                bestDist = d;
                commonTarget = targetRb;
            }
        }

        if (commonTarget == null)
            return;

        var netObj = commonTarget.GetComponentInParent<NetworkObject>();
        if (netObj != null)
            grabbedObjectId = netObj.Id;

        if (grabbedTargetController != null)
        {
            if (grabbedTargetController == ragdoll || ragdoll.IsBeingGrabbed)
                return;

            Debug.Log($"[PerformGrab] Yêu cầu cầm {grabbedTargetController.gameObject.name}");
            
            // ✅ BƯỚC 1 (Gửi yêu cầu): Chỉ gửi RPC lên Server, KHÔNG tạo Joint ngay
            Rpc_RequestGrab(grabbedTargetController.Object);
            
            // ✅ Set flag để FixedUpdateNetwork biết rằng đang đợi phê duyệt
            isGrabbing = true;
            chargeTimer = 0f;
            if (anim != null)
                anim.SetBool("IsGrabbing", true);
            
            Debug.Log($"[PerformGrab] Đợi Server phê duyệt grab...");
        }
    }

    /// <summary>
    /// BƯỚC 1 (Gửi yêu cầu): Grabber gửi yêu cầu cầm lên Server
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestGrab(NetworkObject victimNetObj)
    {
        Debug.Log($"[Rpc_RequestGrab] Server nhận yêu cầu từ {gameObject.name} → {victimNetObj.gameObject.name}");
        
        // BƯỚC 2 (Máy chủ thực thi): Server kiểm tra và phê duyệt
        var victimController = victimNetObj.GetComponent<ActiveRagdollController>();
        if (victimController != null && !victimController.IsBeingGrabbed)
        {
            // ✅ Server gán GrabberPlayerID cho victim (networked variable)
            victimController.GrabberPlayerID = Object.InputAuthority.PlayerId;
            Debug.Log($"[Rpc_RequestGrab] Server phê duyệt: victim's GrabberPlayerID = {victimController.GrabberPlayerID}");
            
            // ✅ Server gọi callback để tất cả machines biết
            Rpc_ApproveGrab(victimNetObj);
        }
        else
        {
            Debug.Log($"[Rpc_RequestGrab] Server TỪNG CHỐI: victim đang bị cầm hoặc không hợp lệ");
        }
    }
    
    /// <summary>
    /// BƯỚC 3 (Đồng bộ toàn mạng): Server broadcast phê duyệt grab tới tất cả machines
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_ApproveGrab(NetworkObject victimNetObj)
    {
        Debug.Log($"[Rpc_ApproveGrab] Tất cả machines nhận được phê duyệt: {gameObject.name} sẽ cầm {victimNetObj.gameObject.name}");
        
        // ✅ Grabber (chính người gọi) tạo Joint cục bộ
        if (Object.HasInputAuthority)
        {
            var victimRagdoll = victimNetObj.GetComponent<ActiveRagdollController>();
            if (victimRagdoll != null)
            {
                Rigidbody victimRb = victimRagdoll.GetComponentInChildren<Rigidbody>();
                if (victimRb != null)
                {
                    Debug.Log($"[Rpc_ApproveGrab] Grabber tạo Joint");
                    AttachHand(leftPhysicsHand, victimRb);
                    AttachHand(rightPhysicsHand, victimRb);
                    
                    if (spineAnchorRb != null)
                        AttachBody(spineAnchorRb, victimRb);
                }
            }
        }
    }

    public void ReleaseGrab()
    {
        if (grabbedObjectId.IsValid && grabbedTargetController != null)
        {
            Debug.Log($"[ReleaseGrab] Yêu cầu thả {grabbedTargetController.gameObject.name}");
            
            // ✅ Gửi RPC lên Server để release grab
            Rpc_RequestRelease(grabbedTargetController.Object);
        }

        foreach (var joint in activeGrabJoints)
        {
            if (joint != null)
                Destroy(joint);
        }
        activeGrabJoints.Clear();

        if (Object != null && Object.IsValid)
        {
            isGrabbing = false;
            chargeTimer = 0f;
            grabbedObjectId = default;
        }

        if (_input != null)
            _input.isGrabPressed = false;

        if (anim != null)
            anim.SetBool("IsGrabbing", false);
    }
    
    /// <summary>
    /// Yêu cầu thả victim lên Server
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestRelease(NetworkObject victimNetObj)
    {
        Debug.Log($"[Rpc_RequestRelease] Server nhận yêu cầu thả từ {gameObject.name}");
        
        var victimController = victimNetObj.GetComponent<ActiveRagdollController>();
        if (victimController != null)
        {
            // ✅ Server reset GrabberPlayerID
            victimController.GrabberPlayerID = -1;
            Debug.Log($"[Rpc_RequestRelease] Server đã reset GrabberPlayerID = -1");
        }
    }

    private void AttachHand(Rigidbody handRb, Rigidbody targetRb)
    {
        if (stats == null)
        {
            Debug.LogError($"[AttachHand] stats is NULL! Cannot attach {handRb.name}");
            return;
        }
        
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

        joint.breakForce = stats.grabBreakForce * 100f;
        joint.breakTorque = stats.grabBreakForce * 100f;
        
        activeGrabJoints.Add(joint);
    }

    private void AttachBody(Rigidbody bodyRb, Rigidbody targetRb)
    {
        if (stats == null)
        {
            Debug.LogError($"[AttachBody] stats is NULL! Cannot attach {bodyRb.name}");
            return;
        }
        
        ConfigurableJoint joint = bodyRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = targetRb;

        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;

        joint.linearLimit = new SoftJointLimit { limit = 0.02f };
        joint.linearLimitSpring = new SoftJointLimitSpring
        {
            spring = stats.grabSpring * 0.8f,
            damper = stats.grabDamper * 2f
        };

        joint.enableCollision = false;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero;
        joint.connectedAnchor = Vector3.zero;

        joint.breakForce = stats.grabBreakForce * 200f;
        joint.breakTorque = stats.grabBreakForce * 200f;
        
        activeGrabJoints.Add(joint);
    }

    public bool IsCharging()
    {
        return Object != null && Object.IsValid && isGrabbing && grabbedRb != null;
    }

    public float GetChargePct()
    {
        return stats != null ? chargeTimer / stats.maxChargeTime : 0;
    }

    public void ResetCombatState()
    {
        lastHitTime.Clear();
        ReleaseGrab();
        if (Object != null && Object.IsValid)
        {
            punchTimer = 0f;
            grabTimer = 0f;
            chargeTimer = 0f;
        }
    }

    public void PerformThrow()
    {
        if (stats == null)
            return;

        float power = chargeTimer / stats.maxChargeTime;
        float force = Mathf.Lerp(stats.minThrowForce, stats.maxThrowForce, power);
        Vector3 throwDir = (transform.forward + Vector3.up * 0.15f).normalized;
        var targetController = grabbedTargetController;
        var targetRb = grabbedRb;
        bool isNetworkedVictim = grabbedObjectId.IsValid && targetController != null;

        ReleaseGrab();

        if (isNetworkedVictim && targetRb != null)
            targetController.Rpc_PlayHitReaction(targetRb.name, throwDir * force, (int)ForceMode.Impulse);
        else if (targetController != null && targetRb != null)
            targetController.PlayHitReaction(targetRb.name, throwDir * force, ForceMode.Impulse);
        else if (targetRb != null)
            targetRb.AddForce(throwDir * force, ForceMode.Impulse);

        if (_input != null)
            _input.isGrabPressed = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (leftHandBone == null || rightHandBone == null)
            FindHandBones();

        if (combatDetect != null)
            combatDetect.DrawDetectGizmos(leftHandBone, rightHandBone, leftPhysicsHand, rightPhysicsHand);
    }
}
