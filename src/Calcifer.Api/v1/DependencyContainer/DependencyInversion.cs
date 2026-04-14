// ============================================================
//  DependencyInversion.cs — COMPLETE UPDATED VERSION
//
//  Registers all services in one place.
//  RBAC engine is registered as a SCOPED service (one per
//  HTTP request). Never singleton — it holds DbContext.
//
//  Isolation rule enforced here:
//  - RBAC services are registered in a dedicated block.
//  - License services are registered in a separate block.
//  - They do NOT reference each other.
// ============================================================

using Calcifer.Api.AuthHandler.Configuration;
using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Rbac.Interfaces;
using Calcifer.Api.DbContexts.Rbac.Services;
using Calcifer.Api.Interface.Common;
using Calcifer.Api.Interface.Licensing;
using Calcifer.Api.Services;
using Calcifer.Api.Services.AuthService;
using Calcifer.Api.Services.LicenseService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Calcifer.Api.DependencyInversion
{
	public static class DependencyInversion
	{
		public static IServiceCollection RegisterServices(
			IServiceCollection services,
			IConfiguration configuration)
		{
			// ── Identity ─────────────────────────────────────────
			services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
			{
				options.Password.RequireDigit = true;
				options.Password.RequiredLength = 8;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = true;
				options.Password.RequireLowercase = true;
				options.User.RequireUniqueEmail = true;
			})
			.AddEntityFrameworkStores<CalciferAppDbContext>()
			.AddDefaultTokenProviders();

			// ── JWT Authentication ────────────────────────────────
			var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
				?? throw new InvalidOperationException("JwtSettings not configured.");

			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtSettings.Issuer,
					ValidAudience = jwtSettings.Audience,
					IssuerSigningKey = new SymmetricSecurityKey(
												Encoding.UTF8.GetBytes(jwtSettings.Secret)),
					ClockSkew = TimeSpan.Zero   // no grace period
				};
			});

			// ── Authorization policies ───────────────────────────
			services.AddAuthorization(options =>
			{
				// Identity-level policies
				options.AddPolicy("SuperAdminPolicy",
					p => p.RequireRole("SuperAdmin"));

				options.AddPolicy("AuthenticatedPolicy",
					p => p.RequireAuthenticatedUser());

				// Example permission-level policies (used alongside [RequirePermission])
				options.AddPolicy("CanReadHCM",
					p => p.RequireClaim("perms", "HCM:Employee:Read"));

				options.AddPolicy("CanManageUsers",
					p => p.RequireClaim("perms", "Administration:UserManagement:Create",
												  "Administration:UserManagement:Update"));
			});

			// ── RBAC Engine ──────────────────────────────────────
			// Scoped: one instance per HTTP request.
			// ISOLATED: No license service imported here.
			services.AddScoped<IRbacService, RbacService>();

			// ── Auth Services ─────────────────────────────────────
			services.AddScoped<AuthService>();
			services.AddScoped<RoleService>();
			services.AddScoped<TokenService>();

			// ── License Services ──────────────────────────────────
			// ISOLATED: No RBAC service imported here.
			services.AddScoped<ILicenseService, LicenseService>();

			// ── Domain Services ───────────────────────────────────
			services.AddScoped<IPublicInterface, PublicService>();

			return services;
		}
	}
}