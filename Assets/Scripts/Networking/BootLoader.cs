using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene("MainMenuScene"); // Khởi tạo mạng xong tự nhảy sang Menu của bạn ngay!
    }
}
