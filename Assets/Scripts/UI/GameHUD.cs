using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// HUD hien thi so mang, thanh charge va man hinh Game Over / Win.
/// Gan vao cung GameObject voi GameManager hoac GameObject rieng.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Tham chieu GameManager")]
    public GameManager gameManager;

    [Header("Lives UI")]
    public Text livesText;           // "LIVES: 3"

    [Header("Charge Bar")]
    public GameObject chargeBarRoot; // An/hien khi dang grab
    public Image      chargeFill;    // Image type = Filled, Fill Method = Horizontal

    [Header("Game Over / Win")]
    public GameObject gameOverPanel;
    public Text       gameOverTitle;   // "GAME OVER" hoac "YOU WIN!"
    public Text       gameOverSubtitle;// "Press R to restart"
    public Button     restartButton;

    [Header("Crosshair")]
    public Image crosshairImage;     // UI Image nho o giua man hinh

    [Header("Stats UI")]
    public Image hpFill;
    public Image staminaFill;

    // Tham chieu den PlayerCombat de lay charge
    PlayerCombat playerCombat;
    PlayerStats playerStats;
    CharacterInput _input;
    bool           wasGameOver = false;


    // ------------------------------------------------------------------ //

    void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            playerCombat = playerGo.GetComponent<PlayerCombat>();
            playerStats = playerGo.GetComponent<PlayerStats>();
            _input = playerGo.GetComponent<CharacterInput>();
        }


        if (gameOverPanel   != null) gameOverPanel.SetActive(false);
        if (chargeBarRoot   != null) chargeBarRoot.SetActive(false);
        if (restartButton   != null) restartButton.onClick.AddListener(RestartGame);

        UpdateLivesUI();
    }

    void Update()
    {
        UpdateLivesUI();
        UpdateStatsUI();
        UpdateChargeBar();
        CheckGameOver();

        // Phim R de restart (Dung Input System moi)
        if (_input != null && _input.isRestartRequest) 
        {
            _input.UseRestartRequest();
            RestartGame();
        }
    }

    // ------------------------------------------------------------------ //

    void UpdateLivesUI()
    {
        if (livesText == null) return;
        if (playerStats != null)
            livesText.text = "LIVES: " + playerStats.lives;
        else if (gameManager != null)
            livesText.text = "LIVES: " + gameManager.GetLives();
    }

    void UpdateStatsUI()
    {
        if (playerStats == null) return;

        if (hpFill != null)
            hpFill.fillAmount = playerStats.currentStability / playerStats.maxStability;
        
        if (staminaFill != null)
            staminaFill.fillAmount = playerStats.currentStamina / playerStats.maxStamina;
    }

    void UpdateChargeBar()
    {
        if (chargeBarRoot == null || chargeFill == null || playerCombat == null) return;
        bool isCharging = playerCombat.IsCharging();
        chargeBarRoot.SetActive(isCharging);
        if (isCharging)
            chargeFill.fillAmount = playerCombat.GetChargePct();
    }

    void CheckGameOver()
    {
        if (gameManager == null || gameOverPanel == null) return;
        if (wasGameOver) return;

        if (gameManager.IsGameOver())
        {
            wasGameOver = true;
            ShowEndScreen(win: false);
        }
    }

    public void ShowEndScreen(bool win)
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);

        if (gameOverTitle != null)
            gameOverTitle.text = win ? "YOU WIN! 🎉" : "GAME OVER";

        if (gameOverSubtitle != null)
            gameOverSubtitle.text = "Press R to restart";

        // Hien thi hieu ung pop-in
        gameOverPanel.transform.localScale = Vector3.zero;
        StartCoroutine(PopIn(gameOverPanel.transform));
    }

    IEnumerator PopIn(Transform t)
    {
        float elapsed = 0f;
        float duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float s = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
