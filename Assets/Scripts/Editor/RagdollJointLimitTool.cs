using UnityEngine;
using UnityEditor;

public class RagdollJointLimitTool : EditorWindow
{
    [MenuItem("Tools/Set Ragdoll Joint Limits")]
    public static void SetJointLimits()
    {
        GameObject physicRigObj = GameObject.Find("physicRig");
        if (physicRigObj == null)
            physicRigObj = GameObject.Find("Player/panda/physicRig");

        if (physicRigObj == null)
        {
            Debug.LogError("Không tìm thấy physicRig!");
            return;
        }

        int count = 0;
        ConfigurableJoint[] allJoints = physicRigObj.GetComponentsInChildren<ConfigurableJoint>();

        foreach (ConfigurableJoint joint in allJoints)
        {
            string boneName = joint.gameObject.name.ToLower();

            // Tất cả đều khóa vị trí (chỉ cho xoay, không cho dịch chuyển)
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // Mặc định: cho xoay có giới hạn
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            // Áp dụng giới hạn theo tên xương
            if (boneName.Contains("thigh"))
            {
                // Đùi: gập trước/sau nhiều, sang ngang ít
                SetAngularLimits(joint, -20f, 80f, 40f, 30f);
            }
            else if (boneName.Contains("shin") || boneName.Contains("foot.l") || boneName.Contains("foot.r"))
            {
                // Bắp chân / bàn chân: chỉ gập về 1 chiều (như đầu gối)
                SetAngularLimits(joint, 0f, 120f, 10f, 10f);
            }
            else if (boneName.Contains("upper_arm") || boneName.Contains("shoulder"))
            {
                // Cánh tay trên / vai: xoay nhiều chiều
                SetAngularLimits(joint, -70f, 70f, 70f, 50f);
            }
            else if (boneName.Contains("forearm"))
            {
                // Cẳng tay: chủ yếu gập 1 chiều (như khuỷu tay)
                SetAngularLimits(joint, 0f, 140f, 10f, 10f);
            }
            else if (boneName.Contains("hand"))
            {
                // Bàn tay: giới hạn nhỏ
                SetAngularLimits(joint, -30f, 30f, 20f, 20f);
            }
            else if (boneName.Contains("breast") || boneName.Contains("spine"))
            {
                // Cột sống / ngực: giới hạn nhỏ, không gập nhiều
                SetAngularLimits(joint, -20f, 20f, 15f, 15f);
            }
            else if (boneName.Contains("head") || boneName.Contains("neck"))
            {
                // Đầu / cổ: quay trái phải, gật đầu
                SetAngularLimits(joint, -40f, 40f, 40f, 30f);
            }
            else if (boneName.Contains("pelvis"))
            {
                // Xương chậu: giới hạn vừa phải
                SetAngularLimits(joint, -30f, 30f, 20f, 20f);
            }
            else
            {
                // Mặc định cho các xương khác
                SetAngularLimits(joint, -45f, 45f, 30f, 30f);
            }

            EditorUtility.SetDirty(joint);
            count++;
        }

        Debug.Log($"[Joint Limits] Đã set giới hạn cho {count} khớp xương trong physicRig!");
    }

    private static void SetAngularLimits(ConfigurableJoint joint, 
        float lowX, float highX, float limitY, float limitZ)
    {
        // Giới hạn trục X (gập/duỗi chính)
        SoftJointLimit lowXLimit = joint.lowAngularXLimit;
        lowXLimit.limit = lowX;
        joint.lowAngularXLimit = lowXLimit;

        SoftJointLimit highXLimit = joint.highAngularXLimit;
        highXLimit.limit = highX;
        joint.highAngularXLimit = highXLimit;

        // Giới hạn trục Y (sang ngang)
        SoftJointLimit yLimit = joint.angularYLimit;
        yLimit.limit = limitY;
        joint.angularYLimit = yLimit;

        // Giới hạn trục Z (xoay)
        SoftJointLimit zLimit = joint.angularZLimit;
        zLimit.limit = limitZ;
        joint.angularZLimit = zLimit;
    }
}
