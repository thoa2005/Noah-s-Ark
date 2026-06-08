using UnityEngine;
using Fusion;

/// <summary>
/// Custom Scene Manager để track progress loading từ Fusion.
/// </summary>
public class CustomNetworkSceneManager : NetworkSceneManagerDefault
{
    public delegate void ProgressDelegate(float progress);
    public static event ProgressDelegate OnProgressChanged;

    protected override void OnLoadSceneProgress(SceneRef sceneRef, float progress)
    {
        base.OnLoadSceneProgress(sceneRef, progress);
        Debug.Log($"[Loading] Progress: {progress:P0}");
        OnProgressChanged?.Invoke(progress);
    }
}
