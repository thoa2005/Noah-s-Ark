using UnityEngine;
using UnityEditor;

public class RagdollSalvageTool : EditorWindow
{
    [MenuItem("Tools/1. Salvage Physics Rig (CỨU HỘ)")]
    public static void SalvageRig()
    {
        GameObject panda = GameObject.Find("Player/panda");
        if (panda == null) return;

        Transform oldPhysicRig = panda.transform.Find("physicRig");
        Transform metaRig = panda.transform.Find("metarig");

        if (oldPhysicRig == null || metaRig == null)
        {
            Debug.LogError("Không tìm thấy physicRig cũ hoặc metarig!");
            return;
        }

        // 1. Đổi tên cái cũ thành phế liệu
        oldPhysicRig.name = "physicRig_TRASH";

        // 2. Tạo bộ xương mới tinh từ metarig (để lấy chuẩn form dáng mới)
        GameObject newPhysicRig = Instantiate(metaRig.gameObject, panda.transform);
        newPhysicRig.name = "physicRig";

        // 3. Quét và copy toàn bộ Collider, Rigidbody, Joint từ TRASH sang MỚI
        Transform[] oldBones = oldPhysicRig.GetComponentsInChildren<Transform>();
        int savedCount = 0;

        foreach (Transform oldB in oldBones)
        {
            Transform newB = FindDeepChild(newPhysicRig.transform, oldB.name);
            if (newB != null)
            {
                // Copy Rigidbody
                Rigidbody rbOld = oldB.GetComponent<Rigidbody>();
                if (rbOld != null)
                {
                    Rigidbody rbNew = newB.gameObject.AddComponent<Rigidbody>();
                    rbNew.mass = rbOld.mass;
                    rbNew.linearDamping = rbOld.linearDamping;
                    rbNew.angularDamping = rbOld.angularDamping;
                    rbNew.isKinematic = rbOld.isKinematic;
                    rbNew.useGravity = rbOld.useGravity;
                }

                // Copy Capsule Collider
                CapsuleCollider colOld = oldB.GetComponent<CapsuleCollider>();
                if (colOld != null)
                {
                    CapsuleCollider colNew = newB.gameObject.AddComponent<CapsuleCollider>();
                    colNew.center = colOld.center;
                    colNew.radius = colOld.radius;
                    colNew.height = colOld.height;
                    colNew.direction = colOld.direction;
                    savedCount++;
                }

                // Copy Configurable Joint
                ConfigurableJoint jointOld = oldB.GetComponent<ConfigurableJoint>();
                if (jointOld != null)
                {
                    ConfigurableJoint jointNew = newB.gameObject.AddComponent<ConfigurableJoint>();
                    // Chỉ copy những cái cơ bản để tránh nổ vật lý
                    jointNew.xMotion = jointOld.xMotion;
                    jointNew.yMotion = jointOld.yMotion;
                    jointNew.zMotion = jointOld.zMotion;
                    jointNew.angularXMotion = jointOld.angularXMotion;
                    jointNew.angularYMotion = jointOld.angularYMotion;
                    jointNew.angularZMotion = jointOld.angularZMotion;
                }
            }
        }

        // Cập nhật lại đường dẫn cho file Setup
        ActiveRagdollInitialiser init = panda.transform.parent.GetComponent<ActiveRagdollInitialiser>();
        if (init != null) init.physicsRig = newPhysicRig.transform;

        // Xóa phế liệu
        DestroyImmediate(oldPhysicRig.gameObject);

        Debug.Log($"ĐÃ GIẢI CỨU THÀNH CÔNG {savedCount} COLLIDERS!");
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
