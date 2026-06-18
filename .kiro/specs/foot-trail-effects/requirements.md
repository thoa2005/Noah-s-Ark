# Requirements Document

## Introduction

Tính năng **Foot Trail Effects** thêm hiệu ứng khói/vệt (trail/smoke) ở chân nhân vật dưới dạng cosmetic có thể thay đổi được. Người chơi chọn một kiểu hiệu ứng trong menu tùy chỉnh nhân vật, và hiệu ứng đó tự động kích hoạt khi nhân vật di chuyển (đi bộ, chạy) và tắt khi đứng yên hoặc ở trên không. Project sử dụng Unity URP, phong cách stylized cartoon phù hợp cốt truyện Noah's Ark.

Tính năng mở rộng hệ thống customization hiện có (CharacterData / CharacterSkinManager) và nâng cấp RunDustVFX thành một hệ thống VFX dạng cosmetic plug-in.

---

## Glossary

- **Foot_Trail_System**: Hệ thống C# quản lý toàn bộ vòng đời hiệu ứng chân, bao gồm load, spawn, activate/deactivate.
- **Trail_Preset**: ScriptableObject mô tả một kiểu hiệu ứng chân (tên, Prefab, thumbnail). Ví dụ: "Rainbow Trail", "Star Dust", "Smoke Puff", "Sparkle Dust", "Bubble Pop", "Flower Petals", "Electric Sparks", "Snow Flurry", "Fire Embers", "Rainbow Bubbles".
- **Trail_Prefab**: Prefab Unity chứa một hoặc nhiều ParticleSystem tạo nên hiệu ứng chân tương ứng với Trail_Preset.
- **Foot_Anchor**: Transform gắn trên xương chân trái và chân phải của nhân vật, dùng làm điểm spawn hiệu ứng.
- **GroundDetect**: Component hiện có kiểm tra nhân vật có đang chạm đất không (field `isGrounded`).
- **PlayerMovement**: Component hiện có điều khiển di chuyển nhân vật qua Rigidbody.
- **CharacterData**: ScriptableObject hiện có lưu thông tin skin nhân vật (mesh, material).
- **Trail_Catalog**: ScriptableObject danh sách tất cả Trail_Preset có trong game.
- **Cosmetic_Menu**: Màn hình UI để người chơi xem và chọn Trail_Preset.
- **None_Preset**: Trail_Preset đặc biệt đại diện cho "không có hiệu ứng", tắt hoàn toàn Foot_Trail_System.

---

## Requirements

### Requirement 1: Quản lý vòng đời hiệu ứng chân

**User Story:** Là một lập trình viên, tôi muốn một component trung tâm quản lý spawn và deactivate Trail_Prefab, để code VFX không bị phân tán và dễ mở rộng.

#### Acceptance Criteria

1. THE Foot_Trail_System SHALL expose một public method `ApplyPreset(Trail_Preset preset)` để thay thế Trail_Prefab đang chạy bằng preset mới.
2. WHEN `ApplyPreset` được gọi với một Trail_Preset hợp lệ, THE Foot_Trail_System SHALL destroy Trail_Prefab cũ (nếu có) rồi instantiate Trail_Prefab mới gắn vào Foot_Anchor tương ứng.
3. WHEN `ApplyPreset` được gọi với None_Preset, THE Foot_Trail_System SHALL destroy toàn bộ Trail_Prefab đang active và dừng phát hiệu ứng.
4. IF Trail_Preset.trailPrefab là null, THEN THE Foot_Trail_System SHALL log một cảnh báo (warning) và giữ nguyên trạng thái hiện tại.
5. THE Foot_Trail_System SHALL expose một public property `CurrentPreset` trả về Trail_Preset đang được áp dụng.

---

### Requirement 2: Kích hoạt hiệu ứng theo trạng thái di chuyển

**User Story:** Là một game designer, tôi muốn hiệu ứng chỉ phát khi nhân vật đang chạy/đi bộ trên mặt đất, để hiệu ứng cảm giác tự nhiên và không lạm dụng.

