#!/bin/bash
# Database Migration Script for Microservices
# This script creates EF Core migrations for each microservice

set -e

echo "Starting database migrations for all microservices..."

# Auth Service
echo "Creating migration for Auth Service..."
cd ../AmesaBackend.Auth
dotnet ef migrations add InitialCreate --context AuthDbContext --output-dir Migrations
echo "✅ Auth Service migration created"

# Content Service
echo "Creating migration for Content Service..."
cd ../AmesaBackend.Content
dotnet ef migrations add InitialCreate --context ContentDbContext --output-dir Migrations
echo "✅ Content Service migration created"

# Notification Service
echo "Creating migration for Notification Service..."
cd ../AmesaBackend.Notification
dotnet ef migrations add InitialCreate --context NotificationDbContext --output-dir Migrations
echo "✅ Notification Service migration created"

# Payment Service
echo "Creating migration for Payment Service..."
cd ../AmesaBackend.Payment
dotnet ef migrations add InitialCreate --context PaymentDbContext --output-dir Migrations
echo "✅ Payment Service migration created"

# Lottery Service
echo "Creating migration for Lottery Service..."
cd ../AmesaBackend.Lottery
dotnet ef migrations add InitialCreate --context LotteryDbContext --output-dir Migrations
echo "✅ Lottery Service migration created"

# Lottery Results Service
echo "Creating migration for Lottery Results Service..."
cd ../AmesaBackend.LotteryResults
dotnet ef migrations add InitialCreate --context LotteryResultsDbContext --output-dir Migrations
echo "✅ Lottery Results Service migration created"

# Analytics Service
echo "Creating migration for Analytics Service..."
cd ../AmesaBackend.Analytics
dotnet ef migrations add InitialCreate --context AnalyticsDbContext --output-dir Migrations
echo "✅ Analytics Service migration created"

echo "✅ All database migrations created successfully!"

