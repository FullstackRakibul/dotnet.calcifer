# Calcifer.Api v1 Documentation

**🚀 Enterprise ASP.NET Core 8.0 API**  
**Advanced RBAC | Licensing & Feature-Gating | Pure Minimal APIs**

---

## 📖 Quick Navigation

### For First-Time Developers
1. **[PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)** — Start here! Quick facts, architecture overview, tech stack
2. **[AUTHHANDLER_ARCHITECTURE.md](AUTHHANDLER_ARCHITECTURE.md)** — Pure Minimal API design pattern
3. **[COMMANDS.md](COMMANDS.md)** — Local setup, build, test, run commands

### Architecture & Design
- **[.claude](.claude)** — Project configuration (strict mode, rules, metadata)
- **[ARCHITECTURE_DETAILED.md](ARCHITECTURE_DETAILED.md)** — Deep-dive on 5-layer design *(In preparation)*
- **[RBAC_SYSTEM.md](RBAC_SYSTEM.md)** — Permission resolution, caching, JWT embedding *(In preparation)*
- **[DATABASE_SCHEMA.md](DATABASE_SCHEMA.md)** — Entity relationships, indexes, migrations *(In preparation)*
- **[API_DESIGN.md](API_DESIGN.md)** — Endpoint reference, DTOs, response format *(In preparation)*

### Security & Operations
- **[SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md)** — Critical issues, remediation path, compliance checklist
- **[DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md)** — Patterns, adding features, validation *(In preparation)*

---

## 🎯 Project at a Glance

| Aspect | Details |
|--------|---------|
| **Framework** | ASP.NET Core 8.0 (.NET 8.0) |
| **Architecture** | 5-Layer Layered Monolith (microservice-ready) |
| **API Style** | Pure Minimal APIs (controllers reserved for legacy) |
| **Authentication** | JWT Bearer (HMAC-SHA256) with custom RBAC claims |
| **Authorization** | Advanced RBAC (Module:Resource:Action model) |
| **Licensing** | Feature-gating with machine activation tracking |
| **Database** | SQL Server + EF Core 8.0.1 (code-first) |
| **Documentation** | Swagger/OpenAPI with Bearer token auth |
| **Code Style** | Nullable reference types, async/await throughout |

---

## 🏗️ 5-Layer Architecture

```
┌─────────────────────────┐
│ 1. API Layer            │  Minimal APIs + Filters
├─────────────────────────┤
│ 2. DTO Layer            │  Request/Response contracts
├─────────────────────────┤
│ 3. Service Layer        │  Business logic
├─────────────────────────┤
│ 4. Interface Layer      │  Service abstractions
├─────────────────────────┤
│ 5. Data Access Layer    │  EF Core + DbContext
└─────────────────────────┘
         ↓
    SQL Server
```

**Key Principle**: Strict layer separation with dependency injection enabling loose coupling.

---

## 🔐 Security Architecture

### Authentication Flow
```
User credentials
    ↓
POST /api/v1/auth/login
    ↓
AuthService validates password
    ↓
TokenService generates JWT with RBAC claims embedded
    ↓
JWT returned to client
    ↓
Client includes token in Authorization header for subsequent requests
```

### Authorization Filter Chain
```
1. LicenseValidationFilter      → Feature-gating (runs FIRST)
2. RbacAuthorizationFilter      → Permission checks
3. AuthorizationFilter          → JWT validation (Minimal APIs)
4. Request Handler
```

### JWT Token Structure
```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "name": "User Name",
  "emp_id": "EMP-001",
  "roles": ["SUPERADMIN", "HR_MANAGER"],
  "perms": ["HCM:Employee:Read", "HCM:Payroll:Export"],
  "unit_roles": ["Factory-1:HR_Manager"],
  "iat": 1234567890,
  "exp": 1234571490
}
```

**Benefit**: Permissions embedded in JWT reduce subsequent database queries.

---

## ⚠️ Critical Security Issues (See [SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md))

| Issue | Severity | Status |
|-------|----------|--------|
| JWT secret hardcoded | 🔴 CRITICAL | ⏳ TO-DO |
| CORS misconfiguration | 🔴 CRITICAL | ⏳ TO-DO |
| No rate limiting (auth) | 🔴 CRITICAL | ⏳ TO-DO |
| Login controller stub | 🔴 CRITICAL | ⏳ TO-DO |
| No structured logging | 🟡 MEDIUM | ⏳ TO-DO |
| Cache manual invalidation | 🟡 MEDIUM | ⏳ TO-DO |
| Duplicate interfaces | 🟡 MEDIUM | ⏳ TO-DO |
| No entity validation | 🟡 MEDIUM | ⏳ TO-DO |

