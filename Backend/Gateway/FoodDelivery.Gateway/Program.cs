using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// JWT validation at the Gateway level
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "FoodDelivery.AuthService",
            ValidAudience = "FoodDelivery.Clients",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    "FoodDelivery_SuperSecret_Key_2024_MustBe32Chars!!")),
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minute tolerance
            NameClaimType = "sub",
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
        
        // Add detailed logging
        opt.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[GATEWAY AUTH] ❌ Token validation FAILED: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"[GATEWAY AUTH] Inner exception: {context.Exception.InnerException.Message}");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst("sub")?.Value ?? "unknown";
                var email = context.Principal?.FindFirst("email")?.Value ?? "unknown";
                Console.WriteLine($"[GATEWAY AUTH] ✅ Token validated for user: {userId}, email: {email}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"[GATEWAY AUTH] ⚠️ Authentication challenge: {context.Error}, {context.ErrorDescription}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var token = context.Token;
                if (!string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($"[GATEWAY AUTH] 📨 Token received (first 20 chars): {token.Substring(0, Math.Min(20, token.Length))}...");
                }
                else
                {
                    Console.WriteLine("[GATEWAY AUTH] ⚠️ No token in request");
                }
                return Task.CompletedTask;
            }
        };
    });

// Add Swagger with multiple service endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for fetching remote Swagger docs
builder.Services.AddHttpClient();

// Configure Kestrel to disable response compression for Swagger
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

builder.Services.AddOcelot();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Enable CORS BEFORE other middleware
app.UseCors("AllowFrontend");

// Serve static files BEFORE Ocelot
app.UseStaticFiles();

// Use default files (index.html)
app.UseDefaultFiles();

// Enable Swagger with multiple endpoints
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // Add each microservice Swagger endpoint
    options.SwaggerEndpoint("http://localhost:5001/swagger/v1/swagger.json", "Auth Service");
    options.SwaggerEndpoint("http://localhost:5002/swagger/v1/swagger.json", "Catalog Service");
    options.SwaggerEndpoint("http://localhost:5003/swagger/v1/swagger.json", "Order Service");
    options.SwaggerEndpoint("http://localhost:5004/swagger/v1/swagger.json", "Payment Service");
    options.SwaggerEndpoint("http://localhost:5005/swagger/v1/swagger.json", "Admin Service");
    
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Food Delivery API Gateway - All Services";
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.EnableTryItOutByDefault();
});

await app.UseOcelot();
app.Run();
