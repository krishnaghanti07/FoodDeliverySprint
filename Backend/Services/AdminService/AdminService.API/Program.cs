using System.Text;
using AdminService.Application.Interfaces;
using AdminService.Application.Services;
using AdminService.Domain.Interfaces;
using AdminService.Infrastructure.Messaging;
using AdminService.Infrastructure.Persistence;
using AdminService.Infrastructure.Repositories;
using FoodDelivery.Shared.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

// ── Configure Serilog ─────────────────────────────────────
SerilogExtensions.ConfigureSerilog(
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build(),
    "AdminService");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── Database ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<AdminDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Repositories ───────────────────────────────────────────────────────
builder.Services.AddScoped<IUserSnapshotRepository, UserSnapshotRepository>();
builder.Services.AddScoped<IOrderSnapshotRepository, OrderSnapshotRepository>();
builder.Services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
builder.Services.AddScoped<IRestaurantSnapshotRepository, RestaurantSnapshotRepository>();
builder.Services.AddScoped<IDeliveryAgentSnapshotRepository, DeliveryAgentSnapshotRepository>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<INotificationHistoryRepository, NotificationHistoryRepository>();

// ── Application Services ───────────────────────────────────────────────
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IAdminReportService, AdminReportService>();
builder.Services.AddScoped<IAdminRestaurantService, AdminRestaurantService>();
builder.Services.AddScoped<IAdminDeliveryAgentService, AdminDeliveryAgentService>();
builder.Services.AddScoped<IAdminComplaintService, AdminComplaintService>();
builder.Services.AddScoped<IAdminNotificationService, AdminNotificationService>();

// ── HttpClient for inter-service communication ─────────────────────────
builder.Services.AddHttpClient();

// ── RabbitMQ Consumers (background services) ───────────────────────────
builder.Services.AddHostedService<UserRegisteredConsumer>();
builder.Services.AddHostedService<OrderPlacedConsumer>();
builder.Services.AddHostedService<AdminPaymentCompletedConsumer>();
builder.Services.AddHostedService<AdminDeliveryStatusConsumer>();

// ── JWT Auth — same key/issuer as all other services ──────────────────
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

// ── Swagger ────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AdminService API",
        Version = "v1",
        Description =
            "Service 4 of 4 — Platform Administration & Reporting\n" +
            "Endpoints: Dashboard · User Management · Order Overrides · Sales Reports · Partner Reports\n" +
            "Role required: Admin"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        Description = "Enter: Bearer {admin_jwt_token}"
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

// ── Auto-migrate + Seed Admin User ────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    try
    {
        db.Database.Migrate();
        AdminService.Infrastructure.Data.DataSeeder.SeedData(db);
        Log.Information("AdminService database migrated and seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "AdminService database migration failed");
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AdminService v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting AdminService on port 5005");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AdminService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}