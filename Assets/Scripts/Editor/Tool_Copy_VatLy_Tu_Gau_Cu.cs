using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Tool_Copy_VatLy_Tu_Gau_Cu : EditorWindow
{
    private GameObject sourcePhysicRig;
    private GameObject targetPhysicRig;
    private GameObject targetMetaRig;

    [MenuItem("Tools/1. Copy Vật Lý Từ Gấu Cũ")]
    public static void ShowWindow()
    {
        GetWindow<Tool_Copy_VatLy_Tu_Gau_Cu>("Copy Vật Lý");
    }

    void OnGUI()
    {
        GUILayout.Label("DI CHUYỂN DỮ LIỆU VẬT LÝ NÂNG CAO", EditorStyles.boldLabel);
        sourcePhysicRig = (GameObject)EditorGUILayout.ObjectField("Nguồn (physicRig CŨ)", sourcePhysicRig, typeof(GameObject), true);
        targetPhysicRig = (GameObject)EditorGUILayout.ObjectField("Đích (physicRig MỚI)", targetPhysicRig, typeof(GameObject), true);
        targetMetaRig = (GameObject)EditorGUILayout.ObjectField("Hồn (metarig MỚI)", targetMetaRig, typeof(GameObject), true);

        EditorGUILayout.Space();

        if (GUILayout.Button("Bắt đầu Chuyển đổi & Sửa lỗi Shin", GUILayout.Height(40)))
        {
            if (sourcePhysicRig != null && targetPhysicRig != null && targetMetaRig != null)
                PerformSalvage();
        }
    }

    public void PerformSalvage()
    {
        Dictionary<string, Transform> newBones = GetAllBones(targetPhysicRig.transform);
        Dictionary<string, Transform> oldBones = GetAllBones(sourcePhysicRig.transform);
        Dictionary<string, Transform> metaBones = GetAllBones(targetMetaRig.transform);

        // Xóa sạch các ActiveRagdollBone cũ để làm mới hoàn toàn, tránh lỗi Missing Rigidbody
        foreach (var b in newBones.Values)
        {
            var existingArb = b.GetComponent<ActiveRagdollBone>();
            if (existingArb) DestroyImmediate(existingArb);
        }

        foreach (var pair in newBones)
        {
            string boneName = pair.Key;
            Transform newBone = pair.Value;

            bool hasPhysics = false;

            if (oldBones.ContainsKey(boneName))
            {
                Transform oldBone = oldBones[boneName];
                Rigidbody oldRb = oldBone.GetComponent<Rigidbody>();
                if (oldRb != null)
                {
                    Rigidbody newRb = newBone.gameObject.GetComponent<Rigidbody>() ?? newBone.gameObject.AddComponent<Rigidbody>();
                    EditorUtility.CopySerialized(oldRb, newRb);
                    hasPhysics = true;
                }

                foreach (var oldCol in oldBone.GetComponents<Collider>())
                {
                    if (oldCol is CapsuleCollider)
                    {
                        CapsuleCollider newCol = newBone.gameObject.AddComponent<CapsuleCollider>();
                        EditorUtility.CopySerialized(oldCol, newCol);
                    }
                }

                ConfigurableJoint oldJoint = oldBone.GetComponent<ConfigurableJoint>();
                if (oldJoint != null)
                {
                    ConfigurableJoint newJoint = newBone.gameObject.GetComponent<ConfigurableJoint>() ?? newBone.gameObject.AddComponent<ConfigurableJoint>();
                    EditorUtility.CopySerialized(oldJoint, newJoint);
                }
            }
            else if (boneName.ToLower().Contains("shin"))
            {
                SetupShinBone(newBone);
                hasPhysics = true;
            }

            // CHỈ GẮN ActiveRagdollBone nếu xương đó CÓ Rigidbody
            if (hasPhysics && metaBones.ContainsKey(boneName))
            {
                var arb = newBone.gameObject.AddComponent<ActiveRagdollBone>();
                arb.targetBone = metaBones[boneName];
            }
        }

        FixJointConnections(newBones);
        EditorUtility.DisplayDialog("Xong!", "Đã dọn dẹp và copy thành công. Giờ sẽ không còn lỗi Missing Rigidbody nữa!", "OK");
    }

    void SetupShinBone(Transform shin)
    {
        Rigidbody rb = shin.gameObject.GetComponent<Rigidbody>() ?? shin.gameObject.AddComponent<Rigidbody>();
        rb.mass = 2.0f;
        rb.angularDamping = 0.05f;

        CapsuleCollider col = shin.gameObject.GetComponent<CapsuleCollider>() ?? shin.gameObject.AddComponent<CapsuleCollider>();
        col.radius = 0.001f; col.height = 0.005f;

        ConfigurableJoint joint = shin.gameObject.GetComponent<ConfigurableJoint>() ?? shin.gameObject.AddComponent<ConfigurableJoint>();
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        var drive = joint.slerpDrive;
        drive.positionSpring = 1500; drive.positionDamper = 100;
        joint.slerpDrive = drive;
        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
    }

    void FixJointConnections(Dictionary<string, Transform> bones)
    {
        foreach (var pair in bones)
        {
            ConfigurableJoint joint = pair.Value.GetComponent<ConfigurableJoint>();
            if (joint != null)
            {
                Transform parent = pair.Value.parent;
                while (parent != null)
                {
                    Rigidbody parentRb = parent.GetComponent<Rigidbody>();
                    if (parentRb != null) { joint.connectedBody = parentRb; break; }
                    parent = parent.parent;
                }
            }
        }
    }

    Dictionary<string, Transform> GetAllBones(Transform root)
    {
        Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (!bones.ContainsKey(t.name)) bones.Add(t.name, t);
        }
        return bones;
    }
}
