using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Infrastructure
{
    public static class DatabaseInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<CalciferAppDbContext>();

            // GUARD CLAUSE: Fast check to see if seeding has already happened.
            // AnyAsync() generates a highly optimized 'SELECT EXISTS' SQL query.
            // If data is there, we exit the method instantly.
            if (await dbContext.CommonStatus.AnyAsync())
            {
                return;
            }

            // 1. Seed Roles
            string[] roles = { "SUPERADMIN", "ADMIN", "MODERATOR", "REGULAR USER" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = role });
                }
            }

            // 2. Seed Super Admin
            var superUserEmail = "superadmin@system.com";
            var superUser = await userManager.FindByEmailAsync(superUserEmail);

            if (superUser == null)
            {
                var newSuperAdmin = new ApplicationUser
                {
                    UserName = superUserEmail,
                    Email = superUserEmail,
                    Name = "System Super Admin",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newSuperAdmin, "p@ssword");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newSuperAdmin, "SUPERADMIN");
                }
            }

            // 3. Seed Common Status
            var statuses = new List<CommonStatus>
            {
                new() { StatusName = "Active", Description = "Active record", Module = "Global", IsActive = true, SortOrder = 1 },
                new() { StatusName = "Inactive", Description = "Inactive record", Module = "Global", IsActive = true, SortOrder = 2 },
                new() { StatusName = "Deleted", Description = "Deleted record", Module = "Global", IsActive = true, SortOrder = 3 },
                new() { StatusName = "Draft", Description = "Draft record", Module = "Global", IsActive = true, SortOrder = 4 },
                new() { StatusName = "Pending", Description = "Pending approval", Module = "Global", IsActive = true, SortOrder = 5 },
                new() { StatusName = "Approved", Description = "Approved record", Module = "Global", IsActive = true, SortOrder = 6 },
                new() { StatusName = "Rejected", Description = "Rejected record", Module = "Global", IsActive = true, SortOrder = 7 },
                new() { StatusName = "Expired", Description = "Expired record", Module = "Global", IsActive = true, SortOrder = 8 },
                new() { StatusName = "Terminated", Description = "Terminated record", Module = "Global", IsActive = true, SortOrder = 9 },
                new() { StatusName = "Suspended", Description = "Suspended record", Module = "Global", IsActive = true, SortOrder = 10 }
            };

            await dbContext.CommonStatus.AddRangeAsync(statuses);
            await dbContext.SaveChangesAsync();
        }
    }
}