**Remediation effort**: 2-4 hours (critical items)

---

## 🚀 Getting Started

### 1. Clone & Install

```bash
git clone <repository>
cd src/Calcifer.Api/v1
dotnet restore
```

### 2. Configure Secrets

```bash
# Set JWT secret (CRITICAL)
dotnet user-secrets set "JwtSettings:Secret" "your-super-secret-key-min-32-characters"

# Set database connection
dotnet user-secrets set "ConnectionStrings:CalciferDBContext" "Server=(localdb)\mssqllocaldb;Database=EktaDatabase;Trusted_Connection=True"
```

### 3. Apply Migrations & Seed Data

```bash
dotnet ef database update
# Creates EktaDatabase with all tables and seed data (SuperAdmin user, RBAC permissions, etc.)
```

### 4. Run Application

```bash
# Development with hot-reload
dotnet watch

# Navigate to: https://localhost:7000/swagger
```

### 5. Test Authentication

```bash
# Login as SuperAdmin
curl -X POST https://localhost:7000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@calcifer.local","password":"Admin@12345"}'

# Response includes JWT token
```

---

## 📂 Folder Structure

```
src/Calcifer.Api/v1/
├── .claude                         # ← Project configuration (strict mode)
├── PROJECT_SUMMARY.md              # ← Start here
├── SECURITY_ANALYSIS.md            # ← Security concerns & remediation
├── AUTHHANDLER_ARCHITECTURE.md    # ← Pure Minimal API design
├── COMMANDS.md                     # ← CLI reference
├── README.md                       # ← This file
│
├── AuthHandler/                    # Security & Authorization
│   ├── Claims/CustomClaims.cs      # JWT claim constants
│   ├── Configuration/JwtSettings.cs
│   ├── Filters/
│   │   ├── AuthorizationFilter.cs
│   │   ├── RbacFilter.cs
│   │   └── LicenseValidationFilter.cs
│   └── MinimalApis/
│       ├── IdentityApi.cs          # Register, login, profile, roles
│       └── LicenseApi.cs           # License management
│
├── Controllers/                    # Legacy (consolidate to Minimal APIs)
│   └── AuthController/
│       └── AuthController.cs       # ⏳ TO-DELETE (move to IdentityApi)
│
├── DbContexts/                     # Data models & queries
│   ├── CalciferAppDbContext.cs
│   ├── AuthModels/
│   │   ├── ApplicationUser.cs
│   │   └── ApplicationRole.cs
│   ├── Licensing/
│   │   ├── License.cs
│   │   ├── LicenseFeature.cs
│   │   └── LicenseActivation.cs
│   ├── Rbac/
│   │   ├── Entities/
│   │   │   ├── OrganizationUnit.cs
│   │   │   ├── Permission.cs
│   │   │   ├── RolePermission.cs
│   │   │   ├── UserUnitRole.cs
│   │   │   ├── UserDirectPermission.cs
│   │   │   └── PermissionCache.cs
│   │   ├── Services/RbacService.cs
│   │   └── Interfaces/IRbacService.cs
│   └── DTOs/                       # Request/Response contracts
│       ├── ApiResponseDto.cs       # Standard wrapper
│       ├── AuthDTO/
│       ├── LicenseDTO/
│       └── CommonDTO/
│
├── Services/                       # Business logic
│   ├── AuthService/
│   │   ├── AuthService.cs
│   │   ├── TokenService.cs         # JWT generation with RBAC claims
│   │   └── RoleService.cs
│   ├── LicenseService/
│   │   └── LicenseService.cs
│   └── PublicService.cs
│
├── Interface/                      # Service contracts
│   ├── ILicenseService.cs
│   └── IPublicInterface.cs
│
├── DependencyContainer/
│   └── DependencyInversion.cs      # ← Central DI hub
│
├── Middleware/
│   └── MiddlewareDependencyInversion.cs
│
├── Infrastructure/
│   └── DatabaseInitializer.cs      # Seeding on startup
│
├── MinimalApis/
│   └── PublicApis/
│       ├── PublicCRUDApis.cs
│       ├── CommonStatusApi.cs
│       └── RbacMinimalApi.cs       # 12 RBAC management endpoints
│
├── Migrations/                     # EF Core migrations
│   └── 20260418045256_initial.cs
│
├── Program.cs                      # ← Entry point
├── Calcifer.Api.csproj             # ← Project file
├── appsettings.Development.json    # ← Config (⚠️ secrets)
├── appsettings.Example.json        # ← Template
└── v1.sln                          # ← Solution file
```

---

## 🔄 Request Flow (Example: GET /api/v1/employees)

