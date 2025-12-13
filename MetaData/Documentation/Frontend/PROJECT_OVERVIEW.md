# AmesaBase Project Overview

## Project Description
AmesaBase is a lottery management system with Angular frontend, .NET backend, and dedicated DevOps infrastructure. The application is a property lottery platform that allows users to participate in lotteries for real estate properties, featuring a "4Wins Model" where profits support community causes.

## Repository Structure
The project is split across three GitHub repositories:

### 1. **AmesaFE** (Frontend Repository)
- **Technology**: Angular with TypeScript
- **Styling**: Tailwind CSS
- **Deployment**: AWS S3 + CloudFront
- **CI/CD**: GitHub Actions workflow for automated deployment
- **Current Branch**: Testing

### 2. **AmesaBE** (Backend Repository)
- **Technology**: .NET 8.0
- **Deployment**: Docker containers on AWS ECS
- **Architecture**: Containerized microservices with load balancing

### 3. **AmesaDevOps** (Infrastructure Repository)
- **Purpose**: Infrastructure as Code, deployment configurations
- **Tools**: AWS configurations, deployment scripts

## AWS Infrastructure
- **Database**: Aurora PostgreSQL
- **Frontend**: S3 static hosting + CloudFront CDN
- **Backend**: ECS (Elastic Container Service) with Docker instances
- **Load Balancing**: Application Load Balancer (ALB) for backend services
- **Environments**: Development, Staging, Production

## Current Status
- **Frontend Repository**: Currently on `Testing` branch, working tree clean
- **CI/CD**: Automated deployment pipeline via GitHub Actions
- **Infrastructure**: Multi-environment setup with proper separation

## Key Technologies
- **Frontend**: Angular, TypeScript, Tailwind CSS
- **Backend**: .NET 8.0, Docker
- **Database**: Aurora PostgreSQL
- **Cloud**: AWS (S3, CloudFront, ECS, ALB, Aurora)
- **CI/CD**: GitHub Actions
- **Infrastructure**: Infrastructure as Code
