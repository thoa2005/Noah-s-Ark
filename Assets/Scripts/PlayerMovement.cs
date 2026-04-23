using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed         = 8f;
    public float jumpForce         = 10f; // Increased slightly for safety if mass is high
    public float groundCheckDistance = 1.3f; // Shoots a ray down from root
    public Transform leftFoot;
    public Transform rightFoot;

    [Header("Punch (Chuot phai)")]
    public float punchForce    = 15f;
    public float punchRadius   = 2f;
    public float punchCooldown = 0.5f;



    [Header("Grab+Throw (Chuot trai): Nhan=cam, Giu=charge, Nha=nem")]
    public float grabRadius    = 2.5f;
    public float grabDistance  = 1.5f;
    public float grabSpring    = 80f;
    public float grabDamper    = 10f;
    public float minThrowForce = 1f;
    public float maxThrowForce = 3f;
    public float maxChargeTime = 1.0f;

    Rigidbody rb;
    bool      isGrounded;
    float     punchTimer;
    Camera    mainCam;
    Vector2   moveInput;

    Rigidbody grabbedRb;
    float     chargeTimer;
    bool      isGrabbing;

    void Start()
    {
        rb             = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam        = Camera.main;
    }

    void OnMove(InputValue v) => moveInput = v.Get<Vector2>();

    void OnJump(InputValue v)
    {
        if (!v.isPressed) return;
        
        Debug.Log($"[Jump Check] isGrounded: {isGrounded}, Distance: {groundCheckDistance}, rb.mass: {rb.mass}");

        if (!isGrounded) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        // Tính tổng khối lượng của Player và cả bộ xương Ragdoll đang cõng
        float totalMass = rb.mass;
        Rigidbody[] allRbs = GetComponentsInChildren<Rigidbody>();
        foreach (var r in allRbs) 
        {
            if (r != rb) totalMass += r.mass;
        }

        // Nhân lực nhảy với tổng khối lượng (chia đôi một chút cho đỡ bay lên cung trăng)
        float finalJumpForce = jumpForce * (totalMass * 0.8f);
        rb.AddForce(Vector3.up * finalJumpForce, ForceMode.Impulse);
        
        Debug.Log($"[Nhảy!] Tổng cân nặng: {totalMass}. Lực nhảy thực tế: {finalJumpForce}");
    }

    void Update()
    {
        RaycastHit hit;
        bool leftGrounded  = leftFoot  != null && Physics.SphereCast(leftFoot.position,  0.1f, Vector3.down, out hit, 0.15f);
        bool rightGrounded = rightFoot != null && Physics.SphereCast(rightFoot.position, 0.1f, Vector3.down, out hit, 0.15f);
        
        // Tia dự phòng bắn từ rốn xuống phòng khi chân bị lệch
        bool centerGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance);
        
        isGrounded = leftGrounded || rightGrounded || centerGrounded;

        punchTimer -= Time.deltaTime;

        var mouse    = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null || keyboard == null) return;

        // CHUOT PHAI: DAM
        if (mouse.rightButton.wasPressedThisFrame && punchTimer <= 0f)
        {
            this.PerformPunch();
        }



        // CHUOT TRAI: GRAB / THROW
        if (!isGrabbing)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                this.PerformGrab();
            }
        }
        else
        {
            if (grabbedRb == null)
            {
                isGrabbing  = false;
                chargeTimer = 0f;
            }
            else if (mouse.leftButton.isPressed)
            {
                chargeTimer = Mathf.Clamp(chargeTimer + Time.deltaTime, 0f, maxChargeTime);
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                this.PerformThrow();
            }
        }
    }



    public void PerformPunch()
    {
        punchTimer  = punchCooldown;
        isGrabbing  = false;
        chargeTimer = 0f;
        grabbedRb   = null;

        var hitsP = Physics.OverlapSphere(transform.position + transform.forward, punchRadius);
        foreach (var h in hitsP)
        {
            if (h.gameObject == gameObject) continue;
            var hrb = h.GetComponent<Rigidbody>();
            if (hrb == null) continue;
            Vector3 pd = (h.transform.position - transform.position).normalized + Vector3.up * 0.3f;
            hrb.AddForce(pd * punchForce, ForceMode.Impulse);
            Debug.Log("[Punch] Hit: " + h.gameObject.name);
        }
    }

    void FixedUpdate()
    {
        Vector3 camF = mainCam != null ? mainCam.transform.forward : Vector3.forward;
        camF.y = 0f; camF.Normalize();
        Vector3 camR = mainCam != null ? mainCam.transform.right : Vector3.right;
        camR.y = 0f; camR.Normalize();
        Vector3 dir = (camF * moveInput.y + camR * moveInput.x).normalized;

        if (dir.magnitude > 0.1f)
        {
            Vector3 vel = dir * moveSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), 12f * Time.fixedDeltaTime);
        }
        else
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = Mathf.Lerp(vel.x, 0f, 10f * Time.fixedDeltaTime);
            vel.z = Mathf.Lerp(vel.z, 0f, 10f * Time.fixedDeltaTime);
            rb.linearVelocity = vel;
        }

        // Spring drag khi dang cam
        if (!isGrabbing || grabbedRb == null) return;

        Vector3 gp  = transform.position + transform.forward * grabDistance + Vector3.up * 0.3f;
        Vector3 tp  = gp - grabbedRb.transform.position;
        Vector3 spf = tp * grabSpring - grabbedRb.linearVelocity * grabDamper;
        grabbedRb.AddForce(spf, ForceMode.Force);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, punchRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward, grabRadius);

        // Ve tia Ground Check
        Gizmos.color = isGrounded ? Color.green : Color.gray;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }


    public void PerformGrab()
    {
        var hitsG  = Physics.OverlapSphere(transform.position + transform.forward, grabRadius);
        float best = Mathf.Infinity;
        Rigidbody pick = null;
        foreach (var h in hitsG)
        {
            if (h.gameObject == gameObject) continue;
            var hrb = h.GetComponent<Rigidbody>();
            if (hrb == null) continue;
            float fd = Vector3.Distance(transform.position, h.transform.position);
            if (fd < best) { best = fd; pick = hrb; }
        }
        if (pick != null)
        {
            grabbedRb   = pick;
            isGrabbing  = true;
            chargeTimer = 0f;
            Debug.Log("[Grab] Caught: " + pick.name);
        }
    }


    public void PerformThrow()
    {
        float pw    = chargeTimer / maxChargeTime;
        float frc   = Mathf.Lerp(minThrowForce, maxThrowForce, pw);
        Vector3 td  = (transform.forward + Vector3.up * 0.1f).normalized;
        var tgt     = grabbedRb;

        isGrabbing  = false;
        chargeTimer = 0f;
        grabbedRb   = null;

        if (tgt != null)
        {
            tgt.AddForce(td * frc, ForceMode.Impulse);
            Debug.Log("[Throw] Final Force: " + frc + " | Dir: " + td);
        }
    }
}
