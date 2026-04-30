# AuthHandler Architecture — Pure Minimal API Design

**Design Pattern**: Pure Minimal API for Identity Infrastructure  
**Status**: Implementation in progress (consolidation path documented)  
**Date**: April 27, 2026  

---

## Overview

The `AuthHandler` folder implements the **security infrastructure** for Calcifer.Api, providing:
- **JWT authentication** with custom RBAC claims
- **Authorization filtering** with RBAC + licensing enforcement
- **Claim management** (constants to avoid magic strings)
- **Minimal API routes** for identity operations (register, login, roles)

The architecture follows the **Pure Minimal API** principle: all identity endpoints use Minimal APIs rather than Controllers, ensuring consistency, reduced overhead, and easier future microservice extraction.

---

## Folder Structure

```
AuthHandler/
├── Claims/
│   └── CustomClaims.cs                 # JWT claim constants (name, region, role, perms, unit_roles)
├── Configuration/
│   └── JwtSettings.cs                  # Strongly-typed JWT configuration
├── Filters/
│   ├── AuthorizationFilter.cs          # IEndpointFilter — global JWT check (minimal APIs)
│   ├── RbacFilter.cs                   # IAsyncAuthorizationFilter — RBAC enforcement (controllers)
│   └── LicenseValidationFilter.cs      # IAsyncAuthorizationFilter — feature-gating
├── MinimalApis/
│   ├── IdentityApi.cs                  # Register, login, profile, password, roles endpoints
│   └── LicenseApi.cs                   # License management endpoints
└── [Controllers/] → DEPRECATED          # Move logic to MinimalApis (consolidation path)
```

---

## 1. Claims Management (`Claims/CustomClaims.cs`)

**Purpose**: Centralized JWT claim type constants to avoid "magic strings"

**Current Implementation**:
```csharp
public class CustomClaims
{
    public const string Name = "name";
    public const string Region = "region";
    public const string Role = "role";
}
```

**Enhanced Implementation** (Recommended):
```csharp
namespace Calcifer.Api.AuthHandler.Claims
{
    /// <summary>
    /// Standard claim type constants for JWT tokens.
    /// Prevents magic strings throughout the codebase.
    /// </summary>
    public static class CustomClaims
    {
        // ── Standard OpenID Connect Claims ────────────────────
        public const string Sub = "sub";                    // User ID (standard)
        public const string Email = "email";                // User email (standard)
        public const string Name = "name";                  // Display name (standard)

        // ── ASP.NET Identity Claims ─────────────────────────
        public const string Role = ClaimTypes.Role;         // ASP.NET role ("role")
        public const string NameIdentifier = ClaimTypes.NameIdentifier; // User ID claim key

        // ── Custom Business Claims ──────────────────────────
        public const string EmployeeId = "emp_id";          // Internal employee ID
        public const string Region = "region";              // Geographic scope
        public const string Department = "dept";            // Department code

        // ── RBAC Claims ──────────────────────────────────────
        /// <summary>
        /// Multi-valued claim containing resolved permissions.
        /// Format: "Module:Resource:Action" (e.g., "HCM:Employee:Read")
        /// Embedded from JWT to avoid database queries during authorization.
        /// </summary>
        public const string Permissions = "perms";

        /// <summary>
        /// Multi-valued claim containing user's unit role assignments.
        /// Format: "UnitId:RoleName" (e.g., "Factory-1:HR_Manager")
        /// Provides context for organization-specific role scoping.
        /// </summary>
        public const string UnitRoles = "unit_roles";

        // ── Metadata Claims ──────────────────────────────────
        public const string IssuedAt = "iat";              // Token issue time (standard)
        public const string ExpiresAt = "exp";             // Token expiration (standard)
        public const string Issuer = "iss";                // Token issuer (standard)
        public const string Audience = "aud";              // Token audience (standard)
    }
}
```

**Usage Throughout Codebase**:
```csharp
// ❌ AVOID: Magic strings
var userId = context.User.FindFirst("sub")?.Value;

// ✅ CORRECT: Use CustomClaims constants
var userId = context.User.FindFirst(CustomClaims.Sub)?.Value;

// ✅ Reading RBAC claims
var permissions = context.User.FindAll(CustomClaims.Permissions);
// Result: ["HCM:Employee:Read", "HCM:Payroll:Export", ...]
```

