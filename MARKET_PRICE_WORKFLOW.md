# Market Price & Pricing Workflow Documentation

## Tổng quan Workflow

### Quy trình xử lý sản phẩm (Product Workflow)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       PRODUCT WORKFLOW - 5 STEPS                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 1: UPLOAD & OCR                                                 │   │
│  │ POST /api/products/upload-ocr                                        │   │
│  │                                                                      │   │
│  │   Staff upload ảnh → AI OCR extract info → Product [DRAFT]          │   │
│  │   - Tên sản phẩm, thương hiệu                                       │   │
│  │   - Barcode (nếu có)                                                │   │
│  │   - Hạn sử dụng (HSD)                                               │   │
│  │   - Danh mục sản phẩm                                               │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                    ↓                                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 2: VERIFY PRODUCT                                               │   │
│  │ POST /api/products/{productId}/verify                                │   │
│  │                                                                      │   │
│  │   Staff xem thông tin → Chỉnh sửa nếu cần → Nhập GIÁ GỐC            │   │
│  │   → Product [VERIFIED] + AI tính giá đề xuất                        │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                    ↓                                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 3: REVIEW PRICING SUGGESTION                                    │   │
│  │ GET /api/products/{productId}/pricing-suggestion                     │   │
│  │                                                                      │   │
│  │   Xem giá đề xuất từ AI + Lý do + So sánh giá thị trường            │   │
│  │   - Suggested Price: 34,000đ (giảm 15%)                             │   │
│  │   - Reasons: "Còn 5 ngày HSD", "Thấp hơn thị trường 10%"           │   │
│  │   - Market prices: LOTTE 34k, 7-11 41k, Vissan 32k                  │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                    ↓                                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 4: CONFIRM PRICE                                                │   │
│  │ POST /api/products/{productId}/confirm-price                         │   │
│  │                                                                      │   │
│  │   Staff chấp nhận giá AI hoặc điều chỉnh → Product [PRICED]         │   │
│  │   - Lưu feedback để cải thiện AI                                    │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                    ↓                                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 5: PUBLISH                                                      │   │
│  │ POST /api/products/{productId}/publish                               │   │
│  │                                                                      │   │
│  │   Đăng sản phẩm lên hệ thống → Product [PUBLISHED]                  │   │
│  │   - Hiển thị cho khách hàng                                         │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Product States

| State | Description | Next Actions |
|-------|-------------|--------------|
| `Draft` | Vừa tạo từ OCR, chờ xác nhận | Verify |
| `Verified` | Đã xác nhận thông tin, có giá đề xuất | Confirm Price |
| `Priced` | Đã confirm giá, sẵn sàng publish | Publish |
| `Published` | Đã đăng, khách hàng có thể mua | - |
| `Expired` | Hết hạn | - |
| `SoldOut` | Hết hàng | - |
| `Hidden` | Ẩn tạm thời | - |
| `Deleted` | Đã xóa | - |

## API Endpoints

### CRUD Operations

```http
GET    /api/products                    # Get all products (paginated)
GET    /api/products/{id}               # Get product by ID
POST   /api/products                    # Create product
POST   /api/products/with-images        # Create product with images
PUT    /api/products/{id}               # Update product
DELETE /api/products/{id}               # Delete product
```

### Query by Status

```http
GET /api/products/by-status/{supermarketId}?status=Draft      # Get products by status
GET /api/products/workflow-summary/{supermarketId}            # Get workflow summary (count by status)
```

### Workflow Actions

#### Step 1: Upload & OCR

```http
POST /api/products/upload-ocr
Content-Type: multipart/form-data

supermarketId: {guid}
createdBy: {staff-id}
file: [image file]
```

**Response:**
```json
{
  "success": true,
  "data": {
    "productId": "...",
    "name": "Bò 2 Lát Vissan 170g",
    "brand": "Vissan",
    "barcode": "8934572020017",
    "category": "Thực phẩm đóng hộp",
    "expiryDate": "2026-02-10",
    "status": "Draft",
    "ocrConfidence": 0.92
  }
}
```

#### Step 2: Verify Product

