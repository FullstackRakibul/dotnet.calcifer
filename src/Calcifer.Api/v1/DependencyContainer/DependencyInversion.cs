using Calcifer.Api.AuthHandler.Configuration;
using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.Interface.Common;
using Calcifer.Api.Interface.Licensing;
using Calcifer.Api.Interface.Rbac;
using Calcifer.Api.Services;
using Calcifer.Api.Services.AuthService;
using Calcifer.Api.Services.LicenseService;
using Calcifer.Api.Services.Rbac;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Calcifer.Api.DependencyInversion
{
	public class DependencyInversion
	{
		internal static void RegisterServices(IServiceCollection services)
		{
			// ── Existing ──────────────────────────────────────────────────
			services.AddScoped<IPublicInterface, PublicService>();

			// ── Auth services ─────────────────────────────────────────────
			services.AddScoped<TokenService>();
			services.AddScoped<AuthService>();
			services.AddScoped<RoleService>();

			// ── RBAC engine ───────────────────────────────────────────────
			services.AddScoped<IRbacService, RbacService>();

			// ── Licensing engine ──────────────────────────────────────────
			services.AddScoped<ILicenseService, LicenseService>();

			// ── Identity ──────────────────────────────────────────────────
			services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
			{
				options.Password.RequireDigit = false;
				options.Password.RequireLowercase = false;
				options.Password.RequireUppercase = false;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequiredLength = 6;
			})
			.AddEntityFrameworkStores<CalciferAppDbContext>()
			.AddDefaultTokenProviders();

			// ── JWT Authentication ─────────────────────────────────────────
			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				var sp = services.BuildServiceProvider();
				var jwtSettings = sp.GetRequiredService<IConfiguration>()
									.GetSection("JwtSettings").Get<JwtSettings>()!;

				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtSettings.Issuer,
					ValidAudience = jwtSettings.Audience,
					IssuerSigningKey = new SymmetricSecurityKey(
												  Encoding.UTF8.GetBytes(jwtSettings.Secret))
				};
			});

			// ── Identity cookie → return 401/403 JSON ─────────────────────
			services.ConfigureApplicationCookie(options =>
			{
				options.Events.OnRedirectToLogin = ctx =>
				{
					ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
					return Task.CompletedTask;
				};
				options.Events.OnRedirectToAccessDenied = ctx =>
				{
					ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
					return Task.CompletedTask;
				};
			});

			// ── Authorization policies ────────────────────────────────────
			services.AddAuthorization(options =>
			{
				options.AddPolicy("SuperAdminPolicy",
					p => p.RequireRole("SUPERADMIN"));

				options.AddPolicy("AdminPolicy",
					p => p.RequireRole("SUPERADMIN", "HR_MANAGER"));

				options.AddPolicy("ModeratorPolicy",
					p => p.RequireRole("SUPERADMIN", "HR_MANAGER", "PRODUCTION_MANAGER"));
			});

			services.AddHttpContextAccessor();
		}
	}
}