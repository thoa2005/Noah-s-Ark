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
      private bool isWakingUp = false; // <-- THÊM DÒNG NÀY VÀO
    [Header("Stability Settings")]
    public float stability = 100f;       // Điểm hiện tại
    public float maxStability = 100f;    // Điểm tối đa
    public float recoveryTime = 3f;      // Thời gian nằm xỉu (giây)

    private ActiveRagdollBone[] bones;
    private ActiveRagdollBalancer balancer;
    private Rigidbody playerRb;
    private Rigidbody hipRb;
    private Transform realHip;
    

    private float lastMuscleSpring, lastMuscleDamper;
    private float lastBalanceSpring, lastBalanceDamper;

    void Awake()
    {
        InitializeRig();
    }

    void Start()
    {
        UpdateAllMuscleDrives();
    }

    [ContextMenu("Re-Initialize Rig")]
    public void InitializeRig()
    {
        playerRb = GetComponent<Rigidbody>();
        if (playerRb == null) return;

        playerRb.mass = 100f;
        playerRb.constraints = RigidbodyConstraints.FreezeRotation;

        List<ActiveRagdollBone> boneList = new List<ActiveRagdollBone>();
        
        // 1. Setup Balancer
        balancer = physicRig.GetComponent<ActiveRagdollBalancer>();
        if (balancer == null) balancer = physicRig.gameObject.AddComponent<ActiveRagdollBalancer>();
        balancer.Setup(playerRb);
        hipRb = physicRig.GetComponent<Rigidbody>();
        if (hipRb != null) hipRb.mass = 20f; // Hong phai nang de lam neo
        
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
                
                bone.Setup(aBone, joint, this);
                bone.targetBone = tBone;
                boneList.Add(bone);

                joint.gameObject.layer = 8; 
                var rb = joint.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearDamping = 1.0f;
                    rb.angularDamping = 10.0f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    
                    // Khối lượng các đốt sống nhẹ hơn để dễ nhấc
                    if (joint.name.ToLower().Contains("spine")) rb.mass = 2f;
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
            // Neu bi NULL thi tu dong nap lai mot lan
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
        
        // float tiltAngle = Vector3.Angle(hipRb.transform.forward, Vector3.up);
        float tiltAngle = Vector3.Angle(realHip.up, Vector3.up); 

        if (tiltAngle > 30f)
        {
         
            // Duy trì trạng thái gồng (x3 lực kéo thẳng, x2 độ cứng cơ bắp)
            currentBalanceSpring *= 3f;
            currentMuscleSpring *= 2f; 
            
            // Bạn có thể nhét thêm Debug.Log ở đây nếu muốn kiểm tra
        }
       
        if (!isKnockedOut && stability > 50f && tiltAngle > 45f && !isWakingUp) // Nếu bị nghiêng quá 75 độ mà không gượng dậy được
        {
            ApplyDamage(100f); // Tự gây "sát thương thăng bằng" để xỉu luôn
        }

        // 3. Cap nhat Thang bang
        balancer.UpdateBalance(currentBalanceSpring, balanceDamper, Quaternion.identity);
        
        // 4. Cap nhat Co bap (Gong cot song)
        UpdateAllMuscleDrives(currentMuscleSpring, muscleDamper);

        // 5. Luc day nhac mông (Stand Up)
        float actualTargetY = playerRb.position.y + targetHeight;
        float diff = actualTargetY - hipRb.position.y;
        if (diff > 0)
        {
            float forceMult = (tiltAngle > 45f) ? 2f : 1f;
            hipRb.AddForce(Vector3.up * standUpForce * diff * forceMult, ForceMode.Force);
            hipRb.linearVelocity *= 0.95f; // Giam luc quan tinh
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

            // GONG TOAN BO COT SONG: Tu spine den spine.006
            if (bone.joint.name.ToLower().Contains("spine"))
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

    // THÁO XÍCH HÔNG: Cho phép hông rơi xuống chạm đất (Rất quan trọng)
    var joint = hipRb.GetComponent<ConfigurableJoint>();
    joint.yMotion = ConfigurableJointMotion.Free;

    yield return new WaitForSeconds(recoveryTime);

    // TỈNH DẬY
    
    // BẮT ĐẦU QUÁ TRÌNH TỈNH DẬY
    isWakingUp = true;     // Bật biển báo "Tôi đang thức dậy, đừng quét!"
    isKnockedOut = false;  // Cấp điện lại cho cơ bắp hoạt động
    
    // Kéo người đứng lên
    joint.yMotion = ConfigurableJointMotion.Locked;
    
    // Chờ 2 giây để kéo thẳng lưng lên (< 45 độ)
    yield return new WaitForSeconds(2f);
    
    // ĐỨNG LÊN HOÀN TẤT
    stability = maxStability;  // Hồi đầy 100 máu
    isWakingUp = false;        // Cất biển báo đi, cho máy quét hoạt động bình thường
     
}

}
