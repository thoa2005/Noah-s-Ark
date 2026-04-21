using UnityEngine;

public class ActiveRagdollBalance : MonoBehaviour
{
    [Header("Balance Settings")]
    public Transform pelvis; // The root physics bone (spine)
    public float balanceForce = 1000f;
    public float upForce = 50f; // Keeps it from sinking

    private Rigidbody rb;

    void Start()
    {
        if (pelvis != null)
        {
            rb = pelvis.GetComponent<Rigidbody>();
            // IMPORTANT: Unparent physics rig to let it be purely physics-driven
            pelvis.parent = null; 
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // 1. Keep Torso Upright (Balance)
        // We calculate the delta between "current up" and "world up"
        Quaternion rot = Quaternion.FromToRotation(pelvis.up, Vector3.up);
        rb.AddTorque(new Vector3(rot.x, rot.y, rot.z) * balanceForce);

        // 2. Anti-Gravity / Hover (Helps with the "lảo đảo" feeling)
        rb.AddForce(Vector3.up * upForce, ForceMode.Acceleration);
    }
}