**Benefit**: Single source of truth for claim names. Refactoring is trivial (update one constant).

---

## 2. JWT Configuration (`Configuration/JwtSettings.cs`)

**Purpose**: Strongly-typed configuration binding for JWT settings

**Implementation**:
```csharp
namespace Calcifer.Api.AuthHandler.Configuration
{
    /// <summary>
    /// Strongly-typed binding for JwtSettings section in appsettings.json.
    /// Provides compile-time safety and IntelliSense for JWT configuration.
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// The symmetric key used for HMAC-SHA256 signing.
        /// CRITICAL: Must be minimum 32 characters.
        /// SECURITY: Store in user-secrets (dev) or Key Vault (prod).
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// The issuer claim value ("iss" in JWT).
        /// Example: "Calcifer.Api"
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// The audience claim value ("aud" in JWT).
        /// Example: "Calcifer.Client"
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Token lifetime in minutes.
        /// Recommended: 60 (1 hour) for production.
        /// </summary>
        public int ExpirationInMinutes { get; set; } = 60;
    }
}
```

**Configuration in `appsettings.json`**:
```json
{
  "JwtSettings": {
    "Secret": "use-dotnet-user-secrets-DO-NOT-HARDCODE-IN-SOURCE",
    "Issuer": "Calcifer.Api",
    "Audience": "Calcifer.Client",
    "ExpirationInMinutes": 60
  }
}
```

**Dependency Injection**:
```csharp
// In DependencyContainer/DependencyInversion.cs
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings not configured");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton(jwtSettings);
```

---

## 3. Filters (Authorization & Feature-Gating)

### A. AuthorizationFilter (Minimal API Global JWT Check)

**Location**: `Filters/AuthorizationFilter.cs`  
**Type**: `IEndpointFilter`  
**Purpose**: Global JWT validation for all Minimal API endpoints in `/api/v1` group

**Implementation**:
```csharp
public class AuthorizationFilter : IEndpointFilter
{
    /// <summary>
    /// Checks if user has valid JWT token.
    /// Applied globally to /api/v1 Minimal API group.
    /// Returns 401 if not authenticated.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var user = context.HttpContext.User;

        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            return Results.Json(new
            {
                status = false,
                message = "Unauthorized access. Please provide a valid token.",
                errorCode = "AUTH_REQUIRED"
            }, statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}
```

**Application in Middleware**:
```csharp
// In Middleware/MiddlewareDependencyInversion.cs
var apiGroup = app.MapGroup("/api/v1")
    .AddEndpointFilter<AuthorizationFilter>()  // ← Global JWT check
    .RequireAuthorization();                    // ← Metadata requirement

apiGroup.MapIdentityApis();
apiGroup.MapRbacApis();
```

**Result**: All `/api/v1/*` endpoints require valid JWT before reaching handler

---

### B. RbacFilter (Role-Based Permission Enforcement)

**Location**: `Filters/RbacFilter.cs`  
**Type**: `IAsyncAuthorizationFilter`  
**Purpose**: Attribute-based RBAC enforcement using `[RequirePermission]`

**Attribute Definition**:
```csharp
/// <summary>
/// Decorate MVC controller actions to require specific RBAC permission.
/// Multiple attributes = AND logic (all must be satisfied).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string Module { get; }      // e.g., "HCM"
    public string Resource { get; }    // e.g., "Employee"
    public string Action { get; }      // e.g., "Read"
    public string Key => $"{Module}:{Resource}:{Action}";

    public RequirePermissionAttribute(string module, string resource, string action)
    {
        Module = module;
        Resource = resource;
        Action = action;
    }
}
```

