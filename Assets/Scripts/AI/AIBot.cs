using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIBot : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed     = 4f;
    public float chaseRange    = 10f;
    public float punchRange    = 1.5f;
    public float punchCooldown = 2f;
    public string targetTag    = "Player";

    [Header("References")]
    public Animator anim; // Kéo Animator của bản metarig vào đây

    Rigidbody rb;
    Transform target;
    float     punchTimer;
    float     scanTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Đảm bảo AI không bị ngã lăn quay khi di chuyển (giống PlayerMovement)
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }

    void FixedUpdate()
    {
        scanTimer -= Time.fixedDeltaTime;
        punchTimer -= Time.fixedDeltaTime;

        // 1. Tìm mục tiêu gần nhất
        if (scanTimer <= 0f || target == null)
        {
            scanTimer = 1f; // Quét mỗi giây cho đỡ nặng máy
            FindTarget();
        }

        if (target == null)
        {
            StopMoving();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        // 2. Xử lý hành động dựa trên khoảng cách
        if (distance <= punchRange)
        {
            // Ở đủ gần -> Dừng lại và Đấm
            StopMoving();
            LookAtTarget();
            TryPunch();
        }
        else if (distance <= chaseRange)
        {
            // Ở xa -> Đuổi theo
            MoveToTarget();
            LookAtTarget();
        }
        else
        {
            // Quá xa -> Đứng chơi
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

        // Di chuyển Rigidbody gốc
        Vector3 vel = direction * moveSpeed;
        vel.y = rb.linearVelocity.y;
        rb.linearVelocity = vel;

        // Cập nhật Animator để đôi chân Ragdoll bước đi
        if (anim != null)
        {
            anim.SetFloat("Forward", 1f, 0.1f, Time.fixedDeltaTime);
        }
    }

    void StopMoving()
    {
        // Giảm dần vận tốc về 0
        Vector3 vel = rb.linearVelocity;
        vel.x *= 0.5f;
        vel.z *= 0.5f;
        rb.linearVelocity = vel;

        if (anim != null)
        {
            anim.SetFloat("Forward", 0f, 0.1f, Time.fixedDeltaTime);
        }
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

    void TryPunch()
    {
        if (punchTimer <= 0f)
        {
            punchTimer = punchCooldown;
            if (anim != null)
            {
                anim.SetTrigger("Punch");
                Debug.Log("[AIBot] PUNCHING target!");
            }
        }
    }
}
