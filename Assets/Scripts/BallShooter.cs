using UnityEngine;

public class BallShooter : MonoBehaviour
{
    [Header("Cài đặt súng bắn tạ")]
    public float shootForce = 100f; // Lực bắn
    public float ballMass = 50f;    // Độ nặng của quả tạ

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Click chuột trái
        {
            // 1. Tạo ra một quả bóng
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            
            // 2. Đặt nó ở ngay trước camera
            ball.transform.position = transform.position + transform.forward * 2f;
            ball.transform.localScale = Vector3.one * 0.5f; // Thu nhỏ lại chút xíu
            
            // 3. Thêm vật lý (Rigidbody)
            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = ballMass;
            
            // 4. Bắn nó bay thẳng tới trước bằng một lực cực mạnh
            rb.AddForce(transform.forward * shootForce, ForceMode.Impulse);

            // 5. Quả tạ tự hủy sau 5 giây để đỡ rác máy
            Destroy(ball, 5f);
        }
    }
}