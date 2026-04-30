# Calcifer.Api — Security Analysis & Remediation Guide

**Date**: April 27, 2026  
**Status**: Critical issues identified, remediation path provided  
**Scope**: Application-level security, infrastructure recommendations  

---

## Executive Summary

Calcifer.Api implements **enterprise-grade security patterns** (RBAC, licensing, JWT) but has **4 critical issues** and **4 medium-risk concerns** requiring immediate attention before production deployment.

### Risk Matrix

| Issue | Severity | Effort | Impact | Status |
|-------|----------|--------|--------|--------|
| JWT secret hardcoded | 🔴 CRITICAL | 15min | **CRITICAL** — Token forgery, session hijacking | ⏳ TO-DO |
| CORS + AllowCredentials | 🔴 CRITICAL | 10min | **HIGH** — Credentials exposed to any origin | ⏳ TO-DO |
| No rate limiting (auth) | 🔴 CRITICAL | 1-2hr | **HIGH** — Brute force, account enumeration | ⏳ TO-DO |
| Login controller stub | 🔴 CRITICAL | 30min | **MEDIUM** — Duplicate endpoint, bypasses security | ⏳ TO-DO |
| No structured logging | 🟡 MEDIUM | 3-4hr | **MEDIUM** — Difficult forensics & debugging | ⏳ TO-DO |
| RBAC cache manual invalidation | 🟡 MEDIUM | 2-3hr | **MEDIUM** — Risk of stale permissions | ⏳ TO-DO |
| Duplicate ILicenseService | 🟡 MEDIUM | 30min | **LOW** — Maintenance burden, type confusion | ⏳ TO-DO |
| No entity validation | 🟡 MEDIUM | 2-3hr | **MEDIUM** — Invalid data can persist to DB | ⏳ TO-DO |

---

## 🔴 CRITICAL ISSUES

### 1. JWT Secret Hardcoded in Source Code

**Location**: `appsettings.Development.json`

**Current Code**:
```json
{
  "JwtSettings": {
    "Secret": "calcifer.micro.core.secret.key.[rakibul.h.rabbi].[microservice].[template]"
  }
}
```

**Risk**: 
- Anyone with repository access obtains the JWT secret
- Attacker can forge tokens and impersonate any user
- No token validity or revocation possible
- **Session Hijacking**: Existing tokens remain valid after secret leak

**Attack Scenario**:
```
1. Attacker obtains appsettings.Development.json from GitHub
2. Attacker runs JWT encoding library locally:
   var jwt = JwtEncoder.Encode(
     new { sub = "admin-user-id", exp = long.MaxValue },
     secret, JwtAlgorithm.HS256);
3. Attacker makes requests as admin user indefinitely
```

**Remediation** ✅

**Step 1: Immediate (Development)**
```bash
# Set secret in local user-secrets (will not be committed)
dotnet user-secrets set "JwtSettings:Secret" "your-super-secret-key-minimum-32-characters-required"
```

**Step 2: Remove hardcoded secret**
```json
// appsettings.Development.json
{
  "JwtSettings": {
    // Secret comes from user-secrets, not appsettings
    "Issuer": "Calcifer.Api",
    "Audience": "Calcifer.Client",
    "ExpirationInMinutes": 60
  }
}
```

**Step 3: Production (Azure Key Vault)**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Load from Key Vault
builder.Configuration
    .AddAzureKeyVault(
        new Uri($"https://{builder.Configuration["KeyVault:Name"]}.vault.azure.net/"),
        new DefaultAzureCredential());

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
```

**Verification**:
```bash
# Verify secret is NOT in appsettings files
grep -r "calcifer.micro.core.secret" appsettings*.json

