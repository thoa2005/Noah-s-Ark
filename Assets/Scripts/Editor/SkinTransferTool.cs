using UnityEngine;
using UnityEditor;

public class SkinTransferTool : EditorWindow
{
    [MenuItem("Tools/Transfer Skin to Physics Rig")]
    public static void TransferSkin()
    {
        // 1. Tìm lớp da gấu
        GameObject meshObj = GameObject.Find("Player/panda/meshes[0]");
        if (meshObj == null) 
        { 
            Debug.LogError("Không tìm thấy lớp da! Kiểm tra lại đường dẫn Player/panda/meshes[0]"); 
            return; 
        }

        SkinnedMeshRenderer skin = meshObj.GetComponent<SkinnedMeshRenderer>();

        // 2. Tìm bộ xương vật lý mới
        Transform physicsRig = GameObject.Find("Player/panda/physicRig")?.transform;
        if (physicsRig == null) 
        { 
            Debug.LogError("Không tìm thấy bộ xương vật lý! Kiểm tra lại đường dẫn Player/panda/physicRig"); 
            return; 
        }

        // 3. Tiến hành tháo móc cài cũ và móc sang xương mới
        Transform[] oldBones = skin.bones;
        Transform[] newBones = new Transform[oldBones.Length];
        int transferred = 0;

        for (int i = 0; i < oldBones.Length; i++)
        {
            if (oldBones[i] != null)
            {
                // Đi tìm cái xương bên physicRig có tên giống hệt cái xương cũ
                Transform newBone = FindDeepChild(physicsRig, oldBones[i].name);
                if (newBone != null)
                {
                    newBones[i] = newBone; // Lắp vào slot
                    transferred++;
                }
                else
                {
                    newBones[i] = oldBones[i]; // Nếu không thấy thì giữ nguyên cũ cho an toàn
                }
            }
        }

        // 4. Xác nhận thay đổi
        skin.bones = newBones;
        
        // Thay đổi luôn cả xương gốc (Root Bone)
        if (skin.rootBone != null)
        {
            Transform newRoot = FindDeepChild(physicsRig, skin.rootBone.name);
            if (newRoot != null) skin.rootBone = newRoot;
        }

        Debug.Log($"THÀNH CÔNG! Đã lột da và móc {transferred} khớp xương sang bộ Physics Rig mới.");
    }

    // Hàm đệ quy đi quét sâu vào từng lớp con để tìm xương
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
