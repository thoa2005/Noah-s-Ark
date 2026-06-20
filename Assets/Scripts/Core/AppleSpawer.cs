using UnityEngine;
using System.Collections;
using Fusion;

public class AppleSpawner : NetworkBehaviour
{
    [Header("Apple Prefab")]
    public NetworkPrefabRef applePrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Apple Settings")]
    public int applesPerRound = 3;
    public float minSpawnDelay = 240f;
    public float maxSpawnDelay = 360f;
    public float firstAppleDelay = 180f; 

    private int spawnedCount = 0;
    private Coroutine spawnCoroutine;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(firstAppleDelay);

        while (spawnedCount < applesPerRound)
        {
            SpawnApple();
            spawnedCount++;

            if (spawnedCount >= applesPerRound)
                yield break;

            float waitTime = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnApple()
    {
        if (spawnPoints.Length == 0) return;
        int randomIndex = Random.Range(0, spawnPoints.Length);
        
        Runner.Spawn(applePrefab, spawnPoints[randomIndex].position, Quaternion.identity);
    }

    public void ResetRound()
    {
        if (!HasStateAuthority) return;

        spawnedCount = 0;
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }
}