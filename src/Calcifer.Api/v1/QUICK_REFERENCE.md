# HTTP 500.30 Error - Quick Reference Card

## 🔴 5 CRITICAL ISSUES FIXED

```
❌ ISSUE 1: JWT Secret Missing
   File: appsettings.Development.json
   Fix: Created appsettings.Production.json with JWT Secret placeholder
   
❌ ISSUE 2: No Production Config
   File: Missing appsettings.Production.json
   Fix: Created appsettings.Production.json
   
❌ ISSUE 3: LogWriter Wrong Path
   File: Helper/LogWriter/LogWriter.cs
   Fix: Updated DependencyInversion.cs to use IWebHostEnvironment.ContentRootPath
   
❌ ISSUE 4: Config Not Loading Env Files
   File: Program.cs
   Fix: Added explicit environment-specific config loading
   
❌ ISSUE 5: Self-Contained Runtime Issues
   File: PublishProfile
   Check: Verify .NET 8.0 runtime on server
```

---

## 🚀 3-STEP QUICK FIX

### Step 1: Update JWT Secret (1 minute)
```
File: src/Calcifer.Api/v1/appsettings.Production.json
Line: ~18

Find:   "Secret": "REPLACE_WITH_YOUR_PRODUCTION_SECRET_KEY..."
Change: "Secret": "MyP@ssw0rd123456789ABCDEFGHIJKLMN!"
                  (minimum 32 characters)
```

### Step 2: Create Logs Directory (2 minutes)
```
On Production Server:
1. Go to: D:\Alpha\Publish File\Calcifer.RBAC\
2. Create folder: logs
3. Properties → Security → Add IIS AppPool\DefaultAppPool → Modify permissions
```

### Step 3: Rebuild and Publish (5 minutes)
```powershell
cd "d:\Alpha\dotnet\dotnet.calcifer\src\Calcifer.Api\v1"
dotnet publish --configuration Release --self-contained --runtime win-x64 `
  --output "D:\Alpha\Publish File\Calcifer.RBAC"
```

---

## 🔧 FILES CREATED/UPDATED

| File | Action | Purpose |
|------|--------|---------|
| `appsettings.Production.json` | ✅ Created | Production config (UPDATE JWT SECRET!) |
| `web.config` | ✅ Created | IIS hosting configuration |
| `Program.cs` | ✅ Updated | Fixed environment config loading |
| `DependencyInversion.cs` | ✅ Updated | Fixed LogWriter IIS path |
| `LogWriter.cs` | ✅ Updated | Better error handling |
| `DIAGNOSTIC_REPORT_500.30.md` | ✅ Created | Full technical diagnosis (read if needed) |
| `ACTION_PLAN_FIX_500.30.md` | ✅ Created | Step-by-step action plan |

---

## ⚠️ CRITICAL CONFIGURATION

### appsettings.Production.json (MUST UPDATE)
```json
{
  "JwtSettings": {
    "Secret": "⚠️ CHANGE THIS TO YOUR 32+ CHARACTER SECRET ⚠️",
    "Issuer": "Calcifer.Api",
    "Audience": "Calcifer.Client",
    "ExpirationInMinutes": 60
  },
  "ConnectionStrings": {
    "CalciferDBContext": "Server=192.168.3.10;Database=EktaDatabase;User Id=sa;Password=n3wdb$3rv3r@hgr0up;..."
  }
}
```

**Status**: 
- ❌ JWT Secret: NOT SET (placeholder text)
- ✅ Connection String: Set to EktaDatabase
- ✅ CORS: Set to production domain

**ACTION**: Replace `Secret` value before publishing!

---

## 📊 WHY 500.30 OCCURRED

```
Timeline of Failure:
1. IIS starts application
2. Program.cs loads configuration
3. DependencyInversion.RegisterServices() is called
4. JwtSettings validation check runs
5. Secret is missing from appsettings.Development.json
6. throw new InvalidOperationException("JwtSettings section is missing...")
7. Application startup fails
8. Worker process crashes
9. IIS shows: HTTP 500.30 - ASP.NET Core app failed to start
```

---

## ✅ VERIFICATION CHECKLIST

Before considering complete:

```
Environment Setup:
  ☐ appsettings.Production.json exists
  ☐ JWT Secret is 32+ characters
  ☐ Database connection string is valid
  ☐ SQL Server is accessible from production server

IIS Configuration:
  ☐ Application pool identity set to ApplicationPoolIdentity
  ☐ ASPNETCORE_ENVIRONMENT = Production
  ☐ Logs directory exists with write permissions
  ☐ web.config is in published directory

Application:
  ☐ Rebuilt with Release configuration
  ☐ Published to D:\Alpha\Publish File\Calcifer.RBAC\
  ☐ All config files included in publish output
  ☐ No errors in stdout logs

Testing:
  ☐ Manual startup test passes (no console errors)
  ☐ IIS can access the app
  ☐ API responds with 401 (not 500.30) when accessing /api/v1/common-status
  ☐ Logs are being created in logs/ directory
```

---

## 🔍 IF STILL FAILING

1. Check error log: `D:\Alpha\Publish File\Calcifer.RBAC\logs\stdout_*`
2. Read: `DIAGNOSTIC_REPORT_500.30.md` (full troubleshooting guide)
3. Verify: `sqlcmd -S 192.168.3.10 -U sa -Q "SELECT name FROM sys.databases"`
4. Check: `.NET 8.0` installed → `dotnet --list-runtimes`

---

## 💾 KEY FILES TO REMEMBER

**Must Update:**
- `appsettings.Production.json` ← **SET JWT SECRET HERE**

**Should Verify:**
- `web.config` ← IIS configuration
- `Program.cs` ← Startup configuration
- `DependencyInversion.cs` ← Service registration

**For Reference:**
- `ACTION_PLAN_FIX_500.30.md` ← Step-by-step guide
- `DIAGNOSTIC_REPORT_500.30.md` ← Full technical details

---

## 📞 SUPPORT

If you're stuck:
1. Read: `ACTION_PLAN_FIX_500.30.md` (7 detailed steps)
2. Check: `DIAGNOSTIC_REPORT_500.30.md` (solutions for each issue)
3. Test: Run app manually to see real error messages
4. Debug: Check `logs/stdout_*.log` for detailed errors