# Confirm user-secrets is set
dotnet user-secrets list
# Output: JwtSettings:Secret = ****** (masked)
```

**Impact**: 🔴 CRITICAL → 🟢 RESOLVED (15 minutes)

---

### 2. CORS Configuration Vulnerability

**Location**: `DependencyContainer/DependencyInversion.cs`

**Current Code** (Inferred from analysis):
```csharp
builder.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://weavo-go.vercel.app", "*")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();  // ⚠️ PROBLEM HERE
    });
});
```

**Risk**:
- CORS spec forbids combining wildcard (`*`) with `AllowCredentials()`
- Browser security model breaks down
- Credentials may be exposed to untrusted origins
- **Credentials Leakage**: If browser somehow sends credentials with wildcard, any origin can intercept

**Attack Scenario**:
```
1. User logs in to attacker's phishing site (attacker.com)
2. Attacker's site makes XMLHttpRequest to https://calcifer-api.com/profile
3. Browser attaches credentials (cookie/auth header)
4. CORS misconfiguration allows response to be read
5. Attacker exfiltrates user data
```

**Remediation** ✅

**Option A: Remove Wildcard** (Recommended for production)
```csharp
builder.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",           // Local dev
                "http://10.10.60.156:4200",        // Internal network
                "https://weavo-go.vercel.app"      // Production frontend
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();  // ✅ Now safe with explicit origins
    });
});
```

**Option B: Remove AllowCredentials** (if wildcard truly needed)
```csharp
policy
    .WithOrigins("*")
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Remove: .AllowCredentials()
    ;
```

**Environment-Specific Configuration**:
```csharp
var allowedOrigins = builder.Environment.IsDevelopment()
    ? new[] { "http://localhost:4200", "http://10.10.60.156:4200" }
    : new[] { "https://weavo-go.vercel.app" };

policy.WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials();
```

**Verification**:
```bash
# Test preflight request
curl -i -X OPTIONS https://localhost:7000/api/v1/auth/login \
  -H "Origin: https://attacker.com" \
  -H "Access-Control-Request-Method: POST"

# Should NOT include Access-Control-Allow-Credentials header
# if Origin is not in allowlist
```

**Impact**: 🔴 CRITICAL → 🟢 RESOLVED (10 minutes)

---

### 3. No Rate Limiting on Authentication Endpoints

**Location**: `AuthHandler/MinimalApis/IdentityApi.cs` (POST /auth/register, POST /auth/login)

**Risk**:
- **Brute Force Attacks**: Attacker can try unlimited passwords against known email
  - Example: 1000 passwords/second × 86400 seconds = 86 million attempts/day
- **Account Enumeration**: Attacker discovers valid email addresses
  - Example: Register endpoint responds differently for existing vs new emails
- **Dictionary Attacks**: Common password lists tried at scale
- **Denial of Service**: Legitimate users locked out by failed login attempts

**Attack Scenario**:
```
Attacker targets user "admin@company.com":
1. Send 10,000 POST /auth/login requests with common passwords
2. No rate limit → all requests processed
3. Server CPU/DB exhausted (DOS)
4. Or attacker finds password (brute force success)

Without rate limiting, attacker can try 1 password per millisecond
```

**Current State**: No rate limiting

**Remediation** ✅

**Step 1: Install AspNetCoreRateLimit NuGet package**
```bash
dotnet add package AspNetCoreRateLimit
```

**Step 2: Configure in `DependencyContainer/DependencyInversion.cs`**
```csharp
// Add services
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IIpPolicyStore, MemoryIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryRateLimitCounterStore>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
```

**Step 3: Configure in `appsettings.Development.json`**
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "IpWhitelist": [ "127.0.0.1", "::1" ],
    "EndpointWhitelist": [ "/api/v1/public" ],
    "ClientWhitelist": [],
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ],
    "IpRateLimitPolicies": {
      "IpRules": [
        {
          "Ip": "192.168.1.*",
          "Rules": [
            {
              "Endpoint": "POST /api/v1/auth/register",
              "Period": "1m",
              "Limit": 5
            },
            {
              "Endpoint": "POST /api/v1/auth/login",
              "Period": "1m",
              "Limit": 10
            }
          ]
        }
      ]
    }
  }
}
```

**Step 4: Add middleware in `Middleware/MiddlewareDependencyInversion.cs`**
```csharp
public static void UseMiddlewareInjection(this WebApplication app)
{
    // Add rate limiting middleware (before routing)
    app.UseIpRateLimiting();
    
    // ... other middleware
}
```

**Step 5: Apply to endpoints (Minimal APIs)**
```csharp
// In IdentityApi.cs
auth.MapPost("/register", handler)
    .WithName("Register")
    .WithOpenApi()
    .Produces(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status429TooManyRequests);  // Rate limit response

auth.MapPost("/login", handler)
    .WithName("Login")
    .WithOpenApi()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status429TooManyRequests);
```

**Testing**:
```bash
# Test rate limiting (should succeed first 5, fail on 6th)
for i in {1..10}; do
  curl -X POST https://localhost:7000/api/v1/auth/register \
    -H "Content-Type: application/json" \
    -d '{"email":"test@test.com","password":"Pwd","name":"Test"}'
  echo ""
done

# Responses:
# 1-5: 201/400 (depends on payload)
# 6-10: 429 Too Many Requests
```

