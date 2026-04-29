using UnityEngine;

/// <summary>
/// Quan ly mot xuong trong Ragdoll, dong bo giua animation va physics.
/// </summary>
public class ActiveRagdollBone : MonoBehaviour
{
    public Transform animBone;
    public ConfigurableJoint joint;
    public Transform targetBone;
    
    private Quaternion initialLocalRotation;


    public void Setup(Transform animBone, ConfigurableJoint joint)
    {
        this.animBone = animBone;
        this.joint = joint;
        
        // Luu lai rotation goc de lam moc tinh toan targetRotation
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
}
