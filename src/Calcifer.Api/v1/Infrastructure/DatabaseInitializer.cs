// ============================================================
//  DatabaseInitializer.cs — COMPLETE UPDATED VERSION
//
//  Seeding order matters:
//      1. CommonStatus  → status lookup rows
//      2. OrgUnits      → org tree (RBAC depends on this)
//      3. RBAC          → roles + permissions + role-permission links
//      4. SuperAdmin    → first user (depends on roles existing)
//
//  All seeders are idempotent — safe on every startup.
// ============================================================

using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Common;
using Calcifer.Api.Rbac.Entities;
using Calcifer.Api.Rbac.Enums;
using Calcifer.Api.Rbac.Seeds;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Infrastructure
{
	public static class DatabaseInitializer
	{
		public static async Task SeedAsync(IServiceProvider services)
		{
			var db = services.GetRequiredService<CalciferAppDbContext>();
			var logger = services.GetRequiredService<ILogger<CalciferAppDbContext>>();

			// Ensure schema is up to date
			await db.Database.MigrateAsync();

			// ── Step 1: CommonStatus rows ─────────────────────────
			await SeedCommonStatusAsync(db, logger);

			// ── Step 2: OrganizationUnit tree ─────────────────────
			await OrgUnitSeeder.SeedAsync(db, logger);
			//if (!await db.OrganizationUnits.AnyAsync(u => u.Id == 1))
			//{
			//	db.OrganizationUnits.Add(new OrganizationUnit
			//	{
			//		Id = 1,
			//		Name = "Root",
			//		ParentId = null,
			//		IsActive = true
			//	});
			//	await db.SaveChangesAsync();
			//}
			// ── Step 3: RBAC roles + permissions + matrix ─────────
			await RbacPermissionSeeder.SeedAsync(services);

			// ── Step 4: Default SuperAdmin user ───────────────────
			await SeedSuperAdminAsync(services, logger);
		}

		// ── CommonStatus seed data ────────────────────────────────

		private static async Task SeedCommonStatusAsync(
			CalciferAppDbContext db, ILogger logger)
		{
			if (await db.CommonStatus.AnyAsync()) return;

			var statuses = new List<CommonStatus>
			{
                // User module statuses
                new() { StatusName = "Active",    Module = "User",    IsActive = true,  SortOrder = 1,  Description = "User is active" },
				new() { StatusName = "Inactive",  Module = "User",    IsActive = false, SortOrder = 2,  Description = "User is inactive" },
				new() { StatusName = "Suspended", Module = "User",    IsActive = false, SortOrder = 3,  Description = "User is suspended" },
				new() { StatusName = "Deleted",   Module = "User",    IsActive = false, SortOrder = 4,  Description = "User is soft-deleted" },

                // License module statuses
                new() { StatusName = "Active",    Module = "License", IsActive = true,  SortOrder = 1,  Description = "License is active" },
				new() { StatusName = "Expired",   Module = "License", IsActive = false, SortOrder = 2,  Description = "License has expired" },
				new() { StatusName = "Revoked",   Module = "License", IsActive = false, SortOrder = 3,  Description = "License has been revoked" },
				new() { StatusName = "Trial",     Module = "License", IsActive = true,  SortOrder = 4,  Description = "License is a trial" },

                // RBAC module statuses
                new() { StatusName = "Active",    Module = "RBAC",    IsActive = true,  SortOrder = 1,  Description = "Role/permission is active" },
				new() { StatusName = "Inactive",  Module = "RBAC",    IsActive = false, SortOrder = 2,  Description = "Role/permission is inactive" },

                // General module statuses
                new() { StatusName = "Active",    Module = "General", IsActive = true,  SortOrder = 1,  Description = "Record is active" },
				new() { StatusName = "Inactive",  Module = "General", IsActive = false, SortOrder = 2,  Description = "Record is inactive" },
				new() { StatusName = "Pending",   Module = "General", IsActive = true,  SortOrder = 3,  Description = "Record is pending review" },
				new() { StatusName = "Archived",  Module = "General", IsActive = false, SortOrder = 4,  Description = "Record is archived" },
			};

			db.CommonStatus.AddRange(statuses);
			await db.SaveChangesAsync();
			logger.LogInformation("DatabaseInitializer: seeded {Count} CommonStatus rows.", statuses.Count);
		}

		// ── SuperAdmin user ───────────────────────────────────────

		private static async Task SeedSuperAdminAsync(
			IServiceProvider services, ILogger logger)
		{
			var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
			var config = services.GetRequiredService<IConfiguration>();

			// Read from config — never hardcode credentials in code
			var email = config["SeedAdmin:Email"] ?? "admin@calcifer.local";
			var password = config["SeedAdmin:Password"] ?? "Admin@12345";
			var employeeId = config["SeedAdmin:EmpId"] ?? "EMP-0001";

			if (await userManager.FindByEmailAsync(email) != null)
			{
				logger.LogInformation("DatabaseInitializer: SuperAdmin already exists, skipping.");
				return;
			}

			var admin = new ApplicationUser
			{
				UserName = email,
				Email = email,
				Name = "System Administrator",
				EmployeeId = employeeId,
				StatusId = 1, // Active
				EmailConfirmed = true
			};

			var result = await userManager.CreateAsync(admin, password);
			if (!result.Succeeded)
			{
				logger.LogError("DatabaseInitializer: failed to create SuperAdmin: {Errors}",
					string.Join(", ", result.Errors.Select(e => e.Description)));
				return;
			}

			await userManager.AddToRoleAsync(admin, DefaultRoles.SuperAdmin);
			logger.LogInformation("DatabaseInitializer: SuperAdmin created → {Email}", email);
		}
	}
}