```http
POST /api/products/{productId}/verify
Content-Type: application/json

{
  "originalPrice": 40000,
  "verifiedBy": "staff-123",
  "name": "Bò 2 Lát Vissan 170g",  // Optional: correct if OCR wrong
  "expiryDate": "2026-02-10"        // Optional: correct if OCR wrong
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "productId": "...",
    "productName": "Bò 2 Lát Vissan 170g",
    "originalPrice": 40000,
    "suggestedPrice": 34000,
    "confidence": 0.85,
    "discountPercent": 15,
    "expiryDate": "2026-02-10",
    "daysToExpiry": 7,
    "reasons": [
      "Còn 7 ngày đến HSD",
      "Giảm 15% để kích thích mua",
      "Thấp hơn giá thị trường 10%"
    ],
    "minMarketPrice": 31900,
    "avgMarketPrice": 34825,
    "maxMarketPrice": 41000,
    "marketPriceSources": [
      {"storeName": "Vissan Mart", "price": 31900},
      {"storeName": "LOTTE Mart", "price": 34000},
      {"storeName": "7-Eleven", "price": 41000}
    ]
  }
}
```

#### Step 3: Get Pricing Suggestion

```http
GET /api/products/{productId}/pricing-suggestion
```

#### Step 4: Confirm Price

```http
POST /api/products/{productId}/confirm-price
Content-Type: application/json

{
  "finalPrice": 35000,           // Optional: custom price
  "acceptedSuggestion": false,   // true if using AI suggested price
  "priceFeedback": "Giá AI hơi thấp, tăng lên 35k",
  "confirmedBy": "staff-123"
}
```

#### Step 5: Publish

```http
POST /api/products/{productId}/publish
Content-Type: application/json

{
  "publishedBy": "staff-123"
}
```

#### Quick Approve (Skip steps for trusted staff)

```http
POST /api/products/{productId}/quick-approve
Content-Type: application/json

{
  "originalPrice": 40000,
  "staffId": "staff-123",
  "acceptAiSuggestion": true,
  "finalPrice": null             // Optional: override if acceptAiSuggestion is false
}
```
# Get workflow summary
GET /api/products/workflow/summary/{supermarketId}

# Get products by status
GET /api/products/workflow/drafts/{supermarketId}
GET /api/products/workflow/verified/{supermarketId}
GET /api/products/workflow/priced/{supermarketId}
GET /api/products/workflow/published/{supermarketId}

# Quick approve (skip steps for trusted staff)
POST /api/products/workflow/{productId}/quick-approve
```

## Decay Function (Hàm giảm giá theo HSD)

Dựa trên % thời gian còn lại của hạn sử dụng:

| % Còn lại | Giảm giá |
|-----------|----------|
| 100%+     | 0%       |
| 80-100%   | 10%      |
| 60-80%    | 20%      |
| 40-60%    | 35%      |
| 20-40%    | 50%      |
| 10-20%    | 65%      |
| 5-10%     | 75%      |
| 0-5%      | 85%      |

### Công thức:
```python
# Tính % thời gian còn lại
days_to_expire = (expiry_date - today).days
shelf_life_days = product.total_shelf_life  # Từ DB hoặc ước tính
remaining_percent = (days_to_expire / shelf_life_days) * 100

# Áp dụng decay schedule
for threshold, discount in DECAY_SCHEDULE:
    if remaining_percent >= threshold:
        return discount
```

## Market Price Benchmark

### Cách 1: Background Crawler (Đêm)
- **BachHoaXanh**: Crawl giá từ bachhoaxanh.com
- **WinMart**: Crawl giá từ winmart.vn
- **CoopOnline**: (có thể thêm)

### Cách 2: Google Custom Search API
- Tìm kiếm `[tên sản phẩm] giá site:bachhoaxanh.com OR site:winmart.vn`
- Parse kết quả để lấy giá

### Cách 3: Crowdsource
- Staff nhập giá từ cửa hàng đối thủ
- Confidence thấp hơn (0.7) so với crawler (0.9)

### Thuật toán Benchmark:
```python
# Giá đề xuất phải thấp hơn giá thị trường X%
# X% phụ thuộc vào số ngày còn lại

if days_to_expire <= 3:
    target_discount = 15%  # Giảm thêm 15% so với thị trường
elif days_to_expire <= 7:
    target_discount = 10%  # Giảm thêm 10%
elif days_to_expire <= 14:
    target_discount = 5%   # Giảm thêm 5%
else:
    target_discount = 0%   # Ngang giá thị trường

