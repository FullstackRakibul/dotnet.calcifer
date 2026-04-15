// ============================================================
//  JwtSettings.cs
//  Strongly-typed binding for the "JwtSettings" section
//  in appsettings.json.
//
//  appsettings.json shape:
//  {
//    "JwtSettings": {
//      "Secret": "YOUR_SUPER_SECRET_KEY_REPLACE_ME_IN_PRODUCTION",
//      "Issuer": "Calcifer.Api",
//      "Audience": "Calcifer.Client",
//      "ExpirationInMinutes": 60
//    }
//  }
//
//  IMPORTANT: Replace the Secret with a random 256-bit key
//  before deploying to production. Minimum 32 characters.
//  Use: dotnet user-secrets set "JwtSettings:Secret" "<key>"
// ============================================================

namespace Calcifer.Api.AuthHandler.Configuration
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationInMinutes { get; set; } = 60;
    }
}