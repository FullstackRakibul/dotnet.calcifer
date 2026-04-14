// ============================================================
//  RbacEnums.cs
//  All RBAC-related enumerations in one file.
//  Kept together so seeder and filter share the same constants.
// ============================================================

namespace Calcifer.Api.DbContexts.Rbac.Enums
{
	// ── Modules (matches Permission.Module column) ───────────────
	// Add new modules here — seeder picks them up automatically.
	public enum RbacModule
	{
		HCM,
		Merchandising,
		SampleDev,
		Production,
		Inventory,
		SupplyChain,
		Quality,
		Finance,
		IE,
		Commercial,
		Compliance,
		Administration,
		MIS,
		Projects,
		OfficeDocs
	}

	// ── Actions ─────────────────────────────────────────────────
	[Flags]
	public enum RbacAction
	{
		None = 0,
		Create = 1 << 0,   // 1
		Read = 1 << 1,   // 2
		Update = 1 << 2,   // 4
		Delete = 1 << 3,   // 8
		Export = 1 << 4,   // 16
		All = Create | Read | Update | Delete   // 15  (no Export by default)
	}

	// ── Built-in role codes (the seed names) ────────────────────
	// Used by seeder and by RbacClaimTypes to avoid magic strings.
	public static class DefaultRoles
	{
		public const string SuperAdmin = "SuperAdmin";
		public const string HRManager = "HRManager";
		public const string ProductionManager = "ProductionManager";
		public const string StoreManager = "StoreManager";
		public const string Employee = "Employee";
		public const string Viewer = "Viewer";
	}

	// ── CommonStatus values (seed IDs match RbacStatusSeeder) ───
	public enum CommonStatusEnum : byte
	{
		Inactive = 0,
		Active = 1,
		Deleted = 2,
		Suspended = 3
	}

	// ── OrganizationUnit levels ──────────────────────────────────
	public enum OrgUnitLevel : byte
	{
		Root = 0,
		Company = 1,
		Division = 2,
		Department = 3,
		Team = 4
	}
}