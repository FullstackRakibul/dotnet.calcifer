# Calcifer.Api v1 — Comprehensive Architecture Documentation

**Project Type:** ASP.NET Core 8.0 Enterprise Web API  
**Architecture Pattern:** 5-Layer Layered Monolith (Microservice-Ready)  
**API Style:** Pure Minimal APIs (controllers reserved for legacy/complex logic)  
**Framework:** .NET 8.0  
**Database:** SQL Server with Entity Framework Core 8.0.1  
**Authentication:** JWT Bearer Token (HMAC-SHA256) with custom RBAC claims  
**Authorization:** Advanced RBAC (Module:Resource:Action model) + Feature-Gating  
**API Documentation:** Swagger/OpenAPI (Swashbuckle 6.4.0)  

**Last Updated:** April 28, 2026  
**Status:** ✅ Production-Ready (security hardening in progress)  

---

## 📖 Documentation Hub

For detailed information, see:
- **[README.md](README.md)** — Quick start & learning paths
- **[PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)** — Quick facts & overview
- **[SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md)** — Critical issues & remediation
- **[AUTHHANDLER_ARCHITECTURE.md](AUTHHANDLER_ARCHITECTURE.md)** — Pure Minimal API design
- **[COMMANDS.md](COMMANDS.md)** — CLI reference & development commands
- **[.claude](.claude)** — Project configuration (strict mode, rules, metadata)

---

## 📁 Complete Folder Architecture

### Project Root Structure
```
Calcifer.Api/ (v1)
├── .claude                             # ← Project config (strict mode, rules)
├── README.md                           # ← Start here (documentation index)
├── PROJECT_SUMMARY.md                  # ← Quick facts (5-10 min read)
├── SECURITY_ANALYSIS.md                # ← Security issues & remediation
├── AUTHHANDLER_ARCHITECTURE.md         # ← Pure Minimal API pattern
├── ARCHITECTURE.md                     # ← This file (comprehensive design)
├── COMMANDS.md                         # ← CLI commands reference
│
├── Program.cs                          # Application entry point & DI setup
├── Calcifer.Api.csproj                # Project file (.NET 8.0)
├── v1.sln                             # Solution file
├── dotnet-tools.json                  # Local tool manifest
│
├── appsettings.json                   # Config template (NEVER commit secrets)
├── appsettings.Development.json       # Dev config (use user-secrets instead)
├── appsettings.Example.json           # Deployment template
│
├── AuthHandler/                       # ✅ Security Infrastructure (Pure Minimal API)
│   ├── Claims/
│   │   └── CustomClaims.cs            # JWT claim constants (name, role, perms, unit_roles, etc.)
│   ├── Configuration/
│   │   └── JwtSettings.cs             # Strongly-typed JWT configuration
│   ├── Filters/
│   │   ├── AuthorizationFilter.cs     # IEndpointFilter — global JWT check (minimal APIs)
│   │   ├── RbacFilter.cs              # IAsyncAuthorizationFilter — [RequirePermission] enforcement
│   │   │                              # Dual-path: Fast JWT claims → Slow DB fallback
│   │   └── LicenseValidationFilter.cs # IAsyncAuthorizationFilter — [RequireFeature] gating
│   └── MinimalApis/
│       ├── IdentityApi.cs             # POST /auth/register, POST /auth/login, GET /auth/me
│       │                              # POST /auth/change-password, role management (12+ endpoints)
│       └── LicenseApi.cs              # License management endpoints
│
├── Controllers/                       # ⏳ LEGACY (consolidation to Minimal APIs in progress)
│   ├── HomeController.cs              # Root endpoints
│   └── AuthController/
│       ├── AuthController.cs          # ⏳ TO-DELETE (move to IdentityApi.cs)
│       └── RoleController.cs          # ⏳ TO-DELETE (move to IdentityApi.cs /roles group)
│
├── DbContexts/                        # Data Access Layer (EF Core models)
│   ├── CalciferAppDbContext.cs        # Main IdentityDbContext
│   │
│   ├── AuthModels/
│   │   ├── ApplicationUser.cs         # User entity (IdentityUser + soft-delete)
│   │   └── ApplicationRole.cs         # Role entity (IdentityRole)
│   │
│   ├── Common/
│   │   ├── AuditBase.cs               # Base class: CreatedAt, UpdatedAt, DeletedAt, etc.
│   │   ├── CommonStatus.cs            # Status master data (User, License, RBAC, General modules)
│   │   └── TableOperationDetails.cs   # Audit trail infrastructure
│   │
│   ├── Licensing/                     # License domain
│   │   ├── License.cs                 # License (key, expiry, maxusers, isactive)
│   │   ├── LicenseType.cs             # License classification
│   │   ├── LicenseFeature.cs          # Feature codes (HCM, Production, Finance, Inventory)
│   │   └── LicenseActivation.cs       # Machine activation tracking
│   │
│   ├── Rbac/                          # ✅ Role-Based Access Control Engine
│   │   ├── Entities/
│   │   │   ├── OrganizationUnit.cs    # Self-referencing org tree (Company→Factory→Dept→Team)
│   │   │   ├── Permission.cs          # Atomic capability (Module:Resource:Action)
│   │   │   ├── RolePermission.cs      # Role ↔ Permission many-to-many link
│   │   │   ├── UserUnitRole.cs        # User assigned to role @ org unit (supports multiple)
│   │   │   ├── UserDirectPermission.cs# Direct grant/deny override (explicit allow/block)
│   │   │   └── PermissionCache.cs     # Cached permissions (5-min TTL for performance)
│   │   ├── Interfaces/
│   │   │   └── IRbacService.cs        # 13 methods: resolution, cache, roles, config
│   │   ├── Services/
│   │   │   └── RbacService.cs         # Full RBAC implementation (dual-path permission check)
│   │   ├── Enums/
│   │   │   └── RbacEnums.cs           # OrgUnitLevel constants
│   │   ├── Extensions/
│   │   │   └── RbacExtensions.cs      # Helper methods
│   │   ├── MinimalApis/
│   │   │   └── RbacMinimalApi.cs      # 12 RBAC management endpoints
│   │   └── Seeds/
│   │       ├── RbacPermissionSeeder.cs# Seed permissions + role-permission matrix
│   │       └── OrgUnitSeeder.cs       # Seed org tree (Company, Factory, HQ, etc.)
│   │
│   ├── Models/
│   │   ├── PublicData.cs              # Domain-specific public data entity
│   │   └── Seeders/                   # Seeder implementations
│   │
│   ├── Enum/
│   │   └── CommonStatusEnum.cs        # Status enumeration
│   │
│   └── DTOs/                          # Data Transfer Objects
│       ├── ApiResponseDto.cs          # Standard response wrapper { status, message, data, errorCode }
│       ├── ClientTypeDto.cs           # Client type DTO
│       ├── PublicDataDTO.cs           # Public data DTO
│       ├── AuthDTO/
│       │   ├── LoginRequestDto.cs
│       │   ├── LoginResponseDto.cs
│       │   ├── RegisterRequestDto.cs
│       │   ├── UserProfileDto.cs
│       │   ├── ChangePasswordDto.cs
│       │   ├── CreateRoleRequestDto.cs
│       │   ├── RoleResponseDto.cs
│       │   └── AssignRoleRequestDto.cs
│       ├── CommonDTO/
│       │   └── CommonStatusDto.cs
│       ├── LicenseDTO/
│       │   └── LicenseDto.cs
│       └── RbacDTO/
│           ├── PermissionDto.cs
│           ├── UserUnitRoleDto.cs
│           └── OrganizationUnitDto.cs
│
├── Services/                          # Business Logic Layer
│   ├── PublicService.cs               # IPublicInterface implementation
│   ├── AuthService/
│   │   ├── AuthService.cs             # Register, login, profile, password change
│   │   ├── TokenService.cs            # JWT generation with RBAC claims embedding
│   │   └── RoleService.cs             # Role CRUD (SuperAdmin only)
│   ├── LicenseService/
│   │   └── LicenseService.cs          # License validation, feature-gating, machine activation
│   └── (RBAC logic in DbContexts/Rbac/Services/)
│
├── Interface/                         # Service Contracts
│   ├── Common/
│   │   └── IPublicInterface.cs        # Public service contract
│   └── Licensing/
│       └── ILicenseService.cs         # License service contract
│       └── (IRbacService in DbContexts/Rbac/Interfaces/)
│
├── DependencyContainer/               # IoC Hub
│   └── DependencyInversion.cs         # ← CENTRAL REGISTRATION POINT
│       ├─ Registers all services, filters, policies
│       ├─ JWT Bearer setup
│       ├─ Authorization policies (SuperAdminPolicy, AdminPolicy, etc.)
│       └─ CORS configuration
│
├── Middleware/                        # HTTP Pipeline
│   └── MiddlewareDependencyInversion.cs # Middleware registration & ordering
│       ├─ Authentication/Authorization headers
│       ├─ Minimal API group setup (/api/v1)
│       └─ Filter application order (License → RBAC → Auth)
│
├── Infrastructure/                    # Infrastructure Services
│   └── DatabaseInitializer.cs         # Seeding on startup (idempotent)
│       ├─ CommonStatus rows
│       ├─ OrgUnitSeeder (org tree)
│       ├─ RbacPermissionSeeder (permissions + matrix)
│       └─ SuperAdmin user creation
│
├── MinimalApis/
│   └── PublicApis/
│       ├── PublicCRUDApis.cs          # GET /api/v1/public
│       ├── CommonStatusApi.cs         # Status lookups
│       ├── RbacMinimalApi.cs          # 12 RBAC management endpoints
│       └── UsageExamples/
│           └── FinancePayroll.cs      # Example module
│
├── Migrations/                        # EF Core Migrations
│   ├── 20260418045256_initial.cs
│   ├── 20260418045256_initial.Designer.cs
│   └── CalciferAppDbContextModelSnapshot.cs
│
├── Properties/
│   └── launchSettings.json            # IIS Express & Kestrel profiles
│
├── bin/                              # Build output (Debug/Release)
├── obj/                              # Intermediate build files
│
└── [Build artifacts, runtime outputs, etc.]
```

