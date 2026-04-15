using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Common;
using Calcifer.Api.DbContexts.Licensing;
using Calcifer.Api.DbContexts.Models;
using Calcifer.Api.DbContexts.Rbac.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Security;


namespace Calcifer.Api.DbContexts
{
    public class CalciferAppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public CalciferAppDbContext(DbContextOptions<CalciferAppDbContext> options)
            : base(options)
        {

        }

		// ── Common ───────────────────────────────────────────────
		public DbSet<CommonStatus> CommonStatus { get; set; }

		// ── Domain ───────────────────────────────────────────────
		public DbSet<PublicData> PublicData { get; set; }

		public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<ApplicationRole> ApplicationRole { get; set; }

		// Licensing
		// ── Licensing ────────────────────────────────────────────
		public DbSet<License> Licenses { get; set; }
		public DbSet<LicenseType> LicenseTypes { get; set; }
		public DbSet<LicenseFeature> LicenseFeatures { get; set; }
		public DbSet<LicenseActivation> LicenseActivations { get; set; }

		// ── RBAC engine ──────────────────────────────────────────
		public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
		public DbSet<Permission> Permissions { get; set; }
		public DbSet<RolePermission> RolePermissions { get; set; }
		public DbSet<UserUnitRole> UserUnitRoles { get; set; }
		public DbSet<UserDirectPermission> UserDirectPermissions { get; set; }
		public DbSet<PermissionCache> PermissionCache { get; set; }


		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// ── ApplicationUser ──────────────────────────────────
			builder.Entity<ApplicationUser>()
				.HasIndex(u => u.EmployeeId)
				.IsUnique()
				.HasDatabaseName("IX_ApplicationUser_EmployeeId");

			// Soft-delete query filter: IsDeleted users are invisible by default.
			// Override with .IgnoreQueryFilters() when you need to see them.
			builder.Entity<ApplicationUser>()
				.HasQueryFilter(u => !u.IsDeleted);

			// ── Licensing ────────────────────────────────────────
			builder.Entity<License>()
				.HasIndex(l => l.LicenseKey)
				.IsUnique()
				.HasDatabaseName("IX_License_LicenseKey");

			builder.Entity<LicenseActivation>()
				.HasIndex(a => new { a.LicenseId, a.MachineId })
				.IsUnique()
				.HasDatabaseName("IX_LicenseActivation_LicenseMachine");

			// License → LicenseType FK (restrict delete: don't drop a type in use)
			builder.Entity<License>()
				.HasOne(l => l.LicenseType)
				.WithMany()
				.HasForeignKey(l => l.LicenseTypeId)
				.OnDelete(DeleteBehavior.Restrict);

			// ── OrganizationUnit (self-referencing tree) ─────────
			builder.Entity<OrganizationUnit>()
				.HasIndex(u => u.Code)
				.IsUnique()
				.HasDatabaseName("IX_OrgUnit_Code");

			builder.Entity<OrganizationUnit>()
				.HasOne(u => u.Parent)
				.WithMany(u => u.Children)
				.HasForeignKey(u => u.ParentId)
				.OnDelete(DeleteBehavior.Restrict); // never cascade-delete a tree node

			// ── Permission ───────────────────────────────────────
			// One row per Module + Resource + Action combination
			builder.Entity<Permission>()
				.HasIndex(p => new { p.Module, p.Resource, p.Action })
				.IsUnique()
				.HasDatabaseName("IX_Permission_ModuleResourceAction");

			// ── RolePermission — composite PK ────────────────────
			builder.Entity<RolePermission>()
				.HasKey(rp => new { rp.RoleId, rp.PermissionId });

			builder.Entity<RolePermission>()
				.HasOne(rp => rp.Role)
				.WithMany(r => r.RolePermissions)
				.HasForeignKey(rp => rp.RoleId)
				.OnDelete(DeleteBehavior.Cascade); // delete role → delete its permission links

			builder.Entity<RolePermission>()
				.HasOne(rp => rp.Permission)
				.WithMany(p => p.RolePermissions)
				.HasForeignKey(rp => rp.PermissionId)
				.OnDelete(DeleteBehavior.Cascade);

			// ── UserUnitRole — composite PK ──────────────────────
			// (UserId, RoleId, UnitId) — one role per user per unit
			builder.Entity<UserUnitRole>()
				.HasKey(uur => new { uur.UserId, uur.RoleId, uur.UnitId });

			builder.Entity<UserUnitRole>()
				.HasOne(uur => uur.User)
				.WithMany(u => u.UnitRoles)
				.HasForeignKey(uur => uur.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<UserUnitRole>()
				.HasOne(uur => uur.Role)
				.WithMany()
				.HasForeignKey(uur => uur.RoleId)
				.OnDelete(DeleteBehavior.Restrict); // don't cascade-delete role assignments

			builder.Entity<UserUnitRole>()
				.HasOne(uur => uur.Unit)
				.WithMany(u => u.UserRoles)
				.HasForeignKey(uur => uur.UnitId)
				.OnDelete(DeleteBehavior.Restrict);

			// ── UserDirectPermission — composite PK ──────────────
			builder.Entity<UserDirectPermission>()
				.HasKey(dp => new { dp.UserId, dp.PermissionId });

			builder.Entity<UserDirectPermission>()
				.HasOne(dp => dp.User)
				.WithMany(u => u.DirectPermissions)
				.HasForeignKey(dp => dp.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<UserDirectPermission>()
				.HasOne(dp => dp.Permission)
				.WithMany(p => p.DirectGrants)
				.HasForeignKey(dp => dp.PermissionId)
				.OnDelete(DeleteBehavior.Cascade);

			// ── PermissionCache ──────────────────────────────────
			// Unique per user + unit combination
			builder.Entity<PermissionCache>()
				.HasIndex(pc => new { pc.UserId, pc.UnitId })
				.IsUnique()
				.HasDatabaseName("IX_PermissionCache_UserUnit");

			builder.Entity<PermissionCache>()
				.HasOne(pc => pc.User)
				.WithMany()
				.HasForeignKey(pc => pc.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<PermissionCache>()
				.HasOne(pc => pc.Unit)
				.WithMany()
				.HasForeignKey(pc => pc.UnitId)
				.OnDelete(DeleteBehavior.SetNull);

			// ── CommonStatus ─────────────────────────────────────
			// Composite index for fast module-scoped lookups
			builder.Entity<CommonStatus>()
				.HasIndex(cs => new { cs.Module, cs.IsActive })
				.HasDatabaseName("IX_CommonStatus_ModuleActive");
		}
	}
}