**Configuration Recommendations**:

| Endpoint | Period | Limit | Rationale |
|----------|--------|-------|-----------|
| POST /auth/register | 1 hour | 5 | Prevent account enumeration |
| POST /auth/login | 1 minute | 10 | Prevent brute force |
| GET /auth/me | 1 minute | 100 | Allow legitimate polling |
| POST /api/v1/* (general) | 1 minute | 100 | General rate limit |

**Whitelist Trusted IPs**:
```json
"IpWhitelist": [
  "127.0.0.1",        // Localhost
  "::1",              // Localhost IPv6
  "10.0.0.0/8",       // Internal network
  "monitoring-ip"     // Health check monitoring
]
```

**Impact**: 🔴 CRITICAL → 🟢 RESOLVED (1-2 hours)

---

### 4. Login Controller Stub (Duplicate Endpoint)

**Location**: `Controllers/AuthController/AuthController.cs`

**Current Code**:
```csharp
[ApiController]
[Route("api/v1/Controllers/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        return Ok("this is a test ");  // ⚠️ STUB — NOT IMPLEMENTED
    }
}
```

**Risk**:
- **Duplicate endpoint**: `/api/v1/auth/login` (Minimal API) vs `/api/v1/Controllers/auth/login` (Controller)
- **Security bypass**: Stub endpoint returns success without authentication
- **Confusion**: API consumers don't know which endpoint to use
- **Testing nightmare**: Unit tests may hit wrong endpoint

**Attack Scenario**:
```
Attacker discovers stub endpoint:
1. POST /api/v1/Controllers/auth/login → returns "this is a test"
2. No authentication required
3. Attacker doesn't need valid credentials (assuming other code expects JWT)
4. Potential for bypassing rate limiting if only applied to /api/v1/auth/login
```

**Remediation** ✅

**Delete the controller entirely**:
```bash
# Remove file
rm Controllers/AuthController/AuthController.cs
```

**Consolidate all auth logic to Pure Minimal API**:

All endpoints move to `AuthHandler/MinimalApis/IdentityApi.cs`:
```csharp
// IdentityApi.cs
auth.MapPost("/register", /* handler */)
auth.MapPost("/login", /* handler */)
auth.MapPost("/change-password", /* handler */)
auth.MapGet("/me", /* handler */)
auth.MapPost("/logout", /* handler */)

var roles = app.MapGroup("/roles")
    .RequireAuthorization("SuperAdminPolicy");
roles.MapGet("/", /* handler */)
roles.MapPost("/", /* handler */)
// ... etc
```

**Update routing documentation**:
```markdown
# Authentication Endpoints (Pure Minimal API)

All auth endpoints now under `/api/v1/auth/` and `/api/v1/roles/`

- POST /api/v1/auth/register
- POST /api/v1/auth/login
- GET /api/v1/auth/me
- POST /api/v1/auth/change-password
- GET /api/v1/roles (SuperAdmin only)
- POST /api/v1/roles/assign (SuperAdmin only)
```

**Verification**:
```bash
# Verify controller endpoint returns 404
curl -X POST https://localhost:7000/api/v1/Controllers/auth/login
# Expected: 404 Not Found

# Verify Minimal API endpoint works
curl -X POST https://localhost:7000/api/v1/auth/login
# Expected: 400 Bad Request (bad payload) or successful auth
```

**Impact**: 🔴 CRITICAL → 🟢 RESOLVED (30 minutes)

---

## 🟡 MEDIUM-RISK ISSUES

### 5. No Structured Logging

**Current State**: Uses default `ILogger<T>` (unstructured text logs)

**Problem**:
- Logs are difficult to query in production
- No correlation IDs for tracing requests across services
- No structured fields for parsing (e.g., parse user_id from string)
- Difficult to set up centralized logging (ELK, Application Insights)

**Remediation** ✅

**Step 1: Install Serilog**
```bash
dotnet add package Serilog
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Enrichers.Context
```

**Step 2: Configure in `Program.cs`**
```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Calcifer.Api")
    .Enrich.WithMachineName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/calcifer_.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddLogging();

// ... rest of setup
```

**Step 3: Use structured logging in services**
```csharp
public class AuthService
{
    private readonly ILogger<AuthService> _logger;