---

## 🏗️ 5-Layer Architecture Pattern

**Principle**: Strict layer separation with dependency injection enabling loose coupling and testability.

```
┌─────────────────────────────────────────────────────────┐
│  1. API LAYER                                           │
│  ├─ Pure Minimal APIs (IdentityApi.cs, RbacMinimalApi)  │
│  ├─ Standard Controllers (legacy, to consolidate)       │
│  └─ HTTP status codes + DTO marshaling                  │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  2. DTO LAYER                                           │
│  ├─ LoginRequestDto, RegisterRequestDto                │
│  ├─ ApiResponseDto<T> (standard wrapper)               │
│  ├─ RoleResponseDto, PermissionDto, etc.               │
│  └─ Zero domain model leakage to clients               │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  3. SERVICE LAYER                                       │
│  ├─ AuthService (register, login, profile)             │
│  ├─ TokenService (JWT with RBAC claims)                │
│  ├─ RoleService (role management)                      │
│  ├─ LicenseService (validation, feature-gating)        │
│  ├─ RbacService (permission resolution)                │
│  └─ PublicService (domain operations)                  │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  4. INTERFACE LAYER                                     │
│  ├─ ILicenseService                                     │
│  ├─ IRbacService                                        │
│  └─ IPublicInterface                                    │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  5. DATA ACCESS LAYER                                   │
│  ├─ CalciferAppDbContext (IdentityDbContext)           │
│  ├─ Entity Models (ApplicationUser, License, etc.)     │
│  ├─ EF Core query builders & change tracking           │
│  └─ Soft-delete query filters enforcement              │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  SQL Server Database                                    │
│  ├─ 11 tables with proper indexes & constraints        │
│  └─ ACID transactions, FK relationships                │
└─────────────────────────────────────────────────────────┘
```

---

## 🔐 Authorization Filter Chain (Execution Order)

**CRITICAL: Filters run in this exact order**

```
Request arrives (e.g., GET /api/v1/hcm/employees with Bearer token)
    ↓
┌───────────────────────────────────────────────────────────────┐
│ STEP 1: LicenseValidationFilter (IAsyncAuthorizationFilter)   │
│  └─ Checks: [RequireFeature("HCM")]                           │
│  └─ Query: Is HCM feature enabled on active, non-expired      │
│     license?                                                  │
│  └─ Result: 403 Forbidden if feature not licensed            │
└───────────────────────────────────────────────────────────────┘
         ↓ (licensed)
┌───────────────────────────────────────────────────────────────┐
│ STEP 2: RbacAuthorizationFilter (IAsyncAuthorizationFilter)   │
│  └─ Checks: [RequirePermission("HCM", "Employee", "Read")]    │
│  └─ Fast Path: Check JWT claims (no DB hit)                  │
│     OR Slow Path: Query DB if claim missing                  │
│  └─ Result: 403 Forbidden if permission denied               │
└───────────────────────────────────────────────────────────────┘
         ↓ (permitted)
┌───────────────────────────────────────────────────────────────┐
│ STEP 3: AuthorizationFilter (IEndpointFilter)                │
│  └─ Applied globally to /api/v1 Minimal API group            │
│  └─ Checks: Is user.Identity.IsAuthenticated?                │
│  └─ Result: 401 Unauthorized if no valid JWT                 │
└───────────────────────────────────────────────────────────────┘
         ↓ (authenticated)
┌───────────────────────────────────────────────────────────────┐
│ STEP 4: Request Handler                                       │
│  └─ Business logic execution                                 │
│  └─ Database queries                                         │
│  └─ Response generation                                      │
└───────────────────────────────────────────────────────────────┘
         ↓
Response sent (200 OK + JSON data OR error)
```

