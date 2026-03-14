# Message Catalog (BE-CloseExp)

## 1. Mục tiêu
Tài liệu này tổng hợp message phản hồi trong backend để:
- Tránh hardcode message rải rác trong service/controller.
- Thống nhất ngôn ngữ với frontend.
- Dễ tra cứu, bảo trì và i18n sau này.

Phạm vi hiện tại: các message đang xuất hiện nhiều ở Application layer (`ApiResponse`, `ErrorResponse`, `throw new ...`).

## 2. Quy ước chuẩn key
Đề xuất dùng key theo mẫu:

`<MODULE>.<ACTION>.<RESULT>`

Ví dụ:
- `AUTH.LOGIN.SUCCESS`
- `AUTH.LOGIN.INVALID_CREDENTIALS`
- `USER.DELETE.SUCCESS`
- `DELIVERY.ORDER.NOT_ASSIGNED`
- `CONFIG.MAPBOX.ACCESS_TOKEN_REQUIRED`

Quy ước giá trị:
- `SUCCESS`: thao tác thành công
- `FAILED`: thao tác thất bại chung
- `NOT_FOUND`: không tìm thấy dữ liệu
- `INVALID_*`: dữ liệu/trạng thái không hợp lệ
- `UNAUTHORIZED/FORBIDDEN`: lỗi quyền
- `*_REQUIRED`: thiếu cấu hình hoặc input bắt buộc

## 3. Danh mục message theo nhóm

### 3.1 Nhóm Common / Response Envelope
Nguồn: `src/CloseExpAISolution.Application/DTOs/Response/ApiResponse.cs`

| Message Key | Loại | Message hiện tại | Ghi chú |
|---|---|---|---|
| `COMMON.SUCCESS.DEFAULT` | Success | `Success` | Message mặc định của `SuccessResponse` |
| `COMMON.SUCCESS.CUSTOM` | Success | Tùy theo nghiệp vụ | Nên luôn trả qua key chuẩn |
| `COMMON.ERROR.CUSTOM` | Error | Tùy theo nghiệp vụ | Nên gom vào catalog |

### 3.2 Nhóm Auth
Nguồn chính: `src/CloseExpAISolution.Application/Services/Class/AuthService.cs`

