# QUICK START: Fix HTTP 500.30 Error - Action Plan

**Date**: May 5, 2026  
**Status**: 5 Critical Issues Identified & Fixed  
**Estimated Time**: 15-20 minutes

---

## ⚠️ ROOT CAUSE SUMMARY

Your application fails to start with HTTP 500.30 due to **5 critical configuration issues**:

1. **BLOCKING**: Missing JWT Secret in configuration (causes immediate crash)
2. **BLOCKING**: Missing appsettings.Production.json (wrong database on production)
3. **HIGH**: LogWriter creates logs in wrong directory on IIS (permission denied)
4. **MEDIUM**: Configuration not explicitly loading environment files
5. **MEDIUM**: Self-contained deployment needs .NET 8.0 on server

---

## ✅ CHANGES ALREADY APPLIED

The following files have been automatically created/updated:

| File | Status | Details |
|------|--------|---------|
| `appsettings.Production.json` | ✅ Created | Production configuration template |
| `web.config` | ✅ Created | IIS hosting configuration |
| `Program.cs` | ✅ Updated | Fixed environment config loading |
| `DependencyInversion.cs` | ✅ Updated | Fixed LogWriter IIS path handling |
| `LogWriter.cs` | ✅ Updated | Added better error handling |
| `DIAGNOSTIC_REPORT_500.30.md` | ✅ Created | Full technical diagnosis |

---

## 🔧 IMMEDIATE ACTIONS REQUIRED (Do These Now)

### ACTION 1: Set JWT Secret in Production Config
**File**: `appsettings.Production.json`

**What to do**:
1. Open `src/Calcifer.Api/v1/appsettings.Production.json`
2. Find this line:
   ```json
   "Secret": "REPLACE_WITH_YOUR_PRODUCTION_SECRET_KEY_MINIMUM_32_CHARACTERS_LONG_REQUIRED!!!",
   ```
3. Replace with a strong secret (minimum 32 characters):
   ```json
   "Secret": "MyP@ssw0rd123456789ABCDEFGHIJKLMN!",
   ```

**Why**: The DependencyInversion throws an exception if Secret is missing or < 32 chars. This is THE main cause of 500.30.

**Tip**: Generate a strong secret:
```powershell
# PowerShell command to generate strong secret:
[System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Guid]::NewGuid().ToString() + [System.Guid]::NewGuid().ToString()))
```

---

### ACTION 2: Verify Database Connection String
**File**: `appsettings.Production.json`

**What to do**:
1. In `appsettings.Production.json`, verify the connection string:
   ```json
   "CalciferDBContext": "Server=192.168.3.10;Database=EktaDatabase;User Id=sa;Password=n3wdb$3rv3r@hgr0up;..."
   ```

2. **Test on production server** (run in PowerShell/CMD):
   ```powershell
   # Test SQL connection
   sqlcmd -S 192.168.3.10 -U sa -P "n3wdb$3rv3r@hgr0up" -Q "SELECT name FROM sys.databases WHERE name='EktaDatabase'"
   ```

3. If you get an error:
   - ❌ Database doesn't exist → Create it using SQL Server Management Studio
   - ❌ Wrong password → Update the password in appsettings.Production.json
   - ❌ Can't connect to server → Check network, firewall, SQL Server is running

**Why**: If the database connection fails, IIS returns 500.30 immediately.

---

### ACTION 3: Create Logs Directory with Permissions
**Server**: Production Server  
**Location**: Same folder as published application

**What to do**:
1. Open File Explorer on production server
2. Navigate to: `D:\Alpha\Publish File\Calcifer.RBAC\` (or wherever your app is published)
3. **Create a new folder**: `logs` (right-click → New Folder)
4. **Set permissions** (right-click `logs` folder → Properties → Security):
   - Click: "Edit"
   - Click: "Add..."
   - Type: `IIS AppPool\DefaultAppPool` (or your app pool name)
   - Click: "Check Names" → OK
   - Select the new user
   - Check: ✅ "Modify" (which includes "Write")
   - Click: OK → OK

**Why**: LogWriter needs to create log files. If folder doesn't exist or lacks permissions, logging will fail and could cause 500.30.

---

### ACTION 4: Verify IIS App Pool Identity
**Server**: Production Server  
**Application**: IIS Manager

**What to do**:
1. On production server, open **IIS Manager**
2. Click: Sites → Your Application Name
3. Right-click: Application Pools → Select Your App Pool
4. Click: Advanced Settings
5. Find: **Identity** (in the Process Model section)
6. Click: "Identity" → "..." → **ApplicationPoolIdentity** → OK

**Why**: IIS needs proper identity to access files and create log directories.

---

### ACTION 5: Set Production Environment in IIS
**Server**: Production Server  
**Application**: IIS Manager

**What to do**:
1. On production server, open **IIS Manager**
2. Click: Sites → Your Application
3. Double-click: **Configuration Editor** (or right-click → Explore)
4. Search for or navigate to: **applicationSettings** section
5. Look for: `ASPNETCORE_ENVIRONMENT`
6. Set value to: `Production`

**Alternative** (via Application Pool):
1. Right-click App Pool → Advanced Settings
2. Find: **Environment Variables** (under Process Model)
3. Click: "..."
4. Add:
   - Name: `ASPNETCORE_ENVIRONMENT`
   - Value: `Production`
5. Click: OK

**Why**: Tells ASP.NET Core to load `appsettings.Production.json` instead of development settings.

---

### ACTION 6: Rebuild and Publish
**Local**: Your Development Machine

**What to do**:
```powershell
# In PowerShell, navigate to project:
cd "d:\Alpha\dotnet\dotnet.calcifer\src\Calcifer.Api\v1"

