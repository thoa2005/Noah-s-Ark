using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Tool_Doi_Xung_Trai_Phai : EditorWindow
{
    private GameObject ragdollRoot;

    [MenuItem("Tools/3. Đối Xứng Vật Lý (Trái sang Phải)")]
    public static void ShowWindow()
    {
        GetWindow<Tool_Doi_Xung_Trai_Phai>("Đối Xứng");
    }

    void OnGUI()
    {
        GUILayout.Label("COPY DỮ LIỆU TỪ TRÁI SANG PHẢI", EditorStyles.boldLabel);
        ragdollRoot = (GameObject)EditorGUILayout.ObjectField("Gốc (physicRig)", ragdollRoot, typeof(GameObject), true);

        if (GUILayout.Button("Thực hiện Đối xứng", GUILayout.Height(40)))
        {
            if (ragdollRoot != null) Mirror();
        }
    }

    void Mirror()
    {
        Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        foreach (var t in ragdollRoot.GetComponentsInChildren<Transform>()) bones[t.name] = t;

        foreach (var pair in bones)
        {
            if (pair.Key.EndsWith(".L"))
            {
                string rightName = pair.Key.Replace(".L", ".R");
                if (bones.ContainsKey(rightName))
                {
                    Transform left = pair.Value;
                    Transform right = bones[rightName];

                    // Copy Rigidbody
                    Rigidbody lRb = left.GetComponent<Rigidbody>();
                    if (lRb != null)
                    {
                        Rigidbody rRb = right.gameObject.GetComponent<Rigidbody>() ?? right.gameObject.AddComponent<Rigidbody>();
                        EditorUtility.CopySerialized(lRb, rRb);
                    }

                    // Copy Colliders
                    foreach (var lCol in left.GetComponents<CapsuleCollider>())
                    {
                        CapsuleCollider rCol = right.gameObject.GetComponent<CapsuleCollider>() ?? right.gameObject.AddComponent<CapsuleCollider>();
                        EditorUtility.CopySerialized(lCol, rCol);
                        rCol.center = new Vector3(-lCol.center.x, lCol.center.y, lCol.center.z);
                    }

                    // Copy Joint
                    ConfigurableJoint lJ = left.GetComponent<ConfigurableJoint>();
                    if (lJ != null)
                    {
                        ConfigurableJoint rJ = right.gameObject.GetComponent<ConfigurableJoint>() ?? right.gameObject.AddComponent<ConfigurableJoint>();
                        EditorUtility.CopySerialized(lJ, rJ);
                    }
                }
            }
        }
        EditorUtility.DisplayDialog("Xong", "Đã copy dữ liệu từ các xương .L sang .R", "OK");
    }
}
