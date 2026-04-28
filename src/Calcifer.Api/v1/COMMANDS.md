# Calcifer.Api — Commands & CLI Reference

**Last Updated**: April 27, 2026  
**OS**: Windows (PowerShell), macOS/Linux (Bash)  
**Framework**: .NET 8.0 / EF Core 8.0.1

---

## Table of Contents

1. [Setup & Configuration](#setup--configuration)
2. [Database Management](#database-management)
3. [Build & Run](#build--run)
4. [Testing](#testing)
5. [Debugging](#debugging)
6. [Deployment](#deployment)
7. [Troubleshooting](#troubleshooting)

---

## Setup & Configuration

### Initial Project Setup

```bash
# Clone repository
git clone <repository-url>
cd src/Calcifer.Api/v1

# Restore NuGet packages
dotnet restore

# Verify installation
dotnet --version
```

### JWT Secret Configuration (CRITICAL)

**⚠️ DO NOT commit secrets to source control**

#### Development (Local Machine)

```bash
# Set JWT secret in user-secrets (Windows & macOS/Linux)
dotnet user-secrets set "JwtSettings:Secret" "your-super-secret-key-minimum-32-characters"

# Verify secret is set
dotnet user-secrets list

# Clear secrets (if needed)
dotnet user-secrets clear
```

**Example secret** (generate strong key):
```bash
# PowerShell - Generate random 32-char key
-join((1..32 | ForEach-Object { [char][byte]::MinValue..(0xFF) | Get-Random }) | Select-Object -First 32)

# Bash - Generate random key
openssl rand -base64 32
```

#### Database Connection String

```bash
# Set database connection for LocalDB
dotnet user-secrets set "ConnectionStrings:CalciferDBContext" "Server=(localdb)\mssqllocaldb;Database=EktaDatabase;Trusted_Connection=True;TrustServerCertificate=True"

# Or for remote SQL Server
dotnet user-secrets set "ConnectionStrings:CalciferDBContext" "Server=your-server;Database=EktaDatabase;User Id=sa;Password=YourPassword;Encrypt=True;TrustServerCertificate=True"
```

#### Production (Azure Key Vault)

```bash
# Set up Key Vault connection (requires Azure CLI)
az login
az keyvault secret set --vault-name <VaultName> --name "JwtSettings--Secret" --value "your-production-secret"

# Verify in application
# (Code uses DefaultAzureCredential to fetch from Key Vault)
```

---

## Database Management

### Migrations

#### Create New Migration

```bash
# Create migration with name
dotnet ef migrations add AddNewFeature

# Example names
dotnet ef migrations add InitialCreate
dotnet ef migrations add AddLicenseFeatures
dotnet ef migrations add UpdateRbacSchema
```

#### Apply Migrations (Update Database)

```bash
# Apply pending migrations to database
dotnet ef database update

# Apply specific migration
dotnet ef database update AddNewFeature

# Revert to previous migration
dotnet ef database update <PreviousMigrationName>

# Revert to initial state (remove all migrations)
dotnet ef database update 0
```

#### Remove Last Migration

```bash
# Remove last migration (if not yet applied)
dotnet ef migrations remove

# Remove specific migration (dangerous — do not use if applied to production)
dotnet ef migrations remove --force
```

#### View All Migrations

```bash
# List applied migrations
dotnet ef migrations list

# View specific migration script
dotnet ef migrations script --from InitialCreate --to AddNewFeature
```

#### Generate SQL Script (for CI/CD)

```bash
# Generate SQL script for all pending migrations
dotnet ef migrations script --output migrations.sql

# Generate script from one migration to another
dotnet ef migrations script InitialCreate AddNewFeature --output upgrade.sql

# Generate script for rollback
dotnet ef migrations script --from AddNewFeature --to InitialCreate --output rollback.sql
```

### Database Initialization

#### On First Run

```bash
# Apply migrations and seed data (runs DatabaseInitializer.cs)
dotnet ef database update

# Result: EktaDatabase created with:
# - CommonStatus rows (User, License, RBAC, General modules)
# - OrganizationUnit tree (Company, Factory, Departments)
# - RBAC permissions and role-permission matrix
# - SuperAdmin user (configured in appsettings)
```

#### Seed Data Configuration

Edit `appsettings.Development.json`:
```json
{
  "SeedAdmin": {
    "Email": "admin@calcifer.local",
    "Password": "Admin@12345",
    "EmpId": "EMP-0001"
  }
}
```

#### Drop and Recreate Database

```bash
# ⚠️ WARNING: Deletes all data
dotnet ef database drop

# Confirm deletion (prompts)
dotnet ef database drop --force

# Recreate from migrations
dotnet ef database update
```

---

## Build & Run

### Development Build & Run

#### With Hot-Reload (Recommended)

```bash
# Run with dotnet watch (auto-recompile on file changes)
dotnet watch

# Runs at: https://localhost:7000 (HTTPS) or http://localhost:5000 (HTTP)
# Swagger UI: https://localhost:7000/swagger/index.html
```

#### Without Watch

```bash
# Build project
dotnet build

# Run compiled application
dotnet run

# Run in specific configuration
dotnet run --configuration Release
```

#### Debug Mode (with VS Code/Visual Studio)

```bash
# Open in VS Code
code .

# Or Visual Studio
devenv .

# Set breakpoints and press F5 to start debugging
```

### Production Build & Run

```bash
# Build release configuration
dotnet build -c Release

# Run release build
dotnet run -c Release

# Publish as self-contained executable
dotnet publish -c Release -o ./publish

# Run published application
./publish/Calcifer.Api.exe         # Windows
./publish/Calcifer.Api             # macOS/Linux
```

### Docker Build (Optional)

```bash
# Create Dockerfile
cat > Dockerfile << 'EOF'
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app
EXPOSE 80 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Calcifer.Api.csproj", ""]
RUN dotnet restore "Calcifer.Api.csproj"
COPY . .
RUN dotnet build "Calcifer.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Calcifer.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Calcifer.Api.dll"]
EOF

# Build Docker image
docker build -t calcifer-api:latest .

# Run Docker container
docker run -p 5000:80 -e "JwtSettings:Secret=your-secret" calcifer-api:latest
```

---

## Testing

### Integration Tests (Postman / cURL)

#### 1. Register New User

```bash
curl -X POST https://localhost:7000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@example.com",
    "password": "Password@123",
    "name": "Test User"
  }'

# Expected response (201 Created):
{
  "status": true,
  "message": "User registered successfully",
  "data": {
    "userId": "abc-123",
    "email": "testuser@example.com",
    "name": "Test User"
  }
}
```

#### 2. Login (Get JWT Token)

```bash
curl -X POST https://localhost:7000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@calcifer.local",
    "password": "Admin@12345"
  }'

# Expected response (200 OK):
{
  "status": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600
  }
}

# Save token for next requests:
export TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

#### 3. Get User Profile (with JWT)

```bash
curl -X GET https://localhost:7000/api/v1/auth/me \
  -H "Authorization: Bearer $TOKEN"

# Expected response (200 OK):
{
  "status": true,
  "data": {
    "userId": "admin-id",
    "email": "admin@calcifer.local",
    "name": "System Administrator",
    "employeeId": "EMP-0001",
    "roles": ["SUPERADMIN"]
  }
}
```

#### 4. Test RBAC Permission (Requires Permission)

```bash
# Endpoint with [RequirePermission("HCM", "Employee", "Read")]
curl -X GET https://localhost:7000/api/v1/employees \
  -H "Authorization: Bearer $TOKEN"

# Responses:
# 200 OK — User has permission
# 403 Forbidden — User lacks permission
# 401 Unauthorized — No token provided
```

#### 5. Test License Feature-Gating

```bash
# Endpoint with [RequireFeature("HCM")]
curl -X GET https://localhost:7000/api/v1/hcm/dashboard \
  -H "Authorization: Bearer $TOKEN"

# Response:
# 200 OK — HCM feature enabled on license
# 403 Forbidden — HCM feature not licensed
```

#### 6. Test Rate Limiting

```bash
# Send 11 login requests to trigger rate limit
for i in {1..11}; do
  curl -X POST https://localhost:7000/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@test.com","password":"wrong"}'
  echo "Request $i"
done

# Responses 1-10: 400 Bad Request (wrong credentials)
# Response 11: 429 Too Many Requests (rate limit exceeded)
```

### Unit Tests (xUnit / NUnit)

```bash
# Create test project (if not already created)
dotnet new xunit -n Calcifer.Api.Tests
cd Calcifer.Api.Tests
dotnet add reference ../Calcifer.Api.csproj

# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "Namespace=Calcifer.Api.Tests.AuthServiceTests"

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Load Testing (k6)

```bash
# Install k6
# macOS: brew install k6
# Windows: choco install k6
# Linux: sudo apt install k6

# Create load test script
cat > load-test.js << 'EOF'
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 10 },  // Ramp up
    { duration: '1m', target: 50 },   // Stay at 50
    { duration: '30s', target: 0 },   // Ramp down
  ],
};

export default function () {
  const res = http.post('https://localhost:7000/api/v1/auth/login', {
    email: 'test@test.com',
    password: 'Password@123',
  }, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(res, {
    'status is 200 or 400': (r) => r.status === 200 || r.status === 400,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
}
EOF

# Run load test
k6 run load-test.js
```

---

## Debugging

### Debug with VS Code

1. Install C# extension
2. Create `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/net8.0/Calcifer.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "serverReadyAction": {
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "{url}",
        "action": "openExternally"
      }
    }
  ]
}
```

3. Set breakpoints and press **F5**

### Debug with Visual Studio

1. Open solution in Visual Studio
2. Set breakpoints (click on line number)
3. Press **F5** or **Debug → Start Debugging**
4. Inspect variables, watch expressions

### Common Debug Scenarios

#### JWT Token Validation Issues

```bash
# Decode JWT token online
# https://jwt.io

# Or use PowerShell
$token = "your-jwt-token"
$parts = $token.Split(".")
$payload = $parts[1]

# Decode base64
[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload + "===="))
```

#### RBAC Permission Issues

```bash
# Check user's permissions
curl -X GET "https://localhost:7000/api/v1/rbac/user/{userId}/permissions" \
  -H "Authorization: Bearer $TOKEN"

# Expected response:
{
  "status": true,
  "data": ["HCM:Employee:Read", "HCM:Employee:Update", ...]
}
```

#### License Feature Not Working

```bash
# Check active license features
curl -X GET "https://localhost:7000/api/v1/licenses/active/features" \
  -H "Authorization: Bearer $TOKEN"

# Expected response:
{
  "status": true,
  "data": {
    "license_key": "...",
    "features": ["HCM", "Finance", ...]
  }
}
```

---

## Deployment

### Pre-Deployment Checklist

- [ ] All secrets in Key Vault (not in source code)
- [ ] Database migrations tested locally
- [ ] CORS configured for production origins
- [ ] Rate limiting configured
- [ ] Logging centralized (Serilog, Application Insights)
- [ ] Security headers configured
- [ ] SSL/TLS certificate installed
- [ ] Database backups enabled
- [ ] Monitoring/alerting set up

### Azure App Service Deployment

```bash
# Create resource group
az group create --name rg-calcifer --location eastus

# Create App Service plan
az appservice plan create \
  --name asp-calcifer \
  --resource-group rg-calcifer \
  --sku B1

# Create Web App
az webapp create \
  --resource-group rg-calcifer \
  --plan asp-calcifer \
  --name calcifer-api

# Deploy from local folder
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deploy.zip .
cd ..

# Upload deployment package
az webapp deployment source config-zip \
  --resource-group rg-calcifer \
  --name calcifer-api \
  --src deploy.zip

# Configure environment variables
az webapp config appsettings set \
  --name calcifer-api \
  --resource-group rg-calcifer \
  --settings JwtSettings__Secret="${{ secrets.JWT_SECRET }}" \
              ConnectionStrings__CalciferDBContext="${{ secrets.DB_CONNECTION }}"
```

### GitHub Actions CI/CD

```yaml
name: Build and Deploy

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: dotnet build -c Release
    
    - name: Test
      run: dotnet test
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: calcifer-api
        publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
        package: ./publish
```

---

## Troubleshooting

### Port Already in Use

```bash
# Find process using port 5000 (Windows)
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# macOS/Linux
lsof -i :5000
kill -9 <PID>

# Or use different port
dotnet run --urls "http://localhost:5001"
```

### Database Connection Errors

```bash
# Test SQL Server connection
dotnet user-secrets list  # Verify connection string is set

# Try connecting with SSMS
# Server: (localdb)\mssqllocaldb
# Database: EktaDatabase
# Auth: Windows auth

# If LocalDB not installed
# Download: https://www.microsoft.com/en-us/sql-server/sql-server-express
```

### JWT Secret Not Found

```bash
# Verify secret is set
dotnet user-secrets list

# If empty, set secret
dotnet user-secrets set "JwtSettings:Secret" "your-secret"

# Check %APPDATA%\Microsoft\UserSecrets folder (Windows)
dir "%APPDATA%\Microsoft\UserSecrets"
```

### Rate Limiting Not Working

```bash
# Verify AspNetCoreRateLimit installed
dotnet list package | grep -i ratelimit

# Verify middleware registered
# Check: Middleware/MiddlewareDependencyInversion.cs

# Verify appsettings.json has IpRateLimiting section
cat appsettings.Development.json | grep -A 20 "IpRateLimiting"
```

### Swagger UI Not Loading

```bash
# Verify Swashbuckle installed
dotnet list package | grep -i swashbuckle

# Navigate to Swagger UI
https://localhost:7000/swagger/index.html

# Check Program.cs has:
# builder.Services.AddSwaggerGen();
# app.UseSwagger();
# app.UseSwaggerUI();
```

### HTTPS Certificate Issues

```bash
# Generate development certificate
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# On macOS/Linux
dotnet dev-certs https --trust

# Verify certificate
dotnet dev-certs https --check
```

---

## Quick Commands Reference

| Task | Command |
|------|---------|
| Create migration | `dotnet ef migrations add <Name>` |
| Apply migrations | `dotnet ef database update` |
| Run app | `dotnet watch` |
| Build | `dotnet build -c Release` |
| Run tests | `dotnet test` |
| Set secret | `dotnet user-secrets set "<Key>" "<Value>"` |
| List secrets | `dotnet user-secrets list` |
| Publish | `dotnet publish -c Release -o ./publish` |
| Check certificate | `dotnet dev-certs https --check` |

---

**Last Updated**: April 27, 2026  
**Maintained by**: Rakibul H. Rabbi
