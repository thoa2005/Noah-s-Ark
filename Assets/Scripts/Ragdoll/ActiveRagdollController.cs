using UnityEngine;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;

/// <summary>
/// Bo dieu khien trung tam: Nap day du cac dot xuong va gong cung cot song de dung day.
/// </summary>
public class ActiveRagdollController : NetworkBehaviour
{
    [Header("Connected Rigs")]
    public Transform animationRig;
    public Transform targetRig;
    public Transform physicRig;

    [Header("Muscle Settings")]
    public float muscleSpring;
    public float muscleDamper;
    public float spineMuscleMultiplier = 5f; // Luc cho toan bo cot song khi dung day

    [Header("Balance Physics")]
    public float balanceSpring;
    public float balanceDamper;
    public float standUpForce = 150f; // Giam xuong de khong bi bay len troi
    public float targetHeight = 0.9f;
    public float boneLerpSpeed = 15f; // Tốc độ mượt của xương ảo

    // --- BIẾN ĐỒNG BỘ MẠNG (NETWORKED) ---
    [Networked] public NetworkBool IsPunching { get; set; }
    [Networked] public Vector2 MoveInput { get; set; }
    // -------------------------------------

    [Header("Leaning (Nghiêng người)")]
    public float leanAmount = 25f;    // Độ nghiêng tối đa
    public float leanSpeed = 5f;     // Tốc độ nghiêng/hồi phục
    private Quaternion currentLeanOffset = Quaternion.identity;

    [Header("State")]
    public PlayerStats stats;
    private bool isWakingUp = false;

    private ActiveRagdollBone[] bones;
    private ActiveRagdollBalancer balancer;
    private Rigidbody playerRb;
    private Rigidbody hipRb;
    public Transform realHip;
    private CharacterInput playerInput;

    private Dictionary<Rigidbody, float> originalMasses = new Dictionary<Rigidbody, float>();
    private List<GameObject> grabbers = new List<GameObject>();
    public bool IsBeingGrabbed
    {
        get
        {
            grabbers.RemoveAll(g => g == null);
            return grabbers.Count > 0;
        }
    }
    private float originalMuscleSpring;

    private float lastMuscleSpring, lastMuscleDamper;
    private float lastBalanceSpring, lastBalanceDamper;

    private float punchMuscleTimer = 0f;
    private float hitStaggerTimer = 0f;

    [Header("Hit Reaction")]
    public float hitStaggerDuration = 0.35f;
    [Range(0.05f, 1f)]
    public float hitStaggerMuscleMult = 0.2f;

    private bool StatsReady => stats != null && stats.NetworkReady;

    // Lưu reference coroutine để có thể cancel khi màn kết thúc giữa chừng
    private Coroutine knockoutCoroutine;

    void Awake()
    {
        InitializeRig();
    }

    void Start()
    {
        playerInput = GetComponent<CharacterInput>(); // <--- CACHE INPUT
        if (stats == null) stats = GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.OnKnockout += OnKnockoutReceived;
            stats.OnWakeUp += OnWakeUpReceived;
        }