| Message Key | Loại | Message hiện tại |
|---|---|---|
| `AUTH.LOGIN.SUCCESS` | Success | `Đăng nhập thành công` |
| `AUTH.LOGIN.GOOGLE_SUCCESS` | Success | `Đăng nhập Google thành công` |
| `AUTH.LOGIN.INVALID_CREDENTIALS` | Error | `Email hoặc mật khẩu không hợp lệ` |
| `AUTH.LOGIN.INVALID_CREDENTIALS_REMAINING` | Error (dynamic) | `Email hoặc mật khẩu không hợp lệ. Còn {attemptsLeft} lần thử` |
| `AUTH.LOGIN.FAILED` | Error | `Đăng nhập thất bại. Vui lòng thử lại sau` |
| `AUTH.LOGIN.GOOGLE_FAILED` | Error | `Đăng nhập Google thất bại. Vui lòng thử lại sau` |
| `AUTH.LOGIN.ACCOUNT_CANNOT_LOGIN` | Error | `Tài khoản không thể đăng nhập` |
| `AUTH.LOGIN.ACCOUNT_NOT_ACTIVE` | Error | `Tài khoản không còn hoạt động` |
| `AUTH.LOGIN.ACCOUNT_PENDING_APPROVAL` | Error | `Tài khoản đang chờ Admin phê duyệt. Vui lòng đợi thông báo qua email` |
| `AUTH.LOGIN.ACCOUNT_REJECTED` | Error | `Tài khoản đã bị từ chối phê duyệt. Vui lòng liên hệ Admin` |
| `AUTH.LOGIN.ACCOUNT_BANNED` | Error | `Tài khoản đã bị khóa vĩnh viễn bởi Admin` |
| `AUTH.LOGIN.ACCOUNT_DELETED` | Error | `Tài khoản đã bị xóa` |
| `AUTH.LOGIN.ACCOUNT_LOCKED_TEMP` | Error (dynamic) | `Tài khoản đang bị khóa tạm thời. Vui lòng thử lại sau {remainingMinutes} phút` |
| `AUTH.LOGIN.ACCOUNT_LOCKED_BY_FAILED_ATTEMPTS` | Error (dynamic) | `Tài khoản đã bị khóa tạm thời do đăng nhập sai quá {maxAttempts} lần. Vui lòng thử lại sau {lockoutMinutes} phút` |
| `AUTH.REGISTER.SUCCESS` | Success | (qua `SuccessWithMessage`) |
| `AUTH.REGISTER.FAILED` | Error | `Đăng ký thất bại. Vui lòng thử lại sau` |
| `AUTH.REGISTER.EMAIL_EXISTS` | Error | `Email đã được đăng ký` |
| `AUTH.REGISTER.INVALID_TYPE` | Error | `Loại đăng ký không hợp lệ` |
| `AUTH.REGISTER.TYPE_NOT_ALLOWED` | Error | `Loại đăng ký này không được phép. Chỉ Vendor và SupplierStaff (nhân viên siêu thị) mới có thể đăng ký công khai.` |
| `AUTH.REGISTER.SUPERMARKET_INFO_REQUIRED` | Error | `Vui lòng nhập thông tin siêu thị/cơ sở` |
| `AUTH.REGISTER.SUPERMARKET_EXISTS` | Error (dynamic) | `Cơ sở '{name} - {address}' đã tồn tại trong hệ thống` |
| `AUTH.REFRESH.SUCCESS` | Success | `Làm mới token thành công` |
| `AUTH.REFRESH.INVALID_TOKEN` | Error | `Refresh token không hợp lệ` |
| `AUTH.REFRESH.EXPIRED_TOKEN` | Error | `Refresh token đã hết hạn` |
| `AUTH.REFRESH.REUSED_REVOKED_TOKEN` | Error | `Phát hiện token đã bị thu hồi được sử dụng lại. Tất cả phiên đăng nhập đã bị vô hiệu hóa` |
| `AUTH.REFRESH.FAILED` | Error | `Làm mới token thất bại. Vui lòng thử lại` |
| `AUTH.LOGOUT.SUCCESS` | Success | `Đăng xuất thành công` / `Đã đăng xuất` |
| `AUTH.LOGOUT.FAILED` | Error | `Đăng xuất thất bại. Vui lòng thử lại` |
| `AUTH.SESSION.REVOKE_ALL_SUCCESS` | Success | `Đã thu hồi tất cả phiên đăng nhập` |
| `AUTH.SESSION.REVOKE_ALL_FAILED` | Error | `Thu hồi phiên đăng nhập thất bại. Vui lòng thử lại` |
| `AUTH.EMAIL.NOT_FOUND` | Error | `Email không tồn tại` |
| `AUTH.EMAIL.NOT_REQUIRED_VERIFICATION` | Error | `Tài khoản không cần xác minh email` |
| `AUTH.OTP.TOO_MANY_ATTEMPTS` | Error | `Nhập sai OTP quá nhiều lần. Vui lòng yêu cầu gửi lại mã OTP mới` |
| `AUTH.OTP.EXPIRED` | Error | `Mã OTP đã hết hạn. Vui lòng yêu cầu gửi lại mã mới` |
| `AUTH.OTP.INVALID_REMAINING` | Error (dynamic) | `Mã OTP không đúng. Còn {attemptsLeft} lần thử` |
| `AUTH.OTP.RESEND_WAIT` | Error (dynamic) | `Vui lòng đợi {waitSeconds} giây trước khi gửi lại mã OTP` |
| `AUTH.OTP.RESEND_SUCCESS` | Success | `Đã gửi lại mã OTP. Vui lòng kiểm tra email` |
| `AUTH.OTP.SEND_IF_EXISTS` | Success | `Nếu email tồn tại, mã OTP đã được gửi` |
| `AUTH.VERIFY_EMAIL.SUCCESS_PENDING` | Success | `Xác minh email thành công! Tài khoản đang chờ quản trị viên phê duyệt` |
| `AUTH.VERIFY_EMAIL.SUCCESS_CAN_LOGIN` | Success | `Xác minh email thành công! Bạn có thể đăng nhập ngay bây giờ` |
| `AUTH.PASSWORD.RESET_SUCCESS` | Success | `Đặt lại mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới` |
| `AUTH.GOOGLE.INVALID_TOKEN` | Error | `Google token không hợp lệ hoặc đã hết hạn` |
| `AUTH.GOOGLE.EMAIL_NOT_REGISTERED` | Error | `Email này chưa được đăng ký trong hệ thống. Vui lòng đăng ký tài khoản trước` |