**Filter Implementation** (Dual-Path Resolution):
```csharp
public sealed class RbacAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IRbacService _rbac;

    public RbacAuthorizationFilter(IRbacService rbac) => _rbac = rbac;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                status = false,
                message = "Unauthorized",
                errorCode = "AUTH_REQUIRED"
            });
            return;
        }

        // Path 1: Fast JWT check (no database)
        var jwtPerms = user.FindAll(CustomClaims.Permissions)
            .Select(c => c.Value)
            .ToList();

        // Path 2: Slow database check (fallback if JWT claim missing)
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var requirements = context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .ToList();

        if (requirements.Count == 0) return; // No RBAC requirement

        foreach (var req in requirements)
        {
            bool allowed;

            if (jwtPerms.Count > 0)
            {
                // Fast path: check JWT claims (no DB hit)
                allowed = MatchesPermissionPattern(jwtPerms, req.Module, req.Resource, req.Action);
            }
            else
            {
                // Slow path: database query (fallback)
                allowed = await _rbac.HasPermissionAsync(userId, req.Module, req.Resource, req.Action);
            }

            if (!allowed)
            {
                context.Result = new ObjectResult(new
                {
                    status = false,
                    message = $"Access denied: {req.Key}",
                    errorCode = "PERMISSION_DENIED"
                })
                { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }
        }
    }

    private bool MatchesPermissionPattern(List<string> perms, string module, string resource, string action)
    {
        var required = $"{module}:{resource}:{action}";
        return perms.Any(p => p == required || p == $"{module}:*:*" || p == "*:*:*");
    }
}
```

**Usage**:
```csharp
// In Controllers (legacy) or Minimal API metadata
[RequirePermission("HCM", "Employee", "Read")]
public async Task<IActionResult> GetEmployees()
{
    // Only executes if user has "HCM:Employee:Read" permission
}

// Multiple requirements (AND logic)
[RequirePermission("HCM", "Employee", "Read")]
[RequirePermission("HCM", "Employee", "Export")]
public async Task<IActionResult> ExportEmployees()
{
    // Only executes if BOTH permissions granted
}
```

---

### C. LicenseValidationFilter (Feature-Gating)

**Location**: `Filters/LicenseValidationFilter.cs`  
**Type**: `IAsyncAuthorizationFilter` (controller) + `IEndpointFilter` (minimal APIs)  
**Purpose**: Feature-level gating via `[RequireFeature]` attribute

**Attribute**:
```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireFeatureAttribute : Attribute
{
    public string FeatureCode { get; }  // e.g., "HCM", "Production", "Finance"

    public RequireFeatureAttribute(string featureCode) => FeatureCode = featureCode;
}
```

**Filter Implementation** (single-query check):
```csharp
public class LicenseValidationFilter : IAsyncAuthorizationFilter
{
    private readonly ILicenseService _license;
    private readonly ILogger<LicenseValidationFilter> _logger;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var requirement = context.ActionDescriptor.EndpointMetadata
            .OfType<RequireFeatureAttribute>()
            .FirstOrDefault();

        if (requirement == null) return; // No feature restriction

        var isEnabled = await _license.IsFeatureEnabledAsync(requirement.FeatureCode);

        if (!isEnabled)
        {
            _logger.LogWarning("Feature {FeatureCode} not enabled", requirement.FeatureCode);
            
            context.Result = new ObjectResult(new
            {
                status = false,
                message = $"Feature '{requirement.FeatureCode}' not enabled on current license",
                errorCode = "FEATURE_DISABLED"
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
```

**Usage**:
```csharp
[RequireFeature("HCM")]
[RequirePermission("HCM", "Employee", "Read")]
public async Task<IActionResult> GetEmployees()
{
    // Filter order matters:
    // 1. LicenseValidationFilter runs first → rejects if HCM not licensed
    // 2. RbacFilter runs second → rejects if user lacks permission
}
```

**Minimal API Usage**:
```csharp
hcmGroup.MapGet("/employees", GetEmployeesHandler)
    .WithMetadata(new RequireFeatureAttribute("HCM"))
    .WithMetadata(new RequirePermissionAttribute("HCM", "Employee", "Read"))
    .RequireAuthorization();
```

---

## 4. Minimal API Routes (`MinimalApis/IdentityApi.cs`)

**Purpose**: Pure HTTP routing for identity operations (register, login, profile, roles)

**Endpoint Groups**:

### Auth Endpoints (Public + Authenticated)

