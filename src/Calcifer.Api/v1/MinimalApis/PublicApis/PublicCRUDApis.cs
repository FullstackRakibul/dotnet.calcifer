using Calcifer.Api.Helper.ApiResponse;
using Calcifer.Api.Interface.Common;

namespace Calcifer.Api.MinimalApis.PublicApis
{
    public static class PublicCRUDApis
    {
        public static IEndpointRouteBuilder MapPublicCrudApi(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/public")
                           .WithTags("Public");
            // Get public data
            group.MapGet("/", async (IPublicInterface publicInterface) =>
            {
                var response = await publicInterface.GetPublicDataAsync();
                return Results.Ok(new ApiResponseDto<string>
                {
                    Status = true,
                    Message = "Public data retrieved successfully.",
                    Data = response
                });
            });
            return app;
        }
    }
}
