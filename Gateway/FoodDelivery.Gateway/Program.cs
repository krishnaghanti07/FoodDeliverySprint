using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
                    "FoodDelivery_SuperSecret_Key_2024_MustBe32Chars!!"))
        };
    });

// Add Swagger with multiple service endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for fetching remote Swagger docs
builder.Services.AddHttpClient();

builder.Services.AddOcelot();

var app = builder.Build();

// Enable Swagger with multiple endpoints
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // Add each microservice Swagger endpoint
    options.SwaggerEndpoint("http://localhost:5001/swagger/v1/swagger.json", "Auth Service");
    options.SwaggerEndpoint("http://localhost:5002/swagger/v1/swagger.json", "Catalog Service");
    options.SwaggerEndpoint("http://localhost:5003/swagger/v1/swagger.json", "Order Service");
    options.SwaggerEndpoint("http://localhost:5005/swagger/v1/swagger.json", "Admin Service");
    
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Food Delivery API Gateway - All Services";
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.EnableTryItOutByDefault();
});

// Serve static files
app.UseStaticFiles();

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

await app.UseOcelot();
app.Run();
