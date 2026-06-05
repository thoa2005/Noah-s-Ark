using UnityEngine;
using Fusion;

[DefaultExecutionOrder(100)]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 10f;
    public float fixedPitch = 40f;
    public float rotateSpeed = 120f;
    public float smoothSpeed = 6f;
    public float rotationSmoothSpeed = 5f;

    [Header("Wall Collision")]
    public float minDistance = 3f;
    public float maxDistance = 10f;
    public float zoomSpeed = 35f;
    public float collisionRadius = 0.3f;
    public LayerMask collisionMask = ~0;

    [Header("Input")]
    public CharacterInput _input;

    // ------------------------------------------------------------------ //
    //  SPECTATOR MODE
    // ------------------------------------------------------------------ //

    private bool isSpectating = false;
    private Transform spectateTarget;                // Target hiện tại khi spectate
    private GameManager.TeamData spectateTeam;       // Team để tìm đồng đội còn sống
    private Transform boatFallback;                  // Nhìn vào đây khi tất cả đồng đội chết

    // ------------------------------------------------------------------ //
    //  RUNTIME
    // ------------------------------------------------------------------ //

    private float currentYaw = 0f;
    private Vector3 currentTargetPos;
    private Vector3 smoothVelocity;
    private float initialDistance;

    void Start()
    {
        initialDistance = distance;

        // Player scene cũ có thể còn bị serialize trong Inspector.
        // Bỏ target cũ để camera chỉ bind player local spawn bằng Fusion.
        target = null;
        _input = null;
        currentTargetPos = transform.position;
    }

    void LateUpdate()
    {
        if (!isSpectating && !IsTargetLocalPlayer())
        {
            target = null;
            _input = null;
            TryBindLocalPlayer();
        }

        Transform activeTarget = ResolveTarget();
        if (activeTarget == null) return;

        // Clamp Y: không cho camera target lao xuống dưới mức sàn
        Vector3 rawPos = activeTarget.position;
        rawPos.y = Mathf.Max(rawPos.y, 0.5f);

        // Làm mượt vị trí target (triệt tiêu rung lắc ragdoll)
        currentTargetPos = Vector3.SmoothDamp(currentTargetPos, rawPos, ref smoothVelocity, 0.2f);

        // Xoay camera
        if (_input != null && _input.isCameraRotatePressed)
            currentYaw += _input.lookInput.x * rotateSpeed * Time.deltaTime;

        // Zoom
        if (_input != null && _input.zoomInput.y != 0)
        {
            float zoomAmount = _input.zoomInput.y * zoomSpeed * 0.01f;
            distance = Mathf.Clamp(distance - zoomAmount, minDistance, maxDistance);
            _input.zoomInput = Vector2.zero;
        }

        // Tính vị trí camera
        Quaternion rotation = Quaternion.Euler(fixedPitch, currentYaw, 0f);
        Vector3 targetViewPos = currentTargetPos + Vector3.up * 0.5f;
        Vector3 camDir = rotation * Vector3.back;

        Vector3 desiredPos = targetViewPos + camDir * distance;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(targetViewPos - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);

        // Cập nhật spectate target theo đồng đội còn sống
        if (isSpectating) UpdateSpectateTarget();
    }

    // ------------------------------------------------------------------ //
    //  SPECTATOR API
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Gọi khi owner của camera này chết.
    /// Camera chuyển sang nhìn đồng đội còn sống, hoặc thuyền nếu tất cả chết.
    /// </summary>
    public void EnterSpectatorMode(GameManager.TeamData team, Transform boatCenter)
    {
        isSpectating = true;
        spectateTeam = team;
        boatFallback = boatCenter;
        UpdateSpectateTarget();
        Debug.Log($"[CameraFollow] Spectator mode. Target: {spectateTarget?.name ?? "thuyền"}");
    }

    /// <summary>
    /// Gọi khi màn mới bắt đầu, trả camera về follow owner ban đầu.
    /// ownerTransform được bỏ qua — camera luôn quay về target gốc đã gán trong Inspector.
    /// </summary>
    public void ExitSpectatorMode(Transform ownerTransform)
    {
        isSpectating = false;
        spectateTarget = null;
        spectateTeam = null;
        // KHÔNG override target — giữ nguyên target đã gán trong Inspector
        // target chỉ được set 1 lần duy nhất lúc đầu, không bị ghi đè khi respawn
        Debug.Log($"[CameraFollow] Thoát spectator mode → tiếp tục follow {target?.name ?? "NULL"}");
    }

    void UpdateSpectateTarget()
    {
        if (spectateTeam != null)
        {
            var aliveMembers = spectateTeam.GetAliveMembers();
            if (aliveMembers.Count > 0)
            {
                spectateTarget = aliveMembers[0].transform;
                return;
            }
        }
        // Không còn đồng đội → nhìn vào thuyền
        spectateTarget = boatFallback;
    }

    Transform ResolveTarget()
    {
        if (isSpectating) return spectateTarget;
        return target;
    }

    private bool IsTargetLocalPlayer()
    {
        if (target == null) return false;

        NetworkObject netObj = target.GetComponent<NetworkObject>();
        return netObj != null && netObj.HasInputAuthority;
    }

    private void TryBindLocalPlayer()
    {
        NetworkObject[] networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);

        foreach (NetworkObject netObj in networkObjects)
        {
            if (netObj == null || !netObj.HasInputAuthority) continue;

            CharacterInput input = netObj.GetComponent<CharacterInput>();
            if (input == null) continue;

            target = netObj.transform;
            _input = input;
            currentTargetPos = target.position;

            Debug.Log($"[CameraFollow] Bind local player: {target.name}");
            return;
        }
    }

    // ------------------------------------------------------------------ //
    //  UTILITY
    // ------------------------------------------------------------------ //

    public void ResetDistance()
    {
        distance = initialDistance;
    }
}
