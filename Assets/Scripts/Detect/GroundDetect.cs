using UnityEngine;

public class GroundDetect : MonoBehaviour
{
    [Header("Ground Sensor")]
    public float groundCheckDistance = 1.3f;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    public Transform leftFoot;
    public Transform rightFoot;

    public bool isGrounded { get; private set; }

    void FixedUpdate()
    {
        RaycastHit hit;
        bool leftG = leftFoot != null && Physics.SphereCast(leftFoot.position, groundCheckRadius, Vector3.down, out hit, groundCheckRadius * 1.5f, groundLayer);
        bool rightG = rightFoot != null && Physics.SphereCast(rightFoot.position, groundCheckRadius, Vector3.down, out hit, groundCheckRadius * 1.5f, groundLayer);
        bool centerG = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance, groundLayer);

        isGrounded = leftG || rightG || centerG;
    }
}
