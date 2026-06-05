using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    void Start()
    {
        // Vừa mở game lên, mạng khởi tạo xong là tự động nhảy sang Menu chính luôn
        SceneManager.LoadScene("MainMenuScene");
    }
}
