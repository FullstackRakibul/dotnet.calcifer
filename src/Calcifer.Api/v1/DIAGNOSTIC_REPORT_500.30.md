# HTTP Error 500.30 - ASP.NET Core App Failed to Start
## Complete Diagnostic Report for Calcifer.Api

**Generated**: May 5, 2026  
**Environment**: Production Deployment  
**Error**: Worker process fails. The app doesn't start.

---

## 🔴 CRITICAL ISSUES FOUND

### Issue #1: MISSING JWT SECRET IN CONFIGURATION (BLOCKING)
**Severity**: 🔴 **CRITICAL - APP WILL NOT START**  
**Location**: `appsettings.Development.json`

**Problem**:
- Your `appsettings.Development.json` file is **MISSING** the `JwtSettings:Secret` property
- The code in `DependencyContainer/DependencyInversion.cs` (line 81-87) **throws an exception** at startup:

```csharp
var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JwtSettings section is missing from configuration. " +
        "Run: dotnet user-secrets set \"JwtSettings:Secret\" \"<your-32-char-secret>\"");

if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
    throw new InvalidOperationException(
        $"JWT Secret must be at least 32 characters (current: {jwtSettings.Secret?.Length ?? 0})...");
```

**Current State**:
```json
// ❌ WRONG - appsettings.Development.json
{
  "JwtSettings": {
    "_NOTE": "Secret MUST be set via user-secrets...",
    "Issuer": "Calcifer.Api",
    "Audience": "Calcifer.Client",
    "ExpirationInMinutes": 60
    // ❌ NO Secret property!
  }
}
```

**Why This Causes 500.30**:
- Application startup fails during DependencyInversion.RegisterServices()
- Worker process crashes before HTTP pipeline is established
- IIS shows "worker process fails" error

**✅ SOLUTION**:
Option A - Use appsettings.Production.json (Recommended for production):
```json
{
  "ConnectionStrings": {
    "CalciferDBContext": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "your-super-strong-secret-key-minimum-32-characters-long!",
    "Issuer": "Calcifer.Api",
    "Audience": "Calcifer.Client",
    "ExpirationInMinutes": 60
  },
  "CorsSettings": {
    "AllowedOrigins": ["https://yourdomain.com"]
  }
}
```

Option B - Use Environment Variables (Best practice for IIS):
- Set in IIS Application Pool > Advanced Settings > Environment Variables:
  - `JwtSettings__Secret=your-super-strong-secret-key-minimum-32-characters-long!`
  - `ConnectionStrings__CalciferDBContext=Your_Connection_String`

Option C - Use Key Vault (Enterprise):
- Configure Azure Key Vault integration in Program.cs

---

### Issue #2: NO PRODUCTION CONFIGURATION FILE
**Severity**: 🟠 **HIGH**  
**Location**: Missing `appsettings.Production.json`

**Problem**:
- No production-specific configuration file exists
- Production deployment will fall back to `appsettings.Development.json` (WRONG DATABASE!)
- Development database name: `WeavoGoRBACdatabase`
- Publish profile database name: `EktaDatabase` (DIFFERENT!)

**Current File Structure**:
```
✅ appsettings.Development.json   (exists - for dev only)
✅ appsettings.Example.json        (exists - template)
❌ appsettings.Production.json     (MISSING - needed for prod)
❌ appsettings.Staging.json        (MISSING - optional)
```

**Why This Matters**:
- When deployed to IIS as `Production` environment, it looks for `appsettings.Production.json`
- If missing, falls back to base `appsettings.json` or development settings
- Wrong database name → 500.30 connection failure

**✅ SOLUTION - Create appsettings.Production.json**:
See below in "CONFIGURATION FILES TO CREATE" section.

---

### Issue #3: LOG WRITER PATH CREATION PROBLEMS
**Severity**: 🟠 **HIGH**  
**Location**: `Helper/LogWriter/LogWriter.cs` (Line 52)

**Problem**:
```csharp
public DynamicLogWriter(string? baseDirectory = null)
{
    _baseDirectory = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;  // ⚠️ WRONG ON IIS!
    _logDirectory = Path.Combine(_baseDirectory, "logs");
    EnsureLogDirectoryExists();  // Creates folder only if Directory.CreateDirectory works
}
```

**On IIS Production Server**:
- `AppDomain.CurrentDomain.BaseDirectory` returns: `C:\Windows\System32\inetsrv\` (INACCESSIBLE!)
- Or: Application pool identity may not have permissions to create folders
- Result: Logs folder never created → Logging fails silently
- This could trigger 500 errors if logging is called during startup errors

**Current Implementation**:
```csharp
private void EnsureLogDirectoryExists()
{
    if (!Directory.Exists(_logDirectory))
    {
        Directory.CreateDirectory(_logDirectory);  // ⚠️ May fail silently on IIS
    }
}
```

**Where Logs Actually Go** (Expected vs Reality):
| Environment | Expected Path | Actual Path | Status |
|---|---|---|---|
| Development | `{AppDirectory}/logs/` | `{AppDirectory}/logs/` | ✅ Works |
| IIS Hosted | `C:\inetpub\wwwroot\logs\` | `C:\Windows\System32\inetsrv\logs\` | ❌ Wrong! |

**✅ SOLUTIONS**:

**Solution A** - Use IIS Application Path (Recommended):
```csharp
// In DependencyInversion.cs, pass IIS-aware path:
services.AddSingleton<ILogWriter>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logDir = Path.Combine(env.ContentRootPath, "logs");  // ✅ Uses IIS app root
    return new DynamicLogWriter(env.ContentRootPath);
});
```

**Solution B** - Use Environment Variable:
```csharp
var logPath = Environment.GetEnvironmentVariable("LOG_PATH") 
    ?? Path.Combine(_baseDirectory, "logs");
