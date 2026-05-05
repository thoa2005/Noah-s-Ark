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

                    // 1. Copy Rigidbody
                    Rigidbody lRb = left.GetComponent<Rigidbody>();
                    if (lRb != null)
                    {
                        Rigidbody rRb = right.gameObject.GetComponent<Rigidbody>() ?? right.gameObject.AddComponent<Rigidbody>();
                        EditorUtility.CopySerialized(lRb, rRb);
                    }

                    // 2. Copy Colliders
                    foreach (var lCol in left.GetComponents<CapsuleCollider>())
                    {
                        CapsuleCollider rCol = right.gameObject.GetComponent<CapsuleCollider>() ?? right.gameObject.AddComponent<CapsuleCollider>();
                        EditorUtility.CopySerialized(lCol, rCol);
                        rCol.center = new Vector3(-lCol.center.x, lCol.center.y, lCol.center.z);
                    }

                    // 3. Copy Joint (Thông minh hơn)
                    ConfigurableJoint lJ = left.GetComponent<ConfigurableJoint>();
                    if (lJ != null)
                    {
                        ConfigurableJoint rJ = right.gameObject.GetComponent<ConfigurableJoint>() ?? right.gameObject.AddComponent<ConfigurableJoint>();
                        
                        // Lưu lại reference xương cha cũ để xử lý sau khi copy
                        Rigidbody lConnectedBody = lJ.connectedBody;

                        EditorUtility.CopySerialized(lJ, rJ);

                        // Đảo ngược Anchor theo trục X
                        rJ.anchor = new Vector3(-lJ.anchor.x, lJ.anchor.y, lJ.anchor.z);
                        rJ.connectedAnchor = new Vector3(-lJ.connectedAnchor.x, lJ.connectedAnchor.y, lJ.connectedAnchor.z);

                        // Gán lại xương cha tương ứng bên phải
                        if (lConnectedBody != null)
                        {
                            string lParentName = lConnectedBody.name;
                            if (lParentName.EndsWith(".L"))
                            {
                                string rParentName = lParentName.Replace(".L", ".R");
                                if (bones.ContainsKey(rParentName))
                                {
                                    rJ.connectedBody = bones[rParentName].GetComponent<Rigidbody>();
                                }
                            }
                            else
                            {
                                // Nếu xương cha là xương trục giữa (như spine), giữ nguyên
                                rJ.connectedBody = lConnectedBody;
                            }
                        }
                    }
                }
            }
        }
        EditorUtility.DisplayDialog("Xong", "Đã cập nhật đối xứng chuẩn (đã đảo trục X và nối lại xương cha).", "OK");
    }
}
