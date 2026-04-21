using UnityEngine;

public class ActiveRagdollInitialiser : MonoBehaviour
{
    public Transform physicsRig;
    public Transform masterRig;

    [ContextMenu("Run Final Setup")]
    public void FinalSetup()
    {
        if (!physicsRig || !masterRig) return;

        // 1. Clean up Master Rig (it should have no physics)
        Rigidbody[] mRbs = masterRig.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in mRbs) DestroyImmediate(rb);
        
        Joint[] mJoints = masterRig.GetComponentsInChildren<Joint>(true);
        foreach (var j in mJoints) DestroyImmediate(j);

        Collider[] mCols = masterRig.GetComponentsInChildren<Collider>(true);
        foreach (var c in mCols) DestroyImmediate(c);

        // 2. Link Physics Rig to Master Rig
        ConfigurableJoint[] pJoints = physicsRig.GetComponentsInChildren<ConfigurableJoint>(true);
        foreach (var joint in pJoints)
        {
            Transform pBone = joint.transform;
            Transform mBone = FindChildRecursive(masterRig, pBone.name);
            
            if (mBone != null)
            {
                ActiveRagdollBone arb = pBone.gameObject.GetComponent<ActiveRagdollBone>();
                if (!arb) arb = pBone.gameObject.AddComponent<ActiveRagdollBone>();
                arb.targetBone = mBone;
                Debug.Log("Link SUCCESS: " + pBone.name);
            }
        }

        // 3. Make sure physics rig is unparented or not affected by Animator
        // Usually we unparent it to world space or keep it under a static anchor
        Debug.Log("Active Ragdoll Setup Complete!");
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
