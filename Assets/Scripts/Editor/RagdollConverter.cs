using UnityEngine;
using UnityEditor;

public class RagdollConverter : EditorWindow
{
    [MenuItem("Tools/Convert CharacterJoint to ConfigurableJoint")]
    public static void ConvertJoints()
    {
        GameObject rig = GameObject.Find("Player/panda/physicRig");
        if (rig == null)
        {
            Debug.LogError("physicRig not found! Make sure the path is Player/panda/physicRig");
            return;
        }

        int convertedCount = 0;
        CharacterJoint[] cJoints = rig.GetComponentsInChildren<CharacterJoint>();

        foreach (var cj in cJoints)
        {
            GameObject go = cj.gameObject;
            Rigidbody connectedBody = cj.connectedBody;
            Vector3 anchor = cj.anchor;
            Vector3 axis = cj.axis;
            Vector3 swingAxis = cj.swingAxis;

            // Xóa khớp nối cũ (cột lỏng lẻo)
            DestroyImmediate(cj);

            // Thêm khớp nối mới (có cơ bắp)
            ConfigurableJoint conf = go.AddComponent<ConfigurableJoint>();
            conf.connectedBody = connectedBody;
            conf.anchor = anchor;
            conf.axis = axis;
            conf.secondaryAxis = swingAxis;

            // Khóa vị trí để xương không bị đứt lìa
            conf.xMotion = ConfigurableJointMotion.Locked;
            conf.yMotion = ConfigurableJointMotion.Locked;
            conf.zMotion = ConfigurableJointMotion.Locked;

            // Cho phép xoay tự do để lò xo hoạt động
            conf.angularXMotion = ConfigurableJointMotion.Free;
            conf.angularYMotion = ConfigurableJointMotion.Free;
            conf.angularZMotion = ConfigurableJointMotion.Free;

            // Bơm "Cơ bắp" (Lò xo) vào khớp nối
            JointDrive drive = new JointDrive();
            drive.positionSpring = 1500f; // Lực cố gắng đạt được tư thế của hoạt ảnh
            drive.positionDamper = 100f;  // Lực hãm để không bị giật cục
            drive.maximumForce = Mathf.Infinity;
            
            conf.slerpDrive = drive;

            convertedCount++;
        }

        Debug.Log($"Thành công! Đã chuyển đổi {convertedCount} khớp nối sang ConfigurableJoint.");
    }
}