```csharp
public static IEndpointRouteBuilder MapIdentityApis(this IEndpointRouteBuilder app)
{
    // ── Auth group (mixed public & authenticated) ─────────────
    var auth = app.MapGroup("/auth")
                  .WithTags("Auth")
                  .WithOpenApi();

    // POST /auth/register (public — no token required)
    auth.MapPost("/register", async (
        RegisterRequestDto dto,
        AuthService authService,
        HttpContext ctx) =>
    {
        var callerRole = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
        var (success, message, profile) = await authService.RegisterAsync(dto, callerRole);
        
        return success
            ? Results.Created($"/auth/me",
                new ApiResponseDto<UserProfileDto> { Status = true, Data = profile })
            : Results.BadRequest(new ApiResponseDto<string> { Status = false, Message = message });
    })
    .WithName("Register")
    .WithOpenApi()
    .Produces(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest);

    // POST /auth/login (public — no token required)
    auth.MapPost("/login", async (LoginRequestDto dto, AuthService authService) =>
    {
        var (success, message, response) = await authService.LoginAsync(dto);

        return success
            ? Results.Ok(new ApiResponseDto<LoginResponseDto>
            {
                Status = true,
                Data = response
            })
            : Results.BadRequest(new ApiResponseDto<object> { Status = false });
    })
    .WithName("Login")
    .WithOpenApi()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status429TooManyRequests); // Rate limit response

    // GET /auth/me (requires token)
    auth.MapGet("/me", async (HttpContext ctx, AuthService authService) =>
    {
        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

        var profile = await authService.GetProfileAsync(userId);
        return profile == null
            ? Results.NotFound()
            : Results.Ok(new ApiResponseDto<UserProfileDto> { Status = true, Data = profile });
    })
    .RequireAuthorization()
    .WithName("GetProfile")
    .WithOpenApi()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized);

    // POST /auth/change-password (requires token)
    auth.MapPost("/change-password", async (
        ChangePasswordDto dto,
        HttpContext ctx,
        AuthService authService) =>
    {
        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

        var (success, message) = await authService.ChangePasswordAsync(userId, dto);
        return Results.Ok(new ApiResponseDto<string> { Status = success, Message = message });
    })
    .RequireAuthorization()
    .WithName("ChangePassword")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized);

    // ── Role management endpoints (SuperAdmin only) ────────────
    var roles = app.MapGroup("/roles")
                   .WithTags("Roles")
                   .RequireAuthorization("SuperAdminPolicy");

    // GET /roles (list all roles)
    roles.MapGet("/", async (RoleService roleService) =>
    {
        var all = await roleService.GetAllRolesAsync();
        return Results.Ok(new ApiResponseDto<IEnumerable<RoleResponseDto>>
        {
            Status = true,
            Data = all
        });
    })
    .WithName("GetAllRoles")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status403Forbidden);

    // POST /roles (create role)
    roles.MapPost("/", async (CreateRoleRequestDto dto, RoleService roleService) =>
    {
        var (success, message, role) = await roleService.CreateRoleAsync(dto);
        return success
            ? Results.Created($"/roles/{role!.Id}", new ApiResponseDto<RoleResponseDto> { Status = true, Data = role })
            : Results.BadRequest(new ApiResponseDto<string> { Status = false, Message = message });
    })
    .WithName("CreateRole")
    .Produces(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status403Forbidden);

    // DELETE /roles/{roleId}
    roles.MapDelete("/{roleId}", async (string roleId, RoleService roleService) =>
    {
        var success = await roleService.DeleteRoleAsync(roleId);
        return success
            ? Results.NoContent()
            : Results.NotFound(new ApiResponseDto<string> { Status = false });
    })
    .WithName("DeleteRole")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status403Forbidden);

    // ... additional role endpoints (assign, remove, list user roles)

    return app;
}
```

**Endpoint Summary**:
| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | /api/v1/auth/register | None | Create new user |
| POST | /api/v1/auth/login | None | Obtain JWT token |
| GET | /api/v1/auth/me | JWT | Get own profile |
| POST | /api/v1/auth/change-password | JWT | Update password |
| GET | /api/v1/roles | SuperAdmin | List all roles |
| POST | /api/v1/roles | SuperAdmin | Create custom role |
| DELETE | /api/v1/roles/{roleId} | SuperAdmin | Delete role |
| POST | /api/v1/roles/assign | SuperAdmin | Assign user to role |
| POST | /api/v1/roles/remove | SuperAdmin | Remove user from role |