# Clean previous build
dotnet clean

# Restore packages
dotnet restore

# Build for Release
dotnet build --configuration Release

# Publish (this creates the files for deployment)
dotnet publish --configuration Release --self-contained --runtime win-x64 --output "D:\Alpha\Publish File\Calcifer.RBAC"
```

**Why**: Ensures all fixes are compiled and includes new config files.

---

### ACTION 7: Test Application Startup
**Server**: Production Server  
**Command**: PowerShell (Admin)

**What to do**:
```powershell
# Stop IIS
iisreset /stop

# Navigate to app directory
cd "D:\Alpha\Publish File\Calcifer.RBAC"

# Run app directly (to see errors)
$env:ASPNETCORE_ENVIRONMENT="Production"
.\dotnet.exe .\Calcifer.Api.dll

# Watch for errors in console - if app starts successfully:
# ✅ [Startup] Environment: Production
# ✅ [Startup] ContentRootPath: D:\Alpha\Publish File\Calcifer.RBAC
# ✅ Hosting environment: Production
# Then press Ctrl+C to stop
```

**If you see errors**:
- Copy the error message and search in [DIAGNOSTIC_REPORT_500.30.md](DIAGNOSTIC_REPORT_500.30.md)
- Or check stdout log: `D:\Alpha\Publish File\Calcifer.RBAC\logs\stdout_*`

---

## 🚀 TESTING CHECKLIST

Before considering this complete, verify:

- [ ] **Program runs without errors** (see Action 7)
- [ ] **appsettings.Production.json** has valid JWT Secret (32+ chars)
- [ ] **Database connection** works (tested with sqlcmd)
- [ ] **Logs directory** exists with proper permissions
- [ ] **IIS Environment** set to "Production"
- [ ] **No Console Errors** when app starts
- [ ] **Can call API**: `curl http://localhost/api/v1/common-status` (should return 401 Unauthorized, not 500.30)

---

## 🔍 DEBUGGING IF STILL FAILING

If 500.30 still occurs:

### Step 1: Check stdout Log
```powershell
# On production server:
Get-Content "D:\Alpha\Publish File\Calcifer.RBAC\logs\stdout_*" -Tail 50
# Shows last 50 lines of error details
```

### Step 2: Check Event Viewer
```powershell
# On production server, run in PowerShell (Admin):
Get-EventLog Application -Source "IIS AspNetCore Module" -Newest 10 | Format-List
```

### Step 3: Enable Detailed Errors
Edit `web.config`:
```xml
<aspNetCore processPath="..." stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

Then check `logs\stdout_TIMESTAMP.log` for detailed errors.

### Step 4: Manual Database Migration
```powershell
# If database doesn't have tables:
cd "d:\Alpha\dotnet\dotnet.calcifer\src\Calcifer.Api\v1"
dotnet ef database update --startup-project . --project .
```

---

## 📋 FILES REFERENCE

| File | Purpose | Status |
|------|---------|--------|
| [DIAGNOSTIC_REPORT_500.30.md](DIAGNOSTIC_REPORT_500.30.md) | Full technical diagnosis with solutions | Read if issues persist |
| `appsettings.Production.json` | Production configuration (JWT secret, DB connection) | ✅ Created - Update JWT Secret |
| `web.config` | IIS hosting settings | ✅ Created |
| `Program.cs` | Startup configuration | ✅ Updated |
| `DependencyInversion.cs` | Service registration | ✅ Updated |
| `LogWriter.cs` | Logging implementation | ✅ Updated |

---

## ⏱️ ESTIMATED TIME

| Action | Time |
|--------|------|
| 1-2: Update config files | 3 min |
| 3-4: IIS setup (permissions, identity) | 5 min |
| 5: Set Environment variable | 2 min |
| 6: Rebuild & publish | 3 min |
| 7: Test startup | 2 min |
| **Total** | **~15 min** |

---

## ❓ FAQ

**Q: Why does 500.30 occur specifically?**  
A: Worker process crashes during startup because JWT Secret is missing. IIS shows 500.30 instead of the real error.

**Q: Where are logs created?**  
A: In `{AppDirectory}/logs/YYYY-MM-DD_LogType.txt`  
Example: `D:\Alpha\Publish File\Calcifer.RBAC\logs\2026-05-05_Error.txt`

**Q: Can I use Development config on Production?**  
A: ❌ NO. Development database (`WeavoGoRBACdatabase`) is different from Production (`EktaDatabase`).

**Q: What if I forgot my JWT Secret?**  
A: Generate a new one using the PowerShell command in Action 1. Make sure it's 32+ characters.

**Q: Does the app need .NET 8.0 on the server?**  
A: No, because publish profile uses `SelfContained=true`. The runtime is bundled. But verify `dotnet --list-runtimes` shows 8.0.x.

---

## ✅ NEXT STEPS

1. **Complete Actions 1-7** in order
2. **Run test** from Action 7
3. **Verify checklist** items pass
4. If issues remain, **read** `DIAGNOSTIC_REPORT_500.30.md` for detailed solutions
5. **Contact** your database admin if connection string is wrong

---

**Need Help?** Check the detailed diagnostic report: [DIAGNOSTIC_REPORT_500.30.md](DIAGNOSTIC_REPORT_500.30.md)
