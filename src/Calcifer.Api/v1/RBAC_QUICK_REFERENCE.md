# RBAC Administration Module - Quick Reference Guide

## 📚 File Map

### Core Services (Implement Business Logic)
```
Rbac/Services/
├── RoleManagementService.cs      → Role CRUD + permission assignments
├── UserAdminService.cs            → User CRUD + admin operations
├── OrganizationUnitService.cs     → Org unit hierarchy management
├── AuditLogService.cs             → Audit log queries + export
├── ActiveSessionService.cs        → Session tracking + revocation
└── SystemStatusService.cs         → System health monitoring
```

### Read Repositories (Complex Query Logic)
```
Rbac/Repositories/
├── UserReadRepository.cs          → Hydrated user queries with roles/permissions
└── AuditLogRepository.cs          → Filtered audit log pagination
```

### Service Contracts
```
Rbac/Interfaces/
└── IAdministrationServices.cs     → 6 service interfaces
```

### Data Transfer Objects
```
Rbac/DTOs/
├── RbacDTOs.cs                   → Request types + filters
├── ApiResponseDto.cs             → Standard response wrapper
├── RoleDto.cs                    → Role with counts
├── AdminUserDto.cs               → User with roles/permissions
├── OrgUnitDto.cs                 → Org unit with children
├── AuditLogDto.cs                → Audit log entry
├── PaginatedResponse.cs           → Pagination wrapper
├── AdminOverviewStats.cs          → Dashboard stats
└── (Others in DTOs folder)
```

### API Endpoints
```
Rbac/MinimalApis/
└── AdministrationMinimalApi.cs   → All 30+ endpoints registered here
```

---

## 🔌 Dependency Injection Setup

**Location:** `DependencyContainer/DependencyInversion.cs`

```csharp
// Services
services.AddScoped<IRoleManagementService, RoleManagementService>();
services.AddScoped<IUserAdminService, UserAdminService>();
services.AddScoped<IOrganizationUnitService, OrganizationUnitService>();
services.AddScoped<IAuditLogService, AuditLogService>();
services.AddScoped<IActiveSessionService, ActiveSessionService>();
services.AddScoped<ISystemStatusService, SystemStatusService>();

// Repositories
services.AddScoped<IUserReadRepository, UserReadRepository>();
services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Logging (already configured)
services.AddDynamicLogWriter();
```

---

## 🌐 API Endpoints

**Base Route:** `/api/v1/rbac/admin/`

### Overview
```
GET  /overview/stats      → AdminOverviewStats (dashboard)
GET  /system-status       → SystemHealthDto (health check)
```

### Permissions
```
GET  /permissions         → PermissionDto[]
GET  /permissions/{id}    → PermissionDto
```

### Roles (5 endpoints)
```
GET    /roles            → RoleDto[]
GET    /roles/{id}       → RoleDto
POST   /roles            → Create
PUT    /roles/{id}       → Update
DELETE /roles/{id}       → Delete
```

### Role Permissions (4 endpoints)
```
GET    /roles/{roleId}/permissions           → RolePermissionDto[]
POST   /roles/{roleId}/permissions           → Assign single
DELETE /roles/{roleId}/permissions/{permId}  → Remove single
PUT    /roles/{roleId}/permissions           → Bulk sync (replace all)
```

### Users (5 endpoints)
```
GET    /users            → AdminUserDto[] (hydrated with roles/perms)
GET    /users/{id}       → AdminUserDto
POST   /users            → Create
PUT    /users/{id}       → Update
DELETE /users/{id}       → Delete/deactivate
```

### User Unit Roles (3 endpoints)
```
GET  /users/{userId}/unit-roles              → UserUnitRoleDto[]
POST /users/{userId}/unit-roles              → Assign role in unit
POST /users/{userId}/unit-roles/revoke       → Revoke from unit
```

### User Direct Permissions (3 endpoints)
```
GET    /users/{userId}/direct-permissions           → DirectPermissionDto[]
POST   /users/{userId}/direct-permissions           → Grant permission
DELETE /users/{userId}/direct-permissions/{permId}  → Revoke permission
```

### User Effective Permissions (1 endpoint)
```
GET /users/{userId}/permissions  → UserPermissionSummary (computed from roles + direct)
```

### Organization Units (6 endpoints)
```
GET    /org-units        → OrgUnitDto[] (flat)
GET    /org-units/tree   → OrgUnitDto[] (nested)
GET    /org-units/{id}   → OrgUnitDto
POST   /org-units        → Create
PUT    /org-units/{id}   → Update
DELETE /org-units/{id}   → Delete
```

### Audit Logs (2 endpoints)
```
GET /audit-logs           → PaginatedResponse<AuditLogDto>
                            (filters: search, module, action, status, userId, fromDate, toDate)
GET /audit-logs/export    → Blob (CSV or XLSX)
```

### Active Sessions (3 endpoints)
```
GET  /sessions                        → ActiveSessionDto[]
POST /sessions/{sessionId}/revoke     → Revoke specific
POST /sessions/revoke-all             → Revoke all except current
```