**Key Principle**: License gates at module level (first check), RBAC gates at permission level (second check), authentication gates at user level (third check).

---

## 🔑 Pure Minimal API Architecture

**Modern .NET Standard** for identity/auth infrastructure (ASP.NET Core 6+)

### Design Principles

1. **Minimal APIs as Primary Surface** — All new endpoints via `/api/v1/*` minimal APIs
2. **Controllers Reserved** — Only for complex, view-based, or legacy logic
3. **Consistent URL Structure** — No `/api/v1/Controllers/auth/*` prefix
4. **Lower Overhead** — No MVC controller machinery needed
5. **Easier Testing** — Thin HTTP layer simplifies unit tests
6. **Microservice Ready** — Simpler to extract as separate services

### Current State vs. Target State

```
CURRENT (Mixed):
  ✅ /api/v1/auth/login (Minimal API) — CORRECT
  ✅ /api/v1/auth/register (Minimal API) — CORRECT
  ❌ /api/v1/Controllers/auth/login (Controller) — DEPRECATED
  ❌ /api/v1/Controllers/role/* (Controller) — DEPRECATED

TARGET (Pure Minimal API):
  ✅ /api/v1/auth/register (Minimal API)
  ✅ /api/v1/auth/login (Minimal API)
  ✅ /api/v1/auth/me (Minimal API)
  ✅ /api/v1/auth/change-password (Minimal API)
  ✅ /api/v1/roles/* (Minimal API group)
  ✅ /api/v1/rbac/* (Minimal API group — 12 routes)
  ✅ /api/v1/public/* (Minimal API)
  ✅ /api/v1/licenses/* (Minimal API)
```

### Consolidation Path

**Phase 1** (30 min):
- Delete `Controllers/AuthController/AuthController.cs`
- Verify all auth logic in `AuthHandler/MinimalApis/IdentityApi.cs`
- Update client docs: old routes deprecated

**Phase 2** (30 min):
- Delete `Controllers/AuthController/RoleController.cs`
- Ensure all role endpoints in `/api/v1/roles/*` group (in IdentityApi.cs)
- Test all endpoints

**Result**: Pure Minimal API for identity/auth infrastructure

---

## 🔐 Security Architecture

### JWT Token Structure

```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "name": "User Name",
  "emp_id": "EMP-001",
  "iat": 1234567890,
  "exp": 1234571490,
  
  "roles": ["SUPERADMIN", "HR_MANAGER"],
  
  "perms": [
    "HCM:Employee:Read",
    "HCM:Employee:Update",
    "HCM:Payroll:Export",
    "*:*:*"
  ],
  
  "unit_roles": [
    "Factory-1:HR_Manager",
    "Factory-2:Accountant"
  ]
}
```

**Key Fields**:
- `perms` — Embedded RBAC permissions (reduces DB queries)
- `unit_roles` — Organization unit role context
- All claims resolved by `TokenService` after successful login

### RBAC Permission Model

**Format**: `Module:Resource:Action`

```
Module         Resource        Action
  ↓              ↓               ↓
"HCM"      +   "Employee"  +  "Read"    → "HCM:Employee:Read"
"HCM"      +   "Payroll"   +  "Export"  → "HCM:Payroll:Export"
"Finance"  +   "*"         +  "*"       → "Finance:*:*" (all finance)
"*"        +   "*"         +  "*"       → "*:*:*" (superadmin)
```

### Three-Part Authorization Check

```csharp
// 1. Feature-Gating (LicenseValidationFilter)
[RequireFeature("HCM")]
public async Task<IActionResult> GetEmployees() { }
// Result: 403 if HCM not licensed

// 2. Permission Check (RbacFilter)
[RequirePermission("HCM", "Employee", "Read")]
public async Task<IActionResult> GetEmployees() { }
// Result: 403 if user lacks "HCM:Employee:Read"

// 3. Authentication (AuthorizationFilter)
.RequireAuthorization()
// Result: 401 if no valid JWT
```

---

## 💾 Database Schema (11 Tables)

### Entity Relationship Diagram (Logical)

