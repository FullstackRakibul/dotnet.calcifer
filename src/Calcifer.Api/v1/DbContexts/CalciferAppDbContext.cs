using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Common;
using Calcifer.Api.DbContexts.Licensing;
using Calcifer.Api.DbContexts.Models;


namespace Calcifer.Api.DbContexts
{
    public class CalciferAppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public CalciferAppDbContext(DbContextOptions<CalciferAppDbContext> options)
            : base(options)
        {

        }

        public DbSet<PublicData> PublicData { get; set; }
        public DbSet<CommonStatus> CommonStatus { get; set; }
        public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<ApplicationRole> ApplicationRole { get; set; }

        // Licensing
        public DbSet<License> Licenses { get; set; }
        public DbSet<LicenseType> LicenseTypes { get; set; }
        public DbSet<LicenseFeature> LicenseFeatures { get; set; }
        public DbSet<LicenseActivation> LicenseActivations { get; set; }


		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<ApplicationUser>()
				.HasIndex(u => u.EmployeeId)
				.IsUnique();

			// License key must be unique for fast validation lookups
			builder.Entity<License>()
				.HasIndex(l => l.LicenseKey)
				.IsUnique();

			// Prevent duplicate machine activations at DB level — code lies, constraints don't
			builder.Entity<LicenseActivation>()
				.HasIndex(a => new { a.LicenseId, a.MachineId })
				.IsUnique();

			
		}
	}
}
