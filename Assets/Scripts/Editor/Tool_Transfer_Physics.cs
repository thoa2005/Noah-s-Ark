using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

public class Tool_Transfer_Physics : EditorWindow
{
    private GameObject oldPhysicRig;
    private GameObject newPhysicRig;

    [MenuItem("Tools/3. Copy Thông Số Vật Lý (Transfer Physics)")]
    public static void ShowWindow()
    {
        GetWindow<Tool_Transfer_Physics>("Transfer Physics");
    }

    void OnGUI()
    {
        GUILayout.Label("COPY THÔNG SỐ TỪ RAGDOLL CŨ SANG MỚI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Sử dụng ComponentUtility để copy y hệt dữ liệu (như Copy/Paste tay).", MessageType.Info);

        oldPhysicRig = (GameObject)EditorGUILayout.ObjectField("Bộ Xương CŨ", oldPhysicRig, typeof(GameObject), true);
        newPhysicRig = (GameObject)EditorGUILayout.ObjectField("Bộ Xương MỚI", newPhysicRig, typeof(GameObject), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Thực Hiện Copy (Transfer Now)", GUILayout.Height(40)))
        {
            if (oldPhysicRig != null && newPhysicRig != null)
            {
                Transfer();
            }
        }
    }

    void Transfer()
    {
        Dictionary<string, Transform> newBones = new Dictionary<string, Transform>();
        foreach (var t in newPhysicRig.GetComponentsInChildren<Transform>())
        {
            if (!newBones.ContainsKey(t.name)) newBones.Add(t.name, t);
        }

        Transform[] oldBones = oldPhysicRig.GetComponentsInChildren<Transform>();

        foreach (var oldT in oldBones)
        {
            if (newBones.TryGetValue(oldT.name, out Transform newT))
            {
                // Xóa các thành phần cũ trên xương mới để tránh xung đột
                foreach (var c in newT.GetComponents<Component>())
                {
                    if (!(c is Transform)) DestroyImmediate(c);
                }

                // 1. Copy Rigidbody
                Rigidbody oldRb = oldT.GetComponent<Rigidbody>();
                if (oldRb != null)
                {
                    ComponentUtility.CopyComponent(oldRb);
                    ComponentUtility.PasteComponentAsNew(newT.gameObject);
                }

                // 2. Copy Colliders
                foreach (var oldCol in oldT.GetComponents<Collider>())
                {
                    ComponentUtility.CopyComponent(oldCol);
                    ComponentUtility.PasteComponentAsNew(newT.gameObject);
                }

                // 3. Copy Joint
                ConfigurableJoint oldJoint = oldT.GetComponent<ConfigurableJoint>();
                if (oldJoint != null)
                {
                    ComponentUtility.CopyComponent(oldJoint);
                    ComponentUtility.PasteComponentAsNew(newT.gameObject);
                }
            }
        }

        // --- Bước cuối: Nối lại Connected Body ---
        foreach (var oldT in oldBones)
        {
            if (newBones.TryGetValue(oldT.name, out Transform newT))
            {
                ConfigurableJoint newJoint = newT.GetComponent<ConfigurableJoint>();
                ConfigurableJoint oldJoint = oldT.GetComponent<ConfigurableJoint>();
                if (newJoint != null && oldJoint != null && oldJoint.connectedBody != null)
                {
                    if (newBones.TryGetValue(oldJoint.connectedBody.name, out Transform connectedT))
                    {
                        newJoint.connectedBody = connectedT.GetComponent<Rigidbody>();
                    }
                }
            }
        }

        EditorUtility.DisplayDialog("Xong", "Đã copy thành công bằng ComponentUtility!", "OK");
    }
}
