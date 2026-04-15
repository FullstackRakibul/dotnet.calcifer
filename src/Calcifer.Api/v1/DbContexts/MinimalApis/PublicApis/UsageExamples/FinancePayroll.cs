using Calcifer.Api.AuthHandler.Filters;
using Calcifer.Api.DbContexts.Rbac.Interfaces;
namespace Calcifer.Api.DbContexts.MinimalApis.PublicApis.UsageExamples
{
	/// <summary>Stub interface — replace with real implementation when Finance module is built.</summary>
	public interface IPayrollService
	{
		Task<IEnumerable<object>> GetAllAsync();
		Task<byte[]> ExportAsync();
	}

	public static class FinancePayroll
	{
		public static void FinancePayrollApis(this IEndpointRouteBuilder app){

			var group = app.MapGroup("/finance/payroll")
						  .WithTags("Finance Payroll");

			group.MapGet("/", async (IRbacService rbac, IPayrollService payrollService, HttpContext ctx) =>
			{
				var payroll = await payrollService.GetAllAsync();
				return Results.Ok(payroll);
			})
			.WithMetadata(new RequirePermissionAttribute("Finance", "Payroll", "Read"))
			.RequireAuthorization();

			group.MapPost("/export", async (IRbacService rbac, IPayrollService payrollService, HttpContext ctx) =>
			{
				var fileBytes = await payrollService.ExportAsync();
				return Results.File(fileBytes, "text/csv", "payroll.csv");
			})
			.WithMetadata(new RequirePermissionAttribute("Finance", "Payroll", "Export"))
			.RequireAuthorization();
		}
		
	}
}
