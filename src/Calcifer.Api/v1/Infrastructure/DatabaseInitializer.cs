using Microsoft.EntityFrameworkCore;
using Calcifer.Api.DbContexts;

namespace Calcifer.Api.Infrastructure
{
    public class DatabaseInitializer
    {
		public static void Initialize(CalciferAppDbContext context)
		{
			try
			{
				// Ensure database is created and migrations are applied
				context.Database.Migrate();

				// Seed data
				//CommonStatusSeeder.Seed(context);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Database initialization failed: {ex.Message}");
				throw;
			}
		}
	}
}
