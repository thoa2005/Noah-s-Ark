using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    public GameObject questPanel; // Kéo bảng NVP5 của bạn vào đây
    public GameObject hintUI;     // Tạo 1 Text UI "Nhấn F để vá" rồi kéo vào đây
    private bool isPlayerInRange = false;

    void Update()
    {
        // Nếu người chơi trong vùng và nhấn F
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            questPanel.SetActive(true); // Bật bảng NVP5 lên
            hintUI.SetActive(false);    // Tắt thông báo đi
            Cursor.lockState = CursorLockMode.None; // Mở chuột
            Cursor.visible = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) {
            isPlayerInRange = true;
            hintUI.SetActive(true); // Hiện chữ "Nhấn F"
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) {
            isPlayerInRange = false;
            hintUI.SetActive(false); // Ẩn chữ "Nhấn F"
        }
    }
}