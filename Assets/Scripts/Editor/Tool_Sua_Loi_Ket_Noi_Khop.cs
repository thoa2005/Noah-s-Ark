using UnityEngine;
using UnityEditor;

public class Tool_Sua_Loi_Ket_Noi_Khop : EditorWindow
{
    [MenuItem("Tools/2. Sửa Lỗi Kết Nối Khớp")]
    public static void ShowWindow()
    {
        GetWindow<Tool_Sua_Loi_Ket_Noi_Khop>("Sửa Khớp");
    }

    void OnGUI()
    {
        GUILayout.Label("SỬA LỖI KẾT NỐI KHỚP VẬT LÝ", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Nhấn nút bên dưới để tự động tìm và nối lại các khớp (ConfigurableJoint) vào xương cha của chúng.", MessageType.Info);

        if (GUILayout.Button("Sửa Ngay!", GUILayout.Height(40)))
        {
            Fix();
        }
    }

    public static void Fix()
    {
        var joints = Object.FindObjectsByType<ConfigurableJoint>(FindObjectsSortMode.None);
        int fixedCount = 0;

        foreach (var joint in joints)
        {
            if (joint.connectedBody == null)
            {
                Transform parent = joint.transform.parent;
                while (parent != null)
                {
                    Rigidbody rb = parent.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        joint.connectedBody = rb;
                        Debug.Log($"<color=green>{joint.name} -> {rb.name} CONNECTED</color>");
                        fixedCount++;
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }

        EditorUtility.DisplayDialog("Kết quả", $"Đã sửa xong {fixedCount} khớp nối!", "OK");
    }
}
