# Calcifer.Api - Architecture Documentation

**Project Type:** ASP.NET Core 8.0 Web API  
**Framework:** .NET 8.0  
**Database:** SQL Server with Entity Framework Core  
**Authentication:** JWT Bearer Token  
**API Documentation:** Swagger/OpenAPI

---

## 📁 Folder Architecture

### Project Root Structure
```
Calcifer.Api/
├── Program.cs                          # Application entry point & DI setup
├── Calcifer.Api.csproj                # Project configuration
├── appsettings.json                   # Configuration settings
├── appsettings.Development.json       # Development-specific settings
├── launchSettings.json                # Launch configurations
│
├── AuthHandler/                       # Authentication & Authorization Layer
│   ├── Claims/
│   │   └── CustomClaims.cs           # Custom JWT claims definitions
│   ├── Configuration/
│   │   └── JwtSettings.cs            # JWT configuration
│   ├── Filters/
│   │   ├── AuthorizationFilter.cs    # Authorization validation filter
│   │   ├── LicenseValidationFilter.cs# License enforcement filter
│   │   └── RequireFeatureAttribute.cs# Feature-based access control attribute
│   └── MinimalApis/
│       ├── IdentityApi.cs            # Identity management endpoints
│       └── LicenseApi.cs             # License management endpoints
│
├── Controllers/                       # API Controllers Layer
│   ├── HomeController.cs             # Home/root endpoints
│   └── AuthController/
│       ├── AuthController.cs         # Authentication endpoints
│       └── RoleController.cs         # Role management endpoints
│
├── DbContexts/                        # Data Access Layer
│   ├── CalciferAppDbContext.cs       # Main EF Core DbContext
│   ├── AuthModels/
│   │   ├── ApplicationUser.cs         # User entity
│   │   └── ApplicationRole.cs         # Role entity
│   ├── Common/
│   │   ├── CommonStatus.cs            # Status entity
│   │   └── TableOperationDetails.cs  # Audit/operation tracking
│   ├── Enum/
│   │   └── CommonStatusEnum.cs        # Status enumeration
│   ├── Licensing/                     # License domain models
│   │   ├── License.cs
│   │   ├── LicenseActivation.cs
│   │   ├── LicenseFeature.cs
│   │   └── LicenseType.cs
│   ├── Models/
│   │   ├── PublicData.cs             # Public domain model
│   │   └── Seeders/                  # Database seed data
│   └── MinimalApis/
│       └── PublicApis/
│           ├── CommonStatusApi.cs
│           └── PublicCRUDApis.cs
│
├── DTOs/                              # Data Transfer Objects Layer
│   ├── ApiResponseDto.cs             # Standard API response wrapper
│   ├── ClientTypeDto.cs              # Client type DTO
│   ├── PublicDataDTO.cs              # Public data DTO
│   ├── AuthDTO/
│   │   ├── AssignRoleRequestDto.cs
│   │   ├── CreateRoleRequestDto.cs
│   │   ├── LoginRequestDto.cs
│   │   └── RegisterRequestDto.cs
│   ├── CommonDTO/
│   │   └── CommonStatusDto.cs
│   └── LicenseDTO/
│       └── LicenseDto.cs
│
├── Services/                          # Business Logic Layer
│   ├── PublicService.cs              # Public domain business logic
│   ├── AuthService/
│   │   ├── AuthService.cs            # Authentication logic
│   │   ├── RoleService.cs            # Role management logic
│   │   └── TokenService.cs           # JWT token generation
│   └── LicenseService/
│       └── LicenseService.cs         # License business logic
│
├── Interface/                         # Abstraction/Contract Layer
│   ├── Common/
│   │   └── IPublicInterface.cs       # Public domain contracts
│   └── Licensing/
│       └── ILicenseService.cs        # License service contract
│
├── DependencyContainer/               # IoC Setup
│   └── DependencyInversion.cs        # Service registration container
│
├── Middleware/                        # Middleware Pipeline
│   └── MiddlewareDependencyInversion.cs # Middleware registration
│
├── Infrastructure/                    # Infrastructure Services
│   └── DatabaseInitializer.cs        # DB initialization & seeding
│
├── Migrations/                        # EF Core Migrations
│   ├── 20260328175339_InitialCreate.cs
│   ├── 20260328175339_InitialCreate.Designer.cs
│   └── CalciferAppDbContextModelSnapshot.cs
│
├── Properties/                        # Project properties
│   └── launchSettings.json
│
├── bin/                              # Build output (Debug/Release)
├── obj/                              # Intermediate build files
└── Controllers/
    └── HomeController.cs             # Home endpoint controller
```

---

## 🏗️ Internal Architecture

### Layered Architecture Pattern

