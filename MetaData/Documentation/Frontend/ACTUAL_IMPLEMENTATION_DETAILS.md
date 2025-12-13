# Actual Implementation Details

## Frontend (AmesaFE) - Current Repository

### Application Type
**Lottery Management System** - Property lottery platform with community support focus

### Key Features Discovered
- **4Wins Model**: Profits support community causes
- **Property Lotteries**: Users can participate in real estate lotteries
- **Community Support**: Social impact through lottery profits
- **Multi-language Support**: Internationalization with translation service
- **Theme Support**: Dark/light mode switching
- **Real-time Features**: SignalR integration for live updates

### Angular Architecture
- **Standalone Components**: Modern Angular architecture
- **Lazy Loading**: Custom preloading strategy for performance
- **Route Guards**: Authentication protection for member areas
- **Performance Monitoring**: Route performance interceptor

### Pages/Components
- **Home**: Main landing page with lottery information
- **About**: Company story and 4Wins model explanation
- **How It Works**: Lottery process explanation
- **Registration**: User signup
- **Member Settings**: Protected user area
- **Partners**: Legal and accounting partners
- **Promotions**: Lottery promotions
- **Responsible Gambling**: Responsible gaming information
- **FAQ**: Frequently asked questions
- **Help Center**: Support information
- **Sponsorship**: Community support information

### Environment Configuration
```typescript
// Development
apiUrl: 'https://dev-api.amesa.com'
frontendUrl: 'https://dev.amesa.com'

// Production  
apiUrl: 'https://api.amesa.com'
frontendUrl: 'https://amesa.com'
```

## Backend (AmesaBE) - .NET 8.0

### Structure
- **AmesaBackend**: Main .NET 8.0 project
- **AmesaBackend.Tests**: Test project
- **Validators**: Custom validation logic
- **wwwroot**: Static files

### Key Technologies
- **.NET 8.0**: Latest framework version
- **Docker**: Containerization for ECS deployment
- **Aurora PostgreSQL**: Database integration

## AWS Infrastructure - Actual Implementation

### S3 Buckets (Confirmed)
- **amesa-frontend-prod**: Production frontend hosting
- **amesa-frontend-dev**: Development frontend hosting  
- **amesa-frontend-stage**: Staging frontend hosting
- **amesa-frontend-static**: Static assets with OAC (Origin Access Control)

### CloudFront Distributions (5 Active) - ALL OPERATIONAL ✅
1. **E2XBDFAUZJTI59** - Amesa Frontend - Development ✅
   - Domain: `d2rmamd755wq7j.cloudfront.net`
   - Origin: `amesa-frontend-dev.s3.amazonaws.com`
   - Origin Path: `/browser`
   - Status: Working (200 OK, 6,484 bytes)
   - Last Modified: October 9, 2025, 12:54 PM GMT+3

2. **E3GU3QXUR43ZOH** - Amesa Frontend - Production ✅
   - Domain: `dpqbvdgnenckf.cloudfront.net`
   - Origin: `amesa-frontend-prod.s3-website.eu-north-1.amazonaws.com`
   - Origin Path: `/browser` (Fixed 2025-10-09)
   - Status: Working (200 OK, 20,990 bytes)
   - Last Modified: October 9, 2025, 12:54 PM GMT+3

3. **E1D7XQHFF1469W** - Amesa Frontend - Staging ✅
   - Domain: `d2ejqzjfslo5hs.cloudfront.net`
   - Origin: `amesa-frontend-stage.s3-website.eu-north-1.amazonaws.com`
   - Origin Path: `/browser` (Fixed 2025-10-09)
   - Status: Working (200 OK, 20,990 bytes)
   - Last Modified: October 9, 2025, 12:54 PM GMT+3

4. **EJY57DPCK1LYS** - Amesa Backend API Distribution
   - Domain: `dgfh2mwcqhlbw.cloudfront.net`
   - Origin: `amesa-backend-alb-50` (Application Load Balancer)
   - Last Modified: October 7, 2025, 08:51 PM GMT+3

5. **E12EBI6VC526PS** - Amesa Frontend Distribution with OAC
   - Domain: `d3bkt41uo2lxir.cloudfront.net`
   - Origin: `amesa-frontend-static.s3.amazonaws.com`
   - Last Modified: October 7, 2025, 05:25 PM GMT+3

### ECS Cluster - "Amesa"
- **Region**: eu-north-1
- **Status**: Active
- **Services**: 2 active services
- **Tasks**: 2 running tasks
- **Launch Type**: Fargate (serverless)

#### ECS Services
1. **amesa-backend-service**
   - Task Definition: `amesa-backend:1`
   - Status: Active
   - Tasks: 1/1 running
   - Created: 2 days ago

2. **amesa-backend-stage-service**
   - Task Definition: `amesa-backend-staging:4`
   - Status: Active
   - Tasks: 1/1 running
   - Created: 17 hours ago

### Aurora PostgreSQL Databases (4 Instances)
1. **amesadbmain** (Regional cluster)
   - Status: Available
   - Engine: Aurora PostgreSQL
   - Region: eu-north-1

2. **amesadbmain1** (Writer instance)
   - Status: Available
   - Size: Serverless v2 (0.5 - 128 ACU)
   - CPU: 0.41%
   - VPC: vpc-0faee...

3. **amesadbmain-stage** (Regional cluster)
   - Status: Available
   - Engine: Aurora PostgreSQL
   - Region: eu-north-1

4. **amesadbmain1-stage** (Writer instance)
   - Status: Available
   - Size: Serverless v2 (0.5 - 128 ACU)
   - CPU: 0.40%
   - Current Activity: 2 Connections
   - VPC: vpc-0faee...

## GitHub Actions Workflows

### Current Workflows
1. **angular.js.yml**: Build & Deploy to GitHub Pages
   - Node.js 24
   - Production build with base-href
   - GitHub Pages deployment

2. **node.js.yml**: Node.js CI
   - Multi-version testing (18.x, 20.x, 22.x)
   - Build and test validation

3. **jekyll-gh-pages.yml**: Angular Build
   - Node.js 20
   - Test and build validation

### Build Process
- **Angular Build**: `ng build --configuration=production`
- **Output**: `dist/demo/browser/` directory
- **Deployment**: GitHub Pages with .nojekyll file

## Package.json Scripts
```json
{
  "build": "ng build",
  "build:dev": "ng build --configuration=development", 
  "build:stage": "ng build --configuration=production",
  "build:prod": "ng build --configuration=production",
  "test:ci": "ng test --no-watch --browsers=ChromeHeadless"
}
```

## Key Dependencies
- **Angular**: 20.2.1 (latest)
- **TypeScript**: 5.9.2
- **Tailwind CSS**: 3.4.3
- **SignalR**: 9.0.6 (real-time features)
- **RxJS**: 7.8.1

## Current Status
- **Repository**: AmesaFE (frontend)
- **Branch**: Testing
- **Working Tree**: Has uncommitted environment file changes
- **Deployment**: AWS S3+CloudFront (All environments operational ✅)
- **Last Update**: 2025-10-09 - Fixed CloudFront origin path configurations

## Business Model
- **Property Lotteries**: Users buy tickets for real estate
- **4Wins Model**: 
  - Winner gets property
  - Community gets support
  - Company gets sustainable business
  - Society gets social impact
- **Legal Partners**: Zeiba & Partners (legal), PiK Podatki (accounting)
- **Responsible Gaming**: Built-in responsible gambling features
