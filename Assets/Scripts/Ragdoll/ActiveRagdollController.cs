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
    public float muscleSpring = 15000f;
    public float muscleDamper = 1000f;
    public float spineMuscleMultiplier = 5f; // Luc cho toan bo cot song khi dung day

    [Header("Balance Physics")]
    public float balanceSpring = 60000f;
    public float balanceDamper = 1500f;
    public float standUpForce = 150f; // Giam xuong de khong bi bay len troi
    public float targetHeight = 0.9f;

    [Header("State")]
    public bool isKnockedOut = false;
    private bool isWakingUp = false;
    [Header("Stability Settings")]
    public float stability = 100f;       // Điểm hiện tại
    public float maxStability = 100f;    // Điểm tối đa
    public float recoveryTime = 3f;      // Thời gian nằm xỉu (giây)

    private ActiveRagdollBone[] bones;
    private ActiveRagdollBalancer balancer;
    private Rigidbody playerRb;
    private Rigidbody hipRb;
    private Transform realHip;
    private CharacterInput playerInput; // <--- INPUT SYSTEM MỚI

    private float lastMuscleSpring, lastMuscleDamper;
    private float lastBalanceSpring, lastBalanceDamper;

    void Awake()
    {
        InitializeRig();
    }

    void Start()
    {
        playerInput = GetComponent<CharacterInput>(); // <--- CACHE INPUT
        UpdateAllMuscleDrives();
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
        Debug.Log($"[ActiveRagdoll] Da nap thanh cong {bones.Length} xuong vao bo nao.");
    }

    void FixedUpdate()
    {
        if (bones == null || bones.Length == 0 || balancer == null || playerRb == null || hipRb == null)
        {
            if (bones == null || bones.Length == 0) InitializeRig();
            return;
        }

        if (isKnockedOut)
        {
            UpdateAllMuscleDrives(0, 0);
            balancer.UpdateBalance(0, 0, Quaternion.identity);
            return;
        }

        float currentBalanceSpring = balanceSpring;
        float currentMuscleSpring = muscleSpring;

        // --- GỒNG CƠ BẮP KHI ĐẤM ---
        if (playerInput != null && playerInput.isPunching)
        {
            currentMuscleSpring *= 5f;
            currentBalanceSpring *= 2f;
        }

        float tiltAngle = Vector3.Angle(realHip.up, Vector3.up);

        if (tiltAngle > 30f)
        {
            currentBalanceSpring *= 1f;
            currentMuscleSpring *= 1f;
        }

        // Kiểm tra điều kiện xỉu
        if (!isKnockedOut && stability > 50f && tiltAngle > 65f && !isWakingUp)
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
        UpdateAllMuscleDrives(muscleSpring, muscleDamper);
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
        if (isKnockedOut) return;

        stability -= force;
        if (stability <= 0)
        {
            StartCoroutine(KnockoutRoutine());
        }
    }

    private System.Collections.IEnumerator KnockoutRoutine()
    {
        isKnockedOut = true;
        stability = 0;

        var joint = hipRb.GetComponent<ConfigurableJoint>();
        if (joint != null) joint.yMotion = ConfigurableJointMotion.Free;

        yield return new WaitForSeconds(recoveryTime);

        isWakingUp = true;
        isKnockedOut = false;

        if (joint != null) joint.yMotion = ConfigurableJointMotion.Locked;

        yield return new WaitForSeconds(2f);

        stability = maxStability;
        isWakingUp = false;
    }
}
