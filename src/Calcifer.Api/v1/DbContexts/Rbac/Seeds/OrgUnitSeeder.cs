// ============================================================
//  OrgUnitSeeder.cs
//  Seeds the default OrganizationUnit tree.
//  This is the skeleton every real deployment will customise.
//
//  Default tree:
//      ROOT (Level 0)
//      └── HQ (Level 1)
//          ├── HR Department (Level 2)
//          ├── Finance (Level 2)
//          └── Administration (Level 2)
//      └── Factory-1 (Level 1)
//          ├── Production Floor (Level 2)
//          ├── Quality Control (Level 2)
//          └── Warehouse (Level 2)
// ============================================================

using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.Rbac.Entities;
using Calcifer.Api.DbContexts.Rbac.Enums;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.DbContexts.Rbac.Seeds
{
	public static class OrgUnitSeeder
	{
		public static async Task SeedAsync(CalciferAppDbContext db, ILogger logger)
		{
			//if (await db.OrganizationUnits.AnyAsync())
			//{
			//	logger.LogInformation("OrgUnitSeeder: units already exist, skipping.");
			//	return;
			//}
			if (!await db.OrganizationUnits.AnyAsync(u => u.Id == 1))
			{
				db.OrganizationUnits.Add(new OrganizationUnit
				{
					//Id = 1,
					Name = "Root",
					ParentId = null,
					IsActive = true
				});
				await db.SaveChangesAsync();
			}
			var root = new OrganizationUnit
			{
				Code = "ROOT",
				Name = "Organization Root",
				Level = OrgUnitLevel.Root,
				ParentId = null,
				IsActive = true
			};
			db.OrganizationUnits.Add(root);
			await db.SaveChangesAsync(); // need root.Id

			var hq = new OrganizationUnit
			{
				Code = "HQ",
				Name = "Head Office",
				Level = OrgUnitLevel.Company,
				ParentId = root.Id,
				IsActive = true
			};
			var factory1 = new OrganizationUnit
			{
				Code = "FACTORY_1",
				Name = "Factory 1",
				Level = OrgUnitLevel.Company,
				ParentId = root.Id,
				IsActive = true
			};
			db.OrganizationUnits.AddRange(hq, factory1);
			await db.SaveChangesAsync();

			var departments = new[]
			{
				new OrganizationUnit { Code = "HQ_HR",     Name = "HR Department",    Level = OrgUnitLevel.Department, ParentId = hq.Id,       IsActive = true },
				new OrganizationUnit { Code = "HQ_FIN",    Name = "Finance",           Level = OrgUnitLevel.Department, ParentId = hq.Id,       IsActive = true },
				new OrganizationUnit { Code = "HQ_ADMIN",  Name = "Administration",    Level = OrgUnitLevel.Department, ParentId = hq.Id,       IsActive = true },
				new OrganizationUnit { Code = "F1_PROD",   Name = "Production Floor",  Level = OrgUnitLevel.Department, ParentId = factory1.Id, IsActive = true },
				new OrganizationUnit { Code = "F1_QC",     Name = "Quality Control",   Level = OrgUnitLevel.Department, ParentId = factory1.Id, IsActive = true },
				new OrganizationUnit { Code = "F1_STORE",  Name = "Warehouse",         Level = OrgUnitLevel.Department, ParentId = factory1.Id, IsActive = true },
			};
			db.OrganizationUnits.AddRange(departments);
			await db.SaveChangesAsync();

			logger.LogInformation("OrgUnitSeeder: seeded {Count} organization units.",
				2 + departments.Length + 1); // root + level1 + departments
		}
	}
}