# Calcifer.Api - Architecture Summary

## Quick Overview

**Calcifer.Api** is a modern ASP.NET Core 8.0 Web API implementing a **layered architecture** with JWT authentication, role-based access control, and a comprehensive licensing system.

---

## Architecture at a Glance

### Structure: 5-Layer Architecture
```
Controllers/APIs → DTOs → Services → Interfaces → DbContext → SQL Server
```

### Key Layers:

1. **API Layer** (Controllers + Minimal APIs)
   - RESTful endpoints for authentication, licensing, and public data
   - Swagger documentation integration

2. **DTO Layer**
   - Request/response contracts (LoginRequestDto, LicenseDto, etc.)
   - Decouples API from internal models

3. **Service Layer** (Business Logic)
   - AuthService, TokenService, RoleService
   - LicenseService
   - PublicService

4. **Data Access Layer** (EF Core + SQL Server)
   - CalciferAppDbContext
   - Entity models for users, roles, licenses, and public data

5. **Cross-Cutting Concerns** (AuthHandler)
   - JWT validation
   - Role-based authorization
   - License validation filters
   - Feature-gating via custom claims

---

## Core Features

✅ **JWT Bearer Authentication** - Token-based API security  
✅ **Role-Based Access Control** - Granular permission management  
✅ **Licensing System** - Feature-based access control and license activation  
✅ **Audit Trail** - TableOperationDetails for tracking  
✅ **API Documentation** - Swagger/OpenAPI integration  
✅ **Entity Framework Core** - Database abstraction and migrations  

---

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | ASP.NET Core 8.0 |
| Database | SQL Server + EF Core 8.0.1 |
| Auth | JWT Bearer + ASP.NET Identity |
| API Docs | Swagger (Swashbuckle 6.4.0) |

---

## Request Flow

```
HTTP Request
  ↓ [Middleware]
  ↓ [JWT Token Validation]
  ↓ [License Feature Check]
  ↓ [Controller/Minimal API]
  ↓ [Service Layer - Business Logic]
  ↓ [DbContext - EF Core ORM]
  ↓ [SQL Server Database]
  ↓ [Response - DTO Serialization]
  ↓ JSON Response
```

---

## Design Patterns

- **Layered Architecture** - Clear separation of concerns
- **Dependency Injection** - Centralized in DependencyInversion.cs
- **DTO Pattern** - Request/response contracts
- **Repository Pattern** - DbContext abstraction
- **Filter/Middleware Pattern** - Cross-cutting authorization
- **Feature Toggle Pattern** - License-based feature gating

---

## Security Architecture

- **Authentication**: JWT tokens with custom claims
- **Authorization**: Filter-based with attributes
- **License Validation**: Middleware enforcement
- **Feature Gating**: Attribute-based feature access control

---

## Folder Organization

| Folder | Purpose |
|--------|---------|
| `AuthHandler/` | JWT, roles, authorization filters |
| `Controllers/` | API endpoints |
| `Services/` | Business logic |
| `DbContexts/` | EF Core entities and context |
| `DTOs/` | Request/response contracts |
| `Interfaces/` | Service abstractions |
| `Middleware/` | Pipeline configuration |
| `Infrastructure/` | Database initialization |
| `DependencyContainer/` | IoC setup |

---

## Validation Points

✓ Layered architecture properly enforced  
✓ Services are injected and not directly instantiated  
✓ All public APIs use DTOs  
✓ Authorization filters applied to protected endpoints  
✓ Database migrations version controlled  
✓ Null-safety enabled (C# nullable reference types)  
✓ No business logic in controllers  
✓ Centralized configuration management  

---

## How to Use This Document

1. **For Code Review**: Reference the "Layered Architecture" section
2. **For Onboarding**: Start with "Quick Overview" + "Request Flow"
3. **For Security Audit**: Review "Security Architecture" section
4. **For Database Design**: Check "Database Schema Overview" in full document
5. **For Adding Features**: Follow patterns in "Core Design Patterns"
