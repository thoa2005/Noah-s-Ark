# 📝 Danh sách việc cần làm (To-do List) cho Active Ragdoll

Dưới đây là các hạng mục còn lại và các lưu ý vận hành quan trọng sau khi đã tinh chỉnh hệ thống.

---

## 6. Tối ưu hóa & Cài đặt chuyên nghiệp (Optimization)

### 1. Độ phân giải & Tỉ lệ (Resolution & Aspect Ratio)
*   **Resolution:** TRÁNH kéo giãn hình ảnh. Sử dụng `Screen.resolutions` để lấy danh sách chuẩn.
*   **Display Mode:** Ưu tiên `FullScreenWindow` (Borderless) để Alt+Tab mượt.
*   **UI Scaling:** Dùng `Canvas Scaler` (Scale with Screen Size) + `Match = 0.5`.

### 2. Render Scale (Chất lượng hình ảnh 3D) - [GPU]
*   Tách biệt với Độ phân giải. Cho phép giảm xuống 0.7 - 0.8 để tăng FPS cực mạnh mà chữ (UI) vẫn nét căng ở 1080p.

### 3. Anti-aliasing (Khử răng cưa - MSAA) - [GPU]
*   **Tác dụng:** Giúp các cạnh nhân vật mượt mà, không bị nhấp nháy khi di chuyển.
*   **Mức độ:** Off (nhẹ), 2x, 4x (cân bằng), 8x (nặng).

### 4. Texture Quality (Chất lượng vân bề mặt) - [VRAM]
*   **Tác dụng:** Tiết kiệm bộ nhớ Card đồ họa.
*   **Mức độ:** Full (đẹp), Half (tiết kiệm), Quarter (máy yếu).

### 5. LOD Bias (Độ chi tiết theo khoảng cách) - [CPU & GPU]
*   **Tác dụng:** Giảm số lượng đa giác vẽ khi vật thể ở xa.
*   **Mức độ:** 0.1 - 0.5 (máy yếu), 2.0+ (máy mạnh).

### 6. Shadows (Bóng đổ) - [CỰC NẶNG GPU]
*   **Shadow Distance:** Khoảng cách vẽ bóng. Game Party chỉ cần 20m - 40m (Mặc định Unity là 150m - rất lãng phí).
*   **Shadow Resolution:** Độ phân giải bóng (Low, Medium, High).
*   **Shadow Cascades:** Chia lớp bóng. Máy yếu nên để `No Cascades` hoặc `2 Cascades`.

### 7. Post-Processing (Hậu kỳ) - [GPU]
*   **Ambient Occlusion (SSAO):** Đổ bóng góc tường. Cực nặng cho GPU, nên có nút Bật/Tắt.
*   **Bloom:** Hiệu ứng hào quang/ánh sáng rực. Nên có nút Bật/Tắt.
*   **Motion Blur:** Làm mờ chuyển động. Phụ thuộc sở thích người chơi.

### 8. Occlusion Culling (Ẩn vật thể bị che) - [CPU & GPU]
*   **Cơ chế:** Không vẽ những gì bị che khuất hoàn toàn (sau bức tường, dưới sàn).
*   **Thực hiện:** Phải Bake trong cửa sổ `Window > Rendering > Occlusion Culling`.

### 9. Frame Rate Control (Kiểm soát khung hình)
*   **V-Sync:** Chống xé hình.
*   **Target FPS:** Khóa ở 60/120 FPS để tránh nóng máy, tốn điện vô ích.

## 7. Giai đoạn tiếp theo (Roadmap)
- [ ] Triển khai Menu cài đặt (Độ phân giải, Âm lượng).
- [ ] Thay đổi Shader cho toàn bộ Model sang Simple Lit/Unlit.
- [ ] Tối ưu hóa hệ thống vật lý (Physics Layers).
- [ ] Tích hợp Âm thanh (Impact SFX, Background Music).

---

## 4. Nhật ký các lỗi đã Fixed (QUAN TRỌNG)

### ✅ Lỗi Tự động Grab (Auto-Grab Bug)
*   **Nguyên nhân:** Kẹt phím ảo trong Input System khi mất focus hoặc sau khi ném đồ.
*   **Cách chúng ta đã sửa:**
    1.  Thêm `OnApplicationFocus` để Reset toàn bộ phím khi Tab-out.
    2.  Ép `isGrabPressed = false` ngay sau lệnh `PerformThrow()` và `ReleaseGrab()`.
    3.  Thêm **Grab Cooldown (0.2s)** trong `PlayerCombat` để ngăn việc spam tìm đồ vật 60 lần/giây.

### ✅ Animation Rig "Cứng nhắc"
*   **Cách chúng ta đã sửa:**
    1.  Sử dụng `Bone Lerp Speed` (30-50) để tạo độ dẻo cho xương vật lý.
    2.  Thêm **Procedural Leaning**: Tự động nghiêng cột sống theo hướng di chuyển.

### ✅ Sát thương bị nhân bản (Double Damage)
*   **Cách chúng ta đã sửa:** Chuyển từ Delay theo từng xương sang **Delay theo từng nhân vật** (Character-based delay).

---

## 5. Lưu ý về rủi ro và ổn định (Risk & Stability)
*   **Hệ thống Tag:** Logic tránh sát thương va chạm phụ thuộc hoàn toàn vào Tag `"Ground"`. 
    *   *Lưu ý:* Luôn gán Tag cho sàn nhà.
*   **Giao diện (UI):** Nút Reset Camera hiện đang dùng `OnGUI`. Nên chuyển về Canvas khi làm UI chính thức.
*   **Hiệu suất:** `lastHitTime` cần được `Clear()` khi đổi Scene để tránh rác bộ nhớ.
