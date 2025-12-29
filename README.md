# Backend – AI-powered Near-Expiry Food Trading Platform

## 1. Giới thiệu
Đây là **hệ thống Backend** cho đồ án tốt nghiệp  
**“Ứng dụng AI để phát triển nền tảng bán các sản phẩm cận hạn sử dụng”**.

Backend chịu trách nhiệm:
- Quản lý nghiệp vụ sản phẩm cận hạn
- Xử lý đơn hàng, đóng gói và giao hàng
- Tích hợp AI OCR để trích xuất hạn sử dụng
- Đề xuất giá bán dựa trên quy tắc
- Cung cấp RESTful API cho Web và Mobile App

---

## 2. Mục tiêu Backend
- Xây dựng hệ thống API ổn định, bảo mật và dễ mở rộng
- Áp dụng **Clean Architecture** và **Repository Pattern**
- Đảm bảo toàn vẹn dữ liệu giao dịch
- Tích hợp AI ở mức phù hợp với phạm vi đồ án tốt nghiệp

---

## 3. Phạm vi chức năng Backend

### 3.1 Quản lý người dùng & phân quyền
- Đăng nhập / đăng xuất
- Phân quyền theo vai trò:
  - Admin
  - Supermarket Staff
  - Internal Packaging Staff
  - Delivery Staff
  - Marketing Staff
  - Food Vendor
- Tự động khóa tài khoản sau 5 lần đăng nhập sai

---

### 3.2 Quản lý sản phẩm cận hạn
- Tạo và quản lý sản phẩm cận hạn
- Lưu trữ thông tin:
  - Tên sản phẩm
  - Ngày sản xuất
  - Hạn sử dụng
  - Giá gốc
  - Giá đề xuất
  - Siêu thị cung cấp
- Cho phép chỉnh sửa dữ liệu AI nhận diện nếu cần

---

### 3.3 Tích hợp AI OCR
- Nhận hình ảnh sản phẩm từ nhân viên siêu thị
- Gửi ảnh đến dịch vụ OCR
- Trích xuất:
  - Ngày sản xuất
  - Hạn sử dụng
- Lưu kết quả OCR vào hệ thống
- Ghi nhận trạng thái xác thực AI

**Công nghệ dự kiến:**
- Google Vision API / Azure OCR / Tesseract OCR

---

### 3.4 Gợi ý giá bán (Pricing Recommendation)
- Áp dụng mô hình **Rule-based Pricing**
- Giá đề xuất phụ thuộc:
  - Số ngày còn lại của hạn sử dụng
  - Loại sản phẩm
- Cho phép nhân viên siêu thị điều chỉnh giá

---

### 3.5 Quản lý đơn hàng
- Tạo đơn hàng theo khung giờ cố định
- Gom sản phẩm từ nhiều siêu thị
- Cập nhật trạng thái đơn hàng:
  - Created
  - Confirmed
  - Packed
  - Delivering
  - Completed / Failed
- Ghi log lịch sử trạng thái đơn hàng

---

### 3.6 Đóng gói & giao hàng
- Xác nhận đóng gói theo mã đơn
- Phân công đơn cho nhân viên giao hàng
- Theo dõi kết quả giao hàng
- Hỗ trợ giao:
  - Điểm nhận
  - Giao tận nhà

---

## 4. Kiến trúc Backend

### 4.1 Kiến trúc tổng thể
Backend được xây dựng theo **Layered Architecture**, tuân thủ **Clean Architecture**:


### 4.2 Mô tả các layer
- **API Layer**: Controllers, Request/Response DTO
- **Application Layer**: Use Cases, Services, Business Logic
- **Domain Layer**: Entities, Enums, Interfaces
- **Infrastructure Layer**: Database, Repository, External Services (AI OCR)

---

## 5. Công nghệ sử dụng

### Backend
- Ngôn ngữ: **.NET / Java**
- RESTful API
- Repository Pattern
- Dependency Injection

### Cơ sở dữ liệu
- SQL Server / Firebase hoặc tương đương
- Transaction đảm bảo toàn vẹn dữ liệu

### Bảo mật
- JWT Authentication
- Role-based Authorization
- Account Lockout Policy

---

## 6. Thiết kế cơ sở dữ liệu (Tóm tắt)
Các bảng chính:
- Users
- Roles
- Products
- NearExpiryBatches
- Orders
- OrderItems
- Deliveries
- AuditLogs

Cơ sở dữ liệu tuân thủ chuẩn **3NF**.

---

## 7. Yêu cầu phi chức năng
- **Hiệu năng**: API phản hồi ≤ 5 giây với các request chính
- **Độ tin cậy**:
  - Sử dụng transaction khi tạo đơn hàng
  - Rollback khi xảy ra lỗi
- **Bảo mật**:
  - Mã hóa mật khẩu
  - JWT + phân quyền
- **Khả năng bảo trì**:
  - Clean Architecture
  - Code có comment và tài liệu API

---

## 8. API Documentation
- API được tài liệu hóa bằng **Swagger / OpenAPI**
- Bao gồm:
  - Authentication APIs
  - Product APIs
  - Order APIs
  - Delivery APIs

---

## 9. Kế hoạch phát triển Backend (5 tháng)

| Tháng | Nội dung |
|----|--------|
| 1 | Phân tích yêu cầu, thiết kế kiến trúc |
| 2 | Thiết kế DB, xây dựng API nền tảng |
| 3 | Hoàn thiện nghiệp vụ sản phẩm & đơn hàng |
| 4 | Tích hợp AI OCR & gợi ý giá |
| 5 | Kiểm thử, tối ưu, hoàn thiện tài liệu |

---

## 10. Kết luận
Backend của hệ thống được thiết kế nhằm đảm bảo:
- Tính mở rộng
- Độ ổn định
- Phù hợp phạm vi đồ án tốt nghiệp

Hệ thống có khả năng phát triển thành sản phẩm thực tế trong tương lai với việc mở rộng AI và quy mô triển khai.

---
