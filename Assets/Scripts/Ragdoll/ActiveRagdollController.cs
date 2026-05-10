using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Bo dieu khien trung tam: Nap day du cac dot xuong va gong cung cot song de dung day.
/// </summary>
public class ActiveRagdollController : MonoBehaviour
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

    [Header("State")]
    public PlayerStats stats;
    private bool isWakingUp = false;

    private ActiveRagdollBone[] bones;
    private ActiveRagdollBalancer balancer;
    private Rigidbody playerRb;
    private Rigidbody hipRb;
    private Transform realHip;
    private CharacterInput playerInput;

    private Dictionary<Rigidbody, float> originalMasses = new Dictionary<Rigidbody, float>();
    private int grabberCount = 0;
    public bool IsBeingGrabbed => grabberCount > 0;
    private float originalMuscleSpring;

    private float lastMuscleSpring, lastMuscleDamper;
    private float lastBalanceSpring, lastBalanceDamper;

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
        StartCoroutine(KnockoutRoutine());
    }

    void OnWakeUpReceived()
    {
        isWakingUp = false;
    }

    [ContextMenu("Re-Initialize Rig")]
    public void InitializeRig()
    {
        playerRb = GetComponent<Rigidbody>();
        if (playerRb == null) return;

        playerRb.constraints = RigidbodyConstraints.FreezeRotation;

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

    void FixedUpdate()
    {
        if (bones == null || bones.Length == 0 || balancer == null || playerRb == null || hipRb == null)
        {
            if (bones == null || bones.Length == 0) InitializeRig();
            return;
        }

        if (stats != null && stats.isKnockedOut)
        {
            UpdateAllMuscleDrives(0, 0);
            balancer.UpdateBalance(0, 0, Quaternion.identity);
            return;
        }

        float currentBalanceSpring = IsBeingGrabbed ? 0 : balanceSpring;
        float currentMuscleSpring = GetTargetMuscleSpring();

        // --- GỒNG CƠ BẮP KHI ĐẤM ---
        if (playerInput != null && playerInput.isPunching && !IsBeingGrabbed)
        {
            currentMuscleSpring *= 5f;
            currentBalanceSpring *= 2f;
        }

        float tiltAngle = Vector3.Angle(realHip.up, Vector3.up);

        if (tiltAngle > 30f)
        {
            // Giữ nguyên hoặc điều chỉnh tùy ý
        }

        // Kiểm tra điều kiện xỉu (Nghiêng quá 65 độ)
        if (stats != null && !stats.isKnockedOut && stats.currentStability > 50f && tiltAngle > 65f && !isWakingUp)
        {
            ApplyDamage(100f);
        }

        // 3. Cap nhat Thang bang
        balancer.UpdateBalance(currentBalanceSpring, balanceDamper, Quaternion.identity);

        // 4. Cap nhat Co bap (Dùng nhãn isSpine tối ưu)
        UpdateAllMuscleDrives(currentMuscleSpring, muscleDamper);

        // 5. Luc day nhac mông (Stand Up)
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

        foreach (var bone in bones)
        {
            if (bone != null) bone.SyncRotation();
        }

        CheckParameterChanges();
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
        if (stats != null && stats.isKnockedOut) return 0f;
        if (IsBeingGrabbed) return 1000f;
        return muscleSpring;
    }

    public void SetGrabbedState(bool state)
    {
        if (state)
        {
            grabberCount++;
            if (grabberCount == 1) // Người đầu tiên tóm
            {
                foreach (var rb in originalMasses.Keys)
                {
                    if (rb != null) rb.mass = 2.0f;
                }
            }
        }
        else
        {
            grabberCount--;
            if (grabberCount <= 0) // Người cuối cùng thả
            {
                grabberCount = 0;
                foreach (var kvp in originalMasses)
                {
                    if (kvp.Key != null) kvp.Key.mass = kvp.Value;
                }
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
        string koStr = (stats != null && stats.isKnockedOut) ? "YES" : "NO";

        Debug.Log(
            $"[GRAB STATUS] {gameObject.name} | " +
            $"Grabbed:{IsBeingGrabbed} | Grabbers:{grabberCount} | " +
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
        if (stats == null || stats.isKnockedOut) return;
        stats.TakeDamage(force);
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
    }
}
