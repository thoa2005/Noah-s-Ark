using UnityEngine;

public class ActiveRagdollSetup : MonoBehaviour
{
    public Transform physicsMetarig;
    public Transform masterMetarig;

    [ContextMenu("Setup Mapping")]
    public void SetupMapping()
    {
        if (physicsMetarig == null || masterMetarig == null)
        {
            Debug.LogError("Please assign both Metarigs!");
            return;
        }

        ActiveRagdollBone[] bones = physicsMetarig.GetComponentsInChildren<ActiveRagdollBone>(true);
        // If we haven't added the components yet, let's find bones with ConfigurableJoints
        ConfigurableJoint[] joints = physicsMetarig.GetComponentsInChildren<ConfigurableJoint>(true);

        foreach (var joint in joints)
        {
            Transform pBone = joint.transform;
            // Find matching bone name in master
            Transform tBone = FindDeepChild(masterMetarig, pBone.name);

            if (tBone != null)
            {
                ActiveRagdollBone arb = pBone.gameObject.GetComponent<ActiveRagdollBone>();
                if (arb == null) arb = pBone.gameObject.AddComponent<ActiveRagdollBone>();
                arb.targetBone = tBone;
                Debug.Log("Mapped: " + pBone.name + " -> " + tBone.name);
            }
            else
            {
                Debug.LogWarning("Could not find master bone for: " + pBone.name);
            }
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
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
