// ============================================================
//  TokenService.cs — UPDATED
//
//  Changes from original:
//  - Loads resolved permission claims from IRbacService
//  - Embeds them as "perms" claims in the JWT
//  - Each claim value is "Module:Resource:Action"
//  - All unit-role assignments merged into one global token
//
//  Token anatomy:
//      sub     = userId
//      email   = user email
//      name    = user display name
//      emp_id  = employee ID
//      roles   = ["HRManager", "Viewer"]           (ASP.NET role claims)
//      perms   = ["HCM:Employee:Read", ...]        (resolved permissions)
//      unit_roles = [{"unit":"Factory1","role":"HRManager"}, ...]
// ============================================================

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Calcifer.Api.AuthHandler.Configuration;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Rbac.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Calcifer.Api.Services.AuthService
{
	public class TokenService
	{
		private readonly JwtSettings _jwt;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IRbacService _rbac;

		public TokenService(
			IOptions<JwtSettings> jwt,
			UserManager<ApplicationUser> userManager,
			IRbacService rbac)
		{
			_jwt = jwt.Value;
			_userManager = userManager;
			_rbac = rbac;
		}

		public async Task<string> GenerateTokenAsync(ApplicationUser user)
		{
			// ── 1. Identity claims ────────────────────────────────
			var claims = new List<Claim>
			{
				new(JwtRegisteredClaimNames.Sub,   user.Id),
				new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
				new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
				new("name",   user.Name),
				new("emp_id", user.EmployeeId),
			};

			// ── 2. ASP.NET role claims ────────────────────────────
			var roles = await _userManager.GetRolesAsync(user);
			claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

			// ── 3. Unit-role context claims ───────────────────────
			// Tells the frontend which role the user has at each unit.
			var unitRoles = await _rbac.GetUserUnitRolesAsync(user.Id);
			foreach (var ur in unitRoles.Where(ur => !ur.IsExpired))
			{
				claims.Add(new Claim("unit_role",
					$"{ur.UnitName}:{ur.RoleName}"));
			}

			// ── 4. Permission claims (the RBAC engine output) ─────
			// This is the "perms" claim set. RbacAuthorizationFilter
			// reads these at request time — no DB hit needed.
			var permissions = await _rbac.BuildJwtPermissionClaimsAsync(user.Id);
			claims.AddRange(permissions.Select(p => new Claim("perms", p)));

			// ── 5. Sign and encode ────────────────────────────────
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _jwt.Issuer,
				audience: _jwt.Audience,
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(_jwt.ExpirationInMinutes),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}