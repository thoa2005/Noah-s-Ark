using UnityEngine;

public class ActiveRagdollBalance : MonoBehaviour
{
    [Header("Balance Settings")]
    public Transform pelvis; // The root physics bone (spine)
    public float balanceForce = 1000f;
    public float upForce = 50f; // Keeps it from sinking

    private Rigidbody playerRb;
    private Rigidbody rb;

    void Start()
    {
        if (pelvis != null)
        {
            rb = pelvis.GetComponent<Rigidbody>();
            
            // BẬT INTERPOLATE ĐỂ HẾT GIẬT
            if (rb != null) rb.interpolation = RigidbodyInterpolation.Interpolate;

            playerRb = GetComponentInParent<Rigidbody>();
            if (playerRb != null) playerRb.interpolation = RigidbodyInterpolation.Interpolate;
            
            if (playerRb == null) 
            {
                Debug.LogError("KHÔNG TÌM THẤY PLAYER RIGIDBODY TRÊN CHA! Hãy đảm bảo script gắn dưới object Player.");
                return;
            }

            // Tạo tracker joint để kéo xương chậu theo Player
            ConfigurableJoint tracker = pelvis.gameObject.AddComponent<ConfigurableJoint>();
            tracker.connectedBody = playerRb;
            
            // Tự động tính toán khoảng cách để cái bụng không bị hút về tâm Player
            tracker.autoConfigureConnectedAnchor = true;
            
            // Tăng mạnh lực kéo vị trí để con Gấu bám sát cái lồng Player
            JointDrive drive = new JointDrive();
            drive.positionSpring = 500f; // Tăng từ 1500 lên 5000
            drive.positionDamper = 100f; 
            drive.maximumForce = float.MaxValue;
            
            tracker.xDrive = drive;
            tracker.yDrive = drive;
            tracker.zDrive = drive;
            
            // Cài đặt lực giữ thăng bằng (không cho úp mặt xuống đất)
            tracker.rotationDriveMode = RotationDriveMode.Slerp;
            JointDrive angularDrive = new JointDrive();
            angularDrive.positionSpring = 10000f; 
            angularDrive.positionDamper = 100f;
            angularDrive.maximumForce = float.MaxValue;
            
            tracker.slerpDrive = angularDrive;
            // Trả về identity để nó đứng thẳng theo hướng của lồng Player
            tracker.targetRotation = Quaternion.identity;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Bỏ logic AddTorque cũ đi vì SlerpDrive của Joint giữ thăng bằng tốt và mượt hơn rất nhiều!
        
        // Anti-Gravity / Hover (Chống bị chìm xuống sàn)
        rb.AddForce(Vector3.up * upForce, ForceMode.Acceleration);
    }
}
