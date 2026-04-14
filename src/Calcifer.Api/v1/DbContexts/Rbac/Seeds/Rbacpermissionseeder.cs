// ============================================================
//  RbacPermissionSeeder.cs
//  Seeds the COMPLETE permission matrix from your design doc.
//
//  This seeder is idempotent — safe to run on every startup.
//  It only inserts what doesn't already exist.
//
//  The matrix:
//      SuperAdmin     → CRUD on everything
//      HRManager      → CRUD on HCM; R on all others they have access
//      ProductionMgr  → CRUD on Production, Quality, IE; R elsewhere
//      StoreManager   → CRUD on Inventory; R on Production
//      Employee       → R on self (HCM only)
//      Viewer         → R on all modules (no Administration)
// ============================================================

using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Rbac.Entities;
using Calcifer.Api.DbContexts.Rbac.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security;

namespace Calcifer.Api.DbContexts.Rbac.Seeds
{
	public static class RbacPermissionSeeder
	{
		// ── Module × Resource definitions ────────────────────────
		// Each module has at least one resource. Expand as needed.
		private static readonly Dictionary<string, string[]> ModuleResources = new()
		{
			["HCM"] = ["Employee", "LeaveRequest", "Attendance", "PaySlip"],
			["Merchandising"] = ["Product", "PriceList", "Catalogue"],
			["SampleDev"] = ["SampleRequest", "BOM"],
			["Production"] = ["WorkOrder", "ProductionPlan", "ShiftReport"],
			["Inventory"] = ["StockItem", "StockMovement", "Warehouse"],
			["SupplyChain"] = ["PurchaseOrder", "Supplier", "Shipment"],
			["Quality"] = ["QCReport", "Defect", "Inspection"],
			["Finance"] = ["Payroll", "Invoice", "Budget"],
			["IE"] = ["ProcessStudy", "Efficiency", "Layout"],
			["Commercial"] = ["SalesOrder", "Customer", "Quotation"],
			["Compliance"] = ["AuditLog", "PolicyDocument"],
			["Administration"] = ["SystemConfig", "UserManagement"],
			["MIS"] = ["Dashboard", "Report"],
			["Projects"] = ["Project", "Milestone", "Task"],
			["OfficeDocs"] = ["Document", "Template"]
		};

		private static readonly string[] AllActions = ["Create", "Read", "Update", "Delete"];

		// ── Permission matrix (role → module → actions) ──────────
		// "CRUD" = all four actions. "R" = Read only. null/missing = no access.
		private static readonly Dictionary<string, Dictionary<string, string[]>> RoleMatrix = new()
		{
			[DefaultRoles.SuperAdmin] = AllModules(AllActions),

			[DefaultRoles.HRManager] = new()
			{
				["HCM"] = AllActions,
				["Merchandising"] = ["Read"],
				["SampleDev"] = ["Read"],
				["Production"] = ["Read"],
				["Inventory"] = ["Read"],
				["SupplyChain"] = ["Read"],
				["Quality"] = ["Read"],
				["Finance"] = ["Read"],
				["IE"] = ["Read"],
				["Commercial"] = ["Read"],
				["Compliance"] = ["Read"],
				// Administration → no access
				["MIS"] = ["Read"],
				["Projects"] = ["Read"],
				["OfficeDocs"] = ["Read"]
			},

			[DefaultRoles.ProductionManager] = new()
			{
				["HCM"] = ["Read"],
				["Merchandising"] = ["Read"],
				["SampleDev"] = ["Read"],
				["Production"] = AllActions,
				["Inventory"] = ["Read"],
				["SupplyChain"] = ["Read"],
				["Quality"] = AllActions,
				// Finance → no access
				["IE"] = AllActions,
				["Commercial"] = ["Read"],
				["Compliance"] = ["Read"],
				// Administration → no access
				["MIS"] = ["Read"],
				["Projects"] = ["Read"],
				["OfficeDocs"] = ["Read"]
			},

			[DefaultRoles.StoreManager] = new()
			{
				["HCM"] = ["Read"],
				["Merchandising"] = ["Read"],
				// SampleDev → no access
				["Production"] = ["Read"],
				["Inventory"] = AllActions,
				["SupplyChain"] = ["Read"],
				["Quality"] = ["Read"],
				// Finance → no access
				// IE → no access
				["Commercial"] = ["Read"],
				// Compliance → no access
				// Administration → no access
				["MIS"] = ["Read"],
				["Projects"] = ["Read"],
				["OfficeDocs"] = ["Read"]
			},

			// Employee: Read on HCM:self only
			// Modelled as Read on HCM; the "self" scope is enforced
			// in the service layer (filter by userId), not at permission level.
			[DefaultRoles.Employee] = new()
			{
				["HCM"] = ["Read"],
				["OfficeDocs"] = ["Read"]
			},

			[DefaultRoles.Viewer] = new()
			{
				["HCM"] = ["Read"],
				["Merchandising"] = ["Read"],
				["SampleDev"] = ["Read"],
				["Production"] = ["Read"],
				["Inventory"] = ["Read"],
				["SupplyChain"] = ["Read"],
				["Quality"] = ["Read"],
				["Finance"] = ["Read"],
				["IE"] = ["Read"],
				["Commercial"] = ["Read"],
				["Compliance"] = ["Read"],
				// Administration → no access
				["MIS"] = ["Read"],
				["Projects"] = ["Read"],
				["OfficeDocs"] = ["Read"]
			}
		};

