# 📝 Danh sách việc cần làm (To-do List) cho Active Ragdoll

Dưới đây là các hạng mục còn lại và các lưu ý vận hành quan trọng sau khi đã tinh chỉnh hệ thống.

---

## 1. Hệ thống Choáng & Mềm người (Stagger System)
*   **Mục tiêu:** Giúp nhân vật biết "ngã" khi bị đấm thay vì đứng thẳng tưng như tượng gỗ.
*   **Giải pháp:** 
    *   Thêm biến `staggerTimer` vào `ActiveRagdollController`.
    *   Trong hàm `ApplyDamage`, kích hoạt `staggerTimer` (khoảng 0.5s).
    *   Trong `FixedUpdate`, nếu đang bị choáng, hạ `muscleSpring` từ 15,000 xuống còn ~500.
    *   Thêm ngưỡng (Threshold): Chỉ bị choáng khi lực đấm (`force`) > một mức nhất định.

## 2. Animation Phản hồi (Hit Reactions)
*   **Mục tiêu:** Kết hợp giữa vật lý và Animation để có hình ảnh đẹp nhất.
*   **Giải pháp:** 
    *   Tạo các State `GetHit` ngắn trong Animator (chỉ cần gập người hoặc ngả đầu).
    *   Kích hoạt Trigger trong Animator cùng lúc với việc hạ `muscleSpring`.

## 3. Tối ưu hóa Thăng bằng (Balancer Tuning)
*   **Trạng thái:** Đang theo dõi.
*   **Lưu ý:** Nếu nhân vật quá cứng, hãy giảm `balanceSpring` trong Balancer xuống (~5,000 - 10,000).

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
