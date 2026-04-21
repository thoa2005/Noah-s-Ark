using UnityEngine;

public class ActiveRagdollBone : MonoBehaviour
{
    public Transform targetBone;
    private ConfigurableJoint joint;
    private Quaternion startingRotation;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        if (joint != null)
        {
            startingRotation = transform.localRotation;
        }
    }

    void FixedUpdate()
    {
        if (joint != null && targetBone != null)
        {
            // Sync the joint's target rotation to match the animated bone's rotation
            joint.targetRotation = CopyRotation();
        }
    }

    private Quaternion CopyRotation()
    {
        // This is the standard formula to convert local animation rotation 
        // into the coordinate space that ConfigurableJoint.targetRotation expects.
        return Quaternion.Inverse(targetBone.localRotation) * startingRotation;
    }
}
