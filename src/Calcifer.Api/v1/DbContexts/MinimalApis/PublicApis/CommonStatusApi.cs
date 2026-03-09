using Calcifer.Api.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.DbContexts.MinimalApis.PublicApis
{
    public static class CommonStatusApi
    {
        public static void RegisterCommonStatusApis(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/common-status")
                           .WithTags("Common Status");

            // GET: All Statuses (Publicly readable for authenticated users)
            group.MapGet("/", async (CalciferAppDbContext db) =>
            {
                var statuses = await db.CommonStatus
                    .OrderBy(s => s.Module)
                    .ThenBy(s => s.SortOrder)
                    .Select(s => new CommonStatusResponseDto
                    {
                        Id = s.Id,
                        StatusName = s.StatusName,
                        Description = s.Description,
                        Module = s.Module,
                        IsActive = s.IsActive,
                        SortOrder = s.SortOrder
                    })
                    .ToListAsync();

                return Results.Ok(statuses);
            })
            .RequireAuthorization();

            // GET: Status by Id
            group.MapGet("/{id:int}", async (int id, CalciferAppDbContext db) =>
            {
                var status = await db.CommonStatus.FindAsync(id);

                if (status == null) return Results.NotFound("Status not found.");

                var response = new CommonStatusResponseDto
                {
                    Id = status.Id,
                    StatusName = status.StatusName,
                    Description = status.Description,
                    Module = status.Module,
                    IsActive = status.IsActive,
                    SortOrder = status.SortOrder
                };

                return Results.Ok(response);
            })
            .RequireAuthorization();

            // POST: Create new Status (Restricted to Super Admin)
            group.MapPost("/", async (CommonStatusRequestDto request, CalciferAppDbContext db) =>
            {
                var newStatus = new Common.CommonStatus
                {
                    StatusName = request.StatusName,
                    Description = request.Description,
                    Module = request.Module,
                    IsActive = request.IsActive,
                    SortOrder = request.SortOrder
                };

                db.CommonStatus.Add(newStatus);
                await db.SaveChangesAsync();

                return Results.Created($"/api/common-status/{newStatus.Id}", newStatus);
            })
            .RequireAuthorization("SuperAdminPolicy");

            // PUT: Update Status (Restricted to Super Admin)
            group.MapPut("/{id:int}", async (int id, CommonStatusRequestDto request, CalciferAppDbContext db) =>
            {
                var status = await db.CommonStatus.FindAsync(id);
                if (status == null) return Results.NotFound("Status not found.");

                status.StatusName = request.StatusName;
                status.Description = request.Description;
                status.Module = request.Module;
                status.IsActive = request.IsActive;
                status.SortOrder = request.SortOrder;

                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .RequireAuthorization("SuperAdminPolicy");

            // DELETE: Remove Status (Restricted to Super Admin)
            group.MapDelete("/{id:int}", async (int id, CalciferAppDbContext db) =>
            {
                var status = await db.CommonStatus.FindAsync(id);
                if (status == null) return Results.NotFound("Status not found.");

                db.CommonStatus.Remove(status);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .RequireAuthorization("SuperAdminPolicy");
        }
    }
}