---

## 5. Filter Execution Order

**Critical**: Filters run in specific order to enforce layered security

```
Request arrives
    ↓
┌─────────────────────────────────────────┐
│ 1. LicenseValidationFilter              │
│    (runs FIRST to gate at module level) │
│    [RequireFeature("HCM")]              │
│                                         │
│    If no license feature → 403 STOP    │
└─────────────────────────────────────────┘
    ↓ (passed license check)
┌─────────────────────────────────────────┐
│ 2. RbacAuthorizationFilter              │
│    (runs SECOND for permission check)   │
│    [RequirePermission]                  │
│                                         │
│    If no permission → 403 STOP          │
└─────────────────────────────────────────┘
    ↓ (passed RBAC check)
┌─────────────────────────────────────────┐
│ 3. AuthorizationFilter (minimal APIs)   │
│    (global JWT validation)              │
│    Applied to /api/v1 group             │
│                                         │
│    If no JWT → 401 STOP                 │
└─────────────────────────────────────────┘
    ↓ (passed all auth checks)
┌─────────────────────────────────────────┐
│ 4. Request Handler                      │
│    (controller action or minimal API)   │
└─────────────────────────────────────────┘
    ↓
Response sent to client
```

**Registration in Middleware**:
```csharp
// Middleware/MiddlewareDependencyInversion.cs
public static void UseMiddlewareInjection(this WebApplication app)
{
    // ── Security filters (in execution order) ──────────────────
    app.UseAuthentication();     // Validate JWT signature
    app.UseAuthorization();      // Execute [Authorize], [RequirePermission], etc.
    
    // Global Minimal API filter (applies to /api/v1 group)
    var apiGroup = app.MapGroup("/api/v1")
        .AddEndpointFilter<LicenseValidationFilter>()   // 1st
        .AddEndpointFilter<RbacAuthorizationFilter>()   // 2nd
        .AddEndpointFilter<AuthorizationFilter>();     // 3rd (final JWT check)
    
    // Map routes
    apiGroup.MapIdentityApis();  // ← All endpoints under /api/v1
    apiGroup.MapPublicApis();
    apiGroup.MapRbacApis();
}
```

---

## 6. Dependency Injection Hub (`DependencyContainer/DependencyInversion.cs`)

**Central Registration Point** for all AuthHandler services and configurations

```csharp
public static void AddAuthenticationServices(this IServiceCollection services, IConfiguration config)
{
    // ── JWT Configuration ────────────────────────────────────────
    var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()
        ?? throw new InvalidOperationException("JwtSettings not configured");
    
    services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
    services.AddSingleton(jwtSettings);

    // ── ASP.NET Identity ─────────────────────────────────────────
    services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<CalciferAppDbContext>()
    .AddDefaultTokenProviders();

    // ── JWT Bearer Authentication ────────────────────────────────
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    // ── Authorization Policies ───────────────────────────────────
    services.AddAuthorization(options =>
    {
        options.AddPolicy("SuperAdminPolicy", policy =>
            policy.RequireRole(DefaultRoles.SuperAdmin));

        options.AddPolicy("AdminPolicy", policy =>
            policy.RequireRole(DefaultRoles.SuperAdmin, "HR_MANAGER"));

        options.AddPolicy("ModeratorPolicy", policy =>
            policy.RequireRole(DefaultRoles.SuperAdmin, "HR_MANAGER", "PRODUCTION_MANAGER"));
    });

    // ── CORS Configuration ───────────────────────────────────────
    services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", policy =>
        {
            var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            
            policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // ── AuthHandler Services ─────────────────────────────────────
    services.AddScoped<TokenService>();
    services.AddScoped<RoleService>();
    services.AddScoped<AuthService>();

    // ── Filters ──────────────────────────────────────────────────
    services.AddScoped<AuthorizationFilter>();
    services.AddScoped<RbacAuthorizationFilter>();
    services.AddScoped<LicenseValidationFilter>();
}
```