```
┌─────────────────────────────────────────────────────────────────────┐
│                    IDENTITY & AUDIT LAYER                          │
├─────────────────────────────────────────────────────────────────────┤
│
│  ┌──────────────────┐         ┌──────────────────────┐
│  │ ApplicationUser  │         │  ApplicationRole     │
│  ├──────────────────┤         ├──────────────────────┤
│  │ Id (PK)          │         │ Id (PK)              │
│  │ Email (unique)   │◄────────┤ Name (e.g. Admin)    │
│  │ EmployeeId ──────┼─┐       │ Description          │
│  │  (unique index)  │ │       │ IsActive             │
│  │ Name             │ │       │ CreatedAt            │
│  │ Region           │ │       └──────────────────────┘
│  │ IsActive         │ │
│  │ IsDeleted        │ │       (FK: IdentityRole via Identity)
│  │ CreatedAt        │ │
│  │ UpdatedAt        │ │
│  │ DeletedAt        │ │
│  │ CreatedBy        │ │
│  │ UpdatedBy        │ │
│  │ DeletedBy        │ │
│  └──────────────────┘ │
│         │FK           │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                   COMMON REFERENCE LAYER                           │
├─────────────────────────────────────────────────────────────────────┤
│
│  ┌────────────────────┐
│  │  CommonStatus      │
│  ├────────────────────┤
│  │ Id (PK)            │
│  │ StatusName         │
│  │ Module (enum)      │ ← (User/License/RBAC/General)
│  │ IsActive           │
│  │ SortOrder          │
│  │ Description        │
│  └────────────────────┘
│        ▲ (FK)
│        │
│        └──(License.CommonStatusId, ApplicationUser.CommonStatusId)
│
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                   LICENSE & FEATURE LAYER                          │
├─────────────────────────────────────────────────────────────────────┤
│
│  ┌──────────────────┐
│  │  License         │
│  ├──────────────────┤
│  │ Id (PK)          │
│  │ LicenseKey       │◄─ (unique index)
│  │  (e.g. 8/25/26)  │
│  │ LicenseGuid      │
│  │ FK→LicenseType   │
│  │ FK→CommonStatus  │
│  │ IssuedAt         │
│  │ ExpiresAt        │
│  │ MaxUsers         │
│  │ IsActive         │
│  │ CreatedAt        │
│  │ UpdatedAt        │
│  │ DeletedAt        │
│  └──────────────────┘
│      │   │ (FK)
│      │   └───────────┐
│      │               │
│      │ FK            │ FK
│      ↓               ↓
│  ┌──────────────┐ ┌──────────────────────┐
│  │ LicenseType  │ │ LicenseFeature       │
│  ├──────────────┤ ├──────────────────────┤
│  │ Id           │ │ Id (PK)              │
│  │ Name         │ │ FK→License           │
│  │ Description  │ │ FeatureCode          │
│  │ IsActive     │ │  (HCM, Production,   │
│  └──────────────┘ │   Finance, Inventory)│
│                   │ Description          │
│  ┌──────────────────────┐ IsEnabled    │
│  │ LicenseActivation    │ CreatedAt    │
│  ├──────────────────────┤ └──────────────────────┘
│  │ Id (PK)              │
│  │ FK→License           │
│  │ MachineId            │◄─ (unique index with LicenseId)
│  │ ActivatedAt          │
│  │ IsActive             │
│  │ ActivatedByUserId    │
│  └──────────────────────┘
│
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│              ORGANIZATION & RBAC LAYER                             │
├─────────────────────────────────────────────────────────────────────┤
│
│  ┌─────────────────────┐
│  │ OrganizationUnit    │ ← Self-referencing tree
│  ├─────────────────────┤
│  │ Id (PK)             │
│  │ Code (unique index) │
│  │ Name                │◄─ (e.g. "Factory-1", "HR Dept")
│  │ Description         │
│  │ Level (0-3)         │
│  │ FK→ParentId (self)  │◄─ (nullable, self-reference)
│  │ IsActive            │
│  │ CreatedAt           │
│  │ UpdatedAt           │
│  │ DeletedAt           │
│  └─────────────────────┘
│      │ (tree structure)
│      └─ Company
│         ├─ Factory-1
│         │  ├─ HR Department
│         │  └─ Finance Department
│         └─ Factory-2
│            └─ Operations
│
│  ┌────────────────────┐       ┌──────────────────────┐
│  │ Permission         │       │  UserUnitRole        │
│  ├────────────────────┤       ├──────────────────────┤
│  │ Id (PK)            │       │ UserId + RoleId +    │
│  │ Module (e.g. HCM)  │◄──┐   │ UnitId (Composite PK)│
│  │ Resource           │   │   │ FK→ApplicationUser   │
│  │  (e.g. Employee)   │   │   │ FK→ApplicationRole   │
│  │ Action             │   │   │ FK→OrganizationUnit  │
│  │  (e.g. Read)       │   │   │ ValidFrom (datetime?)│
│  │ Description        │   │   │ ValidTo (datetime?)  │
│  │ IsActive           │   │   │ IsActive             │
│  │ UniqueIndex:       │   │   │ AssignedAt           │
│  │  Module+Resource   │   │   │ AssignedBy           │
│  │  +Action           │   └───│ (FK→RolePermission)  │
│  └────────────────────┘       └──────────────────────┘
│         ▲ (FK)
│         │
│  ┌──────────────────────────┐
│  │ RolePermission           │
│  ├──────────────────────────┤
│  │ RoleId + PermissionId    │
│  │  (Composite PK)          │
│  │ FK→ApplicationRole       │
│  │ FK→Permission            │
│  │ CreatedAt                │
│  │ CreatedBy                │
│  └──────────────────────────┘
│
│  ┌──────────────────────────┐
│  │ UserDirectPermission     │
│  ├──────────────────────────┤
│  │ Id (PK)                  │
│  │ FK→ApplicationUser       │
│  │ FK→Permission            │
│  │ IsGranted (bool)         │◄─ (true=grant, false=deny)
│  │ ExpiresAt (datetime?)    │
│  │ GrantedBy                │
│  │ IsDeleted (soft-delete)  │
│  │ DeletedAt                │
│  │ RevokedBy                │
│  └──────────────────────────┘
│
│  ┌────────────────────────────────┐
│  │ PermissionCache (5-min TTL)    │
│  ├────────────────────────────────┤
│  │ UserId + UnitId (Composite PK) │
│  │ PermissionsJson (JSON array)   │
│  │ GeneratedAt (datetime)         │
│  └────────────────────────────────┘
│
└─────────────────────────────────────────────────────────────────────┘
```

### Table Definitions

| Table | Rows | Purpose | Key Features |
|-------|------|---------|--------------|
| **ApplicationUser** | ~100 | ASP.NET Core Identity user | Email unique, soft-delete, audit trail, EmployeeId unique index |
| **ApplicationRole** | ~10 | ASP.NET Core Identity role | SUPERADMIN, ADMIN, MODERATOR, etc. |
| **UserRole** (Identity) | ~200 | IdentityUserRole FK | Links users to roles (ASP.NET managed) |
| **CommonStatus** | ~20 | Reference data | Active/Inactive/Pending per module |
| **License** | ~50 | License records | Key-based, expiry-based, max-users based |
| **LicenseType** | ~5 | License classification | Standard, Premium, Enterprise, etc. |
| **LicenseFeature** | ~20 | Feature codes per license | HCM, Production, Finance, Inventory |
| **LicenseActivation** | ~100 | Machine activation | Machine unique per license |
| **OrganizationUnit** | ~50 | Org tree (self-ref) | Company → Factory → Dept → Team |
| **Permission** | ~150 | Atomic permissions | Module:Resource:Action (unique composite) |
| **RolePermission** | ~500 | Role-permission matrix | Links 10 roles × 150 permissions |
| **UserUnitRole** | ~200 | User @ role @ unit assignment | Time-bounded assignments |
| **UserDirectPermission** | ~50 | Direct grant/deny | Overrides role-based permissions |
| **PermissionCache** | ~100 | Cached permissions (5-min TTL) | UserId+UnitId key, JSON permissions |

### Data Relationships

#### Soft-Delete Pattern

All user-managed entities follow soft-delete (not hard delete):
```csharp
public class ApplicationUser : IdentityUser
{
    public bool IsDeleted { get; set; }        // false = active, true = deleted
    public DateTime? DeletedAt { get; set; }   // when deleted
    public string? DeletedBy { get; set; }     // who deleted
}

// Query filter: HasQueryFilter(u => !u.IsDeleted)
// Result: All queries exclude soft-deleted by default
```

**Benefit**: No data loss, audit trail preserved, can restore user

#### Audit Trailing

All entities inherit from `AuditBase`:
```csharp
public abstract class AuditBase
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
}
```

**Benefit**: Complete operational history, compliance audits

#### Organizational Scoping

`UserUnitRole` links user to organization unit to role:
```
User "Alice"
  ├─ Role "HR_Manager" @ Unit "Factory-1"      (can manage Factory-1 HR)
  └─ Role "Accountant" @ Unit "Factory-2"      (can manage Factory-2 accounting)

Permission check:
  GET /api/v1/hcm/payroll?factoryId=2
  User.Permissions include "Factory-2:Accountant"?
  → User can access Factory-2 payroll, NOT Factory-1
```

**Benefit**: Multi-tenant-like control within single org, delegated authority

---

## 🔄 Request Flow Lifecycle

### Example: GET /api/v1/hcm/employees

