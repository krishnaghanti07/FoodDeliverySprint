using System.Text;
using FoodDelivery.Shared.Extensions;
using FoodDelivery.Shared.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OrderService.Application.Interfaces;
using OrderService.Application.Saga;
using OrderService.Application.Services;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;
using Serilog;

// ── Configure Serilog ─────────────────────────────────────
SerilogExtensions.ConfigureSerilog(
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build(),
    "OrderService");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── Database ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<OrderDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
    // Enable sensitive data logging to see parameter values in SQL queries
    opt.EnableSensitiveDataLogging();
    opt.EnableDetailedErrors();
    opt.LogTo(Console.WriteLine, LogLevel.Information);
});

// ── Repositories ───────────────────────────────────────────────────────
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();

// ── Application Services ───────────────────────────────────────────────
builder.Services.AddScoped<ICartService, CartAppService>();
builder.Services.AddScoped<IOrderService, OrderAppService>();
builder.Services.AddScoped<IPaymentService, PaymentAppService>();
builder.Services.AddScoped<IDeliveryService, DeliveryAppService>();
builder.Services.AddScoped<IRatingService, RatingAppService>();
builder.Services.AddScoped<ICouponService, CouponAppService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<OrderCancellationService>();

// ── HttpClient for inter-service communication ─────────────────────────
builder.Services.AddHttpClient();

// ── Saga Orchestrator ──────────────────────────────────────────────────
builder.Services.AddScoped<IOrderSaga, OrderSagaOrchestrator>();

// ── RabbitMQ Publisher (singleton — one connection) ────────────────────
builder.Services.AddSingleton<IRabbitMqPublisher>(
    new RabbitMqPublisher(builder.Configuration["RabbitMQ:Host"] ?? "localhost"));

// ── RabbitMQ Consumers (hosted background services) ────────────────────
builder.Services.AddHostedService<PaymentCompletedConsumer>();
builder.Services.AddHostedService<PaymentFailedConsumer>();

// ── JWT Authentication ─────────────────────────────────────────────────
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
        Title = "OrderService API",
        Version = "v1",
        Description = "Cart · Orders · Payments · Delivery — with Saga Pattern + RabbitMQ"
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

// ── Auto-migrate on startup ────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    try
    {
        db.Database.Migrate();
        Log.Information("OrderService database migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "OrderService database migration failed");
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderService v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting OrderService on port 5003");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}