suggested_price = min(decay_price, min_market_price * (1 - target_discount))
```

## Database Schema

### MarketPrice Entity
```csharp
public class MarketPrice
{
    public Guid MarketPriceId { get; set; }
    public string Barcode { get; set; }           // Unique với Source
    public string ProductName { get; set; }
    public decimal Price { get; set; }            // Giá bán
    public decimal? OriginalPrice { get; set; }   // Giá gốc (nếu có)
    public string Source { get; set; }            // bachhoaxanh, winmart, google, crowdsource
    public string? SourceUrl { get; set; }
    public string? StoreName { get; set; }
    public string? Unit { get; set; }
    public string? Weight { get; set; }
    public string Region { get; set; }            // VN
    public bool IsInStock { get; set; }
    public DateTime CollectedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    public decimal Confidence { get; set; }       // 0-1
    public string Status { get; set; }            // active, expired
}
```

### PriceFeedback Entity (Human-in-the-loop)
```csharp
public class PriceFeedback
{
    public Guid PriceFeedbackId { get; set; }
    public string Barcode { get; set; }
    public decimal SuggestedPrice { get; set; }   // Giá AI đề xuất
    public decimal FinalPrice { get; set; }       // Giá staff chọn
    public decimal OriginalPrice { get; set; }    // Giá gốc
    public float ActualDiscountPercent { get; set; }
    public int DaysToExpire { get; set; }
    public string? Category { get; set; }
    public bool WasAccepted { get; set; }         // Staff có chấp nhận không
    public string? RejectionReason { get; set; }  // Lý do từ chối
    public int StaffId { get; set; }
    public int SupermarketId { get; set; }
    public decimal? MarketPriceRef { get; set; }  // Giá thị trường tham chiếu
    public string? MarketPriceSource { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## API Endpoints

### AI Repository (Python)

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/api/v1/pricing/suggest` | POST | Đề xuất giá với market benchmark |
| `/api/v1/pricing/crawl` | POST | Trigger crawl giá từ các nguồn |
| `/api/v1/pricing/market/{barcode}` | GET | Lấy giá thị trường đã cache |

### BE Repository (.NET)

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/api/MarketPrices/{barcode}` | GET | Lấy giá thị trường |
| `/api/MarketPrices/search` | GET | Tìm theo tên sản phẩm |
| `/api/MarketPrices/crawl` | POST | Trigger crawl |
| `/api/MarketPrices/crowdsource` | POST | Nhập giá thủ công |
| `/api/MarketPrices/feedback` | POST | Lưu feedback từ staff |
| `/api/MarketPrices/accuracy/by-category` | GET | Thống kê độ chính xác AI |

## Pricing Response mới

```json
{
  "suggested_price": 45000,
  "discount_percent": 25.5,
  "confidence": 0.85,
  "min_suggested_price": 40500,
  "max_suggested_price": 49500,
  "expected_sell_rate": 78.5,
  "estimated_time_to_sell": "2-3 ngày",
  "competitiveness": 0.82,
  "reasons": [
    "Sản phẩm còn 7 ngày → giảm 25%",
    "Giá thấp hơn thị trường 12% (BachHoaXanh: 51,000đ)",
    "Nhu cầu cao cho loại sản phẩm này"
  ],
  "market_price_info": {
    "min_market_price": 51000,
    "avg_market_price": 55000,
    "source": "bachhoaxanh, winmart",
    "price_vs_market_percent": -11.8,
    "adjustment_applied": "Giảm thêm 10% để cạnh tranh (sắp hết hạn)"
  },
  "urgency_level": "medium",
  "recommended_action": "Giảm giá và đặt ở kệ dễ thấy",
  "model_version": "1.2.0"
}
```

## Background Worker (Nightly Crawl)

Cần tạo background job chạy đêm để:
1. Crawl giá từ tất cả các nguồn
2. Cập nhật database MarketPrices
3. Xóa giá cũ (> 7 ngày)
4. Export training data từ PriceFeedback

## Files đã tạo/sửa

### AI-Repository
- `app/services/market_price_crawler.py` (NEW) - Crawler service
- `app/services/pricing.py` (UPDATED) - Thêm market benchmark, decay function
- `app/models/pricing.py` (UPDATED) - Thêm market price fields
- `app/api/pricing.py` (UPDATED) - Thêm crawl endpoints

### BE-Repository
- `Domain/Entities/MarketPrice.cs` (NEW)
- `Domain/Entities/PriceFeedback.cs` (NEW)
- `Infrastructure/Repositories/IMarketPriceRepository.cs` (NEW)
- `Infrastructure/Repositories/MarketPriceRepository.cs` (NEW)
- `Infrastructure/Context/ApplicationDbContext.cs` (UPDATED)
- `Application/Services/IMarketPriceService.cs` (NEW)
- `Application/Services/MarketPriceService.cs` (NEW)
- `Application/AIService/Models/AIServiceModels.cs` (UPDATED)
- `Application/AIService/Interfaces/IAIServiceClient.cs` (UPDATED)
- `Application/AIService/Clients/AIServiceClient.cs` (UPDATED)
- `API/Controllers/MarketPricesController.cs` (NEW)