		// ── Entry point ──────────────────────────────────────────

		public static async Task SeedAsync(IServiceProvider services)
		{
			var db = services.GetRequiredService<CalciferAppDbContext>();
			var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
			var logger = services.GetRequiredService<ILogger<CalciferAppDbContext>>();

			logger.LogInformation("RBAC Seeder: starting...");

			// 1. Seed all Permission rows
			await SeedPermissionsAsync(db, logger);

			// 2. Seed ApplicationRole rows
			await SeedRolesAsync(roleManager, logger);

			// 3. Assign permissions to roles
			await SeedRolePermissionsAsync(db, logger);

			logger.LogInformation("RBAC Seeder: complete.");
		}

		// ── Step 1: seed Permission rows ─────────────────────────

		private static async Task SeedPermissionsAsync(
			CalciferAppDbContext db, ILogger logger)
		{
			var existingKeys = await db.Permissions
				.Select(p => $"{p.Module}:{p.Resource}:{p.Action}")
				.ToHashSetAsync();

			var toInsert = new List<Permission>();

			foreach (var (module, resources) in ModuleResources)
			{
				foreach (var resource in resources)
				{
					foreach (var action in AllActions)
					{
						var key = $"{module}:{resource}:{action}";
						if (!existingKeys.Contains(key))
						{
							toInsert.Add(new Permission
							{
								Module = module,
								Resource = resource,
								Action = action,
								Description = $"{action} on {module}/{resource}",
								IsActive = true
							});
						}
					}
				}
			}

			if (toInsert.Count > 0)
			{
				db.Permissions.AddRange(toInsert);
				await db.SaveChangesAsync();
				logger.LogInformation("RBAC Seeder: inserted {Count} permissions", toInsert.Count);
			}
		}

		// ── Step 2: seed ApplicationRole rows ────────────────────

		private static async Task SeedRolesAsync(
			RoleManager<ApplicationRole> roleManager, ILogger logger)
		{
			var roles = new[]
			{
				(DefaultRoles.SuperAdmin,        "Full CRUD on all resources",                          true),
				(DefaultRoles.HRManager,         "Full CRUD on HCM; Read elsewhere",                   true),
				(DefaultRoles.ProductionManager, "Full CRUD on Production, QC, IE; Read elsewhere",    true),
				(DefaultRoles.StoreManager,      "Full CRUD on Inventory; Read on Production",         true),
				(DefaultRoles.Employee,          "Read-only on own profile and attendance",             true),
				(DefaultRoles.Viewer,            "Read-only on all modules (no Administration)",        true),
			};

			foreach (var (name, description, isSystem) in roles)
			{
				if (!await roleManager.RoleExistsAsync(name))
				{
					var role = new ApplicationRole
					{
						Name = name,
						Description = description,
						IsSystemRole = isSystem
					};
					var result = await roleManager.CreateAsync(role);
					if (result.Succeeded)
						logger.LogInformation("RBAC Seeder: created role {Role}", name);
					else
						logger.LogError("RBAC Seeder: failed to create role {Role}: {Errors}",
							name, string.Join(", ", result.Errors.Select(e => e.Description)));
				}
			}
		}

		// ── Step 3: assign permissions to roles ──────────────────

		private static async Task SeedRolePermissionsAsync(
			CalciferAppDbContext db, ILogger logger)
		{
			// Load all permissions and roles into memory once
			var allPermissions = await db.Permissions.ToListAsync();
			var allRoles = await db.Roles.ToListAsync();

			var existingLinks = await db.RolePermissions
				.Select(rp => $"{rp.RoleId}:{rp.PermissionId}")
				.ToHashSetAsync();

			var toInsert = new List<RolePermission>();

			foreach (var (roleName, moduleAccess) in RoleMatrix)
			{
				var role = allRoles.FirstOrDefault(r => r.Name == roleName);
				if (role == null) continue;

				foreach (var (module, actions) in moduleAccess)
				{
					// Match permissions: any resource in the module, matching allowed actions
					var matchingPerms = allPermissions
						.Where(p => p.Module == module && actions.Contains(p.Action));

					foreach (var perm in matchingPerms)
					{
						var linkKey = $"{role.Id}:{perm.Id}";
						if (!existingLinks.Contains(linkKey))
						{
							toInsert.Add(new RolePermission
							{
								RoleId = role.Id,
								PermissionId = perm.Id,
								AssignedBy = "Seeder"
							});
							existingLinks.Add(linkKey);
						}
					}
				}
			}

			if (toInsert.Count > 0)
			{
				db.RolePermissions.AddRange(toInsert);
				await db.SaveChangesAsync();
				logger.LogInformation("RBAC Seeder: linked {Count} role-permissions", toInsert.Count);
			}
		}

		// ── Helper: all modules with given actions ────────────────

		private static Dictionary<string, string[]> AllModules(string[] actions)
			=> ModuleResources.Keys.ToDictionary(m => m, _ => actions);
	}
}