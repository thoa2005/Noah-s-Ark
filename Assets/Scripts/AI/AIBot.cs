using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIBot : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 4f;
    public float chaseRange = 10f;
    public float grabRange = 1.8f;
    public float grabCooldown = 0.5f; // Spam grab moi 0.5 giay de test
    public string targetTag = "Player";

    [Header("References")]
    public Animator anim;

    private Rigidbody rb;
    private Transform target;
    private float scanTimer;

    // Giu grab bang cach override input thay vi goi PerformGrab truc tiep
    private CharacterInput input;
    private PlayerCombat combat;
    private float punchTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        // Lay CharacterInput de override isGrabPressed
        input = GetComponent<CharacterInput>();
        combat = GetComponent<PlayerCombat>();
        if (input == null)
            Debug.LogWarning("[AIBot] Khong tim thay CharacterInput!");
        if (combat == null)
            Debug.LogWarning("[AIBot] Khong tim thay PlayerCombat!");

        punchTimer = 1.0f; // Đấm chậm lại một chút để hồi Stamina
    }

    void FixedUpdate()
    {
        // NHỊP ĐẤM: 1 giây 1 phát
        punchTimer -= Time.fixedDeltaTime;
        if (punchTimer <= 0f)
        {
            if (input != null)
            {
                input.isPunching = true; // Bấm nút
                punchTimer = 1.0f;

                // ÉP ANIMATOR PHẢI CHẠY (Bỏ qua các lỗi kẹt Transition)
                if (anim != null)
                {
                    anim.Play("Punch", 0, 0f);
                }

                if (combat != null && combat.stats != null)
                {
                    Debug.Log($"[AIBot] Force Punch! Stamina: {combat.stats.currentStamina}");
                }
            }
        }
        else if (punchTimer < 0.9f) // Nhả nút cực nhanh sau 0.1s
        {
            if (input != null) input.isPunching = false; // Nhả nút để hết "Gồng"
        }

        scanTimer -= Time.fixedDeltaTime;

        // Tim muc tieu moi giay
        if (scanTimer <= 0f || target == null)
        {
            scanTimer = 1f;
            FindTarget();
        }

        if (target == null)
        {
            StopMoving();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= grabRange)
        {
            StopMoving();
            LookAtTarget();
        }
        else if (distance <= chaseRange)
        {
            MoveToTarget();
            LookAtTarget();
        }
        else
        {
            StopMoving();
            target = null;
        }
    }

    void FindTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(targetTag);
        float closestDist = Mathf.Infinity;

        foreach (GameObject p in players)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                target = p.transform;
            }
        }
    }

    void MoveToTarget()
    {
        Vector3 direction = (target.position - transform.position);
        direction.y = 0;
        direction.Normalize();

        Vector3 vel = direction * moveSpeed;
        vel.y = rb.linearVelocity.y;
        rb.linearVelocity = vel;

        if (anim != null)
            anim.SetFloat("Forward", 1f, 0.1f, Time.fixedDeltaTime);
    }

    void StopMoving()
    {
        Vector3 vel = rb.linearVelocity;
        vel.x *= 0.5f;
        vel.z *= 0.5f;
        rb.linearVelocity = vel;

        // Animation bo qua - tranh warning spam neu bot khong co parameter "Forward"
    }

    void LookAtTarget()
    {
        Vector3 lookPos = target.position - transform.position;
        lookPos.y = 0;
        if (lookPos != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.fixedDeltaTime * 5f);
        }
    }

}
