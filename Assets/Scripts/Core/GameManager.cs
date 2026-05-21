using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý vòng lặp game: Team, Round (3 màn), chết không respawn giữa màn,
/// hồi sinh đầu màn mới, kết thúc game sau maxRounds màn.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ------------------------------------------------------------------ //
    //  CẤU HÌNH
    // ------------------------------------------------------------------ //

    [Header("Round Settings")]
    public int maxRounds = 3;
    public float fallLimit = -5f;       // Y thấp hơn mức này = rơi xuống nước = chết
    public float endRoundDelay = 2f;    // Chờ bao lâu trước khi chuyển màn

    [Header("Spawn Points")]
    public Transform[] spawnPoints;     // Gán trong Inspector, mỗi team 1 điểm spawn

    [Header("Spectator")]
    public Transform boatCenter;        // Camera nhìn vào đây khi tất cả đồng đội chết

    // ------------------------------------------------------------------ //
    //  DỮ LIỆU TEAM
    // ------------------------------------------------------------------ //

    [System.Serializable]
    public class TeamData
    {
        public string teamName = "Team";
        public int teamId;
        public List<GameObject> members = new List<GameObject>();
        public int roundsWon = 0;

        /// <summary>Còn ít nhất 1 thành viên đang active trên thuyền.</summary>
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

    [Header("Teams (tự động tạo từ tag nếu để trống)")]
    public List<TeamData> teams = new List<TeamData>();

    // ------------------------------------------------------------------ //
    //  TRẠNG THÁI GAME
    // ------------------------------------------------------------------ //

    private enum GameState { WaitingToStart, RoundActive, RoundEnding, GameOver }
    private GameState state = GameState.WaitingToStart;
    private int currentRound = 0;

    // Thời gian chờ sau khi hồi sinh trước khi bắt đầu check fall
    // (để ragdoll kịp ổn định vật lý, tránh bị loại ngay khi spawn)
    private float roundStartGraceTime = 0f;
    private const float GRACE_DURATION = 2f;

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Nếu chưa gán team nào (test nhanh), tự tạo từ tag Player/Bot trong scene
        if (teams.Count == 0)
            AutoBuildTeamsFromScene();

        StartRound();
    }

    void Update()
    {
        if (state != GameState.RoundActive) return;

        // Chờ grace period sau khi hồi sinh để ragdoll kịp ổn định
        if (roundStartGraceTime > 0f)
        {
            roundStartGraceTime -= Time.deltaTime;
            return;
        }

        // Kiểm tra từng player còn sống có rơi xuống nước không
        foreach (var team in teams)
        {
            foreach (var member in team.GetAliveMembers())
            {
                if (member.transform.position.y < fallLimit)
                    HandlePlayerFall(member);
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  LOGIC CHÍNH
    // ------------------------------------------------------------------ //

    void HandlePlayerFall(GameObject player)
    {
        Debug.Log($"[GameManager] {player.name} rơi xuống nước!");
        DeactivatePlayer(player);
        CheckRoundEnd();
    }

    /// <summary>
    /// Vô hiệu hóa nhân vật khi chết. Không respawn giữa màn.
    /// Reset toàn bộ state vật lý + logic, ẩn nhân vật xuống dưới map.
    /// </summary>
    void DeactivatePlayer(GameObject player)
    {
        // 1. Reset vật lý
        foreach (var rb in player.GetComponentsInChildren<Rigidbody>())
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. Cancel knockout coroutine
        var ragdoll = player.GetComponent<ActiveRagdollController>();
        if (ragdoll != null) ragdoll.CancelKnockout();

        // 3. Reset combat state
        var combat = player.GetComponent<PlayerCombat>();
        if (combat != null) combat.ResetCombatState();

        // 4. Reset stats nếu đang KO
        var stats = player.GetComponent<PlayerStats>();
        if (stats != null && stats.isKnockedOut)
            stats.ResetAfterWakeUp();

        // 5. Ẩn nhân vật
        player.transform.position = new Vector3(0f, -200f, 0f);
        player.SetActive(false);

        // 6. Báo camera chuyển sang spectator mode
        var cam = FindCameraOf(player);
        if (cam != null)
        {
            TeamData myTeam = GetTeamOf(player);
            cam.EnterSpectatorMode(myTeam, boatCenter);
        }

        Debug.Log($"[GameManager] {player.name} đã bị loại khỏi màn {currentRound + 1}.");
    }

    /// <summary>
    /// Kiểm tra còn bao nhiêu team sống. Nếu <= 1 thì kết thúc màn.
    /// </summary>
    void CheckRoundEnd()
    {
        if (state != GameState.RoundActive) return;

        var aliveTeams = teams.FindAll(t => t.IsAlive());

        if (aliveTeams.Count <= 1)
        {
            state = GameState.RoundEnding;

            if (aliveTeams.Count == 1)
            {
                aliveTeams[0].roundsWon++;
                Debug.Log($"[GameManager] {aliveTeams[0].teamName} thắng màn {currentRound + 1}! " +
                          $"Tổng điểm: {aliveTeams[0].roundsWon}");
            }
            else
            {
                Debug.Log($"[GameManager] Màn {currentRound + 1} hòa! Không ai được điểm.");
            }

            StartCoroutine(EndRoundRoutine());
        }
    }

    IEnumerator EndRoundRoutine()
    {
        // Chờ để người chơi thấy kết quả trước khi chuyển màn
        yield return new WaitForSeconds(endRoundDelay);

        currentRound++;

        if (currentRound >= maxRounds)
            EndGame();
        else
            StartRound();
    }

    /// <summary>
    /// Bắt đầu màn mới: hồi sinh tất cả player, reset state.
    /// </summary>
    void StartRound()
    {
        Debug.Log($"[GameManager] ===== BẮT ĐẦU MÀN {currentRound + 1} / {maxRounds} =====");

        for (int i = 0; i < teams.Count; i++)
        {
            var team = teams[i];
            for (int j = 0; j < team.members.Count; j++)
            {
                var member = team.members[j];
                if (member == null) continue;
                RespawnPlayer(member, GetSpawnPosition(i, j));
            }
        }

        state = GameState.RoundActive;
        roundStartGraceTime = GRACE_DURATION; // Chờ 2 giây trước khi check fall
        Debug.Log($"[GameManager] Grace period {GRACE_DURATION}s bắt đầu...");
    }

    /// <summary>
    /// Hồi sinh 1 player: bật lại, đặt vị trí, reset toàn bộ state.
    /// </summary>
    void RespawnPlayer(GameObject player, Vector3 pos)
    {
        player.SetActive(true);

        // Teleport toàn bộ ragdoll (capsule + physicRig + xương) đến spawn point
        // KHÔNG chỉ set transform.position vì ragdoll có nhiều Rigidbody riêng biệt
        var ragdoll = player.GetComponent<ActiveRagdollController>();
        if (ragdoll != null)
            ragdoll.TeleportTo(pos, Quaternion.identity);
        else
        {
            // Fallback nếu không có ragdoll
            player.transform.position = pos;
            player.transform.rotation = Quaternion.identity;
            foreach (var rb in player.GetComponentsInChildren<Rigidbody>())
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        ragdoll?.CancelKnockout();

        var stats = player.GetComponent<PlayerStats>();
        if (stats != null) stats.ResetAfterWakeUp();

        var combat = player.GetComponent<PlayerCombat>();
        if (combat != null) combat.ResetCombatState();

        var input = player.GetComponent<CharacterInput>();
        if (input != null) input.ClearAllInputs();

        // Thoát spectator mode, trả camera về follow owner
        var cam = FindCameraOf(player);
        if (cam != null) cam.ExitSpectatorMode(player.transform);

        Debug.Log($"[GameManager] {player.name} hồi sinh tại {pos}");
    }

    void EndGame()
    {
        state = GameState.GameOver;

        TeamData winner = null;
        int maxScore = -1;
        foreach (var team in teams)
        {
            if (team.roundsWon > maxScore)
            {
                maxScore = team.roundsWon;
                winner   = team;
            }
        }

        if (winner != null)
            Debug.Log($"[GameManager] GAME OVER! {winner.teamName} thắng với {winner.roundsWon} màn!");
        else
            Debug.Log("[GameManager] GAME OVER! Hòa!");

        // TODO: Hiện màn hình kết quả cuối game
    }

    // ------------------------------------------------------------------ //
    //  HELPER
    // ------------------------------------------------------------------ //

    Vector3 GetSpawnPosition(int teamIndex, int memberIndex)
    {
        if (spawnPoints != null && teamIndex < spawnPoints.Length && spawnPoints[teamIndex] != null)
            return spawnPoints[teamIndex].position + Vector3.right * memberIndex * 1.5f;

        // Fallback nếu chưa gán spawn point trong Inspector
        return new Vector3(teamIndex * 4f - 2f, 2f, memberIndex * 1.5f);
    }

    TeamData GetTeamOf(GameObject player)
    {
        foreach (var team in teams)
            if (team.members.Contains(player)) return team;
        return null;
    }

    /// <summary>
    /// Tìm CameraFollow của player. Hiện tại game có 1 camera dùng chung.
    /// Sau này khi split-screen thì mỗi player có camera riêng.
    /// </summary>
    CameraFollow FindCameraOf(GameObject player)
    {
        // Tìm camera trên chính player trước
        var cam = player.GetComponentInChildren<CameraFollow>();
        if (cam != null) return cam;

        // Fallback: tìm camera duy nhất trong scene
        return FindFirstObjectByType<CameraFollow>();
    }

    /// <summary>
    /// Tự động tạo team từ tag trong scene (dùng khi test nhanh, chưa có lobby).
    /// Player tag = Team 1, Bot tag = Team 2.
    /// </summary>
    void AutoBuildTeamsFromScene()
    {
        var team0 = new TeamData { teamId = 0, teamName = "Team 1" };
        var team1 = new TeamData { teamId = 1, teamName = "Team 2" };

        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
            team0.members.Add(p);
        foreach (var b in GameObject.FindGameObjectsWithTag("Bot"))
            team1.members.Add(b);

        if (team0.members.Count > 0) teams.Add(team0);
        if (team1.members.Count > 0) teams.Add(team1);

        Debug.Log($"[GameManager] Auto-build teams: {team0.members.Count} player(s), {team1.members.Count} bot(s)");
    }

    // ------------------------------------------------------------------ //
    //  PUBLIC API
    // ------------------------------------------------------------------ //

    public int  GetCurrentRound() => currentRound + 1;
    public int  GetMaxRounds()    => maxRounds;
    public bool IsGameOver()      => state == GameState.GameOver;
    public List<TeamData> GetTeams() => teams;
}
