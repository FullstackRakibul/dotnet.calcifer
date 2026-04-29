using Calcifer.Api.AuthHandler.Configuration;
using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.Interface.Common;
using Calcifer.Api.Interface.Licensing;
using Calcifer.Api.Services;
using Calcifer.Api.Services.AuthService;
using Calcifer.Api.Services.LicenseService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Calcifer.Api.MinimalApis.PublicApis.UsageExamples;
using Calcifer.Api.Rbac.Interfaces;
using Calcifer.Api.Rbac.Services;
using Calcifer.Api.Rbac.Repositories;
using Calcifer.Api.Helper.LogWriter;

namespace Calcifer.Api.DependencyInversion
{
	public class DependencyInversion
	{
		internal static void RegisterServices(IServiceCollection services, IConfiguration configuration)
		{
			// ── Existing ──────────────────────────────────────────────────
			services.AddScoped<IPublicInterface, PublicService>();

			// ── Auth services ─────────────────────────────────────────────
			services.AddScoped<TokenService>();
			services.AddScoped<AuthService>();
			services.AddScoped<RoleService>();

			// ── RBAC engine ───────────────────────────────────────────────
			// RbacService for permission resolution (core RBAC logic)
			services.AddScoped<RbacService>();

			// RoleManagementService for admin CRUD operations on roles
			services.AddScoped<IRoleManagementService, RoleManagementService>();
			services.AddScoped<IUserAdminService, UserAdminService>();
			services.AddScoped<IOrganizationUnitService, OrganizationUnitService>();
			services.AddScoped<IAuditLogService, AuditLogService>();
			services.AddScoped<IActiveSessionService, ActiveSessionService>();
			services.AddScoped<ISystemStatusService, SystemStatusService>();

			// ── Read repositories (for complex queries) ────────────────────
			services.AddScoped<IUserReadRepository, UserReadRepository>();
			services.AddScoped<IAuditLogRepository, AuditLogRepository>();

			// ── Licensing engine ──────────────────────────────────────────
			services.AddScoped<ILicenseService, LicenseService>();

			// ── Dynamic Log Writer (singleton — shared across all requests) ──
			services.AddDynamicLogWriter();

			// ── Usage example stubs (replace with real implementations) ──
			services.AddScoped<IPayrollService, StubPayrollService>();

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
			// Secret MUST come from user-secrets (dev) or Key Vault (prod).
			// Minimum 32 characters required for HMAC-SHA256.
			var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
				?? throw new InvalidOperationException(
					"JwtSettings section is missing from configuration. " +
					"Run: dotnet user-secrets set \"JwtSettings:Secret\" \"<your-32-char-secret>\"");

			if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
				throw new InvalidOperationException(
					$"JWT Secret must be at least 32 characters (current: {jwtSettings.Secret?.Length ?? 0}). " +
					"Run: dotnet user-secrets set \"JwtSettings:Secret\" \"<your-32-char-secret>\"");

			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ClockSkew = TimeSpan.FromMinutes(1),
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