### 3.3 Nhóm User
Nguồn chính: `src/CloseExpAISolution.Application/Services/Class/UserService.cs`

| Message Key | Loại | Message hiện tại |
|---|---|---|
| `USER.CREATE.SUCCESS` | Success | `Tạo người dùng thành công` |
| `USER.UPDATE.SUCCESS` | Success | `Cập nhật người dùng thành công` |
| `USER.UPDATE_PROFILE.SUCCESS` | Success | `Cập nhật thông tin cá nhân thành công` |
| `USER.DELETE.SUCCESS` | Success | `Xóa người dùng thành công` / `Xóa tài khoản thành công` |
| `USER.EMAIL.ALREADY_EXISTS` | Error | `Email đã tồn tại` |
| `USER.ROLE.INVALID` | Error | `Vai trò không hợp lệ` |
| `USER.NOT_FOUND` | Error | `Không tìm thấy người dùng` |
| `USER.DELETE.FORBIDDEN` | Error | `Bạn không có quyền xóa tài khoản này. Vui lòng liên hệ Admin` |
| `USER.DELETE.ALREADY_DELETED` | Error | `Tài khoản đã bị xóa trước đó` |
| `USER.DELETE.FAILED` | Error | `Xóa tài khoản thất bại. Vui lòng thử lại sau` |

### 3.4 Nhóm Feedback
Nguồn chính: `src/CloseExpAISolution.Application/Services/Class/FeedbackService.cs`

| Message Key | Loại | Message hiện tại |
|---|---|---|
| `FEEDBACK.CREATE.SUCCESS` | Success | `Đánh giá thành công` |
| `FEEDBACK.UPDATE.SUCCESS` | Success | `Cập nhật đánh giá thành công` |
| `FEEDBACK.DELETE.SUCCESS` | Success | `Xóa đánh giá thành công` |
| `FEEDBACK.ORDER.NOT_FOUND` | Error | `Đơn hàng không tồn tại` |
| `FEEDBACK.ALREADY_EXISTS` | Error | `Bạn đã đánh giá đơn hàng này rồi` |
| `FEEDBACK.UPDATE.FORBIDDEN` | Error | `Bạn không có quyền sửa đánh giá này` |
| `FEEDBACK.DELETE.FORBIDDEN` | Error | `Bạn không có quyền xóa đánh giá này` |
| `FEEDBACK.NOT_FOUND` | Error | `Không tìm thấy đánh giá` |

### 3.5 Nhóm Delivery
Nguồn chính: `src/CloseExpAISolution.Application/Services/Class/DeliveryService.cs`

| Message Key | Loại | Message hiện tại |
|---|---|---|
| `DELIVERY.STAFF.NOT_FOUND` | Error | `Không tìm thấy nhân viên giao hàng.` |
| `DELIVERY.TEAM.NOT_FOUND` | Error | `Không tìm thấy nhóm giao hàng.` |
| `DELIVERY.ORDER.NOT_FOUND` | Error | `Không tìm thấy đơn hàng.` |
| `DELIVERY.TEAM.CLAIM_FORBIDDEN` | Error | `Người dùng không có quyền nhận đơn giao hàng.` |
| `DELIVERY.TEAM.NOT_ASSIGNED` | Error | `Bạn không được phân công nhóm giao hàng này.` |
| `DELIVERY.ORDER.NOT_ASSIGNED` | Error | `Bạn không được phân công giao đơn hàng này.` |
| `DELIVERY.TEAM.INVALID_STATUS_PENDING_ACCEPT` | Error | `Nhóm giao hàng không ở trạng thái chờ nhận.` |
| `DELIVERY.TEAM.ALREADY_CLAIMED` | Error | `Nhóm giao hàng đã được nhận bởi shipper khác.` |
| `DELIVERY.TEAM.INVALID_STATUS_TO_START` | Error | `Nhóm giao hàng phải ở trạng thái 'Đã nhận' để bắt đầu giao.` |
| `DELIVERY.ORDER.INVALID_STATUS_TO_CONFIRM` | Error | `Đơn hàng phải ở trạng thái 'Sẵn sàng giao' để xác nhận giao hàng.` |
| `DELIVERY.ORDER.INVALID_STATUS_TO_REPORT_ISSUE` | Error | `Đơn hàng phải ở trạng thái 'Sẵn sàng giao' để báo lỗi.` |
| `DELIVERY.TEAM.INVALID_STATUS_TO_COMPLETE` | Error | `Nhóm giao hàng phải đang trong quá trình giao để hoàn thành.` |

