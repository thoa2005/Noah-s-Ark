using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIBot : MonoBehaviour
{
    public float moveSpeed     = 4f;
    public float chaseRange    = 7f;
    public float punchRange    = 1.8f;
    public float punchForce    = 10f;
    public float punchCooldown = 1.5f;

    Rigidbody rb;
    Transform closestTarget;
    Vector3   wanderPos;
    float     wanderTimer;
    float     punchTimer;
    float     scanTimer;
    Vector3   spawnPos;
    float     fallLimit = -5f;

    void Start()
    {
        rb         = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        spawnPos   = transform.position;
        wanderTimer = 0f;
        scanTimer   = 0f;
        if (GameManager.Instance != null)
            fallLimit = GameManager.Instance.fallLimit;
    }

    void FixedUpdate()
    {
        // Respawn neu roi xuong
        if (transform.position.y < fallLimit)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = spawnPos + new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
            closestTarget = null;
            return;
        }

        punchTimer  -= Time.fixedDeltaTime;
        wanderTimer -= Time.fixedDeltaTime;
        scanTimer   -= Time.fixedDeltaTime;

        // Scan tim target moi 0.5s (tiet kiem CPU)
        if (scanTimer <= 0f || closestTarget == null)
        {
            scanTimer = 0.5f;
            float best = Mathf.Infinity;
            closestTarget = null;

            // Quet Player
            foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
            {
                float fd = Vector3.Distance(transform.position, go.transform.position);
                if (fd < best) { best = fd; closestTarget = go.transform; }
            }

            // Quet Bot khac
            foreach (var go in GameObject.FindGameObjectsWithTag("Bot"))
            {
                if (go == gameObject) continue;
                float fd = Vector3.Distance(transform.position, go.transform.position);
                if (fd < best) { best = fd; closestTarget = go.transform; }
            }
        }

        float dist = closestTarget != null
            ? Vector3.Distance(transform.position, closestTarget.position)
            : 999f;

        if (dist <= punchRange && closestTarget != null)
        {
            // Quay mat
            Vector3 fd2 = closestTarget.position - transform.position;
            fd2.y = 0f;
            if (fd2 != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(fd2), 10f * Time.fixedDeltaTime);

            // Dam
            if (punchTimer <= 0f)
            {
                punchTimer = punchCooldown;
                var trb = closestTarget.GetComponent<Rigidbody>();
                if (trb != null)
                {
                    Vector3 pd = (closestTarget.position - transform.position).normalized + Vector3.up * 0.4f;
                    trb.AddForce(pd * punchForce, ForceMode.Impulse);
                }
            }
        }
        else if (dist <= chaseRange && closestTarget != null)
        {
            // Duoi theo
            Vector3 cd = closestTarget.position - transform.position;
            cd.y = 0f;
            if (cd.magnitude > 0.5f)
            {
                cd.Normalize();
                Vector3 vel = cd * moveSpeed;
                vel.y = rb.linearVelocity.y;
                rb.linearVelocity = vel;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(cd), 10f * Time.fixedDeltaTime);
            }
        }
        else
        {
            // Lang thang
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(2f, 4f);
                Vector2 wr  = Random.insideUnitCircle * 8f;
                wanderPos   = new Vector3(Mathf.Clamp(wr.x, -12f, 12f), 0f, Mathf.Clamp(wr.y, -12f, 12f));
            }
            Vector3 wd = wanderPos - transform.position;
            wd.y = 0f;
            if (wd.magnitude > 0.5f)
            {
                wd.Normalize();
                Vector3 vel = wd * moveSpeed * 0.6f;
                vel.y = rb.linearVelocity.y;
                rb.linearVelocity = vel;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(wd), 10f * Time.fixedDeltaTime);
            }
        }
    }
}
