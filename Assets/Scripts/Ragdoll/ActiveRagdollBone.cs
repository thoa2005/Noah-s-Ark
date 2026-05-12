using UnityEngine;

/// <summary>
/// Quan ly mot xuong trong Ragdoll, dong bo giua animation va physics.
/// </summary>
public class ActiveRagdollBone : MonoBehaviour
{
    // Thêm dòng này vào phần khai báo biến ở đầu class
    private ActiveRagdollController controller;

    public Transform animBone;
    public ConfigurableJoint joint;
    public Transform targetBone;
    public bool isSpine; // <--- NHÃN NHẬN DIỆN XƯƠNG SỐNG

    private Quaternion initialLocalRotation;
    private Quaternion currentTargetRotation;
    public float lerpSpeed = 15f; // Tốc độ mượt (càng thấp càng dẻo/trễ)
    [HideInInspector] public Quaternion externalOffset = Quaternion.identity; // Lực nghiêng từ Controller

    public void Setup(Transform animBone, ConfigurableJoint joint, ActiveRagdollController controller)
    {
        this.animBone = animBone;
        this.joint = joint;
        this.controller = controller; 

        initialLocalRotation = transform.localRotation;
        currentTargetRotation = targetBone != null ? targetBone.localRotation : initialLocalRotation;
        externalOffset = Quaternion.identity;
    }

    public void SyncRotation()
    {
        if (targetBone == null || joint == null) return;

        // Kết hợp Rotation của Animation với lực nghiêng bên ngoài (Offset)
        Quaternion finalTarget = targetBone.localRotation * externalOffset;

        // Làm mượt đích đến: cho phép xương ảo "trôi" theo Animation thay vì đứng khựng
        currentTargetRotation = Quaternion.Slerp(currentTargetRotation, finalTarget, Time.deltaTime * lerpSpeed);

        // Tính toán targetRotation cho ConfigurableJoint dựa trên giá trị đã làm mượt
        joint.targetRotation = Quaternion.Inverse(currentTargetRotation) * initialLocalRotation;
    }

    public void UpdateJointDrive(float spring, float damper)
    {
        if (joint == null) return;

        JointDrive drive = new JointDrive
        {
            positionSpring = spring,
            positionDamper = damper,
            maximumForce = float.MaxValue
        };

        if (joint.rotationDriveMode == RotationDriveMode.Slerp)
            joint.slerpDrive = drive;
        else
        {
            joint.angularXDrive = drive;
            joint.angularYZDrive = drive;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (controller == null) return;
        // Tính lực va chạm dựa trên xung lực (Impulse)
        float force = collision.impulse.magnitude / Time.fixedDeltaTime;

        // Nếu lực đủ mạnh và KHÔNG phải va chạm với sàn nhà (Ground) thì mới báo xỉu
        if (force > 100f && !collision.gameObject.CompareTag("Ground"))
        {
            controller.ApplyDamage(force * 0.0001f);
        }
    }

}
