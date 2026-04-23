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
            
            // TÌM TRỰC TIẾP PLAYER THAY VÌ GETCOMPONENT (tránh lỗi script gắn sai chỗ)
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerRb = playerObj.GetComponent<Rigidbody>();
            }

            if (playerRb == null) 
            {
                Debug.LogError("KHÔNG TÌM THẤY PLAYER RIGIDBODY! Xích tàng hình bị lỗi!");
                return;
            }

            // KHÔNG tách xương ra khỏi physicRig - giữ nguyên cây hierarchy
            // pelvis.parent = null; // ĐÃ XÓA - đây là nguyên nhân gây mất xương khi Play

            // 2. Tạo một 'sợi dây xích tàng hình' kéo xương chậu bám sát theo cái vỏ bọc Player
            ConfigurableJoint tracker = pelvis.gameObject.AddComponent<ConfigurableJoint>();
            tracker.connectedBody = playerRb;
            
            // Cho phép di chuyển tự do nhưng sẽ bị xích kéo lại
            tracker.xMotion = ConfigurableJointMotion.Free;
            tracker.yMotion = ConfigurableJointMotion.Free;
            tracker.zMotion = ConfigurableJointMotion.Free;
            
            // Xoay tự do nhưng sẽ bị xích kéo xoay thẳng lại (giữ thăng bằng)
            tracker.angularXMotion = ConfigurableJointMotion.Free;
            tracker.angularYMotion = ConfigurableJointMotion.Free;
            tracker.angularZMotion = ConfigurableJointMotion.Free;

            // Cài đặt độ cứng của dây xích kéo vị trí
            JointDrive drive = new JointDrive();
            drive.positionSpring = 1000f; // Lực kéo CỰC MẠNH
            drive.positionDamper = 200f;  
            drive.maximumForce = float.MaxValue;
            
            tracker.xDrive = drive;
            tracker.yDrive = drive;
            tracker.zDrive = drive;
            
            // Cài đặt lực giữ thăng bằng (không cho úp mặt xuống đất)
            tracker.rotationDriveMode = RotationDriveMode.Slerp;
            JointDrive angularDrive = new JointDrive();
            angularDrive.positionSpring = 100f; // Giữ thẳng đứng cực khỏe
            angularDrive.positionDamper = 500f;
            angularDrive.maximumForce = float.MaxValue;
            
            tracker.slerpDrive = angularDrive;
            // Xoay đúng hướng với vỏ bọc Player
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
