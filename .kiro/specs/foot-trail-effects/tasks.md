# Implementation Plan: Foot Trail Effects

## Overview

Triển khai hệ thống cosmetic plug-in cho phép người chơi chọn hiệu ứng vệt chân (particle trail) hiển thị khi nhân vật di chuyển. Hệ thống gồm 4 C# scripts mới (TrailPreset, TrailCatalog, FootTrailSystem, CosmeticMenuController + TrailPresetCell), 10 Trail Prefab assets, ScriptableObject assets, và UI prefab — tất cả tích hợp vào Player GameObject hiện có mà không gây coupling với CharacterSkinManager.

## Tasks

- [x] 1. Tạo ScriptableObject data layer (TrailPreset & TrailCatalog)
  - [x] 1.1 Implement `TrailPreset.cs`
    - Tạo file `Assets/Scripts/VFX/FootTrail/TrailPreset.cs`
    - Khai báo `[CreateAssetMenu(menuName = "Noah's Ark/Trail Preset")]` trên class kế thừa `ScriptableObject`
    - Thêm các serialized fields: `string presetName`, `Sprite thumbnail`, `GameObject trailPrefab`
    - Expose public properties `PresetName`, `Thumbnail`, `TrailPrefab`
    - _Requirements: 3.1, 3.2_

  - [ ]* 1.2 Write smoke test cho TrailPreset CreateAssetMenu attribute
    - Verify `TrailPreset` có `[CreateAssetMenu]` với đúng `menuName = "Noah's Ark/Trail Preset"`
    - _Requirements: 3.2_

  - [x] 1.3 Implement `TrailCatalog.cs`
    - Tạo file `Assets/Scripts/VFX/FootTrail/TrailCatalog.cs`
    - Khai báo `[CreateAssetMenu(menuName = "Noah's Ark/Trail Catalog")]`
    - Thêm serialized fields: `TrailPreset nonePreset`, `List<TrailPreset> presets`
    - Implement `GetValidPresets()`: prepend `nonePreset`, bỏ qua null entries và `Debug.LogWarning` cho mỗi null entry
    - Expose public property `NonePreset`
    - _Requirements: 3.3, 3.4, 3.5_

  - [ ]* 1.4 Write property test cho TrailCatalog null filtering (Property 6)
    - **Property 6: TrailCatalog lọc null entries**
    - **Validates: Requirements 3.5**
    - Generate catalog với random mix N valid presets + M null entries
    - Assert `GetValidPresets()` trả về N+1 entries (không có null nào), repeat 100+ lần
    - _// Feature: foot-trail-effects, Property 6_

  - [ ]* 1.5 Write smoke test cho TrailCatalog CreateAssetMenu attribute
    - Verify `TrailCatalog` có `[CreateAssetMenu]` với đúng `menuName = "Noah's Ark/Trail Catalog"`
    - _Requirements: 3.4_

