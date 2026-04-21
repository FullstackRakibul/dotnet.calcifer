<p align="center">
  <img src="calcifer-icon.png" alt="Calcifer" width="120" />
</p>

<h1 align="center">dotnet.calcifer</h1>

<p align="center">
  <strong>Production-Ready .NET 8 Web API Microservice Template</strong>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Calcifer.Microservice.Api.Template">
    <img src="https://img.shields.io/badge/NuGet-v1.1.1--alpha.1-blue?style=flat-square&logo=nuget" alt="NuGet" />
  </a>
  <img src="https://img.shields.io/badge/.NET-8.0-purple?style=flat-square&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT License" />
  <img src="https://img.shields.io/badge/Status-Alpha-orange?style=flat-square" alt="Status" />
</p>

---

A professional-grade, enterprise-ready **.NET 8 Web API Microservice Template** distributed via NuGet. Built with layered architecture principles, pre-configured with JWT Authentication, ASP.NET Core Identity, granular RBAC (Role-Based Access Control), a licensing system, automatic database seeding, and both MVC Controller and Minimal API scaffolding.

## 🌟 Features

| Category | Details |
|----------|---------|
| **Framework** | .NET 8 Web API with C# 12 and nullable reference types |
| **Routing** | Hybrid — MVC Controllers for Auth, Minimal APIs for domain logic |
| **Authentication** | JWT Bearer with HMAC-SHA256, cookie redirect suppression |
| **Identity** | Extended `ApplicationUser` & `ApplicationRole` via ASP.NET Core Identity |
| **Authorization** | Hierarchical role policies + granular permission-based RBAC |
| **Licensing** | Feature-gated access control with license activation workflow |
| **Database** | SQL Server via EF Core 8, automatic migrations & idempotent seeding |
| **Audit Trail** | Base classes tracking `CreatedAt`, `UpdatedAt`, `DeletedBy`, soft-delete |
| **API Docs** | Swagger / OpenAPI with Bearer token pre-wired |
| **CORS** | Pre-configured for front-end integration |

## 🗂️ Project Structure

```
dotnet.calcifer/
├── .github/workflows/         # CI/CD — NuGet publish on release
├── src/Calcifer.Api/
│   ├── .template.config/      # dotnet new template metadata
│   └── v1/
│       ├── AuthHandler/
│       │   ├── Claims/        # CustomClaims
│       │   ├── Configuration/ # JwtSettings
│       │   ├── Filters/       # AuthorizationFilter, LicenseValidationFilter, RbacFilter
│       │   └── MinimalApis/   # IdentityApi, LicenseApi
│       ├── Controllers/
│       │   ├── AuthController/    # AuthController, RoleController
│       │   ├── UsageExamples/     # EmployeeController (reference implementation)
│       │   └── HomeController.cs
│       ├── DTOs/
│       │   ├── AuthDTO/       # Login, Register, AssignRole, CreateRole DTOs
│       │   ├── CommonDTO/     # CommonStatusDto
│       │   ├── LicenseDTO/    # LicenseDto
│       │   ├── ModelsDTO/     # PublicDto
│       │   ├── ApiResponseDto.cs
│       │   ├── ClientTypeDto.cs
│       │   └── PublicDataDTO.cs
│       ├── DbContexts/
│       │   ├── AuthModels/    # ApplicationUser, ApplicationRole
│       │   ├── Common/        # AuditBase, CommonStatus, TableOperationDetails
│       │   ├── Enum/          # CommonStatusEnum
│       │   ├── Licensing/     # License, LicenseActivation, LicenseFeature, LicenseType
│       │   ├── MinimalApis/
│       │   │   └── PublicApis/    # CommonStatusApi, PublicCRUDApis, UsageExamples
│       │   ├── Models/        # PublicData
│       │   ├── Rbac/          # Permission entities, services, seeders, APIs
│       │   └── CalciferAppDbContext.cs
│       ├── DependencyContainer/   # IoC / service registration
│       ├── Infrastructure/        # DatabaseInitializer (seeder pipeline)
│       ├── Interface/             # Service abstractions (Common, Licensing)
│       ├── Middleware/            # Pipeline configuration
│       ├── Migrations/            # EF Core migration files
│       ├── Services/
│       │   ├── AuthService/   # AuthService, RoleService, TokenService
│       │   ├── LicenseService/
│       │   └── PublicService.cs
│       ├── Properties/        # launchSettings.json
│       ├── Program.cs         # Application entry point
│       └── appsettings.Example.json  # ← Copy to appsettings.json
├── dotnet.calcifer.csproj     # NuGet template packaging
├── global.json                # SDK version constraint
├── LICENSE
├── README.md
└── SECURITY.md
```

