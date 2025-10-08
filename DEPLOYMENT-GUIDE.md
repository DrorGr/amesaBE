# Amesa Lottery Platform - Deployment Guide

## Overview
This guide covers the complete deployment process for the Amesa Lottery Platform, including database setup, backend API deployment, and frontend deployment.

## Prerequisites

### System Requirements
- **Operating System**: Ubuntu 20.04+ / CentOS 8+ / Windows Server 2019+
- **RAM**: Minimum 4GB, Recommended 8GB+
- **Storage**: Minimum 50GB SSD
- **CPU**: Minimum 2 cores, Recommended 4+ cores

### Software Requirements
- **.NET 8.0 SDK**
- **PostgreSQL 15+**
- **Redis 6+**
- **Nginx** (for reverse proxy)
- **Docker** (optional, for containerized deployment)
- **SSL Certificate** (Let's Encrypt recommended)

## 1. Database Setup

### PostgreSQL Installation

#### Ubuntu/Debian
```bash
# Update package list
sudo apt update

# Install PostgreSQL
sudo apt install postgresql postgresql-contrib postgis

# Start and enable PostgreSQL
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Switch to postgres user
sudo -u postgres psql
```

#### CentOS/RHEL
```bash
# Install PostgreSQL repository
sudo dnf install -y https://download.postgresql.org/pub/repos/yum/reporpms/EL-8-x86_64/pgdg-redhat-repo-latest.noarch.rpm

# Install PostgreSQL
sudo dnf install -y postgresql15-server postgresql15-contrib postgis33_15

# Initialize database
sudo /usr/pgsql-15/bin/postgresql-15-setup initdb

# Start and enable PostgreSQL
sudo systemctl start postgresql-15
sudo systemctl enable postgresql-15
```

### Database Configuration

```sql
-- Create database and user
CREATE DATABASE amesa_lottery;
CREATE USER amesa_user WITH PASSWORD 'secure_password_here';
GRANT ALL PRIVILEGES ON DATABASE amesa_lottery TO amesa_user;

-- Connect to the database
\c amesa_lottery

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "postgis";

-- Grant schema permissions
GRANT ALL ON SCHEMA public TO amesa_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO amesa_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO amesa_user;
```

### Database Migration

```bash
# Navigate to backend project
cd AmesaBackend

# Run Entity Framework migrations
dotnet ef database update

# Verify tables were created
psql -h localhost -U amesa_user -d amesa_lottery -c "\dt"
```

## 2. Redis Setup

### Installation

#### Ubuntu/Debian
```bash
sudo apt install redis-server
sudo systemctl start redis-server
sudo systemctl enable redis-server
```

#### CentOS/RHEL
```bash
sudo dnf install redis
sudo systemctl start redis
sudo systemctl enable redis
```

### Configuration
```bash
# Edit Redis configuration
sudo nano /etc/redis/redis.conf

# Set password (recommended)
requirepass your_redis_password_here

# Restart Redis
sudo systemctl restart redis-server
```

## 3. Backend API Deployment

### .NET 8.0 Installation

#### Ubuntu/Debian
```bash
# Download and install .NET 8.0
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-8.0
```

#### CentOS/RHEL
```bash
# Install .NET 8.0
sudo dnf install -y dotnet-sdk-8.0
```

### Application Deployment

```bash
# Clone repository
git clone https://github.com/your-org/amesa-lottery.git
cd amesa-lottery/AmesaBackend

# Restore dependencies
dotnet restore

# Build application
dotnet build --configuration Release

# Publish application
dotnet publish --configuration Release --output /var/www/amesa-api

# Create systemd service
sudo nano /etc/systemd/system/amesa-api.service
```

### Systemd Service Configuration

```ini
[Unit]
Description=Amesa Lottery API
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/amesa-api/AmesaBackend.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=amesa-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

```bash
# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable amesa-api
sudo systemctl start amesa-api

# Check status
sudo systemctl status amesa-api
```

## 4. Nginx Configuration

### Installation
```bash
# Ubuntu/Debian
sudo apt install nginx

# CentOS/RHEL
sudo dnf install nginx
```

### Configuration
```bash
# Create Nginx configuration
sudo nano /etc/nginx/sites-available/amesa-api
```

```nginx
server {
    listen 80;
    server_name api.amesa.com;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name api.amesa.com;

    # SSL Configuration
    ssl_certificate /etc/letsencrypt/live/api.amesa.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.amesa.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;

    # Security Headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Rate Limiting
    limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
    limit_req zone=api burst=20 nodelay;

    # API Proxy
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 300s;
        proxy_connect_timeout 75s;
    }

    # WebSocket Support
    location /ws/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Health Check
    location /health {
        proxy_pass http://localhost:5000/health;
        access_log off;
    }

    # Static Files
    location /uploads/ {
        alias /var/www/amesa-api/wwwroot/uploads/;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

```bash
# Enable site
sudo ln -s /etc/nginx/sites-available/amesa-api /etc/nginx/sites-enabled/

# Test configuration
sudo nginx -t

# Restart Nginx
sudo systemctl restart nginx
```

## 5. SSL Certificate (Let's Encrypt)

```bash
# Install Certbot
sudo apt install certbot python3-certbot-nginx

# Obtain SSL certificate
sudo certbot --nginx -d api.amesa.com

# Test auto-renewal
sudo certbot renew --dry-run
```

## 6. Frontend Deployment

### Build Angular Application
```bash
# Navigate to frontend
cd ../src

# Install dependencies
npm install

# Build for production
npm run build:prod

# Copy to web server
sudo cp -r dist/demo/* /var/www/amesa-frontend/
```

### Nginx Configuration for Frontend
```bash
sudo nano /etc/nginx/sites-available/amesa-frontend
```

```nginx
server {
    listen 80;
    server_name amesa.com www.amesa.com;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name amesa.com www.amesa.com;

    # SSL Configuration
    ssl_certificate /etc/letsencrypt/live/amesa.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/amesa.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;

    # Security Headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Root directory
    root /var/www/amesa-frontend;
    index index.html;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/xml+rss application/json;

    # Cache static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Angular routing
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy
    location /api/ {
        proxy_pass https://api.amesa.com/;
        proxy_set_header Host api.amesa.com;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## 7. Docker Deployment (Alternative)

### Dockerfile for Backend
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AmesaBackend/AmesaBackend.csproj", "AmesaBackend/"]
RUN dotnet restore "AmesaBackend/AmesaBackend.csproj"
COPY . .
WORKDIR "/src/AmesaBackend"
RUN dotnet build "AmesaBackend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AmesaBackend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AmesaBackend.dll"]
```

### Docker Compose
```yaml
version: '3.8'

services:
  postgres:
    image: postgis/postgis:15-3.3
    environment:
      POSTGRES_DB: amesa_lottery
      POSTGRES_USER: amesa_user
      POSTGRES_PASSWORD: secure_password_here
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass your_redis_password_here
    volumes:
      - redis_data:/data
    ports:
      - "6379:6379"

  api:
    build: ./AmesaBackend
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=amesa_lottery;Username=amesa_user;Password=secure_password_here
      - ConnectionStrings__Redis=redis:6379,password=your_redis_password_here
    depends_on:
      - postgres
      - redis
    ports:
      - "5000:80"

  nginx:
    image: nginx:alpine
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/ssl
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - api

volumes:
  postgres_data:
  redis_data:
```

## 8. Monitoring and Logging

### Application Monitoring
```bash
# Install monitoring tools
sudo apt install htop iotop nethogs

# Install Prometheus and Grafana (optional)
wget https://github.com/prometheus/prometheus/releases/download/v2.45.0/prometheus-2.45.0.linux-amd64.tar.gz
tar xvfz prometheus-2.45.0.linux-amd64.tar.gz
cd prometheus-2.45.0.linux-amd64
./prometheus --config.file=prometheus.yml
```

### Log Management
```bash
# Configure logrotate
sudo nano /etc/logrotate.d/amesa-api
```

```
/var/www/amesa-api/logs/*.txt {
    daily
    missingok
    rotate 30
    compress
    delaycompress
    notifempty
    create 644 www-data www-data
    postrotate
        systemctl reload amesa-api
    endscript
}
```

## 9. Backup Strategy

### Database Backup
```bash
# Create backup script
sudo nano /usr/local/bin/backup-database.sh
```

```bash
#!/bin/bash
BACKUP_DIR="/var/backups/amesa"
DATE=$(date +%Y%m%d_%H%M%S)
DB_NAME="amesa_lottery"
DB_USER="amesa_user"

mkdir -p $BACKUP_DIR

# Create database backup
pg_dump -h localhost -U $DB_USER -d $DB_NAME | gzip > $BACKUP_DIR/amesa_lottery_$DATE.sql.gz

# Keep only last 30 days of backups
find $BACKUP_DIR -name "amesa_lottery_*.sql.gz" -mtime +30 -delete

echo "Database backup completed: amesa_lottery_$DATE.sql.gz"
```

```bash
# Make executable
sudo chmod +x /usr/local/bin/backup-database.sh

# Add to crontab
sudo crontab -e
# Add: 0 2 * * * /usr/local/bin/backup-database.sh
```

### Application Backup
```bash
# Create application backup script
sudo nano /usr/local/bin/backup-application.sh
```

```bash
#!/bin/bash
BACKUP_DIR="/var/backups/amesa"
DATE=$(date +%Y%m%d_%H%M%S)
APP_DIR="/var/www/amesa-api"

mkdir -p $BACKUP_DIR

# Create application backup
tar -czf $BACKUP_DIR/amesa-api_$DATE.tar.gz -C $APP_DIR .

# Keep only last 7 days of backups
find $BACKUP_DIR -name "amesa-api_*.tar.gz" -mtime +7 -delete

echo "Application backup completed: amesa-api_$DATE.tar.gz"
```

## 10. Security Hardening

### Firewall Configuration
```bash
# Install UFW
sudo apt install ufw

# Configure firewall
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow ssh
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

### System Updates
```bash
# Configure automatic security updates
sudo apt install unattended-upgrades
sudo dpkg-reconfigure -plow unattended-upgrades
```

### SSL/TLS Hardening
```bash
# Test SSL configuration
curl -I https://api.amesa.com
sslscan api.amesa.com
```

## 11. Performance Optimization

### Database Optimization
```sql
-- Analyze tables for query optimization
ANALYZE;

-- Create additional indexes if needed
CREATE INDEX CONCURRENTLY idx_lottery_tickets_house_user ON lottery_tickets(house_id, user_id);
CREATE INDEX CONCURRENTLY idx_transactions_user_date ON transactions(user_id, created_at);
```

### Application Optimization
```bash
# Configure system limits
sudo nano /etc/security/limits.conf
# Add:
# www-data soft nofile 65536
# www-data hard nofile 65536

# Optimize kernel parameters
sudo nano /etc/sysctl.conf
# Add:
# net.core.somaxconn = 65536
# net.ipv4.tcp_max_syn_backlog = 65536
```

## 12. Troubleshooting

### Common Issues

#### Database Connection Issues
```bash
# Check PostgreSQL status
sudo systemctl status postgresql

# Check connection
psql -h localhost -U amesa_user -d amesa_lottery

# Check logs
sudo tail -f /var/log/postgresql/postgresql-15-main.log
```

#### Application Issues
```bash
# Check application status
sudo systemctl status amesa-api

# Check logs
sudo journalctl -u amesa-api -f

# Check application logs
tail -f /var/www/amesa-api/logs/amesa-*.txt
```

#### Nginx Issues
```bash
# Check Nginx status
sudo systemctl status nginx

# Test configuration
sudo nginx -t

# Check logs
sudo tail -f /var/log/nginx/error.log
```

## 13. Maintenance

### Regular Maintenance Tasks
```bash
# Weekly database maintenance
sudo -u postgres psql -d amesa_lottery -c "VACUUM ANALYZE;"

# Monthly log cleanup
sudo find /var/log -name "*.log" -mtime +30 -delete

# Quarterly security updates
sudo apt update && sudo apt upgrade
```

### Health Checks
```bash
# Create health check script
sudo nano /usr/local/bin/health-check.sh
```

```bash
#!/bin/bash

# Check API health
if curl -f http://localhost:5000/health > /dev/null 2>&1; then
    echo "API: OK"
else
    echo "API: FAILED"
    exit 1
fi

# Check database connection
if pg_isready -h localhost -U amesa_user -d amesa_lottery > /dev/null 2>&1; then
    echo "Database: OK"
else
    echo "Database: FAILED"
    exit 1
fi

# Check Redis connection
if redis-cli -a your_redis_password_here ping > /dev/null 2>&1; then
    echo "Redis: OK"
else
    echo "Redis: FAILED"
    exit 1
fi

echo "All services are healthy"
```

This deployment guide provides a comprehensive approach to deploying the Amesa Lottery Platform in a production environment. Adjust configurations based on your specific requirements and infrastructure.
