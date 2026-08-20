using CatalogService.Application.Services;
using CatalogService.Domain.Interfaces;
using CatalogService.Infrastructure.Persistence;
using CatalogService.Infrastructure.Repositories;
using FoodDelivery.Shared.Extensions;
using FoodDelivery.Shared.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;

// ── Configure Serilog ─────────────────────────────────────
SerilogExtensions.ConfigureSerilog(
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build(),
    "CatalogService");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── Database ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<CatalogDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Repositories & Services ────────────────────────────────────────────
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IOperatingHourRepository, OperatingHourRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<CatalogAppService>();

// ── Cloudinary Service ─────────────────────────────────────────────────
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();

// ── JWT Auth (same key/issuer as AuthService — tokens are cross-service) ─
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "sub",
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

// ── Swagger with Bearer ────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CatalogService API",
        Version = "v1",
        Description = "Restaurant discovery, menu management — Roles: Customer, Partner, Admin, DeliveryAgent"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        Description = "Enter: Bearer {your_jwt_token}"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, string.Empty)] = []
    });
});

// Configure Kestrel to disable response compression for Swagger
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// ── CORS for Gateway Swagger ──────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGateway", policy =>
    {
        policy.WithOrigins("http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Auto-migrate ───────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    try
    {
        db.Database.Migrate();
        CatalogService.Infrastructure.Data.DataSeeder.SeedData(db);
        Log.Information("CatalogService database migrated and seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "CatalogService database migration failed");
        throw;
    }
}

// ── Middleware Pipeline ────────────────────────────────────
app.UseGlobalExceptionHandler();
app.UseRequestLogging();
app.UseCors("AllowGateway");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CatalogService v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting CatalogService on port 5002");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CatalogService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}