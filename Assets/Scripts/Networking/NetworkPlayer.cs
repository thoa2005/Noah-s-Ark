using Fusion;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    // Biến static để dễ dàng truy cập nhân vật của chính người chơi trên máy này
    public static NetworkPlayer Local { get; set; }

    [Networked]
    [OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int NetworkedCharacterIndex { get; set; } = -1;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Local = this;
            Debug.Log("Local NetworkPlayer đã được Spawn!");

            // 1. Gán Camera cho nhân vật local
            var camFollow = FindFirstObjectByType<CameraFollow>();
            if (camFollow != null)
            {
                var ragdoll = GetComponent<ActiveRagdollController>();
                if (ragdoll != null && ragdoll.realHip != null)
                    camFollow.target = ragdoll.realHip;
                else
                    camFollow.target = this.transform;

                camFollow._input = GetComponent<CharacterInput>();
            }

            // 2. Gán HUD cho nhân vật local
            if (BattleHUDUI.Instance != null)
            {
                BattleHUDUI.Instance.BindPlayer(GetComponent<PlayerStats>(), GetComponent<PlayerCombat>());
            }

            // 3. Báo cáo nhân vật đã chọn cho Server
            int myIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
            NetworkedCharacterIndex = myIndex;
        }
        else
        {
            // Nếu người chơi khác (hoặc lúc mình join muộn) đã chọn xong nhân vật, apply mesh ngay
            if (NetworkedCharacterIndex != -1)
            {
                ApplyCharacterSkin();
            }
        }
    }
    
    public void OnCharacterIndexChanged()
    {
        ApplyCharacterSkin();
    }

    private void ApplyCharacterSkin()
    {
        if (CharacterSelectionManager.Instance != null && NetworkedCharacterIndex >= 0)
        {
            var dataArray = CharacterSelectionManager.Instance.characterDataArray;
            if (dataArray != null && NetworkedCharacterIndex < dataArray.Length)
            {
                var skinManager = GetComponent<CharacterSkinManager>();
                if (skinManager != null)
                {
                    skinManager.ApplyCharacter(dataArray[NetworkedCharacterIndex]);
                }
            }
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Nếu người chơi thoát khỏi phòng có ID khớp với chủ của nhân vật này
        if (player == Object.StateAuthority)
        {
            // Xóa nhân vật này khỏi server
            Runner.Despawn(Object);
        }
    }
}