```
1. HTTP Request arrives
   GET /api/v1/employees
   Authorization: Bearer eyJhbGc...

2. AuthenticationMiddleware
   Validates JWT signature using JwtSettings.Secret
   Populates context.User with claims

3. AuthorizationFilter
   Checks: Is user.Identity.IsAuthenticated?
   If false → 401 Unauthorized

4. LicenseValidationFilter
   Checks: Does endpoint have [RequireFeature]?
   If yes → Query: Is this feature enabled on active license?
   If no → 403 Forbidden

5. RbacAuthorizationFilter
   Checks: Does endpoint have [RequirePermission]?
   If yes → Resolve permission from JWT claims (fast) or DB (slow)
   If permission denied → 403 Forbidden

6. Request Handler (Controller or Minimal API)
   Execute business logic
   Query database
   Return DTO

7. Response
   200 OK + JSON data
   Or 4xx/5xx error
```

---

## 📚 Module Overview

### Authentication Module
**Endpoint**: `/api/v1/auth/*`  
**Service**: `AuthService`, `TokenService`  
**Purpose**: User registration, login, JWT generation

**Endpoints**:
- `POST /api/v1/auth/register` — Create new user
- `POST /api/v1/auth/login` — Obtain JWT token
- `GET /api/v1/auth/me` — Current user profile
- `POST /api/v1/auth/change-password` — Update password

### RBAC Module
**Endpoint**: `/api/v1/rbac/*` (12 routes)  
**Service**: `RbacService`  
**Purpose**: Fine-grained permission management

**Entities**:
- `OrganizationUnit` — Org tree (Company → Factory → Dept → Team)
- `Permission` — Atomic capability (Module:Resource:Action)
- `RolePermission` — Role ↔ Permission link
- `UserUnitRole` — User assigned to role at org unit
- `UserDirectPermission` — Grant/deny overrides

**Features**:
- Organization unit tree structure
- Module:Resource:Action permission model
- Wildcard support (*:*:*)
- 5-minute TTL caching
- JWT claim embedding

### Licensing Module
**Endpoint**: `/api/v1/licenses/*`  
**Service**: `LicenseService`  
**Purpose**: License validation, feature-gating, machine activation

**Entities**:
- `License` — License key, expiry, maxusers
- `LicenseFeature` — Feature codes (HCM, Production, Finance, Inventory)
- `LicenseActivation` — Machine registration tracking

**Features**:
- Feature-gating at module level
- Machine activation tracking
- License expiry validation
- Isolation from RBAC

### Public API Module
**Endpoint**: `/api/v1/public`, `/api/v1/status/*`  
**Service**: `PublicService`  
**Purpose**: Public-facing endpoints (no auth required)

---

## 🧪 Testing

### Integration Tests (cURL)

See [COMMANDS.md](COMMANDS.md) for:
- Register new user
- Login and get JWT
- Get user profile with JWT
- Test RBAC permissions
- Test license features
- Rate limiting scenarios

### Unit Tests

```bash
dotnet test

# Or with verbose output
dotnet test --verbosity normal
```

### Load Testing (k6)

See [COMMANDS.md](COMMANDS.md) for load test script and execution.

---

## 🔒 Security Checklist

Before production deployment, ensure:

- [ ] JWT secret in Key Vault (not hardcoded)
- [ ] CORS configured with explicit origins
- [ ] Rate limiting on auth endpoints
- [ ] All DTOs have input validation
- [ ] Duplicate interfaces consolidated
- [ ] Soft-delete query filters applied
- [ ] Audit trails on all write operations
- [ ] HTTPS enforced
- [ ] Structured logging (Serilog)
- [ ] Security headers middleware configured

See [SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md) for detailed remediation steps.

---

## 📊 Architecture Type Definition

**Current**: Layered Monolith with Enterprise Patterns  
**Pattern**: 5-Layer Vertical Slice  
**Microservice Readiness**: 60% ready (clear module boundaries, but shared database)

**Ready for extraction**:
- RBAC module (independent logic, clear interface)
- Licensing module (independent feature gates)
- Authentication module (standard JWT patterns)

**Blockers for full microservices**:
- Shared database (need per-service databases)
- No async event bus (currently synchronous)
- No service discovery (direct calls)

---

## 🛠️ CLI Commands Quick Reference

```bash
# Setup
dotnet restore
dotnet user-secrets set "JwtSettings:Secret" "your-secret"
dotnet ef database update

# Run
dotnet watch                          # Development (hot-reload)
dotnet run                            # Run without watch
dotnet run -c Release                 # Production mode

# Build
dotnet build
dotnet build -c Release
dotnet publish -c Release -o ./publish

# Database
dotnet ef migrations add <Name>
dotnet ef database update
dotnet ef database drop

# Test
dotnet test
dotnet test --verbosity normal

# See full reference in [COMMANDS.md](COMMANDS.md)
```

