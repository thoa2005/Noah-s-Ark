using UnityEngine;

public class WaterController : MonoBehaviour
{
    public static WaterController Instance;

    [Header("Wave Settings")]
    public float waveHeight = 0.3f;
    public float waveSpeed = 1f;

    private void Awake()
    {
        Instance = this;
    }

    public float GetWaveHeight(Vector3 worldPos)
    {
        return Mathf.Sin(
            Time.time * waveSpeed
            + worldPos.x * 0.1f
            + worldPos.z * 0.1f
        ) * waveHeight;
    }

    public void SetCalmSea()
    {
        waveHeight = 0.1f;
        waveSpeed = 0.5f;
    }

    public void SetStormSea()
    {
        waveHeight = 0.3f;
        waveSpeed = 1.2f;
    }

    public void SetAfterStormSea()
    {
        waveHeight = 0.15f;
        waveSpeed = 0.7f;
    }
}