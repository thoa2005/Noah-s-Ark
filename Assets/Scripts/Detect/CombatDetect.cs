using UnityEngine;
using System.Collections.Generic;

public class CombatDetect : MonoBehaviour
{
    [Header("Punch Sensor (Red)")]
    public float punchRadius = 2f;
    public Vector3 punchOffset = new Vector3(0, 0.0002f, 0f);

    [Header("Grab Sensor (Yellow)")]
    public float grabRadius = 2.5f;
    public float grabOffset = 0.1f;

    // Hàm vẽ Gizmos được PlayerCombat gọi
    public void DrawDetectGizmos(Transform leftHand, Transform rightHand, Rigidbody leftPhys, Rigidbody rightPhys)
    {
        Gizmos.color = Color.red;
        if (leftHand != null) Gizmos.DrawWireSphere(leftHand.TransformPoint(punchOffset), punchRadius);
        if (rightHand != null) Gizmos.DrawWireSphere(rightHand.TransformPoint(punchOffset), punchRadius);

        Gizmos.color = Color.yellow;
        if (leftPhys != null)
        {
            Vector3 lPos = leftPhys.position + leftPhys.transform.forward * grabOffset;
            Gizmos.DrawWireSphere(lPos, grabRadius);
        }
        if (rightPhys != null)
        {
            Vector3 rPos = rightPhys.position + rightPhys.transform.forward * grabOffset;
            Gizmos.DrawWireSphere(rPos, grabRadius);
        }
    }

    // Quét tìm mục tiêu đấm
    public List<Rigidbody> GetPunchTargets(Transform handBone)
    {
        List<Rigidbody> targets = new List<Rigidbody>();
        if (handBone == null) return targets;

        Vector3 pPos = handBone.TransformPoint(punchOffset);
        foreach (var h in Physics.OverlapSphere(pPos, punchRadius))
        {
            if (h.gameObject == gameObject || h.transform.IsChildOf(transform)) continue;
            var hrb = h.GetComponent<Rigidbody>();
            if (hrb != null) targets.Add(hrb);
        }
        return targets;
    }

    // Quét tìm mục tiêu có thể cầm nắm
    public HashSet<Rigidbody> GetGrabbableTargets(Rigidbody physicsHand)
    {
        HashSet<Rigidbody> result = new HashSet<Rigidbody>();
        if (physicsHand == null) return result;

        Vector3 origin = physicsHand.position + physicsHand.transform.forward * grabOffset;
        foreach (var h in Physics.OverlapSphere(origin, grabRadius))
        {
            if (h.gameObject == gameObject || h.transform.IsChildOf(transform)) continue;
            var hrb = h.attachedRigidbody;
            if (hrb != null) result.Add(hrb);
        }
        return result;
    }


}