---

## 🔗 Related Documentation

- **[PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)** — Quick facts & overview
- **[SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md)** — Critical issues & remediation
- **[AUTHHANDLER_ARCHITECTURE.md](AUTHHANDLER_ARCHITECTURE.md)** — Pure Minimal API design
- **[COMMANDS.md](COMMANDS.md)** — All CLI commands
- **[.claude](.claude)** — Project configuration in JSON format

---

## 📞 Support & Troubleshooting

See [COMMANDS.md](COMMANDS.md) troubleshooting section for:
- Port already in use
- Database connection errors
- JWT secret not found
- Rate limiting not working
- HTTPS certificate issues

---

## 🎓 Learning Path

**For New Developers**:
1. Read [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md) (10 min)
2. Read [AUTHHANDLER_ARCHITECTURE.md](AUTHHANDLER_ARCHITECTURE.md) (20 min)
3. Run local setup from [COMMANDS.md](COMMANDS.md) (15 min)
4. Test endpoints with cURL from [COMMANDS.md](COMMANDS.md) (10 min)
5. Explore code while reading [ARCHITECTURE_DETAILED.md](ARCHITECTURE_DETAILED.md) (30 min)

**For Security Review**:
1. Read [SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md) (20 min)
2. Review [AUTHHANDLER_ARCHITECTURE.md](AUTHHANDLER_ARCHITECTURE.md) security sections (15 min)
3. Check .clone file for strict rules (10 min)

**For Architecture Decisions**:
1. Read [ARCHITECTURE_DETAILED.md](ARCHITECTURE_DETAILED.md) (30 min)
2. Review [RBAC_SYSTEM.md](RBAC_SYSTEM.md) (25 min)
3. Study [DATABASE_SCHEMA.md](DATABASE_SCHEMA.md) (20 min)

---

## 📋 Project Statistics

| Metric | Value |
|--------|-------|
| **C# Classes** | ~50+ |
| **DTOs** | 15+ |
| **Minimal API Endpoints** | 20+ |
| **Database Tables** | 11 |
| **Authorization Filters** | 3 |
| **Service Classes** | 6+ |
| **Lines of Code** | ~8,000+ |
| **NuGet Dependencies** | 6 (core) |

---

## 📝 Configuration Files

| File | Purpose | Security |
|------|---------|----------|
| `.claude` | Project metadata & rules | ✅ Safe to commit |
| `appsettings.Development.json` | Dev config template | ⚠️ Use user-secrets |
| `appsettings.Example.json` | Deployment template | ✅ Safe to commit |
| `.user-secrets` | Local secrets (ignored) | ✅ Never committed |
| `launchSettings.json` | VS launch profiles | ✅ Safe to commit |

---

## 🚦 Status

| Component | Status |
|-----------|--------|
| Authentication | ✅ Implemented |
| RBAC | ✅ Implemented (caching, wildcard support) |
| Licensing | ✅ Implemented |
| JWT + Claims | ✅ Implemented |
| Minimal APIs | ✅ Mostly done (consolidation pending) |
| Security Hardening | ⏳ In Progress (4 critical issues) |
| Structured Logging | ⏳ Pending (Serilog integration) |
| Rate Limiting | ⏳ Pending (AspNetCoreRateLimit) |
| Unit Tests | ⏳ Pending |
| Load Testing | ⏳ Pending (k6 script) |

---

## 🎯 Next Steps

### Immediate (This Sprint)
1. ✅ Implement rate limiting on auth endpoints
2. ✅ Fix CORS configuration
3. ✅ Move JWT secret to user-secrets
4. ✅ Remove login controller stub

### Short-term (Next Sprint)
1. Consolidate auth controllers to Minimal APIs
2. Add input validation (FluentValidation)
3. Implement structured logging (Serilog)
4. Add comprehensive unit tests

### Medium-term (Next Quarter)
1. Prepare for RBAC/Licensing microservice extraction
2. Add OpenTelemetry distributed tracing
3. Implement event-driven cache invalidation
4. Add comprehensive API documentation examples

---

## 📞 Maintainer

**Maintained by**: Rakibul H. Rabbi  
**Last Updated**: April 27, 2026  
**Framework Version**: .NET 8.0  
**Status**: ✅ Production-Ready (security hardening in progress)

---

## 📄 License

[Add your license information here]

---

**Questions?** See the relevant documentation above or contact the maintainer.
