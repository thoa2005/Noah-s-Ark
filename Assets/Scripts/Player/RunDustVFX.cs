using UnityEngine;

public class RunDustVFX : MonoBehaviour
{
    [Header("References")]
    private Rigidbody playerRb;
    private GroundDetect groundDetect;
    private ParticleSystem dustParticle;

    [Header("Settings")]
    [Tooltip("Tốc độ tối thiểu để bắt đầu xịt khói (m/s)")]
    public float minSpeedThreshold = 0.5f;

    private ParticleSystem.EmissionModule emission;

    void Start()
    {
        // Lấy ParticleSystem trên cùng GameObject
        dustParticle = GetComponent<ParticleSystem>();
        if (dustParticle == null)
        {
            Debug.LogError("RunDustVFX: Không tìm thấy ParticleSystem trên " + gameObject.name);
            return;
        }

        // Lấy Rigidbody từ parent (Player root)
        playerRb = GetComponentInParent<Rigidbody>();
        if (playerRb == null)
        {
            Debug.LogError("RunDustVFX: Không tìm thấy Rigidbody trong parent");
            return;
        }

        // Lấy GroundDetect từ parent
        groundDetect = GetComponentInParent<GroundDetect>();
        if (groundDetect == null)
        {
            Debug.LogWarning("RunDustVFX: Không tìm thấy GroundDetect trong parent");
        }

        emission = dustParticle.emission;
    }

    void Update()
    {
        if (playerRb == null || dustParticle == null)
            return;

        // Lấy tốc độ ngang (bỏ qua trục Y để nhảy không tính)
        Vector3 horizontalVel = playerRb.linearVelocity;
        horizontalVel.y = 0f;
        float speed = horizontalVel.magnitude;

        // Kiểm tra điều kiện xịt khói
        bool shouldEmit = false;

        if (speed >= minSpeedThreshold)
        {
            // Nếu có GroundDetect, kiểm tra xem có chạm đất không
            if (groundDetect != null)
            {
                shouldEmit = groundDetect.isGrounded;
            }
            else
            {
                // Nếu không có GroundDetect, chỉ cần speed đủ là xịt
                shouldEmit = true;
            }
        }

        // Điều khiển emission rate
        emission.rateOverDistanceMultiplier = shouldEmit ? 10f : 0f;
    }
}
