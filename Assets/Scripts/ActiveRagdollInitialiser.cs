using UnityEngine;

public class ActiveRagdollInitialiser : MonoBehaviour
{
    public Transform physicsRig;
    public Transform masterRig;

    [ContextMenu("Run Final Setup")]
    public void FinalSetup()
    {
        SetupAtRuntime();
    }

    private void Awake()
    {
        SetupAtRuntime();
    }

    public void SetupAtRuntime()
    {
        if (!physicsRig || !masterRig) return;

        Collider rootCollider = GetComponent<Collider>();
        if (rootCollider == null) rootCollider = GetComponentInParent<Collider>();

        // 1. Clean up Master Rig (it should have no physics)
        Rigidbody[] mRbs = masterRig.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in mRbs) DestroyImmediate(rb);
        
        Joint[] mJoints = masterRig.GetComponentsInChildren<Joint>(true);
        foreach (var j in mJoints) DestroyImmediate(j);

        Collider[] mCols = masterRig.GetComponentsInChildren<Collider>(true);
        foreach (var c in mCols) DestroyImmediate(c);

        // 2. Ignore Collision between Root and Physics Rig
        if (rootCollider != null)
        {
            Collider[] pCols = physicsRig.GetComponentsInChildren<Collider>(true);
            foreach (var pc in pCols)
            {
                Physics.IgnoreCollision(rootCollider, pc);
            }
        }

        // 2. Link Physics Rig to Master Rig
        ConfigurableJoint[] pJoints = physicsRig.GetComponentsInChildren<ConfigurableJoint>(true);
        Debug.Log($"Found {pJoints.Length} joints in Physics Rig. Starting link...");

        foreach (var joint in pJoints)
        {
            Transform pBone = joint.transform;

#if UNITY_EDITOR
            // DỌN RÁC: Xóa các script bị "Missing" (Chỉ chạy trong Editor)
            UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(pBone.gameObject);
#endif

            Transform mBone = FindChildRecursive(masterRig, pBone.name);
            
            if (mBone != null)
            {
                ActiveRagdollBone arb = pBone.gameObject.GetComponent<ActiveRagdollBone>();
                if (!arb) arb = pBone.gameObject.AddComponent<ActiveRagdollBone>();
                arb.targetBone = mBone;
                // Debug.Log("Link SUCCESS: " + pBone.name + " -> " + mBone.name);
            }
            else
            {
                Debug.LogWarning("Link FAILED: Không tìm thấy xương '" + pBone.name + "' bên Master Rig!");
            }
        }

        Debug.Log("Active Ragdoll Setup Complete!");
    }

    private void LateUpdate()
    {
        if (physicsRig != null && masterRig != null)
        {
            // ÉP HỒN NHẬP XÁC: Bộ xương hoạt hình (Master) luôn phải dính chặt 
            // vào bộ xương vật lý (Physics) ở cấp độ gốc.
            masterRig.position = physicsRig.position;
            masterRig.rotation = physicsRig.rotation;
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