```
1. HTTP Request arrives
   GET /api/v1/hcm/employees
   Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

2. Program.cs middleware pipeline invoked
   ├─ Authentication middleware validates bearer token
   │  └─ Decodes JWT, extracts claims, sets User.Identity
   └─ Authorization middleware configured (no-op for now)

3. Request routed to Minimal API handler
   app.MapGroup("/api/v1/hcm")
       .MapGet("/employees", GetEmployees)
       .RequireAuthorization()           // ← Sets up auth requirement
       .WithName("GetEmployees")
       .Produces<ApiResponseDto<List<EmployeeDto>>>(200)
       .Produces(401)
       .Produces(403);

4. Endpoint filter chain executes (BEFORE handler)

   a) LicenseValidationFilter
      ├─ Checks: [RequireFeature("HCM")] attribute
      ├─ Query: SELECT * FROM LicenseFeature
      │         WHERE FeatureCode = 'HCM'
      │         AND License.IsActive = 1
      │         AND License.ExpiresAt > GETDATE()
      └─ Result: Allowed (feature enabled) OR 403 Forbidden

   b) RbacAuthorizationFilter
      ├─ Checks: [RequirePermission("HCM", "Employee", "Read")] attribute
      ├─ Fast path: Extract from JWT claims.perms
      │   └─ Includes "HCM:Employee:Read"?
      ├─ Slow path (if not in JWT): Query database
      │   ├─ SELECT * FROM PermissionCache
      │   │  WHERE UserId = '@userId'
      │   │  AND GeneratedAt > GETDATE() - 5 min
      │   ├─ Resolve from UserUnitRole → RolePermission
      │   ├─ Apply UserDirectPermission (deny overrides)
      │   └─ Update cache if stale
      └─ Result: Permission granted OR 403 Forbidden

   c) AuthorizationFilter
      ├─ Checks: user.Identity.IsAuthenticated
      ├─ No valid JWT?
      └─ Result: 401 Unauthorized

5. Request handler executes (if all filters pass)
   async Task<IActionResult> GetEmployees(
       IEmployeeService employeeService,
       HttpContext context)
   {
       var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       var unitId = context.User.FindFirst("unit_id")?.Value;
       
       // Business logic
       var employees = await employeeService.GetByUnitAsync(
           Guid.Parse(unitId)
       );
       
       return Ok(new ApiResponseDto<List<EmployeeDto>>
       {
           Status = true,
           Message = "Employees retrieved successfully",
           Data = employees
       });
   }

6. IEmployeeService implementation
   ├─ Query EF Core DbContext
   ├─ SELECT * FROM Employees WHERE UnitId = '@unitId'
   ├─ Map Employee entity to EmployeeDto
   └─ Return list

7. Response generated
   Status Code: 200 OK
   Content-Type: application/json
   Body:
   {
       "status": true,
       "message": "Employees retrieved successfully",
       "data": [
           { "id": "emp-001", "name": "John", "unit": "Factory-1" },
           { "id": "emp-002", "name": "Jane", "unit": "Factory-1" }
       ],
       "errorCode": null
   }

8. Response sent to client
```

---

## 🎯 Design Patterns

### 1. Dependency Injection (IoC)

**Location**: `DependencyInversion.cs`

```csharp
services.AddScoped<IEmployeeService, EmployeeService>();
services.AddScoped<IRbacService, RbacService>();
services.AddSingleton<IConfiguration>(configuration);
```

**Benefit**: Loose coupling, testability, single responsibility

### 2. Filter Chain (Authorization)

**Location**: Filters in `AuthHandler/Filters/`

```
License Filter → RBAC Filter → Auth Filter → Handler
```

**Benefit**: Cross-cutting concerns, clean handler logic, reusable

### 3. DTO Layer

**Location**: `DbContexts/DTOs/`

```csharp
// Domain model (internal)
public class Employee : AuditBase { }

// DTO (external contract)
public class EmployeeDto { }
```

**Benefit**: Protects domain from API client changes, versioning support

### 4. Repository Pattern (via EF Core DbContext)

**Location**: `DbContexts/CalciferAppDbContext.cs`

```csharp
public DbSet<Employee> Employees { get; set; }
// Equivalent to repository
var employee = dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);
```

**Benefit**: Abstraction from database, easier testing

### 5. Query Filter (Soft Delete)

**Location**: `CalciferAppDbContext.OnModelCreating()`

```csharp
modelBuilder.Entity<ApplicationUser>()
    .HasQueryFilter(u => !u.IsDeleted);
// All queries automatically exclude soft-deleted
```

**Benefit**: Prevents accidental exposure of deleted data

### 6. Claims-Based Authorization

**Location**: `AuthHandler/Filters/RbacFilter.cs` + JWT claims

```csharp
[RequirePermission("HCM", "Employee", "Read")]
// Checks: User.FindAll("perms").Contains("HCM:Employee:Read")
```

**Benefit**: Stateless, scalable, no session storage

---

## ✅ Validation Checklist

Before deploying to production:

- [ ] **JWT Secret**: Moved to `appsettings.Production.json` (NOT in repo)
- [ ] **CORS**: Removed wildcard `*` when using `AllowCredentials()`
- [ ] **Rate Limiting**: Added AspNetCoreRateLimit to `/auth/*` endpoints
- [ ] **Logging**: Structured logging with Serilog or NLog configured
- [ ] **HTTPS**: Only HTTPS allowed in production (Startup.cs)
- [ ] **Database**: User-secrets for connection strings, no hardcoded secrets
- [ ] **Migrations**: Run `dotnet ef database update` before deployment
- [ ] **Swagger**: Disabled in production (no `/swagger` UI exposed)
- [ ] **Audit Trail**: Verified CreatedBy, UpdatedBy populated correctly
- [ ] **Cache Invalidation**: Permission cache expires after 5 minutes

---

## 📚 Learning Path

### For New Developers
1. Start: [README.md](README.md) — Overview & setup
2. Quick Facts: [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)
3. Architecture: This file ([ARCHITECTURE.md](ARCHITECTURE.md))
4. Security: [SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md)
5. Auth Deep Dive: [AUTHHANDLER_ARCHITECTURE.md](AUTHHANDLER_ARCHITECTURE.md)
6. Commands: [COMMANDS.md](COMMANDS.md)

### For Security Reviews
1. [SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md) — Issues & risks
2. This file → "Security Architecture" section
3. [AUTHHANDLER_ARCHITECTURE.md](AUTHHANDLER_ARCHITECTURE.md) → Filter chain

### For Integration
1. [COMMANDS.md](COMMANDS.md) — Setup & build
2. API routes in `/api/v1/*`
3. DTOs in `DbContexts/DTOs/`
4. Swagger UI at `/swagger`

---

## 🔧 Development Workflow

### Setup (First Time)
```bash
# Clone & navigate
git clone <repo>
cd Calcifer.Api/v1

# Configure secrets
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:Secret" "your-secret-here"

# Restore & build
dotnet restore
dotnet build

# Apply migrations
dotnet ef database update

# Run
dotnet run
```

### Daily Development
```bash
# Open in VS Code
code .

# Debug (F5) or
dotnet run

# Watch for changes
dotnet watch run

# View Swagger UI
# → http://localhost:5000/swagger

# Run tests
dotnet test
```

