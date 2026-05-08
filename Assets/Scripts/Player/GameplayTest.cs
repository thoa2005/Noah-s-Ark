using UnityEngine;

public class GameplayTest : MonoBehaviour
{
    float timer = 0f;
    PlayerMovement pm;
    bool done1, done2, done3, done4, done5;

    void Start()
    {
        Debug.Log("--- STARTING AUTOMATED SEQUENCE ---");
        GameObject p = GameObject.FindWithTag("Player");
        if(p != null) pm = p.GetComponent<PlayerMovement>();
        else Debug.LogError("Test Runner: Player not found!");
    }

    void Update()
    {
        if (pm == null) return;
        timer += Time.deltaTime;

        if (timer > 2f && !done1) { done1 = true; Debug.Log("Test: Triggering Punch"); pm.PerformPunch(); }
        if (timer > 6f && !done3) { done3 = true; Debug.Log("Test: Triggering Grab"); pm.PerformGrab(); }
        if (timer > 8f && !done4) { done4 = true; Debug.Log("Test: Triggering Throw"); pm.PerformThrow(); }
        if (timer > 10f && !done5) { done5 = true; Debug.Log("--- SEQUENCE COMPLETED ---"); }
    }
}
