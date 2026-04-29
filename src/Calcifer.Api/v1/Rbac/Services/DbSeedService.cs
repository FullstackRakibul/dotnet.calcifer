using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Common;
using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Rbac.Services
{
  /// <summary>
  /// Seeds the database with required RBAC data on application startup:
  /// - CommonStatus records (Active, Inactive, Pending, Locked, Deleted)
  /// - Administration module permissions
  /// - Default system roles (SuperAdmin, Admin, User)
  /// - Default SuperAdmin user with all permissions
  /// 
  /// Usage in Program.cs:
  ///   app.Services.CreateScope().ServiceProvider.GetRequiredService&lt;DbSeedService&gt;().SeedAsync().Wait();
  /// </summary>
  public class DbSeedService
  {
    private readonly CalciferAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userMgr;
    private readonly RoleManager<ApplicationRole> _roleMgr;
    private readonly ILogWriter _logger;

    public DbSeedService(
      CalciferAppDbContext db,
      UserManager<ApplicationUser> userMgr,
      RoleManager<ApplicationRole> roleMgr,
      ILogWriter logger)
    {
      _db = db;
      _userMgr = userMgr;
      _roleMgr = roleMgr;
      _logger = logger;
    }

    public async Task SeedAsync()
    {
      await _logger.LogActionAsync("Database seed", "System", "Starting database seed operation");

      await SeedCommonStatusesAsync();
      await SeedPermissionsAsync();
      await SeedRolesAsync();
      await SeedDefaultAdminAsync();

      await _logger.LogActionAsync("Database seed", "System", "Database seed completed successfully");
    }

    private async Task SeedCommonStatusesAsync()
    {
      if (await _db.CommonStatus.AnyAsync()) return;

      var statuses = new List<CommonStatus>
      {
        new() { Id = 1, StatusName = "Active",   Module = "User", Description = "Active user",   IsActive = true, SortOrder = 1 },
        new() { Id = 2, StatusName = "Inactive", Module = "User", Description = "Inactive user", IsActive = true, SortOrder = 2 },
        new() { Id = 3, StatusName = "Pending",  Module = "User", Description = "Pending user",  IsActive = true, SortOrder = 3 },
        new() { Id = 4, StatusName = "Locked",   Module = "User", Description = "Locked user",   IsActive = true, SortOrder = 4 },
        new() { Id = 5, StatusName = "Deleted",  Module = "User", Description = "Deleted user",  IsActive = true, SortOrder = 5 },
        new() { Id = 6, StatusName = "Active",   Module = "RBAC", Description = "Active RBAC",   IsActive = true, SortOrder = 1 },
      };

      _db.CommonStatus.AddRange(statuses);
      await _db.SaveChangesAsync();
      await _logger.LogActionAsync("Seed statuses", "System", $"Seeded {statuses.Count} common statuses");
    }

    private async Task SeedPermissionsAsync()
    {
      if (await _db.Permissions.AnyAsync()) return;

      var perms = new List<Permission>
      {
        // Administration Module
        P("Administration", "Overview",     "Read",   "View admin overview dashboard"),
        P("Administration", "Users",        "Read",   "View user list and details"),
        P("Administration", "Users",        "Create", "Create new users"),
        P("Administration", "Users",        "Update", "Edit user profiles and assign roles"),
        P("Administration", "Users",        "Delete", "Delete/deactivate users"),
        P("Administration", "Roles",        "Read",   "View roles and permission matrix"),
        P("Administration", "Roles",        "Create", "Create new roles"),
        P("Administration", "Roles",        "Update", "Edit role permissions"),
        P("Administration", "Roles",        "Delete", "Delete custom roles"),
        P("Administration", "Permissions",  "Read",   "View permission catalog"),
        P("Administration", "OrgUnits",     "Read",   "View organization units"),
        P("Administration", "OrgUnits",     "Create", "Create organization units"),
        P("Administration", "OrgUnits",     "Update", "Edit organization units"),
        P("Administration", "OrgUnits",     "Delete", "Delete organization units"),
        P("Administration", "AuditLogs",    "Read",   "View and export audit logs"),
        P("Administration", "Sessions",     "Read",   "View active sessions"),
        P("Administration", "Sessions",     "Delete", "Revoke sessions"),
        P("Administration", "SystemStatus", "Read",   "View system health status"),
      };

      _db.Permissions.AddRange(perms);
      await _db.SaveChangesAsync();
      await _logger.LogActionAsync("Seed permissions", "System", $"Seeded {perms.Count} permissions");
    }

    private async Task SeedRolesAsync()
    {
      var systemRoles = new[]
      {
        ("SUPERADMIN",          "Super Administrator — full system access"),
        ("HR_MANAGER",          "Human Resources Manager"),
        ("PRODUCTION_MANAGER",  "Production Manager"),
        ("VIEWER",              "Read-only viewer"),
      };

      foreach (var (name, desc) in systemRoles)
      {
        if (await _roleMgr.RoleExistsAsync(name)) continue;

        var role = new ApplicationRole
        {
          Name = name,
          NormalizedName = name,
          Description = desc,
          IsSystemRole = true,
          StatusId = 1
        };
        await _roleMgr.CreateAsync(role);

        // Grant all permissions to SUPERADMIN
        if (name == "SUPERADMIN")
        {
          var allPerms = await _db.Permissions.ToListAsync();
          foreach (var perm in allPerms)
          {
            _db.RolePermissions.Add(new RolePermission
            {
              RoleId = role.Id,
              PermissionId = perm.Id,
              CreatedBy = "system-seed",
              CreatedAt = DateTime.UtcNow,
              StatusId = 1
            });
          }
          await _db.SaveChangesAsync();
        }
      }

      await _logger.LogActionAsync("Seed roles", "System", $"Seeded {systemRoles.Length} system roles");
    }

    private async Task SeedDefaultAdminAsync()
    {
      const string adminEmail = "admin@calcifer.local";
      if (await _userMgr.FindByEmailAsync(adminEmail) != null) return;

      var admin = new ApplicationUser
      {
        UserName = adminEmail,
        Email = adminEmail,
        EmailConfirmed = true,
        Name = "System Admin",
        FirstName = "System",
        LastName = "Admin",
        EmployeeId = "EMP-000",
        StatusId = 1,
        CreatedBy = "system-seed"
      };

      var result = await _userMgr.CreateAsync(admin, "Admin@123!");
      if (result.Succeeded)
      {
        await _userMgr.AddToRoleAsync(admin, "SUPERADMIN");
        await _logger.LogActionAsync("Seed admin", "System", $"Created default admin: {adminEmail}");
      }
      else
      {
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        await _logger.LogErrorAsync($"Failed to seed admin user: {errors}");
      }
    }

    private static Permission P(string module, string resource, string action, string desc) =>
      new() { Module = module, Resource = resource, Action = action, Description = desc };
  }
}