#### Acceptance Criteria

1. WHILE nhân vật đang chạm đất (`GroundDetect.isGrounded == true`) VÀ tốc độ ngang của Rigidbody lớn hơn hoặc bằng `minSpeedThreshold`, THE Foot_Trail_System SHALL enable emission trên tất cả ParticleSystem của Trail_Prefab đang active.
2. WHEN tốc độ ngang của nhân vật giảm xuống dưới `minSpeedThreshold`, THE Foot_Trail_System SHALL disable emission trên tất cả ParticleSystem của Trail_Prefab đang active trong vòng 1 frame.
3. WHEN `GroundDetect.isGrounded` chuyển sang `false`, THE Foot_Trail_System SHALL disable emission ngay lập tức trên tất cả ParticleSystem của Trail_Prefab đang active.
4. THE Foot_Trail_System SHALL đọc `minSpeedThreshold` từ một serializable field với giá trị mặc định là `0.5f` (m/s).
5. WHERE tốc độ ngang của Rigidbody vượt quá `runSpeedThreshold`, THE Foot_Trail_System SHALL tăng `rateOverDistanceMultiplier` của ParticleSystem lên gấp đôi so với giá trị đi bộ để tạo hiệu ứng chạy mạnh hơn.

---

### Requirement 3: Dữ liệu Trail_Preset dạng ScriptableObject

**User Story:** Là một artist/designer, tôi muốn định nghĩa kiểu hiệu ứng chân bằng ScriptableObject trong Unity Editor, để có thể tạo thêm preset mới mà không cần sửa code.

#### Acceptance Criteria

1. THE Trail_Preset SHALL là một ScriptableObject với các field: `string presetName`, `Sprite thumbnail`, `GameObject trailPrefab`.
2. THE Trail_Preset SHALL được tạo từ menu `Create > Noah's Ark > Trail Preset` trong Unity Editor.
3. THE Trail_Catalog SHALL là một ScriptableObject chứa `List<Trail_Preset> presets` và một field `Trail_Preset nonePreset` để chỉ định None_Preset.
4. THE Trail_Catalog SHALL được tạo từ menu `Create > Noah's Ark > Trail Catalog` trong Unity Editor.
5. IF `Trail_Catalog.presets` chứa phần tử null, THEN THE Foot_Trail_System SHALL bỏ qua phần tử đó khi hiển thị danh sách và log một cảnh báo.
6. THE Trail_Catalog SHALL bao gồm các preset ví dụ dựng sẵn sau đây để artist/designer tham khảo và tạo nhanh:
   - **Rainbow Trail**: vệt cầu vồng 7 màu kéo dài theo bước chân.
   - **Star Dust**: vệt ngôi sao nhỏ lấp lánh rơi xuống.
   - **Smoke Puff**: khói trắng bồng bềnh bay lên theo từng bước.
   - **Sparkle Dust**: bụi ánh kim nhấp nháy phát sáng.
   - **Bubble Pop**: bong bóng xà phòng nổi lên và vỡ.
   - **Flower Petals**: cánh hoa nhỏ rơi theo từng bước chân.
   - **Electric Sparks**: tia điện nhỏ bắn ra xung quanh.
   - **Snow Flurry**: bông tuyết nhỏ bay ra và tan dần.
   - **Fire Embers**: tàn lửa nhỏ bốc lên và mờ dần.
   - **Rainbow Bubbles**: bong bóng nhiều màu cầu vồng nổi lên và vỡ.

---

### Requirement 4: Giao diện người dùng chọn hiệu ứng (Cosmetic_Menu)

**User Story:** Là một người chơi, tôi muốn xem thumbnail và tên của từng hiệu ứng chân rồi chọn cái tôi thích, để nhân vật của tôi trông thú vị hơn.

#### Acceptance Criteria

