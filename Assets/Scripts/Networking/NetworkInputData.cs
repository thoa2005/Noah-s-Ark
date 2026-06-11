using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;
    public NetworkBool isPunching;
    public NetworkBool isJumpPressed;
    public NetworkBool isGrabPressed;
}
