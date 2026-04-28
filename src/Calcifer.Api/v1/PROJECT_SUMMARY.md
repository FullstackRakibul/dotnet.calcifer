# Calcifer.Api v1 — Project Summary

**Last Updated**: April 27, 2026  
**Status**: Production-Ready (with security hardening pending)  
**Framework**: ASP.NET Core 8.0 (.NET 8.0)  
**Language**: C# with nullable reference types  

---

## TL;DR

**Calcifer.Api** is an enterprise-grade REST API built on a **5-layer layered architecture** with:
- **Advanced RBAC** (Role-Based Access Control) with Module:Resource:Action permission model
- **Licensing & Feature-Gating** engine for SaaS-style feature management
- **Pure Minimal API** design (controllers reserved for legacy/complex logic only)
- **JWT Bearer** authentication with embedded RBAC claims to reduce database load
- **Soft-delete** enforcement via EF Core query filters

**Architecture Type**: Layered Monolith (60% ready for microservice extraction)  
**Target Users**: Enterprise customers with multi-tenant RBAC requirements

---

## Key Statistics

| Metric | Value |
|--------|-------|
| **Total Classes** | ~50+ |
| **DTOs** | 15+ |
| **Minimal API Endpoints** | 20+ |
| **Controllers** | 4 (to be consolidated) |
| **Database Tables** | 11 (EF Core mapped) |
| **Authorization Filters** | 3 |
| **RBAC Entities** | 6 |
| **NuGet Dependencies** | 6 (core) |
| **Lines of Code** | ~8,000+ |

---

## Technology Stack

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **Framework** | ASP.NET Core | 8.0 | Modern async-first web framework |
| **Language** | C# | Latest | Strongly-typed, null-safe |
| **Database** | SQL Server | LocalDB/Remote | Relational data storage |
| **ORM** | Entity Framework Core | 8.0.1 | Code-first migrations, LINQ |
| **Authentication** | JWT Bearer | HMAC-SHA256 | Stateless token-based auth |
| **Identity** | ASP.NET Identity | 8.0.1 | User/role management |
| **API Documentation** | Swagger/OpenAPI | 6.4.0 | Interactive API explorer |
| **Dependency Injection** | Built-in Microsoft.DependencyInjection | 8.0 | Inversion of control |
| **CORS** | Built-in ASP.NET CORS | 8.0 | Cross-origin resource sharing |

---

## Architecture Overview

### 5-Layer Architecture

```
┌─────────────────────────────────────────────┐
│  API LAYER (Minimal APIs + Controllers)     │
│  ├─ Pure Minimal APIs (IdentityApi.cs)      │
│  ├─ RBAC Management (RbacMinimalApi.cs)     │
│  └─ Public APIs (PublicCRUDApis.cs)         │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  DTO LAYER (Request/Response Contracts)     │
│  ├─ LoginRequestDto, RegisterRequestDto     │
│  ├─ ApiResponseDto<T> (standard wrapper)    │
│  └─ Domain DTOs (PublicDataDTO, etc.)       │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  SERVICE LAYER (Business Logic)             │
│  ├─ AuthService (register, login)           │
│  ├─ TokenService (JWT with RBAC claims)     │
│  ├─ RoleService (role CRUD)                 │
│  ├─ LicenseService (feature-gating)         │
│  ├─ RbacService (permission resolution)     │
│  └─ PublicService (domain operations)       │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  INTERFACE LAYER (Service Abstractions)     │
│  ├─ ILicenseService                         │
│  ├─ IRbacService                            │
│  └─ IPublicInterface                        │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  DATA ACCESS LAYER (EF Core + DbContext)    │
│  ├─ CalciferAppDbContext                    │
│  ├─ Entity Models (ApplicationUser, License)│
│  ├─ Migrations (EF versioning)              │
│  └─ Query Filters (soft-delete enforcement) │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  SQL Server Database                        │
└─────────────────────────────────────────────┘
```

### Cross-Cutting Concerns

**Authorization Filter Chain** (runs in order):

```
1. LicenseValidationFilter     [RequireFeature]      → Feature-gating (runs FIRST)
         ↓
2. RbacAuthorizationFilter     [RequirePermission]   → Permission checks
         ↓
3. AuthorizationFilter         (global)              → JWT validation (minimal APIs)
         ↓
4. Request Handler
```

---

## Core Modules

### 1. Authentication & Identity (`AuthHandler/MinimalApis/IdentityApi.cs`)

**Endpoints**:
- `POST /api/v1/auth/register` — User registration (public)
- `POST /api/v1/auth/login` — Obtain JWT token (public)
- `GET /api/v1/auth/me` — Current user profile (authenticated)
- `POST /api/v1/auth/change-password` — Password update (authenticated)

**JWT Token Structure**:
```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "name": "User Name",
  "emp_id": "EMP-001",
  "roles": ["SUPERADMIN", "HR_MANAGER"],
  "perms": ["HCM:Employee:Read", "HCM:Employee:Update"],
  "unit_roles": ["Factory-1:HR_Manager"],
  "iat": 1234567890,
  "exp": 1234571490
}
```

