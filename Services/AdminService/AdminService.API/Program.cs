using System.Text;
using AdminService.Application.Interfaces;
using AdminService.Application.Services;
using AdminService.Domain.Interfaces;
using AdminService.Infrastructure.Messaging;
using AdminService.Infrastructure.Persistence;
using AdminService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<AdminDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Repositories ───────────────────────────────────────────────────────
builder.Services.AddScoped<IUserSnapshotRepository, UserSnapshotRepository>();
builder.Services.AddScoped<IOrderSnapshotRepository, OrderSnapshotRepository>();
builder.Services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();

// ── Application Services ───────────────────────────────────────────────
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IAdminReportService, AdminReportService>();

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
                                           Encoding.UTF8.GetBytes(jwtKey))
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

builder.Services.AddControllers();

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
    db.Database.Migrate();

    // Seed sample data
    AdminService.Infrastructure.Data.DataSeeder.SeedData(db);
}

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
app.Run();