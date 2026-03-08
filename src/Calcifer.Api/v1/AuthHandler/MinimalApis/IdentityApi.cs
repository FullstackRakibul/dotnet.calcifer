using Microsoft.AspNetCore.Identity;
using Calcifer.Api.DbContexts.AuthModels;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.AuthHandler.MinimalApis
{
    public static class IdentityApi
    {
        public static void RegisterIdentityApi(this IEndpointRouteBuilder app)
        {

            var group = app.MapGroup("/identity")
                           .WithTags("Identity");

            group.MapGet("/user/{id}", async (string id, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                return user != null ? Results.Ok(new { user.Id, user.Email, user.Name }) : Results.NotFound("User not found");
            })
            .RequireAuthorization("AdminPolicy");

			group.MapGet("/user", async (UserManager<ApplicationUser> userManager) =>
			{
				var users = await userManager.Users.Select(user=> new { user.Id, user.Email , user.Name}).ToListAsync();
				return users.Any() ? Results.Ok(users) : Results.NotFound("No users found");
			})
			.RequireAuthorization();

			group.MapGet("/roles/{id}", async (string id, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user == null) return Results.NotFound("User not found");

                var roles = await userManager.GetRolesAsync(user);
                return Results.Ok(roles);
            })
            .RequireAuthorization();
        }
    }
}