### 3.6 Nhóm Product / Workflow / Import
Nguồn chính:
- `src/CloseExpAISolution.Application/Services/Class/ProductService.cs`
- `src/CloseExpAISolution.Application/Services/Class/ProductWorkflowService.cs`
- `src/CloseExpAISolution.Application/Services/Class/ExcelImportService.cs`

| Message Key | Loại | Message hiện tại |
|---|---|---|
| `PRODUCT.UNIT.DEFAULT_NOT_FOUND` | Error | `Không tìm thấy đơn vị đo mặc định. Vui lòng seed bảng Units trước.` |
| `PRODUCT.NOT_FOUND` | Error (dynamic) | `Không tìm thấy sản phẩm với id {id}` |
| `PRODUCT.CREATE.POST_CHECK_NOT_FOUND` | Error | `Product not found after create.` |
| `WORKFLOW.SUPERMARKET.NOT_FOUND` | Error (dynamic) | `Supermarket with ID {supermarketId} not found.` |
| `WORKFLOW.UNIT.NOT_FOUND` | Error | `No Unit found in database. Please seed Units first.` |
| `WORKFLOW.PRODUCT.NOT_FOUND` | Error (dynamic) | `Product {productId} not found` |
| `WORKFLOW.PRODUCT.INVALID_STATUS_VERIFY` | Error (dynamic) | `Product must be in Draft status to verify. Current status: {status}` |
| `WORKFLOW.PRODUCT.INVALID_STATUS_PRICE_SUGGESTION` | Error | `Product must be verified before getting pricing suggestion` |
| `WORKFLOW.PRODUCT.INVALID_STATUS_CONFIRM_PRICE` | Error (dynamic) | `Product must be in Verified status to confirm price. Current status: {status}` |
| `WORKFLOW.PRICING.NOT_FOUND` | Error | `Pricing not found. Please get pricing suggestion first.` |
| `WORKFLOW.PRODUCT.INVALID_STATUS_PUBLISH` | Error (dynamic) | `Product must be in Priced status to publish. Current status: {status}` |
| `WORKFLOW.PRODUCT.BARCODE_ALREADY_EXISTS` | Error (dynamic) | `Product with barcode {barcode} already exists (ProductId: {productId}). Use CreateProductLotFromExisting instead.` |
| `WORKFLOW.LOT.NOT_FOUND` | Error (dynamic) | `ProductLot {lotId} not found` |
| `WORKFLOW.LOT.PRODUCT_NOT_FOUND` | Error (dynamic) | `Product {productId} not found` |
| `WORKFLOW.LOT.PRICING_REQUIRED` | Error | `Please get pricing suggestion first.` |
| `WORKFLOW.LOT.INVALID_STATUS_PUBLISH` | Error (dynamic) | `ProductLot must be in Priced status to publish. Current: {status}` |
| `IMPORT.EXCEL.NO_WORKSHEETS` | Error | `Excel file contains no worksheets` |
| `IMPORT.EXCEL.WORKSHEET_EMPTY` | Error | `Excel worksheet is empty` |
| `IMPORT.EXCEL.FILE_EMPTY` | Error | `Excel file is empty` |
| `IMPORT.SUPERMARKET.NOT_FOUND` | Error (dynamic) | `Supermarket {supermarketId} not found` |
| `IMPORT.UNIT.NOT_FOUND` | Error | `No Unit found in database` |
| `IMPORT.PRODUCT_NAME.REQUIRED` | Error | `Product name is required` |

### 3.7 Nhóm MarketStaff / Supermarket
Nguồn chính:
- `src/CloseExpAISolution.Application/Services/Class/MarketStaffService.cs`
- `src/CloseExpAISolution.Application/Services/Class/SupermarketService.cs`

