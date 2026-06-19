using UnityEngine;
using System.Collections;

public class AppleSpawner : MonoBehaviour
{
    [Header("Apple Prefab")]
    public GameObject applePrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Apple Settings")]
    public int applesPerRound = 3;

    public float minSpawnDelay = 240f;
    public float maxSpawnDelay = 360f;
    public float firstAppleDelay = 180f; 

    private int spawnedCount = 0;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

IEnumerator SpawnRoutine()
{
    // chờ vài phút đầu trận
    yield return new WaitForSeconds(firstAppleDelay);

    while (spawnedCount < applesPerRound)
    {
        SpawnApple();

        spawnedCount++;

        if (spawnedCount >= applesPerRound)
            yield break;

        float waitTime =
            Random.Range(
                minSpawnDelay,
                maxSpawnDelay
            );

        yield return new WaitForSeconds(waitTime);
    }
}

    void SpawnApple()
    {
        if (spawnPoints.Length == 0)
            return;

        int randomIndex =
            Random.Range(
                0,
                spawnPoints.Length
            );

        Instantiate(
            applePrefab,
            spawnPoints[randomIndex].position,
            Quaternion.identity
        );
    }

    public void ResetRound()
    {
        spawnedCount = 0;

        StopAllCoroutines();

        StartCoroutine(
            SpawnRoutine()
        );
    }
}