- [x] 2. Implement `FootTrailSystem.cs` — core logic
  - [x] 2.1 Scaffold FootTrailSystem và dependency resolution
    - Tạo file `Assets/Scripts/VFX/FootTrail/FootTrailSystem.cs`
    - Khai báo class `FootTrailSystem : MonoBehaviour`
    - Thêm serialized fields theo design: `catalog`, `leftFootAnchor`, `rightFootAnchor`, `minSpeedThreshold = 0.5f`, `runSpeedThreshold = 4.0f`, `walkEmissionRate = 1.0f`, `runEmissionRate = 2.0f`
    - Implement `Awake()`: `GetComponentInParent<Rigidbody>()` và `GetComponentInParent<GroundDetect>()`; `Debug.LogError` + `enabled = false` nếu catalog hoặc `_rb` null; `Debug.LogWarning` nếu `_groundDetect` null
    - Expose public property `CurrentPreset`
    - _Requirements: 1.5, 6.1, 7.3_

  - [x] 2.2 Implement `ApplyPreset()` và instance management
    - Implement private `DestroyTrailInstances()`: Destroy `_leftInstance` và `_rightInstance` nếu not null
    - Implement private `SpawnTrailInstances(TrailPreset preset)`: Instantiate prefab vào `leftFootAnchor` và `rightFootAnchor`; nếu anchor null thì log warning và bỏ qua anchor đó; cache instances
    - Implement private `CacheEmissions()`: lấy tất cả `ParticleSystem` từ instance, cache `EmissionModule` array
    - Implement public `ApplyPreset(TrailPreset preset)`:
      - Nếu `preset.trailPrefab == null` và `preset != nonePreset`: `Debug.LogWarning`, return (giữ nguyên state)
      - Nếu `preset == nonePreset`: `DestroyTrailInstances()`, set `_currentPreset = nonePreset`, return
      - Destroy instances cũ → Spawn instances mới → Cache emissions → `_currentPreset = preset`
    - Implement public `SaveAndApplyPreset(TrailPreset preset)`: gọi `ApplyPreset` rồi lưu `PlayerPrefs.SetString("FootTrailPreset", preset.presetName)` (dùng `"None"` cho nonePreset)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 5.1, 5.4, 6.3, 6.4, 7.2_

  - [ ]* 2.3 Write property test cho ApplyPreset cập nhật CurrentPreset (Property 1)
    - **Property 1: ApplyPreset cập nhật CurrentPreset**
    - **Validates: Requirements 1.2, 1.5**
    - Generate random valid preset từ catalog; gọi `ApplyPreset`; assert `CurrentPreset == preset`; repeat 100+ lần
    - _// Feature: foot-trail-effects, Property 1_

  - [ ]* 2.4 Write property test cho ApplyPreset(None) xóa instances (Property 2)
    - **Property 2: ApplyPreset(None) xóa toàn bộ instances**
    - **Validates: Requirements 1.3**
    - Với mọi preset đang active, gọi `ApplyPreset(nonePreset)`; assert không còn Trail Prefab instance nào dưới Foot Anchors
    - _// Feature: foot-trail-effects, Property 2_

  - [ ]* 2.5 Write property test cho preset trailPrefab null không thay đổi state (Property 3)
    - **Property 3: Preset với trailPrefab null không thay đổi state**
    - **Validates: Requirements 1.4**
    - Apply valid preset trước; tạo fake preset với `trailPrefab = null`; gọi `ApplyPreset(fakePreset)`; assert `CurrentPreset` không đổi
    - _// Feature: foot-trail-effects, Property 3_

  - [ ]* 2.6 Write unit test cho null anchor và default threshold
    - **FootTrailSystem_NullAnchor_SpawnsOnRemainingAnchor**: verify chỉ 1 instance spawn khi 1 anchor null
    - **DefaultThreshold_Is0Point5**: verify `minSpeedThreshold == 0.5f`
    - _Requirements: 2.4, 6.4_

  - [x] 2.7 Implement `Update()` — emission control theo movement state
    - Implement private `SetEmission(bool enabled, float rate)`: duyệt qua `_emissionsLeft` và `_emissionsRight`, set `enabled` và `rateOverDistanceMultiplier`
    - Implement `Update()`:
      - Early-exit nếu `_currentPreset == nonePreset || (_leftInstance == null && _rightInstance == null)`
      - Tính `horizontalSpeed = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).magnitude`
      - `shouldEmit = _groundDetect.isGrounded && horizontalSpeed >= minSpeedThreshold` (fallback `true` nếu `_groundDetect == null`)
      - `rate = horizontalSpeed >= runSpeedThreshold ? runEmissionRate : walkEmissionRate`
      - Gọi `SetEmission(shouldEmit, rate)`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 7.2, 7.3_

  - [ ]* 2.8 Write property test cho emission bật khi và chỉ khi đủ điều kiện (Property 4)
    - **Property 4: Emission bật khi và chỉ khi đủ điều kiện**
    - **Validates: Requirements 2.1, 2.2, 2.3**
    - Generate random cặp `(horizontalSpeed, isGrounded)`; mock Rigidbody + GroundDetect; tick Update; assert `emission.enabled == (isGrounded && speed >= minSpeedThreshold)`; repeat 100+ lần
    - _// Feature: foot-trail-effects, Property 4_

  - [ ]* 2.9 Write property test cho rateOverDistanceMultiplier scale theo tốc độ (Property 5)
    - **Property 5: rateOverDistanceMultiplier scale theo tốc độ**
    - **Validates: Requirements 2.5**
    - Generate random speed > `runSpeedThreshold`; assert `rateOverDistanceMultiplier == runEmissionRate`
    - Generate random speed trong `[minSpeedThreshold, runSpeedThreshold)`; assert `rateOverDistanceMultiplier == walkEmissionRate`
    - _// Feature: foot-trail-effects, Property 5_

  - [x] 2.10 Implement `LoadSavedPreset()` và startup persistence
    - Implement `LoadSavedPreset()`:
      - Đọc `PlayerPrefs.GetString("FootTrailPreset", "None")`
      - Tìm preset trong `catalog.GetValidPresets()` có `presetName == savedName`
      - Nếu không tìm thấy: `Debug.LogWarning`, apply `nonePreset`
      - Nếu tìm thấy: gọi `ApplyPreset(foundPreset)`
    - Gọi `LoadSavedPreset()` trong `Start()`
    - _Requirements: 5.2, 5.3_

  - [ ]* 2.11 Write property test cho preset persistence round-trip (Property 8)
    - **Property 8: Preset persistence round-trip**
    - **Validates: Requirements 5.1, 5.2**
    - Generate random preset p; `ApplyPreset(p)` → lưu PlayerPrefs; khởi tạo FootTrailSystem mới, load PlayerPrefs; assert `CurrentPreset.presetName == p.presetName`; repeat 100+ lần
    - _// Feature: foot-trail-effects, Property 8_

  - [ ]* 2.12 Write unit test cho ApplyPreset_DefaultsToNoneOnStart
    - Verify `CurrentPreset == nonePreset` khi không có PlayerPrefs key
    - _Requirements: 5.3_

