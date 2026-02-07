# 📋 API Documentation - Nhân viên Siêu thị (SupplierStaff)

> **Base URL:** `https://api.closeexp.com` (hoặc `http://localhost:5000` khi dev)  
> **Version:** 1.0  
> **Last Updated:** 2026-02-04

---

## 🔐 Authentication

Tất cả API trong tài liệu này yêu cầu:

```
Authorization: Bearer {access_token}
```

- **Role yêu cầu:** `SupplierStaff`
- Token lấy từ API Login: `POST /api/auth/login`

---

## 📑 Mục lục

1. [Lấy danh sách sản phẩm](#1-lấy-danh-sách-sản-phẩm)
2. [Lấy danh sách lô hàng (có filter hạn)](#2-lấy-danh-sách-lô-hàng-có-filter-hạn)
3. [Lookup: Phân loại hạn sử dụng](#3-lookup-phân-loại-hạn-sử-dụng)
4. [Lookup: Loại định lượng](#4-lookup-loại-định-lượng)

---

## 1. Lấy danh sách sản phẩm

Lấy danh sách **Product** của siêu thị mà nhân viên đang làm việc.

### Request

```http
GET /api/products/my-supermarket
```

### Query Parameters

| Param | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `searchTerm` | string | ❌ | - | Tìm theo tên, thương hiệu hoặc barcode |
| `category` | string | ❌ | - | Lọc theo danh mục sản phẩm |
| `pageNumber` | int | ❌ | 1 | Số trang |
| `pageSize` | int | ❌ | 20 | Số item mỗi trang (max: 200) |

### Example Request

```bash
curl -X GET "https://api.closeexp.com/api/products/my-supermarket?searchTerm=sữa&pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6..."
```

### Response

```json
{
  "isSuccess": true,
  "message": "Tìm thấy 15 sản phẩm",
  "data": {
    "items": [
      {
        "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "supermarketId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
        "name": "Sữa TH True Milk 1L",
        "brand": "TH True Milk",
        "category": "Sữa",
        "barcode": "8936018368019",
        "isFreshFood": false,
        "status": "Published",
        
        "weightType": 1,
        "weightTypeName": "Định lượng cố định",
        "defaultPricePerKg": null,
        
        "originalPrice": 45000,
        "suggestedPrice": 35000,
        "finalPrice": 36000,
        
        "expiryDate": "2026-02-10T00:00:00Z",
        "manufactureDate": "2026-01-01T00:00:00Z",
        "daysToExpiry": 6,
        
        "ocrConfidence": 0.95,
        "pricingConfidence": 0.87,
        "pricingReasons": "Còn 6 ngày, giảm 20%",
        
        "createdBy": "user-guid",
        "createdAt": "2026-02-01T10:00:00Z",
        "verifiedBy": "staff-guid",
        "verifiedAt": "2026-02-01T11:00:00Z",
        "pricedBy": "staff-guid",
        "pricedAt": "2026-02-01T12:00:00Z",
        
        "mainImageUrl": "https://storage.closeexp.com/images/milk.jpg",
        "totalImages": 2,
        "productImages": [
          {
            "productImageId": "img-guid-1",
            "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "imageUrl": "https://storage.closeexp.com/images/milk.jpg",
            "uploadedAt": "2026-02-01T10:00:00Z"
          },
          {
            "productImageId": "img-guid-2",
            "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "imageUrl": "https://storage.closeexp.com/images/milk-back.jpg",
            "uploadedAt": "2026-02-01T10:01:00Z"
          }
        ]
      }
    ],
    "totalResult": 15,
    "page": 1,
    "pageSize": 10
  }
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `productId` | GUID | ID sản phẩm |
| `supermarketId` | GUID | ID siêu thị |
| `name` | string | Tên sản phẩm |
| `brand` | string | Thương hiệu |
| `category` | string | Danh mục |
| `barcode` | string | Mã vạch |
| `isFreshFood` | boolean | Là thực phẩm tươi sống |
| `status` | string | Trạng thái: `Hidden`, `PendingVerification`, `Published`, etc. |
| `weightType` | int | 1 = Định lượng cố định, 2 = Bán theo cân |
| `weightTypeName` | string | Tên hiển thị loại định lượng |
| `defaultPricePerKg` | decimal? | Giá mặc định/kg (chỉ có khi `weightType = 2`) |
| `originalPrice` | decimal | Giá gốc |
| `suggestedPrice` | decimal | Giá AI đề xuất |
| `finalPrice` | decimal | Giá cuối cùng |
| `expiryDate` | DateTime? | Ngày hết hạn |
| `manufactureDate` | DateTime? | Ngày sản xuất |
| `daysToExpiry` | int? | Số ngày còn lại |
| `mainImageUrl` | string? | URL ảnh đại diện chính |
| `totalImages` | int | Tổng số ảnh |
| `productImages` | array | Danh sách tất cả ảnh |

---

## 2. Lấy danh sách lô hàng (có filter hạn)

Lấy danh sách **ProductLot** với đầy đủ thông tin và khả năng lọc theo hạn sử dụng.

### Request

```http
GET /api/products/my-supermarket/lots
```

### Query Parameters

| Param | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `expiryStatus` | int | ❌ | - | Lọc theo trạng thái hạn (xem bảng bên dưới) |
| `weightType` | int | ❌ | - | 1 = Fixed, 2 = Variable |
| `isFreshFood` | boolean | ❌ | - | Lọc đồ tươi |
| `searchTerm` | string | ❌ | - | Tìm theo tên, thương hiệu hoặc barcode |
| `category` | string | ❌ | - | Lọc theo danh mục |
| `pageNumber` | int | ❌ | 1 | Số trang |
| `pageSize` | int | ❌ | 20 | Số item mỗi trang (max: 200) |

#### Giá trị `expiryStatus`

| Value | Name | Mô tả |
|-------|------|-------|
| 1 | Today | Trong ngày (dưới 24 giờ) |
| 2 | ExpiringSoon | Sắp hết hạn (1-2 ngày) |
| 3 | ShortTerm | Còn ngắn hạn (3-7 ngày) |
| 4 | LongTerm | Còn dài hạn (8+ ngày) |
| 5 | Expired | Đã hết hạn |

#### Giá trị `weightType`

| Value | Name | Mô tả |
|-------|------|-------|
| 1 | Fixed | Định lượng cố định (VD: chai 500ml, gói 200g) |
| 2 | Variable | Bán theo cân (VD: rau củ, thịt cá) |

### Example Requests

```bash
# Lấy tất cả lô hàng
curl -X GET "https://api.closeexp.com/api/products/my-supermarket/lots" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6..."

# Lấy lô hàng hết hạn trong ngày
curl -X GET "https://api.closeexp.com/api/products/my-supermarket/lots?expiryStatus=1" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6..."

# Lấy lô hàng đồ tươi, bán theo cân, sắp hết hạn
curl -X GET "https://api.closeexp.com/api/products/my-supermarket/lots?expiryStatus=2&weightType=2&isFreshFood=true" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6..."

# Tìm kiếm + filter
curl -X GET "https://api.closeexp.com/api/products/my-supermarket/lots?searchTerm=thịt&category=Thịt&expiryStatus=1" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6..."
```

### Response

```json
{
  "isSuccess": true,
  "message": "Tìm thấy 25 lô sản phẩm",
  "data": {
    "items": [
      {
        "lotId": "lot-guid-123",
        "productId": "product-guid-456",
        "expiryDate": "2026-02-04T18:00:00Z",
        "manufactureDate": "2026-01-20T00:00:00Z",
        "quantity": 50,
        "weight": 25.5,
        "status": "Active",
        
        "unitId": "unit-guid-789",
        "unitName": "Kg",
        "unitType": "Weight",
        
        "originalUnitPrice": 350000,
        "suggestedUnitPrice": 280000,
        "finalUnitPrice": 290000,
        
        "productName": "Thịt bò Úc",
        "brand": "Meat Plus",
        "category": "Thịt",
        "barcode": "8936018368020",
        "isFreshFood": true,
        
        "weightType": 2,
        "weightTypeName": "Bán theo cân",
        "defaultPricePerKg": 350000,
        
        "supermarketId": "supermarket-guid",
        "supermarketName": "CoopMart Quận 1",
        
        "mainImageUrl": "https://storage.closeexp.com/images/beef.jpg",
        "totalImages": 3,
        "productImages": [
          {
            "productImageId": "img-guid-1",
            "productId": "product-guid-456",
            "imageUrl": "https://storage.closeexp.com/images/beef.jpg",
            "uploadedAt": "2026-01-20T10:00:00Z"
          }
        ],
        
        "expiryStatus": 1,
        "expiryStatusText": "Còn 12 giờ",
        "daysRemaining": 0,
        "hoursRemaining": 12,
        
        "createdAt": "2026-01-20T10:00:00Z"
      }
    ],
    "totalResult": 25,
    "page": 1,
    "pageSize": 20
  }
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `lotId` | GUID | ID lô hàng |
| `productId` | GUID | ID sản phẩm |
| `expiryDate` | DateTime | Ngày hết hạn của lô |
| `manufactureDate` | DateTime | Ngày sản xuất |
| `quantity` | decimal | Số lượng |
| `weight` | decimal | Khối lượng (kg) |
| `status` | string | Trạng thái lô hàng |
| `unitId` | GUID | ID đơn vị |
| `unitName` | string | Tên đơn vị (VD: Kg, Hộp, Chai) |
| `unitType` | string | Loại đơn vị |
| `originalUnitPrice` | decimal | Giá gốc/đơn vị |
| `suggestedUnitPrice` | decimal | Giá đề xuất/đơn vị |
| `finalUnitPrice` | decimal | Giá cuối cùng/đơn vị |
| `productName` | string | Tên sản phẩm |
| `brand` | string | Thương hiệu |
| `category` | string | Danh mục |
| `barcode` | string | Mã vạch |
| `isFreshFood` | boolean | Là thực phẩm tươi |
| `weightType` | int | Loại định lượng |
| `weightTypeName` | string | Tên loại định lượng |
| `defaultPricePerKg` | decimal? | Giá mặc định/kg |
| `supermarketId` | GUID | ID siêu thị |
| `supermarketName` | string | Tên siêu thị |
| `mainImageUrl` | string? | URL ảnh đại diện |
| `totalImages` | int | Tổng số ảnh |
| `productImages` | array | Danh sách ảnh |
| `expiryStatus` | int | Trạng thái hạn (1-5) |
| `expiryStatusText` | string | Mô tả trạng thái hạn (VD: "Còn 12 giờ") |
| `daysRemaining` | int | Số ngày còn lại (âm nếu quá hạn) |
| `hoursRemaining` | int? | Số giờ còn lại (chỉ khi `expiryStatus = 1`) |

### Sorting

Kết quả được sắp xếp tự động theo thứ tự ưu tiên:
1. 🔴 **Today** - Trong ngày (ưu tiên cao nhất)
2. 🟠 **ExpiringSoon** - Sắp hết hạn
3. 🟡 **ShortTerm** - Còn ngắn hạn
4. 🟢 **LongTerm** - Còn dài hạn
5. ⚫ **Expired** - Đã hết hạn (cuối cùng)

---

## 3. Lookup: Phân loại hạn sử dụng

Lấy danh sách các trạng thái hạn sử dụng (dùng cho dropdown/filter).

### Request

```http
GET /api/products/expiry-statuses
```

### Response

```json
{
  "isSuccess": true,
  "data": [
    {
      "value": 1,
      "name": "Today",
      "description": "Trong ngày (dưới 24 giờ)"
    },
    {
      "value": 2,
      "name": "ExpiringSoon",
      "description": "Sắp hết hạn (1-2 ngày)"
    },
    {
      "value": 3,
      "name": "ShortTerm",
      "description": "Còn ngắn hạn (3-7 ngày)"
    },
    {
      "value": 4,
      "name": "LongTerm",
      "description": "Còn dài hạn (8+ ngày)"
    },
    {
      "value": 5,
      "name": "Expired",
      "description": "Đã hết hạn"
    }
  ]
}
```

---

## 4. Lookup: Loại định lượng

Lấy danh sách loại định lượng sản phẩm (dùng cho dropdown/filter).

### Request

```http
GET /api/products/weight-types
```

### Response

```json
{
  "isSuccess": true,
  "data": [
    {
      "value": 1,
      "name": "Fixed",
      "description": "Định lượng cố định (VD: chai 500ml, gói 200g)"
    },
    {
      "value": 2,
      "name": "Variable",
      "description": "Không cố định - bán theo cân (VD: rau củ quả, thịt cá)"
    }
  ]
}
```

---

## 📊 So sánh 2 API chính

| Tiêu chí | `/my-supermarket` | `/my-supermarket/lots` |
|----------|-------------------|------------------------|
| **Dữ liệu** | Product | ProductLot |
| **Filter hạn** | ❌ | ✅ `expiryStatus` |
| **Filter loại cân** | ❌ | ✅ `weightType` |
| **Filter đồ tươi** | ❌ | ✅ `isFreshFood` |
| **Thông tin hạn chi tiết** | `daysToExpiry` | `daysRemaining`, `hoursRemaining`, `expiryStatusText` |
| **Ảnh sản phẩm** | ✅ | ✅ |
| **Use case** | Xem tổng quan sản phẩm | Quản lý lô hàng theo hạn |

---

## ⚠️ Error Responses

### 401 Unauthorized

```json
{
  "isSuccess": false,
  "message": "Không thể xác định người dùng",
  "data": null
}
```

### 400 Bad Request

```json
{
  "isSuccess": false,
  "message": "Bạn chưa được gán vào siêu thị nào",
  "data": null
}
```

### 403 Forbidden

```json
{
  "isSuccess": false,
  "message": "Bạn không có quyền truy cập API này",
  "data": null
}
```

---

## 🛠️ Frontend Integration Examples

### React/TypeScript

```typescript
// Types
interface ProductImage {
  productImageId: string;
  productId: string;
  imageUrl: string;
  uploadedAt: string;
}

interface Product {
  productId: string;
  name: string;
  brand: string;
  category: string;
  mainImageUrl: string | null;
  totalImages: number;
  productImages: ProductImage[];
  weightType: number;
  weightTypeName: string;
  // ... other fields
}

interface ProductLot {
  lotId: string;
  productId: string;
  productName: string;
  expiryStatus: number;
  expiryStatusText: string;
  daysRemaining: number;
  hoursRemaining: number | null;
  mainImageUrl: string | null;
  productImages: ProductImage[];
  // ... other fields
}

interface PaginatedResult<T> {
  items: T[];
  totalResult: number;
  page: number;
  pageSize: number;
}

interface ApiResponse<T> {
  isSuccess: boolean;
  message: string;
  data: T;
}

// API calls
const getMyProducts = async (params?: {
  searchTerm?: string;
  category?: string;
  pageNumber?: number;
  pageSize?: number;
}): Promise<ApiResponse<PaginatedResult<Product>>> => {
  const query = new URLSearchParams(params as any).toString();
  const response = await fetch(`/api/products/my-supermarket?${query}`, {
    headers: {
      'Authorization': `Bearer ${getToken()}`
    }
  });
  return response.json();
};

const getMyProductLots = async (params?: {
  expiryStatus?: number;
  weightType?: number;
  isFreshFood?: boolean;
  searchTerm?: string;
  category?: string;
  pageNumber?: number;
  pageSize?: number;
}): Promise<ApiResponse<PaginatedResult<ProductLot>>> => {
  const query = new URLSearchParams(params as any).toString();
  const response = await fetch(`/api/products/my-supermarket/lots?${query}`, {
    headers: {
      'Authorization': `Bearer ${getToken()}`
    }
  });
  return response.json();
};
```

### Vue.js/Composition API

```typescript
import { ref } from 'vue';
import axios from 'axios';

const useProductLots = () => {
  const lots = ref<ProductLot[]>([]);
  const loading = ref(false);
  const totalResult = ref(0);

  const fetchLots = async (filters: {
    expiryStatus?: number;
    weightType?: number;
    isFreshFood?: boolean;
    searchTerm?: string;
  }) => {
    loading.value = true;
    try {
      const { data } = await axios.get('/api/products/my-supermarket/lots', {
        params: filters
      });
      lots.value = data.data.items;
      totalResult.value = data.data.totalResult;
    } finally {
      loading.value = false;
    }
  };

  return { lots, loading, totalResult, fetchLots };
};
```

---

## 📝 Changelog

| Date | Version | Changes |
|------|---------|---------|
| 2026-02-04 | 1.0 | Initial release |

---

**👨‍💻 Contact:** Nếu có thắc mắc, liên hệ Backend team qua Slack channel `#closeexp-backend`