### Deploy
```bash
# Create release build
dotnet publish -c Release -o ./publish

# Deploy to Azure/Server
# (See COMMANDS.md for details)
```

---

## 🎓 Quick Reference

### Key Files to Know

| File | Purpose | When to Edit |
|------|---------|--------------|
| `Program.cs` | App startup, DI setup | Never (unless adding new service) |
| `DependencyInversion.cs` | Service registration | Adding new service, new policy |
| `MiddlewareDependencyInversion.cs` | Middleware order | Changing filter order, new endpoint group |
| `CalciferAppDbContext.cs` | EF Core models | Adding new entity, query filter |
| `AuthHandler/Filters/*.cs` | Authorization | Changing auth logic (rare) |
| `Services/*/` | Business logic | Most changes here |
| `DbContexts/DTOs/` | API contracts | Versioning API, new fields |
| `Migrations/` | Schema history | Auto-generated, review before commit |

### API Response Format
```json
{
    "status": true|false,
    "message": "Human-readable message",
    "data": { /* your data */ },
    "errorCode": "ERROR_CODE" | null
}
```

### HTTP Status Codes
- `200 OK` — Success
- `201 Created` — Resource created
- `400 Bad Request` — Validation error
- `401 Unauthorized` — No valid JWT
- `403 Forbidden` — JWT valid but insufficient permissions
- `404 Not Found` — Resource not found
- `500 Internal Server Error` — Unexpected error

---

## 📞 Support & Questions

- **Architecture Questions**: See [ARCHITECTURE.md](ARCHITECTURE.md) (this file)
- **Security Issues**: See [SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md)
- **Setup Help**: See [COMMANDS.md](COMMANDS.md)
- **Code Examples**: See [AUTHHANDLER_ARCHITECTURE.md](AUTHHANDLER_ARCHITECTURE.md)

---

---

## 🚀 Implementation & Hardening Roadmap

### Phase 1: Core Logging & Tracing Infrastructure

#### Dynamic Log Writer Helper
**Location**: `Helper/LogWriter/LogWriter.cs`

**Purpose**: Centralized, dynamic logging for all system operations
- ✅ Writes to `/logs` directory (text-based, UTF-8)
- ✅ Daily log files: `YYYY-MM-DD_LogType.txt`
- ✅ Log types: Action, Validation, Error, Response, Try, Failed
- ✅ Correlation IDs for distributed tracing
- ✅ IP address + User ID tracking
- ✅ Exception stack traces

**Usage**:
```csharp
// In DependencyInversion.cs
services.AddDynamicLogWriter();

// In any service/controller
public class AuthService
{
    private readonly ILogWriter _logger;
    
    public AuthService(ILogWriter logger) => _logger = logger;
    
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var correlationId = _logger.GetCorrelationId();
        
        try
        {
            // Action log
            await _logger.LogActionAsync(
                "User Login Attempt",
                "Auth",
                $"Email: {dto.Email}",
                correlationId
            );
            
            // Validation
            if (string.IsNullOrEmpty(dto.Email))
            {
                await _logger.LogValidationAsync(
                    "Email Validation",
                    "Failed",
                    "Email is required",
                    correlationId
                );
                throw new ValidationException("Email required");
            }
            
            // Success response
            var token = GenerateToken(...);
            await _logger.LogResponseAsync(
                "/api/v1/auth/login",
                "POST",
                200,
                "Login successful",
                correlationId
            );
            
            return new LoginResponseDto { Token = token };
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync(
                "Login failed",
                ex,
                correlationId
            );
            throw;
        }
    }
}
```

**Benefits**:
- Full audit trail of all operations
- Correlation IDs link related logs across multiple services
- Searchable text files for compliance audits
- No external dependencies (text files vs. ELK stack)

---

### Phase 2: Enhanced Audit Trail (Enterprise-Grade)

#### Updated AuditBase with Field-Level Changes

```csharp
public abstract class AuditBase
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // ← ALWAYS UTC
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
    
    // NEW: Field-level change tracking
    public string? CorrelationId { get; set; }  // Links to ILogWriter
    public string? IpAddress { get; set; }      // Client IP
    public string? UserAgent { get; set; }      // Browser/device info
}
```

#### New: AuditLog Table

```sql
CREATE TABLE AuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CorrelationId NVARCHAR(MAX),
    EntityName NVARCHAR(MAX),      -- "ApplicationUser", "License", etc.
    EntityId UNIQUEIDENTIFIER,
    Action NVARCHAR(50),            -- "Created", "Updated", "Deleted"
    FieldName NVARCHAR(MAX),        -- "Email", "IsActive", etc.
    OldValue NVARCHAR(MAX),         -- Previous value
    NewValue NVARCHAR(MAX),         -- Current value
    ChangedBy NVARCHAR(MAX),        -- User who made change
    ChangedAt DATETIME,
    IpAddress NVARCHAR(MAX),        -- Source IP
    UserAgent NVARCHAR(MAX),        -- Device info
    CreatedAt DATETIME = GETUTCDATE()
);
```

**Entity Framework Configuration**:
```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; }
    public string EntityName { get; set; }
    public Guid EntityId { get; set; }
    public string Action { get; set; }          // Created, Updated, Deleted
    public string FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
```

---

### Phase 3: JWT & Token Management Hardening

#### JWT Optimization: Move unit_roles to Cache

**Current Issue**: JWT grows large with many org unit assignments
**Solution**: Keep unit_roles out of JWT, use PermissionCache exclusively

```csharp
// BEFORE (Large JWT):
{
  "sub": "user-id",
  "perms": ["HCM:*:*", "Finance:Report:Read", ...],
  "unit_roles": [
    "Factory-1:HR_Manager",
    "Factory-2:Accountant",
    ... (50 more entries)
  ]
}

// AFTER (Optimized JWT):
{
  "sub": "user-id",
  "perms": ["HCM:*:*", "Finance:Report:Read", ...],
  "unit_id": "Factory-1"  // ← Current org context only
}
```

#### Token Revocation List (TRL)

```csharp
// New entity: TokenRevocation
public class TokenRevocation
{
    public Guid Id { get; set; }
    public string TokenJti { get; set; }         // JWT ID claim
    public Guid UserId { get; set; }
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    public string RevokedReason { get; set; }   // "Logout", "SecurityBreach", "PasswordChange"
    public string? IpAddress { get; set; }
    public DateTime ExpiresAt { get; set; }     // Cleanup after token expires
}

// In AuthService
public async Task LogoutAsync(string userId, string token, string reason)
{
    var jti = GetJtiFromToken(token);
    
    var revocation = new TokenRevocation
    {
        TokenJti = jti,
        UserId = Guid.Parse(userId),
        RevokedReason = reason,
        ExpiresAt = DateTime.UtcNow.AddMinutes(70)  // Token TTL + buffer
    };
    
    await _dbContext.TokenRevocations.AddAsync(revocation);
    await _dbContext.SaveChangesAsync();
    
    await _logger.LogActionAsync(
        "User Logout",
        "Auth",
        $"UserId: {userId}, Reason: {reason}"
    );
}

// In RbacFilter (during permission check)
private async Task<bool> IsTokenRevokedAsync(string jti)
{
    return await _dbContext.TokenRevocations
        .AnyAsync(tr => tr.TokenJti == jti && tr.ExpiresAt > DateTime.UtcNow);
}
```

