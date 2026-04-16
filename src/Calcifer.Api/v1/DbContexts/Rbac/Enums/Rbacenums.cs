namespace Calcifer.Api.DbContexts.Rbac.Enums
{
	// ════════════════════════════════════════════════════════════════════════
	//  RbacModule  —  all 15 modules from the permission matrix
	//  Used as the Module field value in Permission rows.
	//  String value matches what is stored in the DB.
	// ════════════════════════════════════════════════════════════════════════
	public static class RbacModule
	{
		public const string HCM = "HCM";
		public const string Merchandising = "Merchandising";
		public const string SampleDev = "SampleDev";
		public const string Production = "Production";
		public const string Inventory = "Inventory";
		public const string SupplyChain = "SupplyChain";
		public const string Quality = "Quality";
		public const string Finance = "Finance";
		public const string IE = "IE";
		public const string Commercial = "Commercial";
		public const string Compliance = "Compliance";
		public const string Administration = "Administration";
		public const string MIS = "MIS";
		public const string Projects = "Projects";
		public const string OfficeDocs = "OfficeDocs";

		public static readonly IReadOnlyList<string> All =
		[
			HCM, Merchandising, SampleDev, Production, Inventory,
			SupplyChain, Quality, Finance, IE, Commercial,
			Compliance, Administration, MIS, Projects, OfficeDocs
		];
	}

	// ════════════════════════════════════════════════════════════════════════
	//  RbacAction  —  the five allowed action verbs
	// ════════════════════════════════════════════════════════════════════════
	public static class RbacAction
	{
		public const string Create = "Create";
		public const string Read = "Read";
		public const string Update = "Update";
		public const string Delete = "Delete";
		public const string Export = "Export";

		public static readonly IReadOnlyList<string> All = [Create, Read, Update, Delete, Export];
		public static readonly IReadOnlyList<string> Crud = [Create, Read, Update, Delete];
		public static readonly IReadOnlyList<string> ReadOnly = [Read];
	}

	// ════════════════════════════════════════════════════════════════════════
	//  DefaultRoles  —  the six seeded role names
	// ════════════════════════════════════════════════════════════════════════
	public static class DefaultRoles
	{
		public const string SuperAdmin = "SUPERADMIN";
		public const string HrManager = "HR_MANAGER";
		public const string ProductionManager = "PRODUCTION_MANAGER";
		public const string StoreManager = "STORE_MANAGER";
		public const string Employee = "EMPLOYEE";
		public const string Viewer = "VIEWER";
	}

	// ════════════════════════════════════════════════════════════════════════
	//  OrgUnitLevel  —  hierarchy levels for OrganizationUnit
	// ════════════════════════════════════════════════════════════════════════
	public static class OrgUnitLevel
	{
		public const string Root = "Root";
		public const string Company = "Company";
		public const string Division = "Division";
		public const string Department = "Department";
		public const string Team = "Team";
	}
}