- [x] 3. Checkpoint — Core system verification
  - Ensure tất cả unit tests và property tests của TrailPreset, TrailCatalog, FootTrailSystem đều pass. Hỏi nếu có vấn đề.

- [x] 4. Implement UI layer (TrailPresetCell & CosmeticMenuController)
  - [x] 4.1 Implement `TrailPresetCell.cs`
    - Tạo file `Assets/UI/Scripts/Views/TrailPresetCell.cs`
    - Thêm serialized fields: `Image thumbnailImage`, `TMP_Text nameLabel`, `GameObject highlightBorder`, `Button button`
    - Expose public property `Preset`
    - Implement `Initialize(TrailPreset preset, CosmeticMenuController controller)`: set thumbnail, name label, đăng ký `button.onClick` → gọi `controller.OnCellClicked(preset)`
    - Implement `SetHighlight(bool active)`: bật/tắt `highlightBorder`
    - _Requirements: 4.1, 4.4_

  - [x] 4.2 Implement `CosmeticMenuController.cs`
    - Tạo file `Assets/UI/Scripts/Views/CosmeticMenuController.cs`
    - Thêm serialized fields: `TrailCatalog catalog`, `FootTrailSystem footTrailSystem`, `Transform gridContainer`, `TrailPresetCell cellPrefab`
    - Implement private `BuildGrid()`: instantiate `TrailPresetCell` cho từng preset trong `catalog.GetValidPresets()`; `Debug.LogError` và early return nếu catalog/footTrailSystem/cellPrefab null
    - Implement public `RefreshHighlight()`: duyệt `_cells`, gọi `SetHighlight(cell.Preset == footTrailSystem.CurrentPreset)`
    - Implement public `OnCellClicked(TrailPreset preset)`: gọi `footTrailSystem.SaveAndApplyPreset(preset)` rồi `RefreshHighlight()`
    - Implement `Start()`: validate dependencies, gọi `BuildGrid()`
    - Implement `OnEnable()`: gọi `RefreshHighlight()`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [ ]* 4.3 Write property test cho grid cell count và highlight (Property 7)
    - **Property 7: Grid UI phản ánh catalog và highlight đúng**
    - **Validates: Requirements 4.1, 4.4**
    - Build CosmeticMenu với catalog N valid presets; assert `gridContainer.childCount == N + 1`
    - Sau `CurrentPreset` thay đổi, assert đúng 1 cell có `highlightBorder` active
    - _// Feature: foot-trail-effects, Property 7_

  - [ ]* 4.4 Write unit tests cho CosmeticMenu edge cases
    - **CosmeticMenu_FirstCellIsNone**: verify cell đầu tiên là None_Preset
    - **PlayerPrefs_NonePreset_SavesStringNone**: verify PlayerPrefs value = `"None"` khi chọn nonePreset
    - **CosmeticMenu_NullCatalog_LogsError**: verify LogError khi catalog null
    - _Requirements: 4.3, 4.5, 5.4_