#### Refresh Token Flow

```csharp
// New entity: RefreshToken
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; }           // Hashed refresh token
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }     // 30 days
    public DateTime? RevokedAt { get; set; }    // NULL = active
    public string? IpAddress { get; set; }      // Issue IP
    public string? DeviceFingerprint { get; set; } // Device tracking
}

// Endpoint: POST /api/v1/auth/refresh
[HttpPost("refresh")]
public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto dto)
{
    try
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == HashToken(dto.RefreshToken));
        
        if (refreshToken == null || refreshToken.RevokedAt.HasValue)
        {
            await _logger.LogValidationAsync(
                "Refresh Token Validation",
                "Failed",
                "Token not found or revoked"
            );
            return Unauthorized();
        }
        
        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        var newAccessToken = await _tokenService.GenerateTokenAsync(user);
        
        await _logger.LogActionAsync(
            "Token Refresh",
            "Auth",
            $"UserId: {user.Id}"
        );
        
        return Ok(new { accessToken = newAccessToken, expiresIn = 3600 });
    }
    catch (Exception ex)
    {
        await _logger.LogErrorAsync("Token refresh failed", ex);
        throw;
    }
}
```

---

### Phase 4: Dynamic Module System

#### Module Registration Architecture

```csharp
// New interface: IModule
public interface IModule
{
    string Name { get; }                        // "Auth", "HCM", "Finance", etc.
    string Version { get; }
    bool IsCore { get; }                        // True for "Auth", False for "HCM"
    void RegisterServices(IServiceCollection services);
    void RegisterEndpoints(WebApplication app);
    void RegisterFilters(WebApplication app);
}

// Core Auth Module (Always loaded)
public class AuthModule : IModule
{
    public string Name => "Auth";
    public string Version => "1.0.0";
    public bool IsCore => true;
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddDynamicLogWriter();
    }
    
    public void RegisterEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithName("AuthModule")
            .WithOpenApi();
        
        group.MapPost("/register", RegisterHandler);
        group.MapPost("/login", LoginHandler);
        group.MapPost("/logout", LogoutHandler);
        group.MapPost("/refresh", RefreshHandler);
    }
    
    public void RegisterFilters(WebApplication app)
    {
        // Auth filters registered globally
    }
}

// RBAC Module (Optional, feature-gated)
public class RbacModule : IModule
{
    public string Name => "RBAC";
    public string Version => "1.0.0";
    public bool IsCore => false;
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IRbacService, RbacService>();
    }
    
    public void RegisterEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/rbac")
            .RequireAuthorization()
            .WithName("RbacModule");
        
        group.MapGet("/permissions", ListPermissions);
        group.MapPost("/assignments", AssignRole);
        // ... 10 more endpoints
    }
    
    public void RegisterFilters(WebApplication app)
    {
        // RBAC filter registered
    }
}

// In Program.cs
var modules = new List<IModule>
{
    new AuthModule(),          // Always loaded (Core)
    new RbacModule(),          // Load if RBAC license feature enabled
    new HcmModule(),           // Load if HCM license feature enabled
    new FinanceModule(),       // Load if Finance license feature enabled
};

foreach (var module in modules)
{
    module.RegisterServices(builder.Services);
}

var app = builder.Build();

foreach (var module in modules)
{
    module.RegisterEndpoints(app);
    module.RegisterFilters(app);
}
```

**Benefits**:
- Modules load dynamically based on license features
- Clean separation of concerns (Auth Core ≠ RBAC Module)
- Easy to add/remove features (Accounting, Production, etc.)
- Scalable to microservices (each module → separate service later)

---

### Phase 5: Security Hardening

#### Rate Limiting

```csharp
// In DependencyInversion.cs
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(
        policyName: "auth-login-limiter",
        configure: opts =>
        {
            opts.PermitLimit = 5;                    // 5 attempts
            opts.Window = TimeSpan.FromHours(1);     // Per hour
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 2;
        }
    );
    
    options.AddFixedWindowLimiter(
        policyName: "auth-register-limiter",
        configure: opts =>
        {
            opts.PermitLimit = 3;                    // 3 registrations
            opts.Window = TimeSpan.FromHours(24);    // Per 24 hours
        }
    );
    
    options.RejectionStatusCode = 429;              // Too Many Requests
});

// In MiddlewareDependencyInversion.cs
app.UseRateLimiter();

// On endpoints
app.MapPost("/auth/login", LoginHandler)
    .RequireRateLimiting("auth-login-limiter");

app.MapPost("/auth/register", RegisterHandler)
    .RequireRateLimiting("auth-register-limiter");
```

#### HTTPS Enforcement + HSTS

```csharp
// In Program.cs
if (!app.Environment.IsDevelopment())
{
    // Force HTTPS redirect
    app.UseHttpsRedirection();
    
    // HTTP Strict Transport Security (1 year + subdomains)
    app.UseHsts();
}

// In launchSettings.json (Development)
"https": {
  "commandName": "Project",
  "launchBrowser": true,
  "launchUrl": "swagger",
  "applicationUrl": "https://localhost:7007;http://localhost:5007",
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  }
}
```

#### DateTime.UtcNow Enforcement

```csharp
// BEFORE (WRONG):
public DateTime CreatedAt { get; set; } = DateTime.Now;   // ❌ Local time

// AFTER (CORRECT):
public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // ✅ UTC

// Rule: ALL DateTime properties MUST use DateTime.UtcNow
// Reason: Distributed systems across multiple timezones
```

#### CORS Configuration (Production)

```csharp
// BEFORE (INSECURE):
.AllowAnyOrigin()
.AllowCredentials()  // ❌ CONFLICT: Can't allow * + credentials

// AFTER (SECURE):
var allowedOrigins = app.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "https://app.calcifer.com" };

services.AddCors(options =>
{
    options.AddPolicy("production", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Pagination-Total");
    });
});

// In appsettings.Production.json
"Cors": {
  "AllowedOrigins": [
    "https://app.calcifer.com",
    "https://weavo-go.vercel.app"
  ]
}
```

#### JWT Secret Management

```csharp
// BEFORE (INSECURE):
"JwtSettings": {
  "Secret": "calcifer.micro.core.secret.key.[rakibul.h.rabbi].[microservice].[template]"  // ❌ IN REPO
}

// AFTER (SECURE):
// appsettings.json (template only)
"JwtSettings": {
  "Secret": "${JWT_SECRET}"  // Placeholder
}

// User Secrets (Development)
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:Secret" "your-secret-here"

// Key Vault (Production)
var keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
var credential = new DefaultAzureCredential();
config.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
```

---

### Phase 6: IP & Device Tracking