---

## 📝 Logging Pattern

Every service method follows this pattern:

```csharp
public async Task<SomeDto> DoSomethingAsync(string input, string userId)
{
    try
    {
        // Log the action
        await _logger.LogActionAsync(
            "Action description",
            "ModuleName",
            $"Detail: {input}",
            _logger.GetCorrelationId());

        // Business logic...
        var result = ...;

        return result;
    }
    catch (Exception ex)
    {
        await _logger.LogErrorAsync("Failed to do something", ex, _logger.GetCorrelationId());
        throw;
    }
}
```

---

## 🔍 How to Use Services

### In Controllers/Minimal APIs
```csharp
public async Task<IResult> CreateRole(
    CreateRoleRequest request,
    IRoleManagementService roleService,
    ILogWriter logger,
    HttpContext ctx)
{
    try
    {
        var userId = ctx.User.FindFirst("ID")?.Value ?? "system";
        var role = await roleService.CreateRoleAsync(request, userId);
        return Results.Created($"/roles/{role.Id}", ApiResponse.Success(role));
    }
    catch (Exception ex)
    {
        await logger.LogErrorAsync("Failed to create role", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to create role"));
    }
}
```

### Injecting Services
```csharp
// Constructor injection
public UserAdminService(
    UserManager<ApplicationUser> userManager,
    CalciferAppDbContext dbContext,
    IUserReadRepository userReadRepository,
    ILogWriter logger)
{
    _userManager = userManager;
    _dbContext = dbContext;
    _userReadRepository = userReadRepository;
    _logger = logger;
}
```

---

## 🗄️ Repository Pattern

Services use repositories for complex queries:

```csharp
// IUserReadRepository example
public async Task<List<AdminUserDto>> GetAllUsersAsync()
{
    return await _userReadRepository.GetAllUsersAsync();
}

// The repository handles:
// - Loading related entities (roles, permissions)
// - Filtering
// - Pagination
// - DTO mapping
```

---

## 🔑 Key Concepts

### AdminUserDto (Hydrated)
Contains:
- User basic info (Id, Email, Name, Phone, Department)
- Base unit assignment
- List of `UserUnitRoleDto` (roles in units with validity dates)
- List of `DirectPermissionDto` (direct grants/denials)

### Bulk Permission Sync
```csharp
// PUT /roles/{roleId}/permissions with body: [permId1, permId2, ...]
// Deletes all existing permissions and replaces with new list
// Use this for the admin UI permission matrix
```

### Pagination
All list endpoints support pagination:
```csharp
// Returns PaginatedResponse<T> with:
// - Items: T[]
// - TotalCount: int
// - Page: int (current page, 1-based)
// - PageSize: int
// - TotalPages: int
```

### Audit Log Filtering
Support complex filters:
- `search`: Text search in UserName, UserEmail, Action, Details
- `module`: Filter by module (e.g., "UserAdministration")
- `action`: Filter by action (e.g., "Create")
- `status`: Filter by status (e.g., "success", "failure")
- `userId`: Filter by user who performed action
- `fromDate`: Filter from date
- `toDate`: Filter to date

---

## ⚠️ Important Notes

1. **Soft Deletes**: Users are marked with `Status = "deleted"`, not hard-deleted
2. **Correlation IDs**: All logs include correlation ID for tracing
3. **Async All The Way**: All I/O operations are async
4. **DTO Mapping**: Never expose entities directly; always use DTOs
5. **Error Handling**: All endpoints catch exceptions and return ApiResponse with error
6. **Logging**: Every significant operation is logged
7. **Authorization**: All endpoints require JWT token (future: add role checks)

---

## 🧪 Testing Tips

### Unit Testing Services
```csharp
var mockRepo = new Mock<IUserReadRepository>();
var mockLogger = new Mock<ILogWriter>();
var service = new UserAdminService(mockUserManager.Object, mockDbContext, mockRepo.Object, mockLogger.Object);

var result = await service.GetAllUsersAsync();
```

### Integration Testing APIs
```csharp
var response = await client.GetAsync("/api/v1/rbac/admin/roles");
Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
var json = await response.Content.ReadAsStringAsync();
```

---

## 🚨 Common Issues & Solutions

### Issue: "Roles are empty"
**Solution**: Check that ASP.NET Identity tables are created and seeded with permissions

### Issue: "Endpoints return 401"
**Solution**: Ensure JWT token is provided in Authorization header

### Issue: "Audit logs not appearing"
**Solution**: Verify `AuditLogs` table exists and service is logging correctly

### Issue: "Org unit tree is null"
**Solution**: Check database for null `ParentId` values (root units)

---

## 📞 Contact & Support

- **Log Writer**: See `Helper/LogWriter/LogWriter.cs` for usage examples
- **Architecture**: See `ARCHITECTURE.md` for full system overview
- **API Blueprint**: See `Rbac/MinimalApis/administration_api_blueprint.md` for detailed endpoint specs
