using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using Vector.Api.Data;
using Vector.Api.Middleware;
using Vector.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Include XML comments for API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
    
    // Ensure all controllers are discovered
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Vector API",
        Version = "v1",
        Description = "Vector Interview Preparation Platform API"
    });
    
    // JWT Bearer authentication for Swagger UI
    // Note: Swagger JWT auth can be added later if needed
    // The API endpoints are protected with [Authorize] attribute
});

// Memory cache for in-memory caching
builder.Services.AddMemoryCache();

// Database with connection pooling
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=vector_db;Username=postgres;Password=postgres";

    // Normalize PostgreSQL URI format (e.g. from Render: postgres://user:pass@host:5432/db)
    var connectionString = rawConnectionString;
    if (rawConnectionString.StartsWith("postgres://") || rawConnectionString.StartsWith("postgresql://"))
    {
        var uri = new Uri(rawConnectionString);
        var userParts = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userParts[0]);
        var password = userParts.Length > 1 ? Uri.UnescapeDataString(userParts[1]) : "";
        var database = uri.AbsolutePath.TrimStart('/');
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={database};Username={user};Password={password}";
    }

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
    var rawRedis = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

    // Normalize Redis URI format (e.g. from Render: redis://red-xxx:6379 or rediss://...)
    var redisConnection = rawRedis;
    if (rawRedis.StartsWith("redis://") || rawRedis.StartsWith("rediss://"))
    {
        var uri = new Uri(rawRedis);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 6379;
        redisConnection = $"{host}:{port}";
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                redisConnection += $",password={Uri.UnescapeDataString(parts[1])}";
        }
        if (rawRedis.StartsWith("rediss://"))
            redisConnection += ",ssl=true,sslprotocols=tls12";
    }

    return ConnectionMultiplexer.Connect(redisConnection);
});
builder.Services.AddSingleton<IRedisService, RedisService>();

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
        ClockSkew = TimeSpan.Zero,
        // Map JWT claim names to ASP.NET Core claim types
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
    
    // Configure JWT for SignalR
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            // If the request is for the SignalR hub, get the token from query string
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/collaboration"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// S3-compatible storage — works with AWS S3 and Cloudflare R2
// Set Storage:ServiceUrl in config to use R2 (or any S3-compatible provider).
// Leave it empty to fall back to standard AWS S3.
var storageServiceUrl = builder.Configuration["Storage:ServiceUrl"];
if (!string.IsNullOrEmpty(storageServiceUrl))
{
    builder.Services.AddSingleton<IAmazonS3>(_ =>
    {
        var accessKey = builder.Configuration["Storage:AccessKeyId"]
            ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "";
        var secretKey = builder.Configuration["Storage:SecretAccessKey"]
            ?? Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "";
        var s3Config = new AmazonS3Config
        {
            ServiceURL = storageServiceUrl,
            ForcePathStyle = true   // Required for R2 and most S3-compatible providers
        };
        return new AmazonS3Client(accessKey, secretKey, s3Config);
    });
}
else
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonS3>();
}

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<ICoachService, CoachService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<ISolutionService, SolutionService>();
builder.Services.AddScoped<ICodeDraftService, CodeDraftService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IPeerInterviewService, PeerInterviewService>();
builder.Services.AddScoped<IInterviewMatchingService, InterviewMatchingService>();
builder.Services.AddScoped<IWhiteboardService, WhiteboardService>();
builder.Services.AddSingleton<IMatchingPresenceService, MatchingPresenceService>();
builder.Services.AddScoped<ICoinService, CoinService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IQuestionVoteService, QuestionVoteService>();
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// SignalR for real-time collaboration
builder.Services.AddSignalR();

// Code Execution Service (Judge0 Official API)
builder.Services.AddHttpClient(nameof(CodeExecutionService), (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var judge0Url = configuration["Judge0:BaseUrl"] ?? "https://ce.judge0.com";
    var judge0ApiKey = configuration["Judge0:ApiKey"];
    
    client.BaseAddress = new Uri(judge0Url);
    client.Timeout = TimeSpan.FromSeconds(60); // Increased timeout for API calls
    
    // Clear default headers
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    
    // Add API key header if provided (for paid tier)
    if (!string.IsNullOrEmpty(judge0ApiKey))
    {
        client.DefaultRequestHeaders.Add("X-RapidAPI-Key", judge0ApiKey);
        client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "judge0-ce.p.rapidapi.com");
    }
});
builder.Services.AddScoped<ICodeExecutionService, CodeExecutionService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Frontend:Url"] ?? "http://localhost:3000",
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromSeconds(86400));
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// CORS must be FIRST to handle preflight OPTIONS requests
app.UseCors("AllowReactApp");

// Enable Swagger for Development and non-Production environments
// Also enable for "Dev" environment name (used in AWS ECS)
if (app.Environment.IsDevelopment() || 
    app.Environment.EnvironmentName == "Staging" || 
    app.Environment.EnvironmentName == "Dev" ||
    app.Environment.EnvironmentName.Equals("Development", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vector API v1");
        c.RoutePrefix = "swagger";
        // Enable deep linking for Swagger UI
        c.EnableDeepLinking();
        // Display operation ID in Swagger UI
        c.DisplayOperationId();
    });
}

// Only use HTTPS redirection if HTTPS is available (not in Docker HTTP-only setup)
var urls = builder.Configuration["ASPNETCORE_URLS"];
if (!string.IsNullOrEmpty(urls) && urls.Contains("https"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
// app.UseMiddleware<ErrorHandlingMiddleware>(); // Will be created later

app.MapControllers();

// Map SignalR hubs
app.MapHub<Vector.Api.Hubs.CollaborationHub>("/api/collaboration");

// Health check endpoints are handled by HealthController
// Removed duplicate MapGet endpoints to avoid conflicts

// Run database migrations and seed data automatically on startup
// This works for all environments (dev, staging, prod) when running in containers
var scope = app.Services.CreateScope();
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Run migrations (separate try-catch so seeding can still run if migrations fail)
    // Skip for InMemory database (used in integration tests)
    try
    {
        // Check if we're using InMemory database
        var providerName = db.Database.ProviderName;
        if (providerName != null && providerName.Contains("InMemory"))
        {
            logger.LogInformation("Using InMemory database - skipping migrations");
        }
        else
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
    }
    catch (Exception ex)
    {
        // Log migration error but continue - database might already be up to date
        logger.LogWarning(ex, "Migration failed (this is OK if tables already exist). Continuing with seeding...");
    }

    // Seed database with initial data (admin user, etc.) - runs even if migrations failed
    try
    {
        logger.LogInformation("Seeding database with initial data...");
        DbSeeder.SeedDatabase(db, logger).GetAwaiter().GetResult();
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        // Log seeding error but don't crash - allow application to start
        logger.LogError(ex, "An error occurred while seeding the database. The application will continue to start.");
        logger.LogWarning("Admin user may not have been created. You can create it manually or restart the container.");
    }
    finally
    {
        scope.Dispose();
    }
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