        originalMuscleSpring = muscleSpring;
        UpdateAllMuscleDrives();
    }

    void OnDestroy()
    {
        // Hủy đăng ký khi object bị xóa để tránh lỗi bộ nhớ
        if (stats != null)
        {
            stats.OnKnockout -= OnKnockoutReceived;
            stats.OnWakeUp -= OnWakeUpReceived;
        }
    }

    void OnKnockoutReceived()
    {
        // Cancel coroutine cũ nếu đang chạy (tránh chạy 2 lần song song)
        if (knockoutCoroutine != null) StopCoroutine(knockoutCoroutine);
        knockoutCoroutine = StartCoroutine(KnockoutRoutine());
    }

    /// <summary>
    /// Hủy KnockoutRoutine và reset trạng thái về bình thường.
    /// Gọi bởi GameManager khi màn kết thúc hoặc hồi sinh đầu màn mới.
    /// </summary>
    public void CancelKnockout()
    {
        if (knockoutCoroutine != null)
        {
            StopCoroutine(knockoutCoroutine);
            knockoutCoroutine = null;
        }
        isWakingUp = false;

        // Restore joint yMotion về Locked nếu đang bị Free
        if (hipRb != null)
        {
            var joint = hipRb.GetComponent<ConfigurableJoint>();
            if (joint != null) joint.yMotion = ConfigurableJointMotion.Locked;
        }
    }

    void OnWakeUpReceived()
    {
        isWakingUp = false;
    }

    public override void Spawned()
    {
        // Khi nhân vật vừa được sinh ra trên mạng (bao gồm cả late joiner)
        if (bones == null || bones.Length == 0) InitializeRig();

        // BẮT BUỘC FUSION CHẠY FixedUpdateNetwork TRÊN TẤT CẢ PROXIES
        Runner.SetIsSimulated(Object, true);

        // ĐÁNH THỨC ANIMATOR ĐỂ CẬP NHẬT TỌA ĐỘ NGAY LẬP TỨC CHO XƯƠNG MỤC TIÊU
        if (animationRig != null)
        {
            var anim = animationRig.GetComponent<Animator>();
            if (anim != null) anim.Update(0f);
        }

        // Dừng toàn bộ gia tốc cũ và dịch chuyển xương để chống giật "Physics Snap"
        ResetRagdollPhysics();
    }

    private void ResetRagdollPhysics()
    {
        if (bones == null) return;

        // 1. Xóa bộ nhớ lực kéo của Unity PhysX bằng isKinematic
        foreach (var bone in bones)
        {
            if (bone != null)
            {
                Rigidbody rb = bone.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }
        }
        if (hipRb != null) hipRb.isKinematic = true;

        // 2. Dịch chuyển tức thời khớp xương về đúng vị trí chuẩn
        foreach (var bone in bones)
        {
            if (bone != null)
            {
                Rigidbody rb = bone.GetComponent<Rigidbody>();
                if (rb != null && bone.targetBone != null)
                {
                    rb.position = bone.targetBone.position;
                    rb.rotation = bone.targetBone.rotation;
                }
            }
        }
        if (hipRb != null)
        {
            // Hiprb neo theo root
            hipRb.position = transform.position + Vector3.up * targetHeight;
        }

        // 3. Khôi phục lại Vật lý và xóa động năng
        foreach (var bone in bones)
        {
            if (bone != null)
            {
                Rigidbody rb = bone.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
        if (hipRb != null)
        {
            hipRb.isKinematic = false;
            hipRb.linearVelocity = Vector3.zero;
            hipRb.angularVelocity = Vector3.zero;
        }
    }

    [ContextMenu("Re-Initialize Rig")]
    public void InitializeRig()
    {
        playerRb = GetComponent<Rigidbody>();
        if (playerRb == null) return;

        playerRb.constraints = RigidbodyConstraints.FreezeRotation;

        IgnoreSelfCollisions(); // Bỏ qua va chạm giữa các xương của CHÍNH MÌNH

        List<ActiveRagdollBone> boneList = new List<ActiveRagdollBone>();

        // 1. Setup Balancer
        balancer = physicRig.GetComponent<ActiveRagdollBalancer>();
        if (balancer == null) balancer = physicRig.gameObject.AddComponent<ActiveRagdollBalancer>();
        balancer.Setup(playerRb);
        hipRb = physicRig.GetComponent<Rigidbody>();

        realHip = physicRig.GetChild(0); // Lấy xương spine

        // 2. Nap TAT CA cac xuong co Rigidbody vao danh sach dieu khien
        ConfigurableJoint[] joints = physicRig.GetComponentsInChildren<ConfigurableJoint>();
        foreach (var joint in joints)
        {
            if (joint == null) continue;

            Transform aBone = FindRecursive(animationRig, joint.name);
            Transform tBone = FindRecursive(targetRig, joint.name);
            if (aBone != null && tBone != null)
            {
                ActiveRagdollBone bone = joint.gameObject.GetComponent<ActiveRagdollBone>();
                if (bone == null) bone = joint.gameObject.AddComponent<ActiveRagdollBone>();

                // Dán nhãn xương sống 1 lần duy nhất để tối ưu hiệu năng
                bone.isSpine = joint.name.ToLower().Contains("spine");

                bone.Setup(aBone, joint, this);
                bone.targetBone = tBone;
                boneList.Add(bone);

                joint.gameObject.layer = gameObject.layer;
                var rb = joint.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearDamping = 1.0f;
                    rb.angularDamping = 10.0f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                }
            }
        }

        bones = boneList.ToArray();

        // CHỈ LƯU MASS GỐC KHI KHÔNG BỊ TÓM (Để tránh lưu nhầm Mass = 1 là gốc)
        if (!IsBeingGrabbed)
        {
            originalMasses.Clear();
            // 1. Lưu Mass của Rigidbody gốc (Quan trọng để nhấc bổng cả người)
            if (playerRb != null && !originalMasses.ContainsKey(playerRb))
                originalMasses.Add(playerRb, playerRb.mass);

            // 2. Lưu Mass của toàn bộ xương
            Rigidbody[] allRbs = physicRig.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in allRbs)
            {
                if (!originalMasses.ContainsKey(rb))
                    originalMasses.Add(rb, rb.mass);
            }
        }

        Debug.Log($"[ActiveRagdoll] Da nap thanh cong {bones.Length} xuong va {originalMasses.Count} Rigidbody vao danh sach can nang.");
    }

    private void IgnoreSelfCollisions()
    {
        // Lấy toàn bộ Collider trên nhân vật (bao gồm Capsule gốc và các khúc xương)
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            for (int j = i + 1; j < colliders.Length; j++)
            {
                // Ép Unity bỏ qua va chạm giữa chúng
                Physics.IgnoreCollision(colliders[i], colliders[j], true);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // CHỈ STATE AUTHORITY MỚI ĐƯỢC PHÉP CẬP NHẬT BIẾN MẠNG (FUSION YÊU CẦU LÀM Ở ĐÂY)
        if (Object != null && Object.IsValid && Object.HasStateAuthority && GetInput(out NetworkInputData inputData))
        {
            IsPunching = inputData.isPunching;
            MoveInput = inputData.moveInput;
        }
    }

    public void TriggerPunchMuscle()
    {
        punchMuscleTimer = 0.5f; // Gồng cơ bắp trong 0.5s kể từ khi vung đấm
    }

    void FixedUpdate()
    {
        if (bones == null || bones.Length == 0 || balancer == null || playerRb == null || hipRb == null)
        {
            if (bones == null || bones.Length == 0) InitializeRig();
            return;
        }

        if (StatsReady && stats.isKnockedOut)
        {
            UpdateAllMuscleDrives(0, 0);
            balancer.UpdateBalance(0, 0, Quaternion.identity);
            return;
        }

        float currentBalanceSpring = IsBeingGrabbed ? 0 : balanceSpring;
        float currentMuscleSpring = GetTargetMuscleSpring();

        if (punchMuscleTimer > 0f) punchMuscleTimer -= Time.fixedDeltaTime;

        bool isHitStaggered = hitStaggerTimer > 0f;
        if (isHitStaggered)
        {
            hitStaggerTimer -= Time.fixedDeltaTime;
            currentMuscleSpring *= hitStaggerMuscleMult;
            currentBalanceSpring = 0f;
        }

        // DÙNG BIẾN ĐÃ ĐỒNG BỘ HOẶC TIMER ĐỂ ÁP DỤNG LỰC CHO TẤT CẢ MỌI MÁY (KỂ CẢ PROXY)
        bool isCurrentlyPunching = punchMuscleTimer > 0f || IsPunching;
        if (isCurrentlyPunching && !IsBeingGrabbed)
        {
            currentMuscleSpring *= 10f;
            currentBalanceSpring *= 0f;
        }

        // CẢ HOST VÀ PROXY ĐỀU CẦN UPDATE CƠ BẮP ĐỂ TẠO DÁNG THEO ANIMATOR
        UpdateAllMuscleDrives(currentMuscleSpring, muscleDamper);

        // Tính toán nghiêng người dựa trên Input đã đồng bộ (tắt khi vừa bị đấm)
        HandleProceduralLeaning(isHitStaggered ? Vector2.zero : MoveInput);

        // 3. Cap nhat Thang bang
        balancer.UpdateBalance(currentBalanceSpring, balanceDamper, Quaternion.identity);

        foreach (var bone in bones)
        {
            if (bone != null)
            {
                bone.lerpSpeed = boneLerpSpeed; // Đồng bộ tốc độ mượt

                // Chỉ nghiêng các xương thuộc cột sống (Spine) để nhìn tự nhiên nhất
                if (bone.isSpine) bone.externalOffset = currentLeanOffset;
                else bone.externalOffset = Quaternion.identity;

                bone.SyncRotation(Time.fixedDeltaTime);
            }
        }

        CheckParameterChanges();

        // TỪ ĐÂY TRỞ XUỐNG CHỈ CÓ STATE AUTHORITY MỚI ĐƯỢC CHẠY (Thăng bằng và Force)
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority) return;

        float tiltAngle = Vector3.Angle(realHip.up, Vector3.up);
        // Kiểm tra điều kiện xỉu (Nghiêng quá 65 độ)
        if (StatsReady && !stats.isKnockedOut && stats.currentStability > 50f && tiltAngle > 65f && !isWakingUp)
        {
            ApplyDamage(100f);
        }



        // 6. Luc day nhac mông (Stand Up) — tắt trong stagger để không triệt tiêu lực đấm
        if (!isHitStaggered)
        {
            float actualTargetY = playerRb.position.y + targetHeight;
            float diff = actualTargetY - hipRb.position.y;
            if (diff > 0)
            {
                float forceMult = (tiltAngle > 45f) ? 2f : 1f;
                hipRb.AddForce(Vector3.up * standUpForce * diff * forceMult, ForceMode.Force);

                Vector3 currentVel = hipRb.linearVelocity;
                currentVel.y *= 0.95f;
                hipRb.linearVelocity = currentVel;
            }
        }
    }

    private void HandleProceduralLeaning(Vector2 moveIn)
    {
        // Lấy hướng di chuyển từ Input đã đồng bộ
        Vector3 moveInput = new Vector3(moveIn.x, 0, moveIn.y);

        Quaternion targetLean = Quaternion.identity;

        if (moveInput.magnitude > 0.1f)
        {
            // Tính toán trục nghiêng (xoay vuông góc với hướng di chuyển)
            Vector3 leanAxis = Vector3.Cross(Vector3.up, moveInput).normalized;
            float angle = leanAmount * moveInput.magnitude;
            targetLean = Quaternion.AngleAxis(angle, leanAxis);
        }

        // Làm mượt quá trình nghiêng và hồi phục
        currentLeanOffset = Quaternion.Slerp(currentLeanOffset, targetLean, Time.fixedDeltaTime * leanSpeed);
    }

    private void UpdateAllMuscleDrives(float spring, float damper)
    {
        if (bones == null) return;
        foreach (var bone in bones)
        {
            if (bone == null || bone.joint == null) continue;

            float s = spring;
            float d = damper;

            // KIỂM TRA NHÃN SIÊU TỐC
            if (bone.isSpine)
            {
                s *= spineMuscleMultiplier;
                d *= 2f;
            }

            bone.UpdateJointDrive(s, d);
        }
    }

    private void CheckParameterChanges()
    {
        if (muscleSpring != lastMuscleSpring || muscleDamper != lastMuscleDamper)
        {
            UpdateAllMuscleDrives();
            lastMuscleSpring = muscleSpring;
            lastMuscleDamper = muscleDamper;
        }

        if (balanceSpring != lastBalanceSpring || balanceDamper != lastBalanceDamper)
        {
            balancer.UpdateBalance(balanceSpring, balanceDamper, Quaternion.identity);
            lastBalanceSpring = balanceSpring;
            lastBalanceDamper = balanceDamper;
        }
    }

    public void UpdateAllMuscleDrives()
    {
        UpdateAllMuscleDrives(GetTargetMuscleSpring(), muscleDamper);
    }

    public float GetTargetMuscleSpring()
    {
        if (StatsReady && stats.isKnockedOut) return 0f;
        if (IsBeingGrabbed) return 1000f;
        return muscleSpring;
    }

    public void SetGrabbedState(bool state, GameObject grabber)
    {
        bool wasGrabbed = IsBeingGrabbed;

        if (state)
        {
            if (grabber != null && !grabbers.Contains(grabber))
                grabbers.Add(grabber);
        }
        else
        {
            if (grabber != null)
                grabbers.Remove(grabber);
        }

        // Dọn dẹp references null (nếu có ai đó bị xóa ngang)
        grabbers.RemoveAll(g => g == null);
        bool isGrabbedNow = grabbers.Count > 0;

        if (!wasGrabbed && isGrabbedNow) // Người đầu tiên tóm
        {
            foreach (var rb in originalMasses.Keys)
            {
                if (rb != null) rb.mass = 1.5f;
            }
        }
        else if (wasGrabbed && !isGrabbedNow) // Người cuối cùng thả
        {
            foreach (var kvp in originalMasses)
            {
                if (kvp.Key != null) kvp.Key.mass = kvp.Value;
            }
        }

        UpdateAllMuscleDrives();
        DebugGrabStatus(); // 1 log bao quát duy nhất
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DebugGrabStatus()
    {
        float curMuscle = GetTargetMuscleSpring();
        float curMass = playerRb != null ? playerRb.mass : 0f;
        string speedStr = IsBeingGrabbed ? "x0.1" : "Normal";
        string jumpStr = IsBeingGrabbed ? "NO" : "YES";
        string grabStr = IsBeingGrabbed ? "NO" : "YES";
        string koStr = (StatsReady && stats.isKnockedOut) ? "YES" : "NO";

        Debug.Log(
            $"[GRAB STATUS] {gameObject.name} | " +
            $"Grabbed:{IsBeingGrabbed} | Grabbers:{grabbers.Count} | " +
            $"Muscle:{curMuscle} | Mass:{curMass:F1} | " +
            $"Speed:{speedStr} | CanJump:{jumpStr} | CanGrab:{grabStr} | KO:{koStr}"
        );
    }

    private void LateUpdate()
    {
        if (bones == null) return;
        foreach (var bone in bones)
        {
            if (bone != null && bone.animBone != null && bone.joint != null)
            {
                bone.animBone.position = bone.joint.transform.position;
                bone.animBone.rotation = bone.joint.transform.rotation;
            }
        }

        if (animationRig != null && physicRig != null)
        {
            animationRig.position = physicRig.position;
            animationRig.rotation = physicRig.rotation;
        }
    }

    private Transform FindRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void ApplyDamage(float force)
    {
        if (stats == null || !StatsReady || stats.isKnockedOut) return;
        stats.Rpc_TakeDamage(force);
    }

    public void PlayHitReaction(string boneName, Vector3 impulse, ForceMode forceMode)
    {
        Rigidbody rb = FindBoneRigidbody(boneName);
        if (rb != null)
            rb.AddForce(impulse, forceMode);

        hitStaggerTimer = Mathf.Max(hitStaggerTimer, hitStaggerDuration);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_PlayHitReaction(NetworkString<_64> boneName, Vector3 impulse, int forceMode)
    {
        PlayHitReaction(boneName.ToString(), impulse, (ForceMode)forceMode);
    }

    private Rigidbody FindBoneRigidbody(string boneName)
    {
        if (physicRig == null || string.IsNullOrEmpty(boneName))
            return null;

        Transform bone = FindRecursive(physicRig, boneName);
        return bone != null ? bone.GetComponent<Rigidbody>() : null;
    }

    /// <summary>
    /// Teleport toàn bộ ragdoll (capsule + physicRig + tất cả xương) đến vị trí mới.
    /// Phải gọi cái này thay vì chỉ set transform.position khi respawn.
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (playerRb == null) return;

        // 1. Tắt physics tạm thời để tránh jitter khi teleport
        Physics.SyncTransforms();

        // 2. Tính offset từ vị trí cũ sang vị trí mới
        Vector3 offset = position - playerRb.position;

        // 3. Teleport capsule gốc
        playerRb.position = position;
        playerRb.rotation = rotation;
        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        transform.position = position;
        transform.rotation = rotation;

        // 4. Teleport physicRig và tất cả xương theo offset
        if (physicRig != null)
        {
            foreach (var rb in physicRig.GetComponentsInChildren<Rigidbody>())
            {
                rb.position += offset;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // 5. Sync lại để Unity biết vị trí mới
        Physics.SyncTransforms();
    }

    private System.Collections.IEnumerator KnockoutRoutine()
    {
        var joint = hipRb.GetComponent<ConfigurableJoint>();
        if (joint != null) joint.yMotion = ConfigurableJointMotion.Free;

        yield return new WaitForSeconds(stats.recoveryTime);

        isWakingUp = true;

        if (joint != null) joint.yMotion = ConfigurableJointMotion.Locked;

        yield return new WaitForSeconds(2f);

        stats.ResetAfterWakeUp();
        knockoutCoroutine = null; // Xóa reference khi hoàn thành tự nhiên
    }
}
