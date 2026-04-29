using UnityEngine;
using UnityEditor;

public class CleanRagdollTool : EditorWindow
{
    [MenuItem("Tools/Dọn dẹp Ragdoll Lỗi")]
    public static void Clean()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("Bạn phải chọn đối tượng 'panda' hoặc 'physicRig' trước khi nhấn nút này!");
            return;
        }

        // Tìm tất cả các đối tượng con và chính nó
        Transform[] allChildren = selected.GetComponentsInChildren<Transform>(true);
        int count = 0;

        foreach (var child in allChildren)
        {
            // Lệnh đặc biệt của Unity để xóa các script bị "Missing"
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            count += removedCount;
        }

        Debug.Log("Đã dọn dẹp xong " + count + " script lỗi (chấm đỏ)!");
    }
}