| Message Key | Loại | Message hiện tại |
|---|---|---|
| `MARKET_STAFF.NOT_FOUND` | Error (dynamic) | `Không tìm thấy nhân viên siêu thị với id {id}` |
| `SUPERMARKET.NOT_FOUND` | Error (dynamic) | `Không tìm thấy siêu thị với id {id}` |

### 3.8 Nhóm Config / Integration
Nguồn chính:
- `src/CloseExpAISolution.Application/AIService/Configuration/AIServiceSettings.cs`
- `src/CloseExpAISolution.Application/Mapbox/Configuration/MapboxSettings.cs`
- `src/CloseExpAISolution.Application/Email/Extensions/EmailServiceExtensions.cs`
- `src/CloseExpAISolution.Application/Services/Class/R2StorageService.cs`

| Message Key | Loại | Message hiện tại |
|---|---|---|
| `CONFIG.AI.BASE_URL_REQUIRED` | Validation | `AIService:BaseUrl is required` |
| `CONFIG.AI.TIMEOUT_INVALID` | Validation | `AIService:TimeoutSeconds must be positive` |
| `CONFIG.AI.RETRY_COUNT_INVALID` | Validation | `AIService:RetryCount cannot be negative` |
| `CONFIG.AI.MAX_IMAGE_SIZE_INVALID` | Validation | `AIService:MaxImageSizeMB must be positive` |
| `CONFIG.MAPBOX.ACCESS_TOKEN_REQUIRED` | Validation | `Mapbox AccessToken is required. Get one at https://console.mapbox.com/` |
| `CONFIG.MAPBOX.BASE_URL_REQUIRED` | Validation | `Mapbox BaseUrl is required.` |
| `CONFIG.EMAIL.SETTINGS_MISSING` | Validation | `EmailSettings configuration is missing` |
| `CONFIG.R2.BUCKET_NAME_REQUIRED` | Validation | `R2Storage:BucketName is required` |

## 4. Message động (template)
Các message có biến cần chuẩn hóa placeholder để frontend render ổn định:

| Key | Template đề xuất |
|---|---|
| `AUTH.OTP.INVALID_REMAINING` | `Mã OTP không đúng. Còn {attemptsLeft} lần thử` |
| `AUTH.OTP.RESEND_WAIT` | `Vui lòng đợi {waitSeconds} giây trước khi gửi lại mã OTP` |
| `AUTH.LOGIN.INVALID_CREDENTIALS_REMAINING` | `Email hoặc mật khẩu không hợp lệ. Còn {attemptsLeft} lần thử` |
| `AUTH.LOGIN.ACCOUNT_LOCKED_TEMP` | `Tài khoản đang bị khóa tạm thời. Vui lòng thử lại sau {remainingMinutes} phút` |
| `WORKFLOW.PRODUCT.INVALID_STATUS_VERIFY` | `Product must be in Draft status to verify. Current status: {status}` |
| `WORKFLOW.LOT.INVALID_STATUS_PUBLISH` | `ProductLot must be in Priced status to publish. Current: {status}` |

## 5. Kế hoạch áp dụng tối thiểu (migration-safe)
1. Tạo class hằng số theo nhóm trong `src/CloseExpAISolution.Application/Messages` (ví dụ `AuthMessages`, `UserMessages`, `DeliveryMessages`, `ConfigMessages`).
2. Thay dần hardcoded string ở service bằng hằng số theo module, ưu tiên luồng Auth/User trước.
3. Giữ nguyên text message hiện tại để không phá frontend; chỉ đổi nguồn lấy message.
4. Sau khi ổn định, bổ sung `MessageCode` vào `ApiResponse<T>` (không bắt buộc ngay) để FE map UI theo key thay vì text.

## 6. Quy định cập nhật catalog
- Mọi message mới phải thêm vào catalog trước hoặc cùng lúc merge code.
- Không thêm literal string trực tiếp trong service nếu đã có key phù hợp.
- Khi đổi wording, giữ nguyên key cũ nếu semantics không đổi để tránh breaking FE.

---
Ghi chú: Catalog này là baseline cho chuẩn hóa message trong backend CloseExpAI. Có thể mở rộng thêm nhóm `Order`, `Pricing`, `OCR`, `Admin` khi rà soát toàn bộ controller/service.