- [x] 5. Tạo ScriptableObject assets và Trail Prefabs
  - [x] 5.1 Tạo "None" TrailPreset asset
    - Tạo ScriptableObject asset `Assets/VFX/FootTrail/Presets/None.asset` qua menu `Noah's Ark/Trail Preset`
    - Set `presetName = "None"`, `thumbnail = null`, `trailPrefab = null`
    - _Requirements: 3.1, 3.3_

  - [x] 5.2 Tạo 10 Trail Prefab Particle System assets
    - Tạo thư mục `Assets/VFX/FootTrail/Prefabs/`
    - Tạo 10 Prefab, mỗi prefab chứa `ParticleSystem` theo cấu hình trong design (simulation space: World, max particles ≤ 200, URP Particles/Unlit material):
      - `RainbowTrail.prefab` — Stretched Billboard, gradient 7 màu, rate over distance 8
      - `StarDust.prefab` — Gravity 0.3, vàng #FFE066, rate 6
      - `SmokePuff.prefab` — size grows over lifetime, trắng → xám, rate 4
      - `SparkleDust.prefab` — Additive, HDR bloom, rate 10
      - `BubblePop.prefab` — Sub emitter burst khi chết, alpha 0.6, rate 5
      - `FlowerPetals.prefab` — Angular velocity, rotation random, gravity 0.2, rate 4
      - `ElectricSparks.prefab` — Stretched Billboard length 5, Trail module, Additive, rate 12
      - `SnowFlurry.prefab` — Gravity −0.1, snowflake texture, rate 6
      - `FireEmbers.prefab` — Gravity −0.5, Additive, Noise module, rate 8
      - `RainbowBubbles.prefab` — Sub emitter rainbow burst, 7 colors, rate 5
    - _Requirements: 3.6, 7.1, 7.4_

  - [x] 5.3 Tạo 10 TrailPreset ScriptableObject assets và TrailCatalog asset
    - Tạo 10 TrailPreset asset trong `Assets/VFX/FootTrail/Presets/`, gán `trailPrefab` tương ứng và `thumbnail` sprite
    - Tạo `TrailCatalog` asset `Assets/VFX/FootTrail/TrailCatalog.asset`, gán `nonePreset` và danh sách 10 presets
    - _Requirements: 3.3, 3.4, 3.6_

  - [ ]* 5.4 Write smoke tests cho Trail Prefab assets
    - Verify tất cả 10 TrailPreset assets tồn tại trong project
    - Verify tất cả Trail Prefab materials dùng shader path chứa `"Particles"` + `"URP"` hoặc `"Universal"`
    - Verify mỗi TrailPrefab ParticleSystem có `main.maxParticles <= 200`
    - _Requirements: 7.1, 7.4_

- [x] 6. Tạo UI Prefab cho Cosmetic Menu
  - [x] 6.1 Tạo `TrailPresetCell` UI prefab
    - Tạo prefab `Assets/UI/Prefabs/TrailPresetCell.prefab`
    - Hierarchy: root `Button` → children: `Image` (thumbnail 80×80), `TMP_Text` (name label), `GameObject` (highlight border với `Outline` hoặc colored panel, màu `#FFD700`)
    - Kích thước cell: 100×120 px theo design
    - Attach `TrailPresetCell.cs`, wire serialized fields
    - _Requirements: 4.1, 4.4_

  - [x] 6.2 Tạo `CosmeticMenu` UI prefab
    - Tạo prefab `Assets/UI/Prefabs/CosmeticMenu.prefab`
    - Hierarchy: `Panel (CosmeticMenu)` → `Header (TitleText "EFFECTS" + CloseButton)`, `SectionLabel "Foot Trail"`, `FootTrailGrid (GridLayoutGroup)`
    - GridLayoutGroup settings: Cell Size 100×120, Spacing 8×8, Fixed Column Count 4, Upper Left
    - Attach `CosmeticMenuController.cs`, wire `gridContainer` → FootTrailGrid, gán `TrailPresetCell` prefab
    - _Requirements: 4.1, 4.3_

