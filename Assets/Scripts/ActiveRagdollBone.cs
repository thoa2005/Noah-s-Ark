using UnityEngine;

public class ActiveRagdollBone : MonoBehaviour
{
    public Transform targetBone; // xương metarig tương ứng
    public float slerpDriveSpring = 1500f;
    public float slerpDriveDamper = 150f;

    private ConfigurableJoint joint;
    private Quaternion startingRotation;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        if (joint == null) return;

        startingRotation = transform.localRotation;

        // Set SlerpDrive để physicRig cố follow metarig mềm mại
        JointDrive drive = new JointDrive
        {
            positionSpring = slerpDriveSpring,
            positionDamper = slerpDriveDamper,
            maximumForce = float.MaxValue
        };
        joint.slerpDrive = drive;
        joint.rotationDriveMode = RotationDriveMode.Slerp;
    }

    void FixedUpdate()
    {
        if (joint == null || targetBone == null) return;
        
        // physicRig cố follow góc xoay của metarig (animation)
        joint.targetRotation = Quaternion.Inverse(targetBone.localRotation) * startingRotation;
    }

    void LateUpdate()
    {
        if (targetBone == null) return;

        // ÉP MESH FOLLOW VẬT LÝ: 
        // Sau khi Animator đã chạy xong, ta ép bộ xương animation dính vào xương vật lý
        // để người chơi nhìn thấy mesh chuyển động theo ragdoll.
        targetBone.position = transform.position;
        targetBone.rotation = transform.rotation;
    }
}