### 2. RBAC (Role-Based Access Control)

**Permission Model**: `Module:Resource:Action`
- **Module**: Business domain (HCM, Finance, Production, Inventory)
- **Resource**: Entity type (Employee, Payroll, WorkOrder, SKU)
- **Action**: Operation (Create, Read, Update, Delete, Export)

**Example Permissions**:
- `HCM:Employee:Read` — Can view employees
- `HCM:Payroll:Export` — Can export payroll data
- `Finance:*:*` — All Finance operations (wildcard)
- `*:*:*` — Superadmin (all permissions)

**Key Entities**:
- **OrganizationUnit** — Self-referencing tree (Company → Factory → Department → Team)
- **Permission** — Atomic capability definition
- **RolePermission** — Role ↔ Permission many-to-many link
- **UserUnitRole** — User assigned to role at org unit (supports multiple roles per user)
- **UserDirectPermission** — Grant/deny overrides for specific users
- **PermissionCache** — 5-minute TTL cache for resolved permissions

**12 RBAC Management Endpoints** (via `RbacMinimalApi.cs`):
- User role assignment/removal
- Permission grant/deny (direct override)
- Role permission management
- Cache invalidation

### 3. Licensing & Feature-Gating

**Entities**:
- **License** — Contains LicenseKey, ExpiryDate, MaxUsers, IsActive
- **LicenseFeature** — Feature codes (HCM, Production, Finance, Inventory) with enable flag
- **LicenseActivation** — Machine registration (one per license+machine combination)

**Key Flow**:
```
[RequireFeature("HCM")] attribute
        ↓
LicenseValidationFilter checks IsFeatureEnabledAsync()
        ↓
Single database query: Is this feature enabled on active license?
        ↓
403 Forbidden if feature disabled
```

**Benefits**:
- Isolated from RBAC (independent feature gates)
- Single-query check (no N+1 problems)
- Machine activation tracking for compliance

### 4. Public API

**Endpoints**:
- `GET /api/v1/public` — Public data (no auth required)
- `GET /api/v1/status/common` — Common status lookups

---

## Security Features

### ✅ Implemented

1. **JWT Bearer Authentication** — Stateless token-based auth with custom claims
2. **RBAC Authorization** — Fine-grained permission control (Module:Resource:Action)
3. **Feature-Gating** — License-based feature enablement
4. **Soft-Delete** — Query filters enforce soft-delete for audit trails
5. **Audit Trails** — CreatedAt, UpdatedAt, CreatedBy, UpdatedBy on all entities
6. **Permission Caching** — 5-minute TTL reduces database queries
7. **Null-Safety** — Nullable reference types enabled throughout

### ⚠️ Concerns (Documented in SECURITY_ANALYSIS.md)

1. **JWT Secret Hardcoded** — Should use user-secrets or Key Vault
2. **CORS Misconfiguration** — Wildcard origin + AllowCredentials
3. **No Rate Limiting** — Auth endpoints vulnerable to brute force
4. **No Structured Logging** — Missing Serilog integration
5. **Duplicate Interfaces** — ILicenseService defined twice

---

## Database Design

**DbContext**: `CalciferAppDbContext` (inherits `IdentityDbContext<ApplicationUser>`)

**11 Core Tables**:
```
ApplicationUser          → ApplicationRole
    ↓
[UnitRoles] → UserUnitRole → Role/Permission/OrgUnit
[DirectPermissions] → UserDirectPermission → Permission
    
License
├─ LicenseFeature (1:many)
├─ LicenseActivation (1:many)
└─ LicenseType (FK)

OrganizationUnit (self-referencing)
└─ Parent/Children navigation

Permission ←→ RolePermission ←→ ApplicationRole
    ↓
UserDirectPermission (grant/deny override)

CommonStatus (master data for all modules)
PermissionCache (performance optimization)
PublicData (domain-specific)
```

**Soft-Delete Pattern**:
```csharp
// ApplicationUser has:
public bool IsDeleted { get; set; }
public DateTime? DeletedAt { get; set; }
public string? DeletedBy { get; set; }

// Query filter enforces:
.HasQueryFilter(u => !u.IsDeleted)
// Result: Deleted users never appear in SELECT by default
// Override: .IgnoreQueryFilters() when needed for audit
```

---

## Dependency Injection

**Central Hub**: `DependencyContainer/DependencyInversion.cs`

**Registered Services**:
```csharp
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<LicenseService>();
builder.Services.AddScoped<RbacService>();
builder.Services.AddScoped<PublicService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });

builder.Services.AddAuthorization(options => {
    options.AddPolicy("SuperAdminPolicy", /* ... */);
    options.AddPolicy("AdminPolicy", /* ... */);
});

builder.AddCors(options => {
    options.AddPolicy("AllowSpecificOrigins", /* ... */);
});
```

---

## Configuration Files