    public async Task<(bool, string, UserProfileDto?)> RegisterAsync(RegisterRequestDto dto, string? callerRole)
    {
        _logger.LogInformation("User registration attempt for email: {Email}", dto.Email);
        
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: email {Email} already exists", dto.Email);
                return (false, "Email already registered", null);
            }
            
            _logger.LogInformation("User {Email} successfully registered", dto.Email);
            return (true, "Registration successful", profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for email: {Email}", dto.Email);
            throw;
        }
    }
}
```

**Step 4: Structured output**
```json
{
  "Timestamp": "2026-04-27T12:34:56.789+00:00",
  "Level": "Information",
  "Application": "Calcifer.Api",
  "MachineName": "server-01",
  "Message": "User registration attempt for email: user@example.com",
  "Email": "user@example.com"
}
```

**Effort**: 3-4 hours  
**Impact**: Medium (Improves troubleshooting significantly)

---

### 6. RBAC Cache Manual Invalidation

**Current State**: `PermissionCache` has 5-minute TTL, manual invalidation required

**Problem**:
- When user's role changes, permissions remain stale for up to 5 minutes
- Developers must remember to call `InvalidateCacheAsync(userId)` after role assignment
- Risk of stale permissions if invalidation forgotten

**Example**:
```
1. User "Ali" has role "Employee" → permission "HCM:Employee:Read"
2. Admin assigns Ali to role "HR_Manager" → has more permissions
3. Cache still shows old "HCM:Employee:Read" for 5 minutes
4. Ali can't use new HR_Manager features yet
5. After 5 minutes: Cache refreshes, new permissions appear
```

**Remediation** ✅

**Event-Driven Cache Invalidation**:
```csharp
// In RoleService.cs
public async Task<(bool, string)> AssignRoleAsync(AssignRoleRequestDto dto)
{
    var result = await _roleManager.AssignRoleAsync(dto);
    if (result.Success)
    {
        // Publish event instead of manual call
        await _eventBus.PublishAsync(new UserRoleAssignedEvent 
        { 
            UserId = dto.UserId, 
            RoleId = dto.RoleId,
            UnitId = dto.UnitId
        });
    }
    return result;
}

// In RbacService.cs (or new event handler)
public async Task OnUserRoleAssignedAsync(UserRoleAssignedEvent @event)
{
    // Automatically invalidate cache
    await InvalidateCacheAsync(@event.UserId);
    _logger.LogInformation("Cache invalidated for user {UserId} after role assignment", @event.UserId);
}
```

**Effort**: 2-3 hours  
**Impact**: Medium (Reduces stale permission risk)

---

### 7. Duplicate ILicenseService Interface

**Current State**: Two definitions
- `Interface/Licensing/ILicenseService.cs`
- `DbContexts/Licensing/ILicenseService.cs`

**Problem**:
- Confusion about which to use
- Type mismatch errors if both are referenced
- Maintenance burden (changes must be duplicated)

**Remediation** ✅

**Keep single source of truth in `Interface/Licensing/ILicenseService.cs`**:
```csharp
// Interface/Licensing/ILicenseService.cs
namespace Calcifer.Api.Interface.Licensing
{
    public interface ILicenseService
    {
        Task<bool> IsFeatureEnabledAsync(string featureCode);
        Task<License> ValidateLicenseKeyAsync(string licenseKey);
        Task<bool> ActivateMachineAsync(string licenseKey, string machineId);
        // ... other methods
    }
}
```

**Remove duplicate** in `DbContexts/Licensing/ILicenseService.cs`

**Update all imports**:
```bash
# Find all usages of DbContexts.Licensing.ILicenseService
grep -r "DbContexts.Licensing.ILicenseService" src/

# Replace with Interface.Licensing.ILicenseService
find src/ -name "*.cs" -type f -exec sed -i 's/DbContexts\.Licensing\.ILicenseService/Interface.Licensing.ILicenseService/g' {} \;
```

**Effort**: 30 minutes  
**Impact**: Low (Maintenance improvement)

---

### 8. No Entity Input Validation

**Current State**: DTOs have no validation attributes

**Problem**:
- Invalid data can be persisted to database
- No early rejection of malformed requests
- Services must duplicate validation logic

**Example**:
```csharp
public record LoginRequestDto
{
    public string Email { get; set; }      // No validation
    public string Password { get; set; }   // No validation
}

// Attacker sends:
{
  "email": "",                            // Empty (invalid)
  "password": "                          // Null (invalid)
}

