using UnityEngine;

/// <summary>
/// Balancer phien ban "Vung nhu ban thach":
/// - Dung Projection de bao ve Master khoi bi Ragdoll keo do.
/// - Khong cho phep phan luc tac dong nguoc lai Master qua manh.
/// </summary>
public class ActiveRagdollBalancer : MonoBehaviour
{
    private ConfigurableJoint balanceJoint;
    private Rigidbody rb;

    public void Setup(Rigidbody playerRb)
    {
        rb = GetComponent<Rigidbody>();
        balanceJoint = GetComponent<ConfigurableJoint>();
        if (balanceJoint == null) balanceJoint = gameObject.AddComponent<ConfigurableJoint>();

        balanceJoint.connectedBody = playerRb;
        
        // --- CHONG GIAN KHOP & BAO VE MASTER ---
        balanceJoint.projectionMode = JointProjectionMode.PositionAndRotation;
        balanceJoint.projectionDistance = 0.01f;
        balanceJoint.projectionAngle = 1f;
        balanceJoint.enablePreprocessing = false; // Giam rung lac khi va cham manh

        // Cho phep di chuyen tu do trong pham vi nho
        balanceJoint.xMotion = ConfigurableJointMotion.Locked;
        balanceJoint.yMotion = ConfigurableJointMotion.Locked;
        balanceJoint.zMotion = ConfigurableJointMotion.Locked;

        balanceJoint.angularXMotion = ConfigurableJointMotion.Free;
        balanceJoint.angularYMotion = ConfigurableJointMotion.Free;
        balanceJoint.angularZMotion = ConfigurableJointMotion.Free;

        balanceJoint.rotationDriveMode = RotationDriveMode.Slerp;
        
        // Drive giu vi tri (Position) - SIET CHAT DE KHONG BI GIAN DAY THUN
        JointDrive positionDrive = new JointDrive
        {
            positionSpring = 100000f, // Tang gap 10 lan
            positionDamper = 2000f,   // Tang damper de chong rung
            maximumForce = float.MaxValue
        };
        balanceJoint.xDrive = positionDrive;
        balanceJoint.yDrive = positionDrive;
        balanceJoint.zDrive = positionDrive;
    }

    public void UpdateBalance(float spring, float damper, Quaternion targetRotation)
    {
        if (balanceJoint == null) return;

        JointDrive rotationDrive = new JointDrive
        {
            positionSpring = spring,
            positionDamper = damper,
            maximumForce   = float.MaxValue
        };

        balanceJoint.slerpDrive = rotationDrive;
        balanceJoint.targetRotation = targetRotation;
        balanceJoint.targetPosition = Vector3.zero;
    }
}
