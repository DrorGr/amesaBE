# Amesa Backend

This is the backend API for the Amesa Lottery system, built with .NET 8 and ASP.NET Core.

## Environment Configuration

The application supports multiple environments: development, dev, stage, and production. Environment-specific configurations are managed through GitHub Secrets and environment variables.

### Required GitHub Secrets

To set up the CI/CD pipeline, you need to configure the following secrets in your GitHub repository:

#### AWS Configuration
- `AWS_ACCESS_KEY_ID` - AWS access key for deployment
- `AWS_SECRET_ACCESS_KEY` - AWS secret key for deployment

#### Development Environment
- `DEV_ECS_CLUSTER` - Development ECS cluster name
- `DEV_ECS_SERVICE` - Development ECS service name
- `DEV_DB_CONNECTION_STRING` - Development database connection string
- `DEV_REDIS_CONNECTION_STRING` - Development Redis connection string
- `DEV_JWT_SECRET_KEY` - Development JWT secret key
- `DEV_SMTP_USERNAME` - Development SMTP username
- `DEV_SMTP_PASSWORD` - Development SMTP password
- `DEV_STRIPE_SECRET_KEY` - Development Stripe secret key
- `DEV_AWS_ACCESS_KEY_ID` - Development AWS access key
- `DEV_AWS_SECRET_ACCESS_KEY` - Development AWS secret key

#### Staging Environment
- `STAGE_ECS_CLUSTER` - Staging ECS cluster name
- `STAGE_ECS_SERVICE` - Staging ECS service name
- `STAGE_DB_CONNECTION_STRING` - Staging database connection string
- `STAGE_REDIS_CONNECTION_STRING` - Staging Redis connection string
- `STAGE_JWT_SECRET_KEY` - Staging JWT secret key
- `STAGE_SMTP_USERNAME` - Staging SMTP username
- `STAGE_SMTP_PASSWORD` - Staging SMTP password
- `STAGE_STRIPE_SECRET_KEY` - Staging Stripe secret key
- `STAGE_AWS_ACCESS_KEY_ID` - Staging AWS access key
- `STAGE_AWS_SECRET_ACCESS_KEY` - Staging AWS secret key

#### Production Environment
- `PROD_ECS_CLUSTER` - Production ECS cluster name
- `PROD_ECS_SERVICE` - Production ECS service name
- `PROD_DB_CONNECTION_STRING` - Production database connection string
- `PROD_REDIS_CONNECTION_STRING` - Production Redis connection string
- `PROD_JWT_SECRET_KEY` - Production JWT secret key
- `PROD_SMTP_USERNAME` - Production SMTP username
- `PROD_SMTP_PASSWORD` - Production SMTP password
- `PROD_STRIPE_SECRET_KEY` - Production Stripe secret key
- `PROD_AWS_ACCESS_KEY_ID` - Production AWS access key
- `PROD_AWS_SECRET_ACCESS_KEY` - Production AWS secret key

### Setting up GitHub Secrets

1. Go to your GitHub repository
2. Navigate to Settings > Secrets and variables > Actions
3. Click "New repository secret"
4. Add each secret with the appropriate value

### Local Development

For local development, you can use the development configuration:

```bash
dotnet restore
dotnet build
dotnet run --project AmesaBackend
```

### Environment Variables

The application uses environment variables for configuration. Create a `.env` file in the root directory:

```env
# Database Configuration
DB_CONNECTION_STRING=Host=localhost;Database=amesa_lottery;Username=postgres;Password=password;Port=5432

# Redis Configuration
REDIS_CONNECTION_STRING=localhost:6379

# JWT Configuration
JWT_SECRET_KEY=your-super-secret-key-that-is-at-least-32-characters-long
JWT_ISSUER=AmesaLottery
JWT_AUDIENCE=AmesaLotteryUsers

# Email Configuration
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
FROM_EMAIL=noreply@amesa.com
FROM_NAME=Amesa Lottery

# Payment Configuration
STRIPE_SECRET_KEY=sk_test_your_stripe_secret_key
STRIPE_PUBLISHABLE_KEY=pk_test_your_stripe_publishable_key

# AWS Configuration
AWS_ACCESS_KEY_ID=your-aws-access-key
AWS_SECRET_ACCESS_KEY=your-aws-secret-key
AWS_REGION=us-east-1
AWS_S3_BUCKET=amesa-uploads
```

### Build and Run Commands

- `dotnet restore` - Restore NuGet packages
- `dotnet build` - Build the solution
- `dotnet test` - Run tests
- `dotnet run --project AmesaBackend` - Run the application
- `dotnet publish` - Publish for deployment

### Deployment

The application automatically deploys based on the branch:

- `dev` branch → Development environment
- `stage` branch → Staging environment
- `main` branch → Production environment

### Docker

The application includes Docker support:

```bash
# Build Docker image
docker build -t amesa-backend .

# Run Docker container
docker run -p 5000:80 amesa-backend
```

### Database

The application uses PostgreSQL as the primary database and Redis for caching.

#### Database Migration

```bash
# Update database schema
dotnet ef database update --project AmesaBackend
```

#### Seeding Data

```bash
# Seed initial data
dotnet run --project AmesaBackend --seed
```

### API Documentation

The API includes Swagger documentation available at `/swagger` when running in development mode.

### Architecture

This is an ASP.NET Core Web API with:
- Entity Framework Core for data access
- JWT authentication and authorization
- SignalR for real-time communication
- Comprehensive logging with Serilog
- Health checks
- CORS support
- Rate limiting
- Error handling middleware
- Security headers middleware

### Testing

The project includes unit tests and integration tests:

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Monitoring and Logging

- Health checks at `/health`
- Structured logging with Serilog
- Application insights integration
- Performance counters

### Security Features

- JWT-based authentication
- Role-based authorization
- CORS configuration
- Security headers
- Rate limiting
- Input validation
- SQL injection protection
