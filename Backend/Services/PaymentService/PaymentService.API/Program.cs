using System.Text;
using FoodDelivery.Shared.Extensions;
using FoodDelivery.Shared.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Services;
using PaymentService.Domain.Interfaces;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Repositories;
using Serilog;

// ── Configure Serilog ─────────────────────────────────────
SerilogExtensions.ConfigureSerilog(
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build(),
    "PaymentService");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── Database ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<PaymentDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Repositories ───────────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IRazorpayOrderRepository, RazorpayOrderRepository>();

// ── Application Services ───────────────────────────────────────────────
builder.Services.AddScoped<IPaymentSimulationService, PaymentSimulationService>();
builder.Services.AddScoped<IRazorpayService, RazorpayService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IPaymentQueryService, PaymentQueryService>();

// ── RabbitMQ Publisher (singleton) ────────────────────────────────────
builder.Services.AddSingleton<IRabbitMqPublisher>(_ =>
    new RabbitMqPublisher(
        builder.Configuration["RabbitMQ:Host"] ?? "localhost"));

// ── RabbitMQ Consumer ─────────────────────────────────────────────────
builder.Services.AddHostedService<OrderPlacedConsumer>();

// ── JWT Auth — same key/issuer as all services ────────────────────────
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
        Title = "PaymentService API",
        Version = "v1",
        Description =
            "Dedicated Payment Microservice\n\n" +
            "POST /api/payments/simulate — Simulate success/failure (COD | Card | Wallet)\n" +
            "POST /api/payments/razorpay/create-order — Razorpay order creation stub\n" +
            "POST /api/payments/razorpay/verify — Razorpay signature verification\n" +
            "POST /api/payments/refund — Admin-only refund processing\n" +
            "GET  /api/payments/order/{orderId} — Payment record by order\n\n" +
            "Publishes: PaymentCompletedEvent | PaymentFailedEvent → RabbitMQ\n" +
            "Consumes:  OrderPlacedEvent → creates Pending payment record"
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
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try
    {
        db.Database.Migrate();
        Log.Information("PaymentService database migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "PaymentService database migration failed");
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PaymentService v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting PaymentService on port 5004");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PaymentService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}