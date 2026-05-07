using Microsoft.EntityFrameworkCore;
using Calcifer.Api.AuthHandler.Configuration;
using Calcifer.Api.DbContexts;
using Calcifer.Api.DependencyInversion;
using Calcifer.Api.Infrastructure;
using Calcifer.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ✅ FIX: Ensure environment-specific configuration is loaded
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var configuration = builder.Configuration;

// ✅ DEBUG: Log which environment and config file is being used (Development only)
if (builder.Environment.IsDevelopment())
{
    System.Console.WriteLine($"[Startup] Environment: {builder.Environment.EnvironmentName}");
    System.Console.WriteLine($"[Startup] ContentRootPath: {builder.Environment.ContentRootPath}");
}

// register services to the container.
DependencyInversion.RegisterServices(builder.Services, configuration);

// config Dependency Injection
builder.Services.AddDbContext<CalciferAppDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("CalciferDBContext")));




// Register controllers & minimal APIs
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


// Load JWT configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton<IConfiguration>(configuration);





// CORS handle 


builder.Services.AddCors(options =>
{
    options.AddPolicy("MyAllowSpecificOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();


// ✅ Use static files (for wwwroot, logs, etc.)
app.UseStaticFiles();

// ✅ Use routing
app.UseRouting();

// ✅ CORS before Auth
app.UseCors("MyAllowSpecificOrigins");

// ✅ Auth middleware
app.UseAuthentication();
app.UseAuthorization();

// ✅ Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DatabaseInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// ✅ Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dev v1");
    });
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // ✅ Error handling in production
    app.UseExceptionHandler("/error");
}

// ✅ Map all controllers and minimal APIs
app.MapControllers();
app.ApplicationMinimalApis();

// ✅ Default fallback route (for 404s)
app.MapFallback(context =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    return context.Response.WriteAsJsonAsync(new { error = "Resource not found", path = context.Request.Path });
});

app.Run();
