using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Calcifer.Api.AuthHandler.Configuration;
using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;

using Calcifer.Api.Services;
using Calcifer.Api.Services.AuthService;
using Calcifer.Api.Interface.Common;
using Calcifer.Api.Infrastructure;

namespace Calcifer.Api.DependencyInversion
{
    public class DependencyInversion
    {

        internal static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped <IPublicInterface ,PublicService>();










            // Register Identity - configure to NOT override default authentication scheme
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password settings (optional, adjust as needed)
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
                .AddEntityFrameworkStores<CalciferAppDbContext>()
                .AddDefaultTokenProviders();
           

            // Register custom services
            services.AddScoped<TokenService>();
            services.AddScoped<AuthService>();





            // Register JWT Authentication - Set JWT Bearer as default scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    var jwtSettings = serviceProvider.GetRequiredService<IConfiguration>().GetSection("JwtSettings").Get<JwtSettings>();

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                    };
                });

            // Configure Identity to NOT redirect to login page for API requests
            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

            // Register Role-based Authorization Policies (Dynamic)
            services.AddAuthorization(options =>
            {
                var roleNames = new[] { "Admin", "Manager", "Officer" }; // Can be fetched from DB if needed
                foreach (var roleName in roleNames)
                {
                    options.AddPolicy($"{roleName}Policy", policy => policy.RequireRole(roleName));
                }
            });


            // Add Http Context 

            services.AddHttpContextAccessor();

		}
	}
}
