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
    
    private Quaternion initialLocalRotation;

// Sửa hàm Setup thành 3 tham số như sau:
    public void Setup(Transform animBone, ConfigurableJoint joint, ActiveRagdollController controller)
    {
        this.animBone = animBone;
        this.joint = joint;
        this.controller = controller; // Lưu sếp lại để báo cáo va chạm

        initialLocalRotation = transform.localRotation;
    }

    public void SyncRotation()
    {
        if (targetBone == null || joint == null) return;

        // Tinh toan targetRotation cho ConfigurableJoint
        // Cong thuc: Rotation hien tai cua Animation so voi Rotation ban dau
        // Luu y: ConfigurableJoint su dung khong gian rotation nguoc (inverse)
        // joint.targetRotation = initialLocalRotation * Quaternion.Inverse(targetBone.localRotation);
        // joint.targetRotation = initialLocalRotation * Quaternion.Inverse(targetBone.localRotation);
//         Quaternion deltaRotation = Quaternion.Inverse(targetBone.localRotation) * initialLocalRotation;
// joint.targetRotation = deltaRotation;
  joint.targetRotation = Quaternion.Inverse(targetBone.localRotation) * initialLocalRotation;

    }

    public void UpdateJointDrive(float spring, float damper)
    {
        if (joint == null) return;

        JointDrive drive = new JointDrive
        {
            positionSpring = spring,
            positionDamper = damper,
            maximumForce   = float.MaxValue
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
        if (force > 1000f && !collision.gameObject.CompareTag("Ground"))
        {
            controller.ApplyDamage(force*0.001f);
        }
    }

}
