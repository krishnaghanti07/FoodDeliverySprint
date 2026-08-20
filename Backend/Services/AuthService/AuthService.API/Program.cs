using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Services;
using FoodDelivery.Shared.Extensions;
using FoodDelivery.Shared.Messaging;
using FoodDelivery.Shared.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;
using System.Security.Claims;

// ── Configure Serilog ─────────────────────────────────────
SerilogExtensions.ConfigureSerilog(
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build(),
    "AuthService");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Dependency Injection ──────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IAuthService, AuthAppService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddSingleton<IRabbitMqPublisher>(
    new RabbitMqPublisher(builder.Configuration["RabbitMQ:Host"] ?? "localhost"));

// ── Cloudinary Service ─────────────────────────────────────────────────
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();

// ── JWT Auth ──────────────────────────────────────────────
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role
        };
        
        // Add logging for token validation
        opt.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("[AUTH] Token validation failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst("sub")?.Value ?? "unknown";
                Log.Debug("[AUTH] Token validated successfully for user: {UserId}", userId);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ── Swagger ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthService API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
        // Use camelCase for JSON serialization
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

// ── Migrate on startup ─────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    try
    {
        db.Database.Migrate();
        AuthService.Infrastructure.Data.DataSeeder.SeedData(db);
        Log.Information("AuthService database migrated and seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "AuthService database migration failed");
        throw;
    }
}

// ── Middleware Pipeline ────────────────────────────────────
app.UseMiddleware<AuthService.API.Middleware.ExceptionHandlingMiddleware>();  // Must be first
app.UseGlobalExceptionHandler();
app.UseRequestLogging();
app.UseCors("AllowGateway");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting AuthService on port 5001");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AuthService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}