```

**Solution C** - Use Azure Blob Storage (Cloud):
```csharp
// Replace file logging with Azure App Insights or Blob Storage
services.AddApplicationInsightsTelemetry();
```

---

### Issue #4: SELF-CONTAINED DEPLOYMENT ISSUES
**Severity**: 🟠 **HIGH**  
**Location**: `Properties/PublishProfiles/FolderProfile.pubxml`

**Problem**:
```xml
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishSingleFile>false</PublishSingleFile>
```

**What This Means**:
- Application is published as self-contained (bundles .NET 8.0 runtime)
- Published to: `D:\Alpha\Publish File\Calcifer.RBAC\`
- Requires: No .NET 8.0 runtime installed on server (included in publish)

**Potential Issues**:
- If server has **different .NET 8.0 version** than dev machine → compatibility issues
- If server has **.NET 7.x** or **.NET 6.x** → will fail
- Application Pool might be set to `No Managed Code` → crashes

**✅ CHECK**:
```powershell
# On production server, run:
dotnet --list-runtimes
# Should show: Microsoft.AspNetCore.App 8.0.x

# Or check:
dir "D:\Alpha\Publish File\Calcifer.RBAC\dotnet.exe"
# Should exist for self-contained deployment
```

---

### Issue #5: CONNECTION STRING MISMATCH
**Severity**: 🟠 **MEDIUM**  
**Location**: Configuration files vs Publish Profile

**Problem**:
```json
// appsettings.Development.json uses:
"CalciferDBContext": "Server=192.168.3.10;Database=WeavoGoRBACdatabase;..."

// But PublishProfile specifies (FolderProfile.pubxml.user):
"CalciferDBContext": "Server=192.168.3.10;Database=EktaDatabase;..."
```

**Why This Matters**:
- Two different databases!
- If deploy uses settings from appsettings.Development.json → connects to `WeavoGoRBACdatabase`
- If deploy uses profile settings → connects to `EktaDatabase`
- If one database doesn't exist or user lacks permissions → connection fails → 500.30

**Current Issue**:
```
Development: WeavoGoRBACdatabase ✅ (probably works)
Production:  EktaDatabase ❌ (might not exist or user lacks access)
```

**✅ SOLUTION - Standardize Connection Strings**:
- Use environment variables in IIS
- Or create proper appsettings.Production.json with correct database
- Verify database exists on server: `sqlcmd -S 192.168.3.10 -U sa -Q "SELECT name FROM sys.databases"`

---

## 🟡 RECOMMENDATIONS FOR QUICK FIX

### IMMEDIATE ACTIONS (Do These First):

#### Step 1: Create appsettings.Production.json
Create file: `src/Calcifer.Api/v1/appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "CalciferDBContext": "Server=192.168.3.10;Database=EktaDatabase;User Id=sa;Password=n3wdb$3rv3r@hgr0up;Trusted_Connection=false;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "JwtSettings": {
    "Secret": "YOUR_PRODUCTION_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS_LONG!!",
    "Issuer": "Calcifer.Api",
    "Audience": "Calcifer.Client",
    "ExpirationInMinutes": 60
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "https://weavo-go.vercel.app",
      "https://your-production-domain.com"
    ]
  }
}
```

#### Step 2: Fix Program.cs to Add Production Configuration
Edit: `Program.cs` (around line 18-20, before DependencyInversion call):

```csharp
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ✅ ADD THIS - Ensure environment-specific config is loaded
if (!app.Environment.IsProduction())
{
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables();
}

// Register services
DependencyInversion.RegisterServices(builder.Services, configuration);
```

#### Step 3: Fix LogWriter to Use Correct Path on IIS
Edit: `Helper/LogWriter/LogWriter.cs` (Line 46-60):

```csharp
public DynamicLogWriter(string? baseDirectory = null)
{
    // ✅ FIX: Use provided base directory (from IIS hosting)
    _baseDirectory = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
    
    // ✅ BETTER: Accept explicit log directory path
    _logDirectory = Path.Combine(_baseDirectory, "logs");
    
    EnsureLogDirectoryExists();
    _currentCorrelationId = Guid.NewGuid().ToString();
}

