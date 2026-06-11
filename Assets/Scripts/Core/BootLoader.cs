using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    void Start()
    {
        // Tự động chuyển sang MainMenuScene ngay sau khi Boot xong
        SceneManager.LoadScene("MainMenuScene");
    }
}