## 🔐 Security Architecture

### Authorization Policies (Hierarchical)

| Policy | Allowed Roles |
|--------|---------------|
| `SuperAdminPolicy` | `SUPERADMIN` |
| `AdminPolicy` | `SUPERADMIN`, `ADMIN` |
| `ModeratorPolicy` | `SUPERADMIN`, `ADMIN`, `MODERATOR` |

### RBAC — Granular Permission System

The template includes a full **Role-Based Access Control** module under `DbContexts/Rbac/`:

- **Permission entities** — `Permission`, `RolePermission`, `UserDirectPermission`
- **Organization units** — `OrganizationUnit`, `UserUnitRole`
- **Permission caching** — `PermissionCache` for fast lookups
- **Filter enforcement** — `RbacFilter` validates permissions per-request
- **Seeder pipeline** — Pre-seeds organizational structure and base permissions

### Licensing System

Feature-gated access control via `AuthHandler/Filters/LicenseValidationFilter`:

- `License`, `LicenseActivation`, `LicenseFeature`, `LicenseType` entities
- `LicenseApi` Minimal API endpoints
- Feature toggle pattern for license-based access control

## 🌱 Database Seeding

On every startup, `DatabaseInitializer.SeedAsync` runs an ordered, idempotent pipeline:

| Step | What is Seeded |
|------|----------------|
| 1 | **Roles** — `SUPERADMIN`, `ADMIN`, `MODERATOR`, `REGULARUSER` |
| 2 | **SuperAdmin user** — `superadmin@system.com` → `SUPERADMIN` role |
| 3 | **CommonStatus** — Active, Inactive, Deleted, Draft, Pending, Approved, Rejected, Expired, Terminated, Suspended |
| 4 | **RBAC Permissions** — Base permission definitions and role mappings |
| 5 | **Organization Units** — Default organizational structure |

> A guard clause checks existing data before each step — no duplicate records are ever inserted.

## 🛠️ Tech Stack

| Component | Technology | Version |
|-----------|------------|---------|
| Framework | ASP.NET Core | 8.0 |
| ORM | Entity Framework Core | 8.0.1 |
| Auth | JWT Bearer + ASP.NET Identity | 8.0.1 |
| API Docs | Swashbuckle (Swagger) | 6.4.0 |
| Database | SQL Server | — |
| SDK | .NET | 10.0.5+ (rollForward) |

## 📦 Installation

### Install the Template

```bash
dotnet new install Calcifer.Microservice.Api.Template
```

### Scaffold a New Project

```bash
dotnet new dotnet.calcifer -n MyProjectName
```

### Configure the Application

```bash
# Navigate to the API project
cd src/Calcifer.Api/v1/

# Copy the example config and fill in your real values
cp appsettings.Example.json appsettings.json
```

Edit `appsettings.json` with your actual connection string and a strong JWT secret:

```json
{
  "ConnectionStrings": {
    "CalciferDBContext": "Server=localhost;Database=YourDB;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "your-strong-secret-key-at-least-32-characters"
  }
}
```

## ⚡ Database Migrations

From inside `src/Calcifer.Api/v1/`:

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project Calcifer.Api.csproj

# Apply all pending migrations
dotnet ef database update --project Calcifer.Api.csproj
```

> Migrations are also automatically applied and the database is seeded when the application starts.

## 🚀 Run the Application

```bash
cd src/Calcifer.Api/v1/
dotnet run
```

The API will be available at `https://localhost:5001` (or the port configured in `launchSettings.json`).  
Swagger UI: `https://localhost:5001/swagger`

## 📐 Request Flow

```
HTTP Request
  ↓  Middleware Pipeline
  ↓  JWT Token Validation
  ↓  RBAC Permission Check
  ↓  License Feature Validation
  ↓  Controller / Minimal API
  ↓  Service Layer (Business Logic)
  ↓  DbContext (EF Core ORM)
  ↓  SQL Server
  ↓  DTO Serialization
  ↓  JSON Response
```

## 🤝 Contributing

Contributions are welcome! Please read the [SECURITY.md](SECURITY.md) for vulnerability reporting guidelines.

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Built with 🔥 by <a href="https://github.com/FullstackRakibul">Rakibul Hasan Rabbi</a>
</p>
