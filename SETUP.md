# AuthGate - Setup Guide

## Architecture Overview

AuthGate is a professional authentication and authorization API built with:
- **.NET 9** with Clean Architecture (Domain, Application, Infrastructure, API)
- **DDD** (Domain-Driven Design) with documented entities
- **CQRS** with MediatR pipeline behaviors (Logging, Validation, Audit)
- **SOLID** principles throughout
- **PostgreSQL** for data persistence
- **JWT** tokens with refresh token rotation
- **MFA TOTP** (Google Authenticator)
- **RBAC/PBAC** (Role-Based and Permission-Based Access Control)
- **Centralized logging** with Serilog + Seq + SQLite
- **Centralized audit** via MediatR Behavior

## Prerequisites

1. **.NET 9 SDK** installed
2. **PostgreSQL** running (localhost:5432)
3. **MailHog** for email testing (localhost:1025) - optional
4. **Seq** for log visualization (localhost:8081) - optional

## Database Setup

1. Create PostgreSQL database:
```sql
CREATE DATABASE AuthGate;
```

2. Update connection string in `appsettings.json` if needed:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=AuthGate;Username=postgres;Password=postgres"
}
```

3. Apply migrations:
```bash
cd src/AuthGate.Auth
dotnet ef database update --project ../AuthGate.Auth.Infrastructure/AuthGate.Auth.Infrastructure.csproj
```

## Configuration

### JWT Settings (`appsettings.json`)
```json
"Jwt": {
  "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm!",
  "Issuer": "AuthGate",
  "Audience": "AuthGate",
  "AccessTokenExpirationMinutes": 15,
  "RefreshTokenExpirationDays": 7
}
```

### Email Settings (MailHog)
```json
"Email": {
  "SmtpHost": "localhost",
  "SmtpPort": "1025",
  "SmtpUsername": "",
  "SmtpPassword": "",
  "FromAddress": "noreply@authgate.com",
  "FromName": "AuthGate"
}
```

## Running the Application

```bash
cd src/AuthGate.Auth
dotnet run
```

The API will be available at:
- **HTTP**: http://localhost:8080
- **HTTPS**: https://localhost:8081
- **Swagger**: http://localhost:8080 (root)

## Project Structure

```
AuthGate/
├── src/
│   ├── AuthGate.Auth.Domain/           # Entities, interfaces, enums
│   │   ├── Common/                     # BaseEntity, IAuditableEntity
│   │   ├── Entities/                   # User, Role, Permission, etc.
│   │   ├── Enums/                      # AuditAction
│   │   └── Repositories/               # Repository interfaces
│   │
│   ├── AuthGate.Auth.Application/      # CQRS, DTOs, Behaviors
│   │   ├── Common/
│   │   │   ├── Behaviors/              # ValidationBehavior, AuditBehavior, LoggingBehavior
│   │   │   └── Interfaces/             # Service interfaces
│   │   ├── DTOs/                       # Data Transfer Objects
│   │   ├── Features/                   # CQRS Commands/Queries
│   │   │   └── Auth/
│   │   │       ├── Commands/           # Login, RefreshToken, etc.
│   │   │       └── Queries/
│   │   └── DependencyInjection.cs
│   │
│   ├── AuthGate.Auth.Infrastructure/   # Data access, services
│   │   ├── Persistence/
│   │   │   ├── AuthDbContext.cs
│   │   │   ├── Configurations/         # EF Core entity configurations
│   │   │   ├── Repositories/           # Repository implementations
│   │   │   └── Migrations/             # EF Core migrations
│   │   ├── Services/                   # JWT, Password, TOTP, Email, Audit
│   │   └── DependencyInjection.cs
│   │
│   └── AuthGate.Auth/                  # API entry point
│       ├── Controllers/                # API Controllers
│       ├── Program.cs                  # Application startup
│       ├── Startup.cs                  # Service configuration
│       └── appsettings.json            # Configuration
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with email/password
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Logout (revoke refresh token)

### Health Check
- `GET /health` - Application health status

## Features Implemented

✅ **Clean Architecture** - Domain, Application, Infrastructure, API layers  
✅ **CQRS** - Commands and Queries via MediatR  
✅ **DDD** - Documented entities with rich domain models  
✅ **Validation** - FluentValidation pipeline behavior  
✅ **Audit Logging** - Centralized via MediatR behavior  
✅ **Structured Logging** - Serilog with console, file, SQLite, and Seq sinks  
✅ **JWT Authentication** - Access + Refresh tokens with rotation  
✅ **Token Security** - Reuse detection and automatic revocation  
✅ **Password Hashing** - BCrypt with work factor 12  
✅ **RBAC/PBAC** - Roles and permissions support  
✅ **MFA Ready** - TOTP infrastructure (implementation pending)  
✅ **Email Service** - MailHog integration for password reset  
✅ **Swagger/OpenAPI** - Full API documentation  
✅ **CORS** - Configured for frontend integration  

## Next Steps

1. **Implement remaining features**:
   - MFA enrollment and verification commands
   - Password reset commands
   - User management endpoints
   - Role and permission management endpoints

2. **Add data seeding**:
   - Default admin user
   - Default roles (Admin, User)
   - Default permissions

3. **Create Angular frontend**:
   - Login page
   - Token management
   - MFA setup
   - User profile

4. **Add tests**:
   - Unit tests for handlers
   - Integration tests for API
   - E2E tests with TestContainers

## Development Notes

- All entities are **XML documented**
- Audit is **automatically captured** via MediatR behavior for commands implementing `IAuditableCommand`
- Logs are **centralized** and enriched with client IP, machine name, thread ID
- **Null safety** enabled throughout the project
- **Repository pattern** with Unit of Work for transaction management

## Troubleshooting

### Database connection fails
- Ensure PostgreSQL is running
- Verify connection string in `appsettings.json`
- Check firewall settings

### Migration errors
- Ensure `dotnet-ef` tool is installed: `dotnet tool install --global dotnet-ef`
- Rebuild solution: `dotnet build`
- Clear migrations and recreate if needed

### Email not sending
- Verify MailHog is running on port 1025
- Check logs in console for SMTP errors

## License

Professional project - All rights reserved.
