# AI-powered Near-Expiry Food Trading Platform

## 1. Giới thiệu
Lãng phí thực phẩm, đặc biệt là các sản phẩm cận hạn sử dụng, đang là một vấn đề nghiêm trọng tại Việt Nam. Nhiều sản phẩm vẫn còn an toàn để sử dụng nhưng không thể bán theo kênh truyền thống, dẫn đến thất thoát kinh tế và ảnh hưởng môi trường.

Đồ án này đề xuất và xây dựng một **nền tảng ứng dụng AI** nhằm kết nối các **siêu thị** với **người bán thực phẩm (food vendors)** để tiêu thụ hiệu quả các sản phẩm cận hạn, góp phần giảm lãng phí và tối ưu chuỗi cung ứng.

---

## 2. Mục tiêu của đồ án
- Xây dựng hệ thống quản lý và phân phối sản phẩm cận hạn sử dụng.
- Ứng dụng AI để:
  - Tự động trích xuất thông tin hạn sử dụng từ hình ảnh sản phẩm.
  - Gợi ý mức giá bán phù hợp dựa trên thời gian còn lại của hạn sử dụng.
- Thiết kế hệ thống theo **Clean Architecture**, dễ bảo trì và mở rộng.
- Đảm bảo hệ thống hoạt động ổn định, bảo mật và đáp ứng yêu cầu thực tế.

---

## 3. Phạm vi hệ thống
### 3.1 Đối tượng sử dụng
- **Admin**: Quản lý hệ thống, người dùng, giao dịch và thống kê.
- **Nhân viên siêu thị**: Đăng tải và quản lý sản phẩm cận hạn.
- **Nhân viên đóng gói nội bộ**: Thu gom, đóng gói và chuẩn bị đơn hàng.
- **Nhân viên giao hàng**: Giao đơn hàng đến điểm nhận hoặc tận nhà.
- **Nhân viên Marketing**: Quản lý chương trình khuyến mãi (chức năng mở rộng).
- **Khách hàng (Food Vendor)**: Đặt hàng, theo dõi giao dịch và phản hồi dịch vụ.

---

## 4. Chức năng chính
### 4.1 Chức năng bắt buộc
- Đăng tải sản phẩm cận hạn sử dụng.
- AI OCR trích xuất thông tin:
  - Ngày sản xuất
  - Hạn sử dụng
- Hệ thống gợi ý giá bán dựa trên:
  - Loại sản phẩm
  - Số ngày còn lại của hạn sử dụng
- Đặt hàng theo khung giờ cố định.
- Quản lý quy trình:
  - Xác nhận đơn hàng
  - Đóng gói
  - Giao hàng
- Theo dõi trạng thái đơn hàng.

### 4.2 Chức năng mở rộng
- Quản lý chiến dịch marketing và chương trình khách hàng thân thiết.
- Thống kê và phân tích dữ liệu nâng cao.
- So sánh giá sản phẩm giữa các siêu thị.

---

## 5. Ứng dụng AI trong hệ thống
### 5.1 AI trích xuất thông tin sản phẩm
- Sử dụng công nghệ **OCR (Optical Character Recognition)** để nhận diện văn bản từ hình ảnh sản phẩm.
- Trích xuất thông tin hạn sử dụng và ngày sản xuất.
- Công nghệ dự kiến:
  - Google Vision API / Azure OCR (hoặc tương đương)
- Nhân viên có thể chỉnh sửa thông tin nếu AI nhận diện sai.

### 5.2 AI gợi ý giá bán
- Áp dụng mô hình **Rule-based Pricing Recommendation**.
- Giá bán được đề xuất dựa trên:
  - Giá gốc
  - Số ngày còn lại của hạn sử dụng
  - Loại sản phẩm
- Mô hình đảm bảo dễ giải thích và phù hợp phạm vi đồ án.

---

## 6. Kiến trúc hệ thống
- Áp dụng **Kiến trúc phân lớp (Layered Architecture)**:
  - Presentation Layer
  - Application Layer
  - Domain Layer
  - Infrastructure Layer
- Tuân thủ nguyên tắc **Clean Architecture**.
- Giao tiếp giữa các thành phần thông qua REST API.

---

## 7. Công nghệ sử dụng
### Backend
- Ngôn ngữ: **.NET / Java**
- RESTful API
- Repository Pattern

### Frontend
- Web: **ReactJS**, Material UI
- Mobile (nhân viên giao hàng): Mobile App

### Cơ sở dữ liệu
- SQL Server / Firebase hoặc tương đương

### Công cụ khác
- JWT Authentication
- API Documentation (Swagger/OpenAPI)

---

## 8. Yêu cầu phi chức năng
- **Hiệu năng**: Thời gian tải trang dashboard ≤ 5 giây.
- **Bảo mật**:
  - Khóa tài khoản sau 5 lần đăng nhập sai.
  - Mở khóa bằng OTP hoặc email xác nhận.
- **Độ tin cậy**:
  - Giao dịch sử dụng transaction để đảm bảo toàn vẹn dữ liệu.
  - Ghi log các thao tác quan trọng.
- **Tính khả dụng**:
  - Thông báo hệ thống được gửi trong vòng ≤ 3 giây sau khi sự kiện xảy ra.
- **Khả năng bảo trì**:
  - Mã nguồn có chú thích rõ ràng.
  - Tài liệu API đầy đủ.

---

## 9. Kế hoạch phát triển
Thời gian thực hiện: **5 tháng**
- Tháng 1: Phân tích yêu cầu, thiết kế hệ thống.
- Tháng 2: Thiết kế cơ sở dữ liệu, xây dựng backend.
- Tháng 3: Phát triển frontend web và tích hợp backend.
- Tháng 4: Tích hợp AI OCR và gợi ý giá.
- Tháng 5: Kiểm thử, hoàn thiện tài liệu và chuẩn bị bảo vệ.

---

## 10. Tài liệu bàn giao
- SRS (Software Requirements Specification)
- Thiết kế kiến trúc hệ thống
- Thiết kế chi tiết
- Kế hoạch và báo cáo kiểm thử
- Hướng dẫn cài đặt và triển khai
- Mã nguồn và gói phần mềm triển khai

---

## 11. Kết luận
Đồ án hướng tới giải quyết một vấn đề thực tế tại Việt Nam thông qua việc ứng dụng công nghệ AI và kiến trúc phần mềm hiện đại. Hệ thống không chỉ mang ý nghĩa học thuật mà còn có tiềm năng phát triển thành sản phẩm thực tế trong tương lai.
