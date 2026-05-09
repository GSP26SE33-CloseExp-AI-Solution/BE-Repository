# BÁO CÁO LOGIC XÁC LẬP THAM SỐ N (READY-TO-SHIP TTL)

## 1. Logic xác lập TTL (Implementation)

Processor `StaleReadyToShipRefundProcessor` (Quartz job) chạy mỗi 5 phút để:

1. Quét đơn `ReadyToShip` có `UpdatedAt < (Now - OrderReadyToShipMaxWaitMinutes)`
2. Tự động chuyển `OrderState.Refunded` kèm lý do "StaleReadyToShip"
3. Tạo `Refund` record với `RefundState.Pending` để xử lý hoàn tiền thủ công

Tham số TTL được đọc từ `SystemConfigKeys.OrderReadyToShipMaxWaitMinutes` (key `ORDER_READY_TO_SHIP_MAX_WAIT_MINUTES`) với cấu hình hiện tại là **90 phút**.

---

## 2. Chiến lược Bootstrap cho giai đoạn MVP (Safe-start)

Để tránh tình trạng hệ thống tự động Refund hàng loạt đơn hàng hợp lệ do thiếu dữ liệu vận hành thực tế (Cold Start), lộ trình triển khai được quy định như sau:

1. **Khởi tạo (N_init):** Thiết lập N = 90 phút (Lấy theo cận trên của N_safety). Việc bắt đầu với ngưỡng cao giúp hệ thống "dung sai" cho các biến số vận hành chưa ổn định trong tuần đầu.
2. **Giai đoạn Hiệu chuẩn (Calibration):** Sau 2–4 tuần chạy thực tế, hệ thống trích xuất log để tính toán P_95 thực tế.
3. **Siết chặt cấu hình:** Cập nhật lại N vào `SystemConfig` theo công thức tối ưu. Nếu P_95 + Buffer nhỏ hơn N_init, hệ thống sẽ hạ N xuống để bảo vệ tối đa chất lượng thực phẩm.

---

## 3. Các thành phần minh chứng (Evidence)

| Thành phần | Cơ sở xác lập | Nguồn dẫn chứng / Trạng thái |
| :--- | :--- | :--- |
| **T_total** | 2-hour baseline | **USDA FSIS** - "Danger Zone". |
| **Pháp lý VN** | Luật gốc | **Luật ATTP 2010** & **Nghị định 15/2018/NĐ-CP**. |
| **P_95** | Methodology | **Beyer et al. (2016)**, *Site Reliability Engineering*. |
| **T_transit** | **Giả định mô hình** | Ước tính nội đô; cần xác thực qua dữ liệu vận hành. |
| **Buffer** | Kỹ thuật & Ops | Tần suất Quartz Job (5 phút) & SLA phản hồi mục tiêu. |

---

## 4. Ma trận đối soát rủi ro (Risk Matrix)

| Kịch bản (T_actual = P_95 + Buffer) | Trạng thái | Hành động hệ thống |
| :--- | :--- | :--- |
| T_actual < 0.8 · N_safety | **Lý tưởng** | Duy trì/Hạ N để tăng độ tươi ngon của sản phẩm. |
| 0.8 · N_safety ≤ T_actual < N_safety | **Rủi ro cao** | Kích hoạt cảnh báo mức 2; Rà soát bottleneck vận hành. |
| T_actual ≥ N_safety | **Bất khả thi** | **Dừng cấu hình:** Yêu cầu thay đổi mô hình kinh doanh hoặc phương thức vận chuyển. |

---

## 5. Tài liệu tham khảo (Citations)

* **Beyer, B., et al. (2016).** *Site Reliability Engineering: How Google Runs Production Systems*. O'Reilly Media.
* **U.S. Food and Drug Administration (2022).** *Food Code*. Section 3-501.19.
* **U.S. Department of Agriculture (FSIS).** *"Danger Zone" (40°F – 140°F)*.
* **Chính phủ Việt Nam (2018).** *Nghị định 15/2018/NĐ-CP* quy định chi tiết thi hành Luật An toàn thực phẩm.

---

## Ghi chú bổ sung

Việc tách bạch rõ ràng giữa **giả định** và **dữ liệu thực tế** trong bảng Evidence sẽ giúp nhóm bạn ghi điểm tuyệt đối về sự trung thực và tính khoa học trong nghiên cứu.

**Mô hình vận hành hiện tại (BE-CloseExp):**
- Quartz Job chạy 5 phút/lần (T_detect = 5 phút)
- `ORDER_READY_TO_SHIP_MAX_WAIT_MINUTES`: 90 phút
- Khi đơn quá hạn: Tự động chuyển `OrderState.Refunded` + Tạo `Refund` record `Pending`
- Phù hợp với mô hình **Pickup** (khách tự lấy) hoặc **Home Delivery** tùy cấu hình