---

## Migration Path: Controllers → Pure Minimal APIs

**Current State**: Mixed (Controllers + Minimal APIs)  
**Target State**: Pure Minimal APIs for identity/auth

### Step 1: Consolidate AuthController

**Delete**: `Controllers/AuthController/AuthController.cs`

```csharp
// OLD: Controllers/AuthController/AuthController.cs
[ApiController]
[Route("api/v1/Controllers/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        // This is now in IdentityApi.cs → /api/v1/auth/login
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        // This is now in IdentityApi.cs → /api/v1/auth/register
    }
}
```

**Result**: All auth endpoints now at `/api/v1/auth/*` (Minimal API)

### Step 2: Update Client Calls

```bash
# OLD
POST /api/v1/Controllers/auth/login
POST /api/v1/Controllers/auth/register

# NEW
POST /api/v1/auth/login
POST /api/v1/auth/register
```

### Step 3: Consolidate RoleController

**Delete**: `Controllers/AuthController/RoleController.cs`

All role endpoints move to `IdentityApi.cs` `/roles` group:
```
OLD:
POST /api/v1/Controllers/role/assign
GET /api/v1/Controllers/role/user/{userId}

NEW:
POST /api/v1/roles/assign
GET /api/v1/roles/user/{userId}
```

---

## Best Practices

### ✅ DO

1. **Use `CustomClaims` constants** — avoid "magic strings"
   ```csharp
   var permissions = user.FindAll(CustomClaims.Permissions);
   ```

2. **Apply filters in correct order** — License → RBAC → Auth
   ```csharp
   [RequireFeature("HCM")]
   [RequirePermission("HCM", "Employee", "Read")]
   public async Task<IActionResult> GetEmployees() { }
   ```

3. **Use Minimal APIs for new endpoints** — consistent, lightweight
   ```csharp
   app.MapPost("/auth/register", handler);
   ```

4. **Register services in DependencyInversion.cs** — central hub
   ```csharp
   services.AddScoped<TokenService>();
   ```

5. **Use DTOs for all request/response** — no domain model leakage
   ```csharp
   auth.MapPost("/login", async (LoginRequestDto dto, ...) => ...);
   ```

### ❌ DON'T

1. Don't hardcode JWT secret — use user-secrets/Key Vault
2. Don't mix Controllers and Minimal APIs — standardize on Minimal APIs
3. Don't duplicate service interfaces — single source of truth
4. Don't forget rate limiting on auth endpoints — security risk
5. Don't use magic strings for claim types — use CustomClaims

---

## Testing AuthHandler

### Unit Tests (AuthService)

```csharp
[TestClass]
public class AuthServiceTests
{
    [TestMethod]
    public async Task LoginAsync_ValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var dto = new LoginRequestDto { Email = "test@test.com", Password = "Password@123" };
        var service = new AuthService(...);

        // Act
        var (success, message, response) = await service.LoginAsync(dto);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(response?.Token);
    }
}
```

### Integration Tests (API Endpoints)

```csharp
[TestClass]
public class AuthApiTests
{
    [TestMethod]
    public async Task Register_NewUser_Returns201Created()
    {
        // Arrange
        var client = new HttpClient { BaseAddress = new Uri("https://localhost:7000") };
        var dto = new RegisterRequestDto { Email = "new@test.com", Password = "Pwd@123", Name = "Test" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", dto);

        // Assert
        Assert.AreEqual(StatusCodes.Status201Created, response.StatusCode);
    }
}
```

---

## Summary

The `AuthHandler` implements a **security-first, Minimal-API-centric** architecture:

- **Claims**: Centralized constants (no magic strings)
- **Configuration**: Strongly-typed JWT settings
- **Filters**: Layered (License → RBAC → Auth)
- **Routing**: Pure Minimal APIs for all identity endpoints
- **DI Hub**: Centralized service registration

**Next Steps**:
1. Consolidate remaining Controllers to Minimal APIs
2. Add rate limiting to auth endpoints
3. Implement structured logging (Serilog)
4. Add comprehensive security header middleware

---

**Status**: ✅ Documented (implementation in progress)  
**Last Updated**: April 27, 2026
