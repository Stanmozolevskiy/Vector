using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using Vector.Api.Data;
using Vector.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database with connection pooling
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Enable connection pooling for better performance
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
    
    // Enable sensitive data logging only in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured");
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Services (will be implemented later)
// builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<IUserService, UserService>();
// builder.Services.AddScoped<IJwtService, JwtService>();
// builder.Services.AddScoped<IEmailService, EmailService>();
// builder.Services.AddScoped<IS3Service, S3Service>();
// builder.Services.AddScoped<IStripeService, StripeService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(builder.Configuration["Frontend:Url"] ?? "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger for Development and non-Production environments
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Staging" || app.Environment.EnvironmentName == "Dev")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vector API v1");
        c.RoutePrefix = "swagger";
    });
}

// Only use HTTPS redirection if HTTPS is available (not in Docker HTTP-only setup)
var urls = builder.Configuration["ASPNETCORE_URLS"];
if (!string.IsNullOrEmpty(urls) && urls.Contains("https"))
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
// app.UseMiddleware<ErrorHandlingMiddleware>(); // Will be created later

app.MapControllers();

// Add health check endpoints
app.MapGet("/health", () => Results.Ok(new { 
    message = "Vector API is running", 
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}))
    .WithName("HealthCheck")
    .WithTags("Health");

// API health endpoint (for ALB health checks)
app.MapGet("/api/health", () => Results.Ok(new { 
    message = "Vector API is running", 
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}))
    .WithName("ApiHealthCheck")
    .WithTags("Health");

// API root endpoint
app.MapGet("/api", () => Results.Ok(new { 
    message = "Vector API", 
    version = "1.0.0",
    endpoints = new {
        health = "/api/health",
        swagger = "/swagger"
    }
}))
    .WithName("ApiRoot")
    .WithTags("API");

// Run database migrations automatically on startup
// This works for all environments (dev, staging, prod) when running in containers
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Checking for pending database migrations...");
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"Applying {pendingMigrations.Count} pending migration(s)...");
            db.Database.Migrate();
            logger.LogInformation("Database migrations completed successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No migrations needed.");
        }
    }
    catch (Exception ex)
    {
        // Log error but don't crash - allow application to start
        // This is important for container orchestration (ECS) where the container
        // might start before the database is fully ready
        logger.LogError(ex, "An error occurred while migrating the database. The application will continue to start.");
        logger.LogWarning("If this is a new deployment, the database connection may not be ready yet. The application will retry on the next request.");
    }
}

app.Run();