private void EnsureLogDirectoryExists()
{
    try
    {
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }
    catch (UnauthorizedAccessException ex)
    {
        // ✅ FIX: Log to console if file write fails (for debugging)
        Console.WriteLine($"[LogWriter] Failed to create log directory: {ex.Message}");
        Console.WriteLine($"[LogWriter] Logs will not be written to file on this environment.");
    }
}
```

#### Step 4: Update DependencyInversion.cs to Handle IIS Paths
Edit: `DependencyContainer/DependencyInversion.cs` (around line 60, where LogWriter is registered):

```csharp
// ── Dynamic Log Writer (singleton — shared across all requests) ──
// ✅ FIX: Pass IIS-aware base directory
services.AddSingleton<ILogWriter>(sp =>
{
    var hostingEnv = sp.GetRequiredService<IWebHostEnvironment>();
    var logDir = hostingEnv.ContentRootPath;  // ✅ Uses actual app directory
    return new DynamicLogWriter(logDir);
});
```

#### Step 5: Verify IIS Application Pool Identity
On production server:
1. Open **IIS Manager**
2. Navigate to: Sites → Your App → Application Pools → YourAppPool
3. Advanced Settings → Identity → Set to: **ApplicationPoolIdentity**
4. Make sure folder `D:\inetpub\wwwroot\logs\` (or wherever your app is) has permissions:
   - Right-click folder → Properties → Security
   - Add: **IIS AppPool\YourAppPoolName** with Full Control
   - Or add: **NETWORK SERVICE** with Modify permissions

---

## 🔧 PUBLISHING CHECKLIST

Before publishing to IIS, verify:

- [ ] **JWT Secret**: Set in `appsettings.Production.json` (minimum 32 chars)
- [ ] **Database Connection**: Test connection to `EktaDatabase` on `192.168.3.10`
- [ ] **SQL User Permissions**: User `sa` has permissions on target database
- [ ] **IIS App Pool Identity**: Has permissions to create `/logs` folder
- [ ] **ASP.NET Core Hosting Bundle**: Installed on server (or self-contained works)
- [ ] **Environment Variable**: IIS set to `ASPNETCORE_ENVIRONMENT=Production`
- [ ] **appsettings.Production.json**: Included in publish output
- [ ] **HTTPS**: IIS binding configured (if required)

---

## 📋 FILES TO CREATE/UPDATE

### 1. Create: `appsettings.Production.json`
```
Location: src/Calcifer.Api/v1/appsettings.Production.json
Purpose: Production-specific configuration
Content: See "IMMEDIATE ACTIONS - Step 1" above
```

### 2. Create: `appsettings.Staging.json` (Optional)
```json
{
  "ConnectionStrings": {
    "CalciferDBContext": "Server=YOUR_STAGING_SERVER;Database=YOUR_STAGING_DB;..."
  },
  "JwtSettings": {
    "Secret": "YOUR_STAGING_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS_LONG!!",
    "Issuer": "Calcifer.Api",
    "Audience": "Calcifer.Client",
    "ExpirationInMinutes": 60
  },
  "CorsSettings": {
    "AllowedOrigins": ["https://staging-domain.com"]
  }
}
```

### 3. Create: `web.config` (For IIS + In-Process Hosting)
```xml
Location: src/Calcifer.Api/v1/web.config
Purpose: IIS configuration for ASP.NET Core Module
Content:
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\Calcifer.Api.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="outofprocess" />
    </system.webServer>
  </location>
</configuration>
```

---

## 🔍 DEBUGGING ON PRODUCTION

If 500.30 still occurs after fixes:

### 1. Enable ASP.NET Core Module Logs:
```cmd
# On server, in Command Prompt (Admin):
cd D:\Alpha\Publish File\Calcifer.RBAC
dotnet Calcifer.Api.dll --environment Production
# Watch for console errors
```

### 2. Check Event Viewer:
```
Windows Event Viewer → Windows Logs → Application
Look for: "IIS AspNetCore Module" events
```

### 3. Enable stdout Logging:
In `web.config`, set:
```xml
<aspNetCore processPath="..." stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```
Then check `logs\stdout` file for detailed errors.

### 4. Manual Test Connection String:
```powershell
# Test SQL connection on server:
sqlcmd -S 192.168.3.10 -U sa -P "n3wdb$3rv3r@hgr0up" -Q "SELECT @@VERSION"
```

---

## 📊 SUMMARY

| Issue | Cause | Impact | Fix |
|-------|-------|--------|-----|
| No JWT Secret | appsettings.Development.json incomplete | 500.30 - App crashes | Add Secret to appsettings.Production.json |
| No Production Config | Missing appsettings.Production.json | Wrong database | Create appsettings.Production.json |
| LogWriter Path | Uses wrong directory on IIS | Logs fail silently | Update to use env.ContentRootPath |
| Connection String | Database name mismatch | 500.30 - Connection fails | Standardize DB name in production config |
| Self-Contained Issues | Runtime compatibility | 500.30 - Runtime error | Verify .NET 8.0 on server |

---

**Next Steps**: Follow the "IMMEDIATE ACTIONS" section above in order. After applying fixes, republish and test.
