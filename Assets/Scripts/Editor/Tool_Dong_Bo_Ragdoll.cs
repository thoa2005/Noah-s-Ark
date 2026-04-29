using UnityEngine;
using UnityEditor;

public class Tool_Sync_Transform_Rig : EditorWindow
{
    public Transform sourceRig; // Thường là metarig (Master)
    public Transform targetRig; // Thường là physicRig (Physics)

    [MenuItem("Tools/Active Ragdoll/Sync Transform Rig")]
    public static void ShowWindow()
    {
        GetWindow<Tool_Sync_Transform_Rig>("Sync Rig Transform");
    }

    private void OnGUI()
    {
        GUILayout.Label("Đồng bộ Transform từ Master sang Physics", EditorStyles.boldLabel);
        
        sourceRig = (Transform)EditorGUILayout.ObjectField("Source (Master/Metarig)", sourceRig, typeof(Transform), true);
        targetRig = (Transform)EditorGUILayout.ObjectField("Target (PhysicRig)", targetRig, typeof(Transform), true);

        EditorGUILayout.Space();

        if (GUILayout.Button("SYNC NOW (Match by Name)"))
        {
            if (sourceRig == null || targetRig == null)
            {
                Debug.LogError("Vui lòng kéo đủ 2 bộ xương vào!");
                return;
            }

            SyncRecursive(sourceRig, targetRig);
            Debug.Log("<color=cyan>Đã đồng bộ xong Transform từ " + sourceRig.name + " sang " + targetRig.name + "</color>");
        }
    }

    private void SyncRecursive(Transform source, Transform targetRoot)
    {
        // Tìm xương tương ứng bên Target theo tên
        Transform targetBone = FindChildRecursive(targetRoot, source.name);

        if (targetBone != null && targetBone != targetRoot)
        {
            Undo.RecordObject(targetBone, "Sync Transform");

            // ÉP THÔNG SỐ:
            targetBone.localPosition = source.localPosition;
            targetBone.localRotation = source.localRotation;
            targetBone.localScale = source.localScale;
            
            // Mark dirty để Unity lưu lại thay đổi
            EditorUtility.SetDirty(targetBone);
        }

        // Tiếp tục đệ quy cho các xương con
        foreach (Transform child in source)
        {
            SyncRecursive(child, targetRoot);
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