- [x] 7. Tích hợp vào Player prefab và Scene wiring
  - [x] 7.1 Gắn FootTrailSystem lên Player prefab
    - Mở Player prefab trong Unity Editor
    - Thêm `FootTrailSystem` component vào Player root GameObject
    - Gán `catalog` → TrailCatalog asset
    - Assign `leftFootAnchor` và `rightFootAnchor` từ skeleton hierarchy (LeftFoot, RightFoot bones)
    - Xác nhận `Rigidbody` và `GroundDetect` tồn tại trên Player root (FootTrailSystem tự resolve qua `GetComponentInParent`)
    - _Requirements: 6.1, 6.3_

  - [x] 7.2 Gắn CosmeticMenu vào UI Canvas trong scene
    - Thêm `CosmeticMenu` prefab vào Canvas (Screen Space - Overlay)
    - Wire `FootTrailSystem` reference trong `CosmeticMenuController`
    - Wire `catalog` reference
    - Test CloseButton ẩn/hiện panel
    - _Requirements: 4.1, 4.2_

  - [ ]* 7.3 Write property test cho skin change isolation (Property 9)
    - **Property 9: Skin change không ảnh hưởng trail preset**
    - **Validates: Requirements 6.2**
    - Apply random trail preset p; gọi `CharacterSkinManager.ApplyCharacter(randomCharData)`; assert `FootTrailSystem.CurrentPreset == p` và trail instances vẫn active; repeat 100+ lần
    - _// Feature: foot-trail-effects, Property 9_

  - [ ]* 7.4 Write integration test cho end-to-end selection
    - Mở CosmeticMenu → click Rainbow Trail → verify `FootTrailSystem.CurrentPreset` updated → verify PlayerPrefs saved
    - Simulate restart → verify preset restored
    - _Requirements: 5.1, 5.2_

  - [ ]* 7.5 Write integration test cho runtime activation
    - Spawn Player; set `velocity.magnitude > minSpeedThreshold`; set `isGrounded = true`; tick 1 frame; verify particles emitting
    - _Requirements: 2.1_

- [x] 8. Final Checkpoint — Ensure all tests pass
  - Ensure tất cả tests pass (smoke, unit, property, integration). Hỏi nếu có vấn đề.

## Notes

- Tasks đánh dấu `*` là optional và có thể bỏ qua cho MVP nhanh hơn
- Mỗi task tham chiếu requirements cụ thể để traceability
- Prefab Trail phải dùng URP Particles/Unlit shader — không dùng Built-in renderer shader
- FootTrailSystem tự resolve Rigidbody/GroundDetect qua `GetComponentInParent` — không cần assign qua Inspector
- RunDustVFX có thể giữ nguyên tạm thời (Phương án B) hoặc xóa sau khi "Smoke Puff" preset hoạt động (Phương án A khuyến nghị)
- Property tests dùng NUnit (built-in Unity Test Framework); custom random generator nếu FsCheck không có sẵn

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.3"] },
    { "id": 1, "tasks": ["1.2", "1.4", "1.5", "2.1"] },
    { "id": 2, "tasks": ["2.2"] },
    { "id": 3, "tasks": ["2.3", "2.4", "2.5", "2.6", "2.7"] },
    { "id": 4, "tasks": ["2.8", "2.9", "2.10", "4.1"] },
    { "id": 5, "tasks": ["2.11", "2.12", "4.2", "5.1"] },
    { "id": 6, "tasks": ["4.3", "4.4", "5.2"] },
    { "id": 7, "tasks": ["5.3", "6.1"] },
    { "id": 8, "tasks": ["5.4", "6.2"] },
    { "id": 9, "tasks": ["7.1"] },
    { "id": 10, "tasks": ["7.2"] },
    { "id": 11, "tasks": ["7.3", "7.4", "7.5"] }
  ]
}
```
