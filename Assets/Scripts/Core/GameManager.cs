using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Fusion;

/// <summary>
/// Quản lý vòng lặp game: Team, Round (3 màn), chết không respawn giữa màn,
/// hồi sinh đầu màn mới, kết thúc game sau maxRounds màn.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    // ------------------------------------------------------------------ //
    //  CẤU HÌNH
    // ------------------------------------------------------------------ //

    [Header("Round Settings")]
    public int maxRounds = 3;
    public float fallLimit = -5f;       // Y thấp hơn mức này = rơi xuống nước = chết
    public float endRoundDelay = 2f;    // Chờ bao lâu trước khi chuyển màn

    [Header("Round Transition")]
public Camera mainCamera;

public Image fadePanel;

public float cameraMoveDuration = 3f;
public float cameraRotateDuration = 3f;

   [Header("Loading Screen")]

public GameObject loadingPanel;

public Image posterImage;

public Image loadingFill;

public TMP_Text loadingText;

public Sprite round1Poster;
public Sprite round2Poster;
public Sprite round3Poster;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;     // Gán trong Inspector, mỗi team 1 điểm spawn

    [Header("Spectator")]
    public Transform boatCenter;        // Camera nhìn vào đây khi tất cả đồng đội chết

    [Header("Weather System")]
public Material skyboxRound1;
public Material skyboxRound2;
public Material skyboxRound3;

public GameObject stormEffects;
public GameObject lightningFlash;
private Coroutine lightningRoutine;

