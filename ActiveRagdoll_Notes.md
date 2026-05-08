# 📝 Danh sách việc cần làm (To-do List) cho Active Ragdoll

Dưới đây là các hạng mục quan trọng cần tinh chỉnh để nhân vật Active Ragdoll có phản ứng vật lý tự nhiên như *Party Animals* hoặc *Gang Beasts*.

---

## 1. Hệ thống Choáng & Mềm người (Stagger System)
*   **Mục tiêu:** Giúp nhân vật biết "ngã" khi bị đấm thay vì đứng thẳng tưng như tượng gỗ.
*   **Giải pháp:** 
    *   Thêm biến `staggerTimer` vào `ActiveRagdollController`.
    *   Trong hàm `ApplyDamage`, kích hoạt `staggerTimer` (khoảng 0.5s).
    *   Trong `FixedUpdate`, nếu đang bị choáng, hạ `muscleSpring` từ 15,000 xuống còn ~500.
    *   Thêm ngưỡng (Threshold): Chỉ bị choáng khi lực đấm (`force`) > một mức nhất định.

## 2. Giải phóng "Xương ảo" (Animation Rig)
*   **Mục tiêu:** Làm cho bộ xương mục tiêu cũng biết nghiêng ngả theo lực tác động.
*   **Giải pháp:** 
    *   Thay vì ép cứng `TargetBone.rotation = AnimatorBone.rotation`, hãy sử dụng `Quaternion.Lerp` để có độ trễ tự nhiên.
    *   Thêm lực quán tính (Procedural Inertia): Khi nhân vật di chuyển hoặc bị đấm, hãy cộng thêm một góc nghiêng nhẹ vào xương ảo để nó không bị "đóng băng" ở tư thế Idle.

## 3. Tối ưu hóa Thăng bằng (Balancer Tuning)
*   **Mục tiêu:** Giảm độ "cứng nhắc" khi đứng thẳng.
*   **Giải pháp:** 
    *   Giảm `balanceSpring` từ mức cực cao (60,000) xuống mức vừa phải (~5,000 - 10,000).
    *   Thử nghiệm việc mở khóa `FreezeRotation` của `playerRb` khi nhân vật đang ở trạng thái di chuyển hoặc bị tác động mạnh.

## 4. Animation Phản hồi (Hit Reactions)
*   **Mục tiêu:** Kết hợp giữa vật lý và Animation để có hình ảnh đẹp nhất.
*   **Giải pháp:** 
    *   Tạo các State `GetHit` ngắn trong Animator (chỉ cần gập người hoặc ngả đầu).
    *   Kích hoạt Trigger trong Animator cùng lúc với việc hạ `muscleSpring`.

---
> **Ghi chú:** Ưu tiên thực hiện mục số 1 trước vì đây là thay đổi mang lại hiệu quả thị giác rõ rệt nhất khi chiến đấu.
