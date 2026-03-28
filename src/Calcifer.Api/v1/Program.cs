using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Calcifer.Api.AuthHandler.Configuration;
using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DependencyInversion;
using Calcifer.Api.Infrastructure;
using Calcifer.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

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
    options.AddPolicy("MyAllowSpecificOrigins",
        builder =>
        {
            builder.WithOrigins(
                    "http://localhost:5173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

var app = builder.Build();



app.UseCors("MyAllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();
// Seed database
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		//var context = services.GetRequiredService<CalciferAppDbContext>();
		await DatabaseInitializer.SeedAsync(services);
	}
	catch (Exception ex)
	{
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "An error occurred while seeding the database.");
	}
}

// Configure the HTTP request pipeline.
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
}




// Register Minimal APIs



app.MapControllers();
app.ApplicationMinimalApis();
app.Run();
