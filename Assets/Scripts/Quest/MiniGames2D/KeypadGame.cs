using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mini-game NVP4: Nhập mã số 2D (Keypad).
/// Người chơi bấm các phím số để nhập đúng mã bí mật.
/// 
/// Kế thừa QuestMinigameUI → tự động mở/đóng panel và báo kết quả về QuestManager.
/// </summary>
public class KeypadGame : QuestMinigameUI
{
    // ------------------------------------------------------------------ //
    //  INSPECTOR
    // ------------------------------------------------------------------ //

    [Header("Mã bí mật (đặt trong Inspector)")]
    public string secretCode = "1234";

    [Header("UI References")]
    public Text   displayText;      // Ô hiển thị các chữ số đã nhập
    public Text   statusText;       // "Đúng!" / "Sai mã!" / v.v.
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Số lần thử tối đa (0 = không giới hạn)")]
    public int maxAttempts = 3;

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI
    // ------------------------------------------------------------------ //

    private string _input        = "";
    private int    _attemptsLeft;

    // ------------------------------------------------------------------ //
    //  OVERRIDE
    // ------------------------------------------------------------------ //

    protected override void OnOpen()
    {
        _input        = "";
        _attemptsLeft = maxAttempts > 0 ? maxAttempts : int.MaxValue;

        UpdateDisplay();

        if (statusText != null) statusText.text = "";
        if (winPanel   != null) winPanel.SetActive(false);
        if (losePanel  != null) losePanel.SetActive(false);
    }

    // ------------------------------------------------------------------ //
    //  PUBLIC (gọi từ các Button số trong UI, gán trong Inspector)
    // ------------------------------------------------------------------ //

    /// <summary>Gọi khi bấm phím số (0-9).</summary>
    public void PressKey(string digit)
    {
        if (!isActive) return;
        if (_input.Length >= secretCode.Length) return;

        _input += digit;
        UpdateDisplay();
    }

    /// <summary>Gọi khi bấm nút Xoá (backspace).</summary>
    public void PressDelete()
    {
        if (!isActive || _input.Length == 0) return;
        _input = _input.Substring(0, _input.Length - 1);
        UpdateDisplay();
    }

    /// <summary>Gọi khi bấm nút Xác nhận.</summary>
    public void PressConfirm()
    {
        if (!isActive) return;

        if (_input == secretCode)
        {
            if (statusText != null) statusText.text = "✅ Mã đúng!";
            winPanel?.SetActive(true);
            Complete();
        }
        else
        {
            _attemptsLeft--;

            if (maxAttempts > 0 && _attemptsLeft <= 0)
            {
                if (statusText != null) statusText.text = "❌ Hết lượt!";
                losePanel?.SetActive(true);
                Fail();
            }
            else
            {
                string attemptsMsg = maxAttempts > 0 ? $" (còn {_attemptsLeft} lượt)" : "";
                if (statusText != null) statusText.text = $"❌ Sai mã!{attemptsMsg}";
                _input = "";
                UpdateDisplay();
            }
        }
    }

    /// <summary>Gọi khi bấm nút Huỷ / Thoát.</summary>
    public void PressCancel()
    {
        if (!isActive) return;
        Close();
    }

    // ------------------------------------------------------------------ //
    //  HELPER
    // ------------------------------------------------------------------ //

    void UpdateDisplay()
    {
        if (displayText == null) return;

        // Hiện dấu * thay cho số thực (như mật khẩu)
        displayText.text = new string('*', _input.Length)
                         + new string('_', secretCode.Length - _input.Length);
    }
}
