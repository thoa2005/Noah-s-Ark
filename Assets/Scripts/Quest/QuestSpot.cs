using UnityEngine;

public class QuestSpot : MonoBehaviour
{
    // Kéo bảng nhiệm vụ vào ô này trong Inspector
    public GameObject questPanel;

    // Kéo file âm thanh vào ô này trong Inspector
    public AudioClip clickSound;

    void Start()
    {
        // Đảm bảo panel luôn ẩn khi scene bắt đầu
        if (questPanel != null)
            questPanel.SetActive(false);
    }

    void OnMouseDown()
    {
        if (!QuestManager.isQuestTaken)
        {
            QuestManager.isQuestTaken = true;

            // Phát âm thanh tại vị trí Cube — không bị ảnh hưởng bởi Destroy
            if (clickSound != null)
                AudioSource.PlayClipAtPoint(clickSound, transform.position);

            if (questPanel != null)
            {
                questPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Tắt PlayerInput để UI nhận được click
                CharacterInput input = FindLocalPlayerInput();
                if (input != null) input.EnableUIMode();
            }

            Destroy(gameObject);
        }
    }

    // Tìm CharacterInput của người chơi local
    private CharacterInput FindLocalPlayerInput()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            return player.GetComponent<CharacterInput>();
        return null;
    }
}