### `appsettings.Development.json`
```json
{
  "ConnectionStrings": {
    "CalciferDBContext": "Server=localhost;Database=EktaDatabase;Trusted_Connection=True"
  },
  "JwtSettings": {
    "Secret": "calcifer.micro.core.secret.key.[rakibul.h.rabbi].[microservice].[template]",
    "Issuer": "Calcifer.Api",
    "Audience": "Calcifer.Client",
    "ExpirationInMinutes": 60
  }
}
```

**⚠️ Note**: Secret should use `dotnet user-secrets` in production

### `appsettings.Example.json`
Template for deployment (replace placeholders)

---

## Key Files Reference

| Purpose | File Path |
|---------|-----------|
| **Entry Point** | [Program.cs](Program.cs) |
| **DI Configuration** | [DependencyContainer/DependencyInversion.cs](DependencyContainer/DependencyInversion.cs) |
| **JWT Settings** | [AuthHandler/Configuration/JwtSettings.cs](AuthHandler/Configuration/JwtSettings.cs) |
| **Auth APIs** | [AuthHandler/MinimalApis/IdentityApi.cs](AuthHandler/MinimalApis/IdentityApi.cs) |
| **RBAC Service** | [DbContexts/Rbac/Services/RbacService.cs](DbContexts/Rbac/Services/RbacService.cs) |
| **RBAC Filter** | [AuthHandler/Filters/RbacFilter.cs](AuthHandler/Filters/RbacFilter.cs) |
| **License Service** | [Services/LicenseService/LicenseService.cs](Services/LicenseService/LicenseService.cs) |
| **DbContext** | [DbContexts/CalciferAppDbContext.cs](DbContexts/CalciferAppDbContext.cs) |
| **Seeding** | [Infrastructure/DatabaseInitializer.cs](Infrastructure/DatabaseInitializer.cs) |

---

## Quick Start

### 1. Setup Local Environment

```bash
# Clone and restore
dotnet restore

# Set JWT secret
dotnet user-secrets set "JwtSettings:Secret" "your-32-character-minimum-secret-key"

# Apply migrations
dotnet ef database update
```

### 2. Run Application

```bash
# Development with hot-reload
dotnet watch

# Navigate to https://localhost:7000/swagger
```

### 3. Test Authentication

```bash
# Register new user
curl -X POST https://localhost:7000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password@123","name":"Test User"}'

# Login (get JWT)
curl -X POST https://localhost:7000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password@123"}'
```

---

## Architecture Pattern: Pure Minimal API

**Current State**: Mixed (Controllers + Minimal APIs)

**Target State**: Pure Minimal API (controllers only for legacy/complex logic)

**Why?**
- Lower overhead (no MVC infrastructure)
- Modern .NET standard (ASP.NET Core 6+)
- Easier microservice extraction
- Better testability
- Consistent API surface

**Migration Path**:
1. Move `AuthController` logic → `IdentityApi.cs` (consolidate)
2. Move `RoleController` logic → `IdentityApi.cs` (consolidate roles under auth group)
3. Remove `/api/v1/Controllers/` prefix routes
4. Ensure all new endpoints use Minimal APIs

**Implemented**: Most endpoints already use Minimal APIs  
**TODO**: Consolidate remaining controller endpoints

---

## Next Steps

### Immediate (Security)
1. ✅ Move JWT secret to `dotnet user-secrets`
2. ✅ Fix CORS configuration (remove wildcard or remove AllowCredentials)
3. ⏳ Add rate limiting to `/auth/register` and `/auth/login`
4. ⏳ Implement input validation on DTOs

### Short-term (Code Quality)
1. Consolidate auth controllers to Minimal APIs
2. Add Serilog for structured logging
3. Remove duplicate ILicenseService interface
4. Add comprehensive unit tests

### Medium-term (Scalability)
1. Add message bus (RabbitMQ) for async events
2. Implement permission cache invalidation via events
3. Add distributed tracing (OpenTelemetry)
4. Prepare for RBAC/Licensing microservice extraction

---

## Documentation

For detailed information, see:
- **[ARCHITECTURE_DETAILED.md](ARCHITECTURE_DETAILED.md)** — Deep-dive on 5-layer design
- **[SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md)** — Security concerns & recommendations
- **[RBAC_SYSTEM.md](RBAC_SYSTEM.md)** — Permission resolution & caching
- **[LICENSING_SYSTEM.md](LICENSING_SYSTEM.md)** — Feature-gating architecture
- **[DATABASE_SCHEMA.md](DATABASE_SCHEMA.md)** — Entity relationships & indexes
- **[API_DESIGN.md](API_DESIGN.md)** — Endpoint reference & response format
- **[COMMANDS.md](COMMANDS.md)** — CLI commands & troubleshooting
- **[DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md)** — Adding features, patterns

---

**Maintained by**: Rakibul H. Rabbi  
**Last Updated**: April 27, 2026  
**Status**: ✅ Production-Ready (pending security hardening)
