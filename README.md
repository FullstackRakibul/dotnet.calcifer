# dotnet.calcifer 🚀

[![NuGet version](https://img.shields.io/nuget/vpre/Calcifer.Microservice.Api.Template.svg?color=blue)](https://www.nuget.org/packages/Calcifer.Microservice.Api.Template)

A professional-grade, production-ready **.NET 8 Web API Microservice Template** distributed via NuGet. Built with Clean Architecture principles, pre-configured with JWT Authentication, ASP.NET Core Identity, Role-Based Authorization, automatic database seeding, and Minimal API scaffolding.

## 🌟 Features

- **.NET 8 Web API** — Latest framework features and C# 12.
- **Hybrid Routing** — MVC Controllers for Authentication, Minimal APIs for domain logic.
- **Robust Security** — JWT Bearer Authentication with HMAC-SHA256 signing and cookie redirect suppression for API consumers.
- **ASP.NET Core Identity** — Extended `ApplicationUser` and `ApplicationRole` models for fine-grained control.
- **Role-Based Authorization Policies** — Pre-defined hierarchical policies: `SuperAdminPolicy`, `AdminPolicy`, `ModeratorPolicy`.
- **Automatic Database Seeding** — Ordered pipeline on startup: **Roles → SuperAdmin → Reference Data** (`CommonStatus`). Guard clause prevents duplicate seeding.
- **Easy EF Core Migrations** — SQL Server configured via connection string; run a single command to apply migrations.
- **CommonStatus Minimal API** — Full CRUD endpoints for managing reference statuses, secured per role policy.
- **Audit Trails** — Base classes tracking `CreatedAt`, `UpdatedAt`, `DeletedBy`, and soft-delete support.
- **Swagger / OpenAPI** — Bearer token authentication pre-wired in Swagger UI for instant endpoint testing.
- **CORS Pre-configured** — Ready for front-end integration (default: `http://localhost:5173`).

## 🗂️ Project Structure

```
src/Calcifer.Api/
└── v1/
    ├── AuthHandler/          # JWT token generation & configuration
    ├── Controllers/          # MVC controllers (Auth endpoints)
    ├── DbContexts/
    │   ├── AuthModels/       # ApplicationUser, ApplicationRole
    │   ├── Common/           # CommonStatus, TableOperationDetails models
    │   ├── Enum/             # Shared enumerations
    │   ├── MinimalApis/
    │   │   └── PublicApis/   # CommonStatusApi, PublicCRUDApis
    │   └── CalciferAppDbContext.cs
    ├── DependencyContainer/  # DependencyInversion (service registration)
    ├── Infrastructure/       # DatabaseInitializer (seeder)
    ├── Interface/            # IPublicInterface and shared contracts
    ├── Middleware/           # Custom middleware pipeline
    ├── Migrations/           # EF Core migration files
    ├── Services/             # AuthService, TokenService, PublicService
    └── Program.cs
```

## 🌱 Database Seeding

On every startup, `DatabaseInitializer.SeedAsync` runs an ordered, idempotent seeding pipeline:

| Step | What is seeded                                                                                                                  |
| ---- | ------------------------------------------------------------------------------------------------------------------------------- |
| 1    | **Roles** — `SUPERADMIN`, `ADMIN`, `MODERATOR`, `REGULARUSER`                                                                   |
| 2    | **SuperAdmin user** — `superadmin@system.com` assigned to `SUPERADMIN` role                                                     |
| 3    | **CommonStatus reference data** — Active, Inactive, Deleted, Draft, Pending, Approved, Rejected, Expired, Terminated, Suspended |

> A guard clause checks `CommonStatus.AnyAsync()` before seeding — no duplicate data is ever inserted.

## 🔐 Authorization Policies

| Policy             | Allowed Roles                      |
| ------------------ | ---------------------------------- |
| `SuperAdminPolicy` | `SUPERADMIN`                       |
| `AdminPolicy`      | `SUPERADMIN`, `ADMIN`              |
| `ModeratorPolicy`  | `SUPERADMIN`, `ADMIN`, `MODERATOR` |

## 🛠️ Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB or full instance)

## 📦 Installation

Install the template globally via the .NET CLI:

```bash
dotnet new install Calcifer.Microservice.Api.Template
```

Scaffold a new project:

```bash
dotnet new dotnet.calcifer -n MyProjectName
```

## ⚡ Database Migrations

From inside `src/Calcifer.Api/v1/`, run:

.

```bash
# Add a new migration
dotnet ef migrations add InitialCreate --project Calcifer.Api.csproj

# Apply all pending migrations
dotnet ef database update --project Calcifer.Api.csproj
```

Migrations are automatically applied and the database is seeded when the application starts.