```csharp
// Middleware to extract IP and device info
public class IpDeviceTrackingMiddleware
{
    private readonly RequestDelegate _next;
    
    public IpDeviceTrackingMiddleware(RequestDelegate next) => _next = next;
    
    public async Task InvokeAsync(HttpContext context, ILogWriter logger)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        
        context.Items["IpAddress"] = ipAddress;
        context.Items["UserAgent"] = userAgent;
        
        await _next(context);
        
        // Log response with IP + device
        await logger.LogResponseAsync(
            context.Request.Path.Value,
            context.Request.Method,
            context.Response.StatusCode,
            "Response logged",
            logger.GetCorrelationId()
        );
    }
}

// In Program.cs
app.UseMiddleware<IpDeviceTrackingMiddleware>();
```

---

### Phase 7: Testing Strategy

#### Unit Tests (xUnit + Moq)

```csharp
public class AuthServiceTests
{
    private readonly Mock<IUserManager> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogWriter> _loggerMock;
    private readonly AuthService _authService;
    
    public AuthServiceTests()
    {
        _userManagerMock = new Mock<IUserManager>();
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogWriter>();
        
        _authService = new AuthService(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object
        );
    }
    
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginDto = new LoginRequestDto { Email = "test@example.com", Password = "password" };
        var user = new ApplicationUser { Id = "user-1", Email = "test@example.com" };
        var token = "jwt-token-here";
        
        _userManagerMock.Setup(um => um.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(true);
        _tokenServiceMock.Setup(ts => ts.GenerateTokenAsync(user))
            .ReturnsAsync(token);
        
        // Act
        var result = await _authService.LoginAsync(loginDto);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(token, result.Token);
        _loggerMock.Verify(l => l.LogActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
    
    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ThrowsException()
    {
        // Arrange
        var loginDto = new LoginRequestDto { Email = "test@example.com", Password = "wrong" };
        _userManagerMock.Setup(um => um.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync((ApplicationUser)null);
        
        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(() => _authService.LoginAsync(loginDto));
        _loggerMock.Verify(l => l.LogErrorAsync(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }
}
```

#### Integration Tests

```csharp
public class AuthApiIntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    
    public AuthApiIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with in-memory
                    services.RemoveAll(typeof(DbContextOptions<CalciferAppDbContext>));
                    services.AddDbContext<CalciferAppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });
        
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task Post_AuthLogin_WithValidCredentials_Returns200()
    {
        // Arrange
        var loginRequest = new { email = "admin@calcifer.com", password = "password123" };
        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json"
        );
        
        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(responseContent);
        Assert.NotNull(result);
    }
    
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync()
    {
        _factory?.Dispose();
        _client?.Dispose();
        return Task.CompletedTask;
    }
}
```

---

### Phase 8: Controller Consolidation

**Keep ONE example controller** returning "Hello, Welcome to Calcifer Cathedra"

```csharp
[ApiController]
[Route("api/v1")]
public class WelcomeController : ControllerBase
{
    private readonly ILogWriter _logger;
    
    public WelcomeController(ILogWriter logger) => _logger = logger;
    
    /// <summary>
    /// Welcome endpoint - Example controller (to be deprecated)
    /// Shows integration with ILogWriter
    /// </summary>
    [HttpGet("")]
    [HttpGet("welcome")]
    public async Task<IActionResult> Welcome()
    {
        await _logger.LogActionAsync(
            "Welcome Endpoint Access",
            "Welcome",
            "User accessed welcome endpoint",
            _logger.GetCorrelationId()
        );
        
        var response = new ApiResponseDto<object>
        {
            Status = true,
            Message = "Hello, Welcome to Calcifer Cathedra! 🏛️",
            Data = new
            {
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Timestamp = DateTime.UtcNow,
                Documentation = "/swagger"
            }
        };
        
        return Ok(response);
    }
}

// DELETE all other controllers (AuthController, RoleController, etc.)
// → Move logic to Minimal APIs
```

---

### Implementation Checklist

#### Before Deploying to Production

- [ ] **Logging**: ILogWriter integrated in all services
- [ ] **Audit Trail**: AuditLog table created, field-level tracking implemented
- [ ] **Correlation IDs**: Passed through middleware → services → responses
- [ ] **JWT Optimization**: unit_roles moved to PermissionCache, JWT tested for size
- [ ] **Token Revocation**: TokenRevocation table, logout endpoint implemented
- [ ] **Refresh Tokens**: RefreshToken endpoint tested, device fingerprinting added
- [ ] **Module System**: All modules load dynamically, RBAC/HCM optional
- [ ] **Rate Limiting**: /auth/login (5/hour), /auth/register (3/24h) tested
- [ ] **HTTPS**: UseHttpsRedirection() + HSTS enabled
- [ ] **CORS**: No wildcard, credential-safe whitelist
- [ ] **DateTime**: All DateTime.Now → DateTime.UtcNow verified
- [ ] **JWT Secret**: Removed from appsettings.json, moved to user-secrets/KeyVault
- [ ] **IP Tracking**: IpDeviceTrackingMiddleware active, logs captured
- [ ] **Unit Tests**: >80% coverage on AuthService, TokenService
- [ ] **Integration Tests**: API endpoints tested end-to-end
- [ ] **Controller Consolidation**: Only WelcomeController remains (example)
- [ ] **Documentation**: Updated ARCHITECTURE.md + COMMANDS.md with new features

---

## 📊 Architecture Evolution

```
CURRENT STATE (v1.0):
├─ 5-layer architecture ✅
├─ JWT authentication ✅
├─ RBAC (Module:Resource:Action) ✅
├─ License feature-gating ✅
├─ Mixed Controllers + Minimal APIs ✅
└─ File-based logging ❌

ENHANCED STATE (v1.1 - This Roadmap):
├─ 5-layer architecture ✅
├─ JWT + Refresh Token flow ✅
├─ Token revocation list ✅
├─ RBAC (Module:Resource:Action) ✅
├─ License feature-gating ✅
├─ Pure Minimal APIs ✅
├─ Dynamic module system ✅
├─ Centralized log writer ✅
├─ Field-level audit trails ✅
├─ Correlation ID tracing ✅
├─ IP/device tracking ✅
├─ Rate limiting ✅
├─ HTTPS enforcement ✅
├─ Redis caching (optional) ⏳
└─ Structured logging + ELK (optional) ⏳

FUTURE STATE (v2.0 - Microservices):
├─ Auth → Standalone microservice
├─ HCM → Standalone microservice
├─ Finance → Standalone microservice
├─ Central API Gateway
├─ Message Queue (RabbitMQ/Kafka)
├─ Distributed tracing (Jaeger)
└─ Multi-region deployment
```

---

**Version**: 1.1 (Enhanced)  
**Last Updated**: April 28, 2026  
**Maintainer**: Development Team  
**Status**: ✅ Production-Ready + Hardening Roadmap  
**Next Phase**: Redis caching + Structured logging integration
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