// Server accepts and queries database anyway
```

**Remediation** ✅

**Step 1: Install FluentValidation**
```bash
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

**Step 2: Create validators**
```csharp
// DTOs/AuthDTO/Validators/LoginRequestValidator.cs
using FluentValidation;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
```

**Step 3: Register in DI**
```csharp
// DependencyContainer/DependencyInversion.cs
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

**Step 4: Automatic validation on Minimal APIs**
```csharp
// In IdentityApi.cs
auth.MapPost("/login", async (
    LoginRequestDto dto,           // Auto-validated by FluentValidation
    AuthService authService) =>
{
    // If validation fails, returns 400 with error details automatically
    // No need for manual validation
    var (success, message, response) = await authService.LoginAsync(dto);
    return success ? Results.Ok(...) : Results.BadRequest(...);
});
```

**Effort**: 2-3 hours  
**Impact**: Medium (Prevents data corruption)

---

## ✅ STRENGTHS (Security Well-Implemented)

### 1. Soft-Delete with Query Filters
```csharp
// Entities marked as soft-deleted are never returned by default
.HasQueryFilter(u => !u.IsDeleted)

// Result: Data preservation for audit, no accidental exposure
```

### 2. Audit Trails
```csharp
public class AuditBase
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// Result: Full forensics trail, compliance ready
```

### 3. RBAC Permission Caching
```csharp
// Permissions resolved once, embedded in JWT
// Subsequent requests don't hit database unless cache expires
// Reduces attack surface (fewer DB queries)
```

### 4. Permission Claim Embedding
```json
{
  "perms": ["HCM:Employee:Read", "HCM:*:*"],
  "unit_roles": ["Factory-1:HR_Manager", "Factory-2:Accountant"]
}
```
Result: Fast authorization without database queries

### 5. Null-Safety
```csharp
// Nullable reference types enabled
#nullable enable

public class User
{
    public string? OptionalField { get; set; }   // Nullable
    public string RequiredField { get; set; }    // Must not be null
}

// Result: Compile-time null checking prevents NPE at runtime
```

---

## Compliance Checklist

- [ ] JWT secret stored in secure vault (user-secrets/Key Vault)
- [ ] CORS configuration uses explicit origins only
- [ ] Rate limiting implemented on auth endpoints
- [ ] All DTOs have input validation
- [ ] Duplicate service interfaces consolidated
- [ ] Structured logging enabled (Serilog)
- [ ] Cache invalidation is event-driven or automatic
- [ ] Soft-delete query filters applied to all sensitive entities
- [ ] Audit trails enabled on all write operations
- [ ] Security headers configured (HSTS, CSP, X-Frame-Options)
- [ ] HTTPS enforced in production
- [ ] Database connection uses encrypted credentials

---

## Remediation Timeline

### Immediate (Before Any Production)
**Effort**: 2 hours  
**Priority**: 🔴 CRITICAL

1. ✅ Move JWT secret to user-secrets
2. ✅ Fix CORS configuration
3. ✅ Remove login controller stub
4. ✅ Add rate limiting

### Short-Term (First Sprint)
**Effort**: 6-8 hours  
**Priority**: 🟡 MEDIUM

1. Add input validation (FluentValidation)
2. Consolidate ILicenseService interface
3. Implement event-driven cache invalidation

### Medium-Term (Next Quarter)
**Effort**: 4-6 hours  
**Priority**: 🟡 LOW-MEDIUM

1. Implement Serilog structured logging
2. Add distributed tracing (OpenTelemetry)
3. Add security headers middleware

---

## Security Headers Middleware (Bonus)

Add to `Middleware/MiddlewareDependencyInversion.cs`:

```csharp
public static void UseSecurityHeaders(this WebApplication app)
{
    app.Use(async (context, next) =>
    {
        // HSTS (HTTP Strict-Transport-Security)
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        
        // CSP (Content-Security-Policy)
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
        
        // Prevent clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // Prevent MIME sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // XSS Protection
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        await next();
    });
}
```

---

## References

- [OWASP JWT Best Practices](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- [OWASP Rate Limiting](https://cheatsheetseries.owasp.org/cheatsheets/Denial_of_Service_Prevention_Cheat_Sheet.html)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Microsoft Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/key-vault/general/integrate-with-app-configuration)

---

**Status**: ⏳ Pending implementation  
**Last Reviewed**: April 27, 2026  
**Next Review**: After remediation complete
