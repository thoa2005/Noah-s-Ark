# 🛠️ Kế hoạch phát triển - Active Ragdoll Project

> Ngày cập nhật: 21/05/2026

---

## 📐 Vision tổng thể

| Hạng mục | Chi tiết |
|----------|----------|
| Số màn / game | 3 màn (rounds) |
| Số player tối đa | 8 người (tương lai), hiện tại 1 Player + 1 Bot |
| Cấu trúc team | Mỗi team 1 / 2 / 4 người, chọn lúc setting |
| Điều kiện kết thúc màn | Chỉ còn 1 team sống sót trên thuyền |
| Điều kiện chết | Rơi xuống nước = chết ngay, không respawn giữa màn |
| Hồi sinh | Chỉ hồi sinh khi bắt đầu màn mới |
| Camera khi chết | Nhìn theo đồng đội còn sống → đồng đội chết hết → nhìn vào thuyền |
| Giữa các màn | Màn hình Loading + kết quả hiện góc trên |
| Tính điểm | Chưa xác định rõ, tạm thời tính điểm theo màn thắng |
| Bot | Sẽ bị xóa khi có đủ người chơi thật |

---

## 🔴 PHẦN 1 — Sửa lỗi hiện tại (Bugs)

> Làm trước vì ảnh hưởng đến gameplay ngay bây giờ

### Bug 2 — `lastHitTime` không được Clear

**File:** `Assets/Scripts/Player/PlayerCombat.cs`

**Vấn đề:** Dictionary ghi lại thời gian đấm trúng để chống double-damage, nhưng không bao giờ được xóa.
Sau khi chết và vào màn mới, cooldown cũ vẫn còn → đòn đánh đầu tiên có thể bị bỏ qua.

**Kế hoạch sửa:**

Bước 1 — Thêm method `ResetCombatState()`:
```csharp
public void ResetCombatState()
{
    lastHitTime.Clear();
    ReleaseGrab();
}
```

Bước 2 — Thêm `OnEnable()` để tự reset khi màn mới bắt đầu:
```csharp
void OnEnable()
{
    lastHitTime.Clear();
}
```

Bước 3 — Gọi `ResetCombatState()` trong GameManager khi bắt đầu màn mới (xem Phần 2).

**Kiểm tra:** Vào màn mới → đấm ngay lập tức → đòn phải được tính bình thường.

---

### Bug 3 — `KnockoutRoutine` không cancel được

**File:** `Assets/Scripts/Ragdoll/ActiveRagdollController.cs`

**Vấn đề:** Coroutine có 2 điểm `WaitForSeconds`. Nếu màn kết thúc trong lúc coroutine đang ngủ,
nó sẽ thức dậy ở màn mới và gọi `stats.ResetAfterWakeUp()` vào thời điểm sai.

**Kế hoạch sửa:**

Bước 1 — Thêm biến lưu reference coroutine:
```csharp
private Coroutine knockoutCoroutine;
```

Bước 2 — Sửa `OnKnockoutReceived()`:
```csharp
void OnKnockoutReceived()
{
    if (knockoutCoroutine != null) StopCoroutine(knockoutCoroutine);
    knockoutCoroutine = StartCoroutine(KnockoutRoutine());
}
```

Bước 3 — Thêm method `CancelKnockout()` để GameManager gọi khi reset màn:
```csharp
public void CancelKnockout()
{
    if (knockoutCoroutine != null)
    {
        StopCoroutine(knockoutCoroutine);
        knockoutCoroutine = null;
    }
    isWakingUp = false;

    var joint = hipRb?.GetComponent<ConfigurableJoint>();
    if (joint != null) joint.yMotion = ConfigurableJointMotion.Locked;
}
```

**Kiểm tra:** Bị KO → màn kết thúc → vào màn mới → nhân vật điều khiển được ngay.

---

## 🟠 PHẦN 2 — Làm lại GameManager (Core gameplay loop)

> Đây là phần quan trọng nhất, cần làm lại gần như hoàn toàn

### Vấn đề hiện tại

`GameManager` đang làm sai hoàn toàn so với design:
- Respawn ngay khi rơi xuống nước ❌
- Không có khái niệm Team ❌
- Không có Round (màn) ❌
- Không tính điểm ❌

