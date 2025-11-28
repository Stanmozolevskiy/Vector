# Vector Backend API

## Project Structure

```
backend/
├── Vector.sln                    # Solution file
└── Vector.Api/                   # Main API project
    ├── Controllers/              # API Controllers
    ├── Services/                # Business logic services
    ├── Models/                  # Entity models
    ├── Data/                    # Database context
    ├── DTOs/                    # Data Transfer Objects
    │   ├── Auth/
    │   ├── User/
    │   └── Subscription/
    ├── Middleware/              # Custom middleware
    ├── Helpers/                 # Helper classes
    ├── Program.cs               # Application entry point
    └── appsettings.json         # Configuration
```

## Setup Complete ✅

The backend project has been initialized with:

- ✅ .NET 8.0 Web API project
- ✅ Entity Framework Core with PostgreSQL
- ✅ JWT Authentication configured
- ✅ Redis connection setup
- ✅ Swagger/OpenAPI documentation
- ✅ CORS configuration
- ✅ Error handling middleware
- ✅ Database models (User, Subscription, Payment, EmailVerification)
- ✅ ApplicationDbContext configured

## Next Steps

1. **Configure Database Connection**
   - Update `appsettings.json` with your PostgreSQL connection string
   - Update Redis connection string if needed

2. **Run Database Migrations**
   ```bash
   cd Vector.Api
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

3. **Configure External Services**
   - Add Stripe API keys to `appsettings.json`
   - Add SendGrid API key to `appsettings.json`
   - Add AWS credentials to `appsettings.json`

4. **Implement Services**
   - Create service interfaces and implementations
   - Implement authentication logic
   - Implement user management
   - Implement subscription and payment logic

5. **Create Controllers**
   - AuthController
   - UserController
   - SubscriptionController
   - StripeController

## Running the Application

```bash
cd backend/Vector.Api
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## Dependencies Installed

- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.Design (8.0.0)
- Npgsql.EntityFrameworkCore.PostgreSQL (8.0.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
- BCrypt.Net-Next (4.0.3)
- Stripe.net (50.0.0)
- SendGrid (9.29.3)
- AWSSDK.S3 (4.0.13.1)
- StackExchange.Redis (2.10.1)
- FluentValidation.AspNetCore (11.3.1)
- Swashbuckle.AspNetCore (10.0.1)

## Notes

- The project is configured but services are not yet implemented
- Database migrations need to be run after configuring the connection string
- External service API keys need to be added to `appsettings.json`
- JWT secret should be changed to a secure random string in production