[Header("Lighting")]
public Light sunLight;

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

    public enum GameState { WaitingToStart, RoundActive, RoundEnding, GameOver }
    [Networked] public GameState state { get; set; }
    [Networked] public int currentRound { get; set; }
    [Networked] public int currentDeadCount { get; set; }

    // Thời gian chờ sau khi hồi sinh trước khi bắt đầu check fall
    // (để ragdoll kịp ổn định vật lý, tránh bị loại ngay khi spawn)
    [Networked] private float roundStartGraceTime { get; set; }
    private const float GRACE_DURATION = 2f;

    // ------------------------------------------------------------------ //
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------------ //
GameObject GetWinningPlayer()
{
    foreach (var team in teams)
    {
        var alive = team.GetAliveMembers();

        if (alive.Count > 0)
            return alive[0];
    }

    return null;
}
IEnumerator WinnerCinematic(GameObject winner)
{
    Vector3 startPos =
        winner.transform.position
        + new Vector3(0, 10, -15);

    Vector3 endPos =
        winner.transform.position
        + new Vector3(0, 3, -5);

    float t = 0;

    while (t < 1)
    {
        t += Time.deltaTime / cameraMoveDuration;

        mainCamera.transform.position =
            Vector3.Lerp(startPos, endPos, t);

        mainCamera.transform.LookAt(winner.transform);

        yield return null;
    }

    float angle = 0;

    float timer = 0;

    while (timer < cameraRotateDuration)
    {
        timer += Time.deltaTime;

        angle += 30f * Time.deltaTime;

        Vector3 offset =
            Quaternion.Euler(0, angle, 0)
            * new Vector3(0, 3, -5);

        mainCamera.transform.position =
            winner.transform.position + offset;

        mainCamera.transform.LookAt(winner.transform);

        yield return null;
    }
}
IEnumerator FadeToBlack()
{
    float alpha = 0;

    while (alpha < 1)
    {
        alpha += Time.deltaTime;

        Color c = fadePanel.color;
        c.a = alpha;

        fadePanel.color = c;

        yield return null;
    }
}

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            currentRound = 1;
            state = GameState.WaitingToStart;
            if (teams.Count == 0)
                AutoBuildTeamsFromScene();

            Rpc_StartGameRoutine();
        }

        SetupRoundWeather();
    }

    public void RegisterPlayer(GameObject player, bool isBot)
    {
        // Chế độ Free-For-All (Mỗi người là 1 Team)
        int newTeamId = teams.Count;
        string tName = isBot ? "Bot " + newTeamId : "Player " + newTeamId;

        // Nếu NetworkPlayer có tên, lấy tên đó làm tên Team để hiển thị lúc chiến thắng cho đẹp
        var np = player.GetComponent<NetworkPlayer>();
        if (np != null && !string.IsNullOrEmpty(np.PlayerName))
        {
            tName = np.PlayerName;
        }

        TeamData newTeam = new TeamData { teamId = newTeamId, teamName = tName };
        newTeam.members.Add(player);
        teams.Add(newTeam);

        Debug.Log($"[GameManager] Đã thêm {tName} vào làm 1 Team độc lập (FFA).");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_StartGameRoutine()
    {
        StartCoroutine(GameStartRoutine());
    }

    IEnumerator GameStartRoutine()
    {
        yield return StartCoroutine(ShowLoadingScreen(1));
        if (HasStateAuthority) StartRound();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (state != GameState.RoundActive) return;

        // Chờ grace period sau khi hồi sinh để ragdoll kịp ổn định
        if (roundStartGraceTime > 0f)
        {
            roundStartGraceTime -= Runner.DeltaTime;
            return;
        }

        // Kiểm tra từng player còn sống có rơi xuống nước không
        foreach (var team in teams)
        {
            foreach (var member in team.GetAliveMembers())
            {
                if (member.transform.position.y < fallLimit)
                {
                    var no = member.GetComponent<NetworkObject>();
                    if (no != null) Rpc_PlayerFell(no.Id);
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayerFell(NetworkId playerId)
    {
        var no = Runner.FindObject(playerId);
        if (no != null)
        {
            HandlePlayerFall(no.gameObject);
        }
    }

    // ------------------------------------------------------------------ //
    //  LOGIC CHÍNH
    // ------------------------------------------------------------------ //

    void HandlePlayerFall(GameObject player)
    {
        Debug.Log($"[GameManager] {player.name} rơi xuống nước!");
        DeactivatePlayer(player);

        // Cộng điểm sinh tồn cho người rơi
        ScoreManager.Instance.AddScore(player, currentDeadCount);

        if (HasStateAuthority)
        {
            currentDeadCount++;
        }

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

        // 6. Báo camera chuyển sang spectator mode CHỈ NẾU ĐÂY LÀ NHÂN VẬT CỦA MÌNH
        var no = player.GetComponent<NetworkObject>();
        if (no != null && no.HasStateAuthority)
        {
            var cam = FindCameraOf(player);
            if (cam != null)
            {
                TeamData myTeam = GetTeamOf(player);
                cam.EnterSpectatorMode(myTeam, boatCenter);
            }
        }

        Debug.Log($"[GameManager] {player.name} đã bị loại khỏi màn {currentRound }.");
    }

    void CheckRoundEnd()
    {
        if (state != GameState.RoundActive) return;

        var aliveTeams = teams.FindAll(t => t.IsAlive());

        if (aliveTeams.Count <= 1)
        {
            state = GameState.RoundEnding;

            NetworkId winnerId = default;

            if (aliveTeams.Count == 1)
            {
                aliveTeams[0].roundsWon++;
                Debug.Log($"[GameManager] {aliveTeams[0].teamName} thắng màn {currentRound }! Tổng điểm: {aliveTeams[0].roundsWon}");
                var winnerObj = GetWinningPlayer();
                if (winnerObj != null)
                {
                    var no = winnerObj.GetComponent<NetworkObject>();
                    if (no != null) winnerId = no.Id;

                    // Thưởng điểm cho người sống sót cuối cùng
                    ScoreManager.Instance.AddScore(winnerObj, currentDeadCount);
                }
            }
            else
            {
                Debug.Log($"[GameManager] Màn {currentRound} hòa! Không ai được điểm.");
            }

            Rpc_EndRoundRoutine(winnerId);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_EndRoundRoutine(NetworkId winnerId)
    {
        GameObject winnerObj = null;
        if (winnerId.IsValid)
        {
            var no = Runner.FindObject(winnerId);
            if (no != null) winnerObj = no.gameObject;
        }
        StartCoroutine(EndRoundRoutine(winnerObj));
    }

    IEnumerator EndRoundRoutine(GameObject winner)
    {
        yield return new WaitForSeconds(endRoundDelay);

        if (winner != null)
        {
            yield return StartCoroutine(WinnerCinematic(winner));
        }

    yield return StartCoroutine(
        FadeToBlack()
    );

    currentRound++;

    if (currentRound > maxRounds)
    {
        EndGame();
    }
    else
    {
        yield return StartCoroutine(
            ShowLoadingScreen(currentRound)
        );

        Color c = fadePanel.color;
        c.a = 0;
        fadePanel.color = c;

        if (HasStateAuthority && currentRound <= maxRounds)
        {
            StartRound();
        }
    }
}
    /// <summary>
    /// Bắt đầu màn mới: hồi sinh tất cả player, reset state.
    /// </summary>
    public void StartRound()
    {
        state = GameState.RoundActive;
        roundStartGraceTime = GRACE_DURATION;

        if (HasStateAuthority)
        {
            currentDeadCount = 0;
        }

        Debug.Log($"[GameManager] ===== BẮT ĐẦU MÀN {currentRound} / {maxRounds} =====");
        Rpc_StartRoundEffects();

        for (int i = 0; i < teams.Count; i++)
        {
            var team = teams[i];
            for (int j = 0; j < team.members.Count; j++)
            {
                var member = team.members[j];
                if (member == null) continue;
                Rpc_RespawnPlayer(member.GetComponent<NetworkObject>().Id, GetSpawnPosition(i, j));
            }
        }

        Debug.Log($"[GameManager] Grace period {GRACE_DURATION}s bắt đầu...");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_StartRoundEffects()
    {
        // Gọi trên mọi Client để chắc chắn thời tiết & spawner được reset đúng lúc
        SetupRoundWeather();
        var appleSpawner = FindFirstObjectByType<AppleSpawner>();
        if (appleSpawner != null) appleSpawner.ResetRound();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_RespawnPlayer(NetworkId playerId, Vector3 pos)
    {
        var no = Runner.FindObject(playerId);
        if (no != null) RespawnPlayer(no.gameObject, pos);
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

        // Thoát spectator mode CHỈ NẾU ĐÂY LÀ NHÂN VẬT CỦA MÌNH
        var no = player.GetComponent<NetworkObject>();
        if (no != null && no.HasStateAuthority)
        {
            var cam = FindCameraOf(player);
            if (cam != null) cam.ExitSpectatorMode(player.transform);
        }

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
    IEnumerator LightningLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(Random.Range(10f, 15f));

        if (lightningFlash != null)
        {
            lightningFlash.SetActive(true);

            AudioManager.Instance.PlayLightningSound();

            yield return new WaitForSeconds(0.2f);

            lightningFlash.SetActive(false);
        }
    }
}

void SetupRoundWeather()
{
    if (currentRound == 1)
    {
        sunLight.color = new Color(1f, 0.75f, 0.45f);

sunLight.intensity = 1.2f;

sunLight.transform.rotation =
    Quaternion.Euler(20f, 30f, 0f);

        RenderSettings.skybox = skyboxRound1;

        RenderSettings.ambientLight =
    new Color(0.8f, 0.55f, 0.35f);

        if (WaterController.Instance != null)
{
    WaterController.Instance.SetCalmSea();
}

        if (stormEffects != null)
            stormEffects.SetActive(false);

        if (lightningFlash != null)
    lightningFlash.SetActive(false);

        if (lightningRoutine != null)
            StopCoroutine(lightningRoutine);
    }
    else if (currentRound == 2)
    {
        sunLight.color =
    new Color(0.6f, 0.65f, 0.75f);

sunLight.intensity = 0.4f;

sunLight.transform.rotation =
    Quaternion.Euler(70f, 30f, 0f);

        RenderSettings.skybox = skyboxRound2;

        RenderSettings.ambientLight =
    new Color(0.25f, 0.25f, 0.35f);

        if (WaterController.Instance != null)
{
    WaterController.Instance.SetStormSea();
}

        if (stormEffects != null)
            stormEffects.SetActive(true);

            AudioManager.Instance.StartRain();

        if (lightningRoutine != null)
            StopCoroutine(lightningRoutine);

        lightningRoutine = StartCoroutine(LightningLoop());
    }
    else if (currentRound == 3)
    {
        sunLight.color =
    Color.white;

sunLight.intensity = 1.3f;

sunLight.transform.rotation =
    Quaternion.Euler(40f, 30f, 0f);

        RenderSettings.skybox = skyboxRound3;

        RenderSettings.ambientLight =
    new Color(0.8f, 0.8f, 0.9f);

        if (WaterController.Instance != null)
{
    WaterController.Instance.SetAfterStormSea();
}

        if (stormEffects != null)
            stormEffects.SetActive(false);

            AudioManager.Instance.StopAmbient();

        if (lightningRoutine != null)
            StopCoroutine(lightningRoutine);
    }

    DynamicGI.UpdateEnvironment();
}
IEnumerator ShowLoadingScreen(int roundNumber)
{
    loadingPanel.SetActive(true);

    if (roundNumber == 1)
        posterImage.sprite = round1Poster;

    else if (roundNumber == 2)
        posterImage.sprite = round2Poster;

    else if (roundNumber == 3)
        posterImage.sprite = round3Poster;

    loadingFill.fillAmount = 0;

    float progress = 0;

    while (progress < 1f)
    {
        progress += Time.deltaTime / 4f;
progress = Mathf.Clamp01(progress);

        loadingFill.fillAmount = progress;

        loadingText.text =
            "Loading... "
            + Mathf.RoundToInt(progress * 100)
            + "%";

        yield return null;
    }

    yield return new WaitForSeconds(1f);

    loadingPanel.SetActive(false);
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

    public int  GetCurrentRound() => currentRound;
    public int  GetMaxRounds()    => maxRounds;
    public bool IsGameOver()      => state == GameState.GameOver;
    public List<TeamData> GetTeams() => teams;

}