1. THE Cosmetic_Menu SHALL hiển thị tất cả Trail_Preset trong Trail_Catalog theo dạng lưới (grid), mỗi ô chứa thumbnail và tên preset.
2. WHEN người chơi click vào một ô preset trong Cosmetic_Menu, THE Cosmetic_Menu SHALL gọi `Foot_Trail_System.ApplyPreset` với preset tương ứng.
3. THE Cosmetic_Menu SHALL luôn hiển thị ô None_Preset đầu tiên trong danh sách với nhãn "None" để người chơi có thể tắt hiệu ứng.
4. WHEN `Foot_Trail_System.CurrentPreset` thay đổi, THE Cosmetic_Menu SHALL cập nhật trạng thái visual (highlight border) của ô đang được chọn trong cùng frame.
5. IF Trail_Catalog chưa được gán vào Cosmetic_Menu, THEN THE Cosmetic_Menu SHALL log lỗi và không hiển thị danh sách.

---

### Requirement 5: Lưu và khôi phục lựa chọn hiệu ứng

**User Story:** Là một người chơi, tôi muốn lựa chọn hiệu ứng chân được nhớ qua các phiên chơi, để tôi không phải chọn lại mỗi lần vào game.

#### Acceptance Criteria

1. WHEN người chơi chọn một Trail_Preset trong Cosmetic_Menu, THE Foot_Trail_System SHALL lưu `Trail_Preset.presetName` vào `PlayerPrefs` với key `"FootTrailPreset"`.
2. WHEN game khởi động và Foot_Trail_System được khởi tạo, THE Foot_Trail_System SHALL đọc key `"FootTrailPreset"` từ PlayerPrefs và tìm Trail_Preset khớp trong Trail_Catalog.
3. IF key `"FootTrailPreset"` không tồn tại hoặc không tìm thấy preset khớp, THE Foot_Trail_System SHALL áp dụng None_Preset làm mặc định.
4. WHEN người chơi chọn None_Preset, THE Foot_Trail_System SHALL lưu giá trị `"None"` vào `PlayerPrefs` với key `"FootTrailPreset"`.

---

### Requirement 6: Tích hợp với hệ thống Customization hiện có

**User Story:** Là một lập trình viên, tôi muốn Foot_Trail_System hoạt động song song với CharacterSkinManager mà không gây xung đột, để hệ thống skin và hiệu ứng chân hoạt động độc lập.

#### Acceptance Criteria

1. THE Foot_Trail_System SHALL là một MonoBehaviour riêng biệt không phụ thuộc vào CharacterSkinManager.
2. WHEN CharacterSkinManager gọi `ApplyCharacter`, THE Foot_Trail_System SHALL không bị ảnh hưởng và giữ nguyên Trail_Preset đang chạy.
3. THE Foot_Trail_System SHALL hỗ trợ tối thiểu 2 Foot_Anchor (chân trái, chân phải) được assign qua Inspector.
4. IF một Foot_Anchor bị null, THEN THE Foot_Trail_System SHALL chỉ spawn Trail_Prefab lên Foot_Anchor còn lại và log cảnh báo.

---

### Requirement 7: Hiệu suất và tương thích Unity URP

**User Story:** Là một lập trình viên, tôi muốn hiệu ứng chân không gây frame drop trên thiết bị mục tiêu, để trải nghiệm chơi game không bị ảnh hưởng.

#### Acceptance Criteria

1. THE Trail_Prefab SHALL sử dụng Material dùng URP Particle Shader (Universal Render Pipeline/Particles/Unlit hoặc tương đương) để hiển thị đúng trong URP pipeline.
2. THE Foot_Trail_System SHALL không gọi `Instantiate` hoặc `Destroy` trong mỗi frame Update — chỉ gọi khi `ApplyPreset` được gọi.
3. WHILE không có Trail_Preset nào đang active (None_Preset), THE Foot_Trail_System SHALL không thực hiện bất kỳ tính toán ParticleSystem nào trong Update.
4. THE Trail_Prefab SHALL giới hạn `maxParticles` ở mức không vượt quá 200 particles trên mỗi ParticleSystem để tránh overdraw quá mức.
