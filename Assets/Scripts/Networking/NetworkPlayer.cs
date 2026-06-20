using Fusion;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    // Biến static để dễ dàng truy cập nhân vật của chính người chơi trên máy này
    public static NetworkPlayer Local { get; set; }
    // 1. Khai báo biến bones để lưu toàn bộ các đốt xương Ragdoll
    private Rigidbody[] bones;

    // Biến lưu gốc cha của mô hình vật lý (Ví dụ chiếc PhysicRig hoặc chiếc Hips)
    [SerializeField] private Transform ragdollRoot;
    [Networked]
    [OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int NetworkedCharacterIndex { get; set; } = -1;

    [Networked, Capacity(24)]
    [OnChangedRender(nameof(OnNameChanged))]
    public string PlayerName { get; set; }

    public override void Spawned()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(gameObject, gameObject.CompareTag("Bot"));
        }

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

            // Apply ngay skin cho chính mình (không đợi OnChanged vì có thể bị bỏ qua)
            ApplyCharacterSkin();

            // 4. Báo cáo tên nhân vật
            PlayerName = PlayerPrefs.GetString("PlayerName", "Player_" + Random.Range(1000, 9999));
            OnNameChanged();
        }
        else
        {
            // Nếu người chơi khác (hoặc lúc mình join muộn) đã chọn xong nhân vật, apply mesh ngay
            if (NetworkedCharacterIndex != -1)
            {
                ApplyCharacterSkin();
            }
            if (!string.IsNullOrEmpty(PlayerName))
            {
                OnNameChanged();
            }
            // 2. TỰ ĐỘNG QUÉT: Code tự tìm sạch toàn bộ Rigidbody nằm bên trong gốc cha Ragdoll
            if (ragdollRoot != null)
            {
                bones = ragdollRoot.GetComponentsInChildren<Rigidbody>();
            }
            else
            {
                // Nếu bạn không kéo thả ragdollRoot, code sẽ quét toàn bộ nhân vật
                bones = GetComponentsInChildren<Rigidbody>();
            }
            foreach (var bone in bones)
            {
                var rb = bone.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false; // Ép buộc bật lại vật lý tự do!
                    rb.useGravity = true;
                }
            }
        }
    }

    public void OnCharacterIndexChanged()
    {
        ApplyCharacterSkin();
    }

    public void OnNameChanged()
    {
        var nameTag = GetComponent<NameTag>();
        if (nameTag == null)
        {
            nameTag = gameObject.AddComponent<NameTag>();
            nameTag.nameColor = new Color(0.2f, 0.8f, 1f);
            nameTag.offset = new Vector3(0, 2.0f, 0);
        }

        if (!string.IsNullOrEmpty(PlayerName))
        {
            nameTag.SetDisplayName(PlayerName);
        }

        // Cập nhật lên HUD chính (góc trái màn hình) nếu đây là nhân vật của mình
        if (Object.HasStateAuthority && BattleHUDUI.Instance != null && !string.IsNullOrEmpty(PlayerName))
        {
            BattleHUDUI.Instance.UpdateHUDPlayerName(PlayerName);
        }
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