```
┌─────────────────────────────────────────────┐
│          API Controllers & Endpoints         │  ← HTTP Entry Points
│   (RESTful APIs via Controllers + MinimalAPIs)
├─────────────────────────────────────────────┤
│          DTOs (Data Transfer Objects)        │  ← Request/Response Contracts
├─────────────────────────────────────────────┤
│       Services (Business Logic)              │  ← Core Application Logic
│  - AuthService / TokenService               │
│  - LicenseService                           │
│  - PublicService                            │
├─────────────────────────────────────────────┤
│       Interfaces (Abstraction Layer)         │  ← Service Contracts
├─────────────────────────────────────────────┤
│       DbContext (EF Core ORM)                │  ← Data Access
├─────────────────────────────────────────────┤
│       Database (SQL Server)                  │  ← Persistent Storage
└─────────────────────────────────────────────┘
```

### Cross-Cutting Concerns

```
┌────────────────────────────────────────────────────┐
│  Authentication Layer (AuthHandler)                 │
│  ├─ JWT Bearer Token validation                     │
│  ├─ Custom Claims extraction                        │
│  └─ Authorization Filters                           │
│     ├─ AuthorizationFilter                          │
│     ├─ LicenseValidationFilter (Feature gating)     │
│     └─ RequireFeatureAttribute (Custom attribute)   │
└────────────────────────────────────────────────────┘
```

### Request Flow

```
HTTP Request
    ↓
Middleware Pipeline
    ↓
Authorization Filter (JWT validation)
    ↓
License Validation Filter (Feature check)
    ↓
Controller/Minimal API Handler
    ↓
DTO Deserialization (Request)
    ↓
Service Layer (Business Logic)
    ↓
DbContext (Data Access)
    ↓
SQL Server Database
    ↓
[Response Path - Reverse]
    ↓
DTO Serialization (Response)
    ↓
HTTP Response (JSON)
```

---

## 🔑 Key Features & Components

### 1. **Authentication & Authorization**
- **JWT Bearer Token** based authentication
- **Role-Based Access Control (RBAC)** via ApplicationRole/ApplicationUser
- **Custom Claims** for extended authorization
- **Feature-Based Validation** for feature toggles/licensing

### 2. **Licensing System**
- License types and features management
- License activation and validation
- Feature-gating via LicenseValidationFilter
- License enforcement at API level

### 3. **Data Access**
- Entity Framework Core with SQL Server
- Database migrations support
- Custom DbContext (CalciferAppDbContext)
- Audit tracking via TableOperationDetails

### 4. **API Documentation**
- Swagger/OpenAPI integration
- Bearer token security definition
- Auto-generated interactive documentation

### 5. **Minimal APIs**
- Modern .NET endpoint mapping
- Public CRUD operations
- License and Identity management endpoints

---

## 📊 Technology Stack

| Layer | Technology |
|-------|-----------|
| **Framework** | ASP.NET Core 8.0 |
| **Database** | SQL Server |
| **ORM** | Entity Framework Core 8.0.1 |
| **Authentication** | JWT Bearer Token |
| **API Documentation** | Swagger/OpenAPI (Swashbuckle 6.4.0) |
| **Identity** | ASP.NET Core Identity |
| **Language** | C# with nullable reference types |

---

## ⚙️ Dependency Injection Setup

The application uses a centralized Dependency Injection container:
- **Location:** `DependencyContainer/DependencyInversion.cs`
- **Registration Point:** `Program.cs` via `DependencyInversion.RegisterServices()`
- **Services Configured:**
  - Authentication services (JWT, roles, claims)
  - Database context
  - Business logic services
  - Middleware components

---

## 🗄️ Database Schema Overview

**Key Entities:**
- **ApplicationUser** - System users
- **ApplicationRole** - User roles
- **License** - License records
- **LicenseType** - License classifications
- **LicenseFeature** - Licensed features
- **LicenseActivation** - License activation history
- **CommonStatus** - Status master data
- **PublicData** - Application domain data

---

## 🔐 Security Architecture

1. **Authentication**: JWT tokens with custom claims
2. **Authorization**: Filters & attributes enforce access control
3. **License Validation**: Feature-gating middleware
4. **Claims-Based Authorization**: Flexible permission system via custom claims

---

## 📋 Development Workflow

- **Build Target:** .NET 8.0
- **Nullable References:** Enabled (strict null checks)
- **Implicit Usings:** Enabled (cleaner imports)
- **Migrations:** Handled by EF Core migration system
- **Configuration:** appsettings.json environment-specific overrides

---

## 🚀 Key Design Patterns

| Pattern | Usage |
|---------|-------|
| **Dependency Injection** | Service registration & resolution (Program.cs) |
| **Repository Pattern** | DbContext abstracts data access |
| **Layered Architecture** | Clear separation of concerns |
| **DTO Pattern** | Data transfer between layers |
| **Filter/Middleware Pattern** | Cross-cutting authorization concerns |
| **Feature Toggle** | LicenseValidationFilter for feature gating |

---

## ✅ Validation Checklist

- [ ] Layered architecture properly enforced
- [ ] All services properly injected via DI
- [ ] DTOs used for all API contracts
- [ ] Authorization/licensing filters applied to protected endpoints
- [ ] Database migrations versioned and tracked
- [ ] Entity references follow naming conventions
- [ ] No business logic in controllers
- [ ] Service interfaces defined and implemented
