using UnityEngine;

/// <summary>
/// Gắn vào Quad/Sprite đốm sáng để nó luôn quay mặt về camera.
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (_cam == null) return;
        transform.forward = _cam.transform.forward;
    }
}
