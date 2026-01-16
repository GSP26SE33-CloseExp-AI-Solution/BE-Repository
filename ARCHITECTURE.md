# CloseExpAISolution - 3-Layer Architecture Structure

## Solution Overview
This solution follows **Clean Architecture** principles with clear separation of concerns across 4 main projects.

---

## Project Structure

```
CloseExpAISolution/
├── CloseExpAISolution.sln
└── src/
    ├── CloseExpAISolution.API/              # Presentation Layer
    ├── CloseExpAISolution.Application/      # Business Logic Layer
    ├── CloseExpAISolution.Domain/           # Core Domain Layer
    └── CloseExpAISolution.Infrastructure/   # Data Access Layer
```

---

## 1. CloseExpAISolution.API (Presentation Layer)

**Purpose**: HTTP endpoints, request/response handling, authentication, and API documentation

### Folder Structure:
```
CloseExpAISolution.API/
├── Controllers/          # API Controllers (AuthController, ProductController, OrderController, etc.)
├── Middleware/           # Custom middleware (Error handling, logging, etc.)
├── Filters/              # Action filters (Authorization, validation, etc.)
├── Extensions/           # Service registration extensions
├── Properties/           # Launch settings
├── Program.cs            # Application entry point
├── appsettings.json      # Configuration
└── appsettings.Development.json
```

**Responsibilities**:
- RESTful API endpoints
- Request validation
- Response formatting
- JWT authentication
- Swagger/OpenAPI documentation
- CORS configuration

---

## 2. CloseExpAISolution.Application (Business Logic Layer)

**Purpose**: Use cases, business logic, DTOs, and service interfaces

### Folder Structure:
```
CloseExpAISolution.Application/
├── Services/             # Business logic services (ProductService, OrderService, etc.)
├── Interfaces/           # Service interfaces (IProductService, IOrderService, etc.)
├── DTOs/
│   ├── Request/          # Request DTOs (CreateProductRequest, LoginRequest, etc.)
│   └── Response/         # Response DTOs (ProductResponse, OrderResponse, etc.)
├── Mappings/             # AutoMapper profiles
├── Validators/           # FluentValidation validators
└── Extensions/           # Service registration extensions
```

**Responsibilities**:
- Business rules and logic
- Data transformation (Entity ↔ DTO)
- Input validation
- Service orchestration
- Use case implementation

---

## 3. CloseExpAISolution.Domain (Core Domain Layer)

**Purpose**: Core business entities, enums, interfaces, and domain logic

### Folder Structure:
```
CloseExpAISolution.Domain/
├── Entities/             # Domain entities (User, Product, Order, NearExpiryBatch, etc.)
├── Enums/                # Enumerations (UserRole, OrderStatus, BatchStatus, etc.)
├── Interfaces/           # Repository interfaces (IRepository<T>, IUnitOfWork, etc.)
└── Exceptions/           # Custom domain exceptions
```

**Responsibilities**:
- Core business entities
- Domain enumerations
- Repository contracts
- Domain exceptions
- Business invariants

**Key Principle**: No dependencies on other layers

---

## 4. CloseExpAISolution.Infrastructure (Data Access Layer)

**Purpose**: Database access, external services, and infrastructure concerns

### Folder Structure:
```
CloseExpAISolution.Infrastructure/
├── Data/                 # DbContext, migrations
├── Repositories/         # Repository implementations (GenericRepository, UnitOfWork)
├── Configurations/       # Entity configurations (Fluent API)
├── Services/             # External services (OCR service, pricing service, email service)
└── Extensions/           # Service registration extensions
```

**Responsibilities**:
- Database context (Entity Framework Core)
- Repository pattern implementation
- Entity configurations
- External API integrations (AI OCR, etc.)
- Data seeding
- Migrations

---

## Dependency Flow

```
API → Application → Domain
  ↓
Infrastructure → Application
```

**Rules**:
- **Domain** has NO dependencies (pure business logic)
- **Application** depends only on Domain
- **Infrastructure** depends on Application and Domain
- **API** depends on Application and Infrastructure (composition root)

---

## Key Technologies

- **.NET 8.0** - Framework
- **ASP.NET Core Web API** - REST API
- **Entity Framework Core** - ORM
- **SQL Server** - Database
- **JWT** - Authentication
- **Swagger/OpenAPI** - API Documentation
- **AutoMapper** - Object mapping
- **FluentValidation** - Input validation

---

## Next Steps

1. Install required NuGet packages
2. Implement Domain entities and enums
3. Create DbContext and configurations
4. Implement repositories and Unit of Work
5. Create DTOs and services
6. Build API controllers
7. Configure authentication and authorization
8. Set up database migrations
9. Integrate AI OCR service
10. Implement pricing recommendation logic

---

## Notes

- Follow SOLID principles
- Use dependency injection
- Implement proper error handling
- Add comprehensive logging
- Write unit and integration tests
- Document all public APIs