### Cấu trúc dữ liệu cần thêm

```csharp
// Thêm class TeamData để quản lý team
[System.Serializable]
public class TeamData
{
    public int teamId;
    public List<GameObject> members = new List<GameObject>();
    public int roundsWon = 0;

    public bool IsAlive()
    {
        members.RemoveAll(m => m == null);
        return members.Exists(m => m.activeSelf);
    }

    public List<GameObject> GetAliveMembers()
    {
        return members.FindAll(m => m != null && m.activeSelf);
    }
}
```

### Các state của GameManager

```
WAITING_TO_START → ROUND_ACTIVE → ROUND_ENDING → LOADING_SCREEN → ROUND_ACTIVE (lặp 3 lần)
                                                                  → GAME_OVER (sau 3 màn)
```

### Kế hoạch sửa GameManager

**Bước 1 — Thêm biến quản lý Round và Team:**
```csharp
public List<TeamData> teams = new List<TeamData>();
private int currentRound = 0;
private const int MAX_ROUNDS = 3;
private enum GameState { WaitingToStart, RoundActive, RoundEnding, GameOver }
private GameState state = GameState.WaitingToStart;
```

**Bước 2 — Sửa logic khi player rơi xuống nước:**
```csharp
void HandlePlayerFall(GameObject p)
{
    // Không respawn — chỉ vô hiệu hóa nhân vật
    DeactivatePlayer(p);

    // Kiểm tra xem còn bao nhiêu team sống
    CheckRoundEnd();
}

void DeactivatePlayer(GameObject p)
{
    // Reset vật lý
    foreach (var rb in p.GetComponentsInChildren<Rigidbody>())
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Cancel knockout coroutine
    var ragdoll = p.GetComponent<ActiveRagdollController>();
    if (ragdoll != null) ragdoll.CancelKnockout();

    // Reset combat
    var combat = p.GetComponent<PlayerCombat>();
    if (combat != null) combat.ResetCombatState();

    // Ẩn nhân vật (dịch xuống dưới map hoặc SetActive false)
    p.transform.position = new Vector3(0, -100f, 0);

    // Báo cho Camera biết player này đã chết (xem Phần 3)
    // CameraManager.Instance.OnPlayerDied(p);
}
```

**Bước 3 — Kiểm tra kết thúc màn:**
```csharp
void CheckRoundEnd()
{
    var aliveTeams = teams.FindAll(t => t.IsAlive());

    if (aliveTeams.Count <= 1)
    {
        state = GameState.RoundEnding;

        // Tính điểm cho team thắng
        if (aliveTeams.Count == 1)
            aliveTeams[0].roundsWon++;

        StartCoroutine(EndRoundRoutine());
    }
}
```

**Bước 4 — Coroutine kết thúc màn:**
```csharp
IEnumerator EndRoundRoutine()
{
    // Chờ 2 giây để người chơi thấy kết quả
    yield return new WaitForSeconds(2f);

    currentRound++;

    if (currentRound >= MAX_ROUNDS)
    {
        // Hết 3 màn → Game Over
        state = GameState.GameOver;
        // TODO: Hiện màn hình kết quả cuối game
    }
    else
    {
        // Còn màn tiếp → Load màn mới
        LoadNextRound();
    }
}
```

**Bước 5 — Load màn mới và hồi sinh tất cả:**
```csharp
void LoadNextRound()
{
    // TODO: Hiện Loading Screen + kết quả góc trên
    // SceneManager.LoadScene(currentRound); hoặc reset scene hiện tại

    // Hồi sinh tất cả player
    foreach (var team in teams)
    {
        foreach (var member in team.members)
        {
            if (member != null)
            {
                RespawnPlayer(member, GetSpawnPosition());
            }
        }
    }

    state = GameState.RoundActive;
}

void RespawnPlayer(GameObject p, Vector3 pos)
{
    p.SetActive(true);
    p.transform.position = pos;

    foreach (var rb in p.GetComponentsInChildren<Rigidbody>())
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    var stats = p.GetComponent<PlayerStats>();
    if (stats != null) stats.ResetAfterWakeUp();

    var ragdoll = p.GetComponent<ActiveRagdollController>();
    if (ragdoll != null) ragdoll.CancelKnockout();

    var combat = p.GetComponent<PlayerCombat>();
    if (combat != null) combat.ResetCombatState();
}
```

