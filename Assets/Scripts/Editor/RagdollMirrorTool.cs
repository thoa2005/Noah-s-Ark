using UnityEngine;
using UnityEditor;

public class RagdollMirrorTool : EditorWindow
{
    [MenuItem("Tools/Internal Mirror Ragdoll")]
    public static void MirrorColliders()
    {
        GameObject physicRig = GameObject.Find("physicRig");
        if (physicRig == null) { physicRig = GameObject.Find("Player/panda/physicRig"); }
        
        if (physicRig == null)
        {
            Debug.LogError("Không tìm thấy physicRig!");
            return;
        }

        Transform[] allBones = physicRig.GetComponentsInChildren<Transform>();
        int count = 0;
        
        foreach (Transform boneL in allBones)
        {
            if (boneL.name.EndsWith(".L"))
            {
                string nameR = boneL.name.Substring(0, boneL.name.Length - 2) + ".R";
                Transform boneR = FindDeepChild(physicRig.transform, nameR);

                if (boneR != null)
                {
                    CapsuleCollider collL = boneL.GetComponent<CapsuleCollider>();
                    if (collL != null)
                    {
                        CapsuleCollider collR = boneR.GetComponent<CapsuleCollider>();
                        if (collR == null) collR = boneR.gameObject.AddComponent<CapsuleCollider>();

                        collR.radius = collL.radius;
                        collR.height = collL.height;
                        collR.direction = collL.direction;
                        Vector3 center = collL.center;
                        center.x *= -1;
                        collR.center = center;
                        count++;
                    }
                }
            }
        }
        Debug.Log("MCP: Đã đối xứng xong " + count + " bộ Collider!");
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
