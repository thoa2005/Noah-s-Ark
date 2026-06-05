using Fusion;
using UnityEngine;

/// <summary>
/// Struct chứa input của player, được Fusion truyền từ client → host mỗi tick.
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveInput;
    public NetworkBool IsJumping;
    public NetworkBool IsPunching;
    public NetworkBool IsGrabbing;
}