---

## 🟡 PHẦN 3 — Làm lại Camera (Spectator mode)

**File:** `Assets/Scripts/Core/CameraFollow.cs`

### Cơ chế camera khi chết

```
Player chết → Camera chuyển sang đồng đội còn sống (random hoặc gần nhất)
Đồng đội chết hết → Camera nhìn vào trung tâm thuyền (fixed point)
```

### Kế hoạch sửa

**Bước 1 — Thêm danh sách targets và spectate target:**
```csharp
public List<Transform> teamTargets = new List<Transform>(); // đồng đội
public Transform boatCenter; // điểm giữa thuyền khi tất cả chết
private Transform currentTarget;
private bool isSpectating = false;
```

**Bước 2 — Thêm method `OnOwnerDied()` được GameManager gọi:**
```csharp
public void OnOwnerDied(GameObject deadPlayer)
{
    isSpectating = true;

    // Tìm đồng đội còn sống
    var aliveTeammate = teamTargets.Find(t => t != null && t.gameObject.activeSelf);

    if (aliveTeammate != null)
    {
        currentTarget = aliveTeammate;
    }
    else
    {
        // Không còn đồng đội → nhìn vào thuyền
        currentTarget = boatCenter;
    }
}
```

**Bước 3 — Sửa `LateUpdate()` để dùng `currentTarget` thay vì `target` cứng:**
```csharp
void LateUpdate()
{
    Transform activeTarget = isSpectating ? currentTarget : target;
    if (activeTarget == null) return;

    // ... phần còn lại giữ nguyên, thay target → activeTarget
}
```

**Bước 4 — Clamp Y để camera không lao xuống biển:**
```csharp
// Sau dòng SmoothDamp
currentTargetPos.y = Mathf.Max(currentTargetPos.y, 0.5f);
```

---

## 🟢 PHẦN 4 — Loading Screen + Hiển thị kết quả

> Làm sau cùng, sau khi gameplay loop đã ổn định

### Cần tạo mới

- **LoadingScreenManager.cs** — quản lý màn hình loading giữa các round
- **RoundResultUI** — hiển thị điểm số góc trên màn hình loading

### Thông tin cần hiển thị trên Loading Screen

```
┌─────────────────────────────────┐
│  Round 1 kết thúc               │
│                                 │
│  🏆 Team 1: 1 điểm              │
│     Team 2: 0 điểm              │
│                                 │
│  [Loading... Round 2]           │
└─────────────────────────────────┘
```

### Kế hoạch sơ bộ

Bước 1 — Tạo Scene riêng cho Loading Screen hoặc dùng Additive loading.  
Bước 2 — GameManager truyền dữ liệu điểm qua `DontDestroyOnLoad` hoặc static class.  
Bước 3 — LoadingScreenManager đọc dữ liệu và hiển thị UI.  
Bước 4 — Sau X giây (hoặc khi load xong) → chuyển sang màn tiếp theo.

---

## 📋 Thứ tự thực hiện

| Thứ tự | Việc cần làm | File | Ghi chú |
|--------|-------------|------|---------|
| 1 | Bug 3 — CancelKnockout | `ActiveRagdollController.cs` | Độc lập, làm trước |
| 2 | Bug 2 — ResetCombatState | `PlayerCombat.cs` | Độc lập, làm trước |
| 3 | Làm lại GameManager | `GameManager.cs` | Phụ thuộc 1+2 |
| 4 | Camera spectator mode | `CameraFollow.cs` | Phụ thuộc 3 |
| 5 | Loading Screen + UI | Script mới | Làm sau cùng |

---

## ❓ Câu hỏi còn mở (cần quyết định sau)

- [ ] Điểm tính như thế nào? Thắng màn = +1 điểm? Hay tính theo số người còn sống?
- [ ] Spawn point của mỗi team ở đâu trên thuyền?
- [ ] Khi tất cả team chết cùng lúc (hòa) thì tính sao?
- [ ] Thời gian loading giữa các màn là bao lâu?
- [ ] Sau 3 màn, màn hình kết quả cuối game trông như thế nào?
