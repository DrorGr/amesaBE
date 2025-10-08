#!/bin/bash

# Amesa Backend Secrets Setup Script
# This script helps you set up AWS Secrets Manager for your backend

set -e

# Configuration
AWS_REGION="eu-north-1"
SECRET_PREFIX="amesa"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to create a secret
create_secret() {
    local secret_name=$1
    local secret_value=$2
    local description=$3
    
    print_status "Creating secret: $secret_name"
    
    if aws secretsmanager describe-secret --secret-id "$SECRET_PREFIX/$secret_name" --region $AWS_REGION &> /dev/null; then
        print_warning "Secret $secret_name already exists. Updating..."
        aws secretsmanager update-secret \
            --secret-id "$SECRET_PREFIX/$secret_name" \
            --secret-string "$secret_value" \
            --region $AWS_REGION
    else
        aws secretsmanager create-secret \
            --name "$SECRET_PREFIX/$secret_name" \
            --description "$description" \
            --secret-string "$secret_value" \
            --region $AWS_REGION
    fi
    
    print_success "Secret $secret_name created/updated successfully"
}

# Function to generate secure password
generate_password() {
    openssl rand -base64 32 | tr -d "=+/" | cut -c1-25
}

# Main setup function
main() {
    print_status "Setting up AWS Secrets Manager for Amesa Backend..."
    
    # Check if AWS CLI is configured
    if ! aws sts get-caller-identity &> /dev/null; then
        print_error "AWS CLI is not configured. Please run 'aws configure' first."
        exit 1
    fi
    
    print_warning "This script will create secrets in AWS Secrets Manager."
    print_warning "Make sure you have the necessary permissions."
    read -p "Do you want to continue? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status "Setup cancelled."
        exit 0
    fi
    
    # Generate secure passwords and keys
    print_status "Generating secure passwords and keys..."
    
    DB_PASSWORD=$(generate_password)
    JWT_SECRET=$(openssl rand -base64 64)
    QR_SECRET=$(openssl rand -base64 64)
    
    # Create secrets
    create_secret "db-password" "$DB_PASSWORD" "Aurora PostgreSQL database password"
    create_secret "jwt-secret" "$JWT_SECRET" "JWT signing secret key"
    create_secret "qr-code-secret" "$QR_SECRET" "QR code generation secret key"
    
    # Prompt for other secrets
    print_status "Please provide the following secrets:"
    
    read -p "SMTP Username: " SMTP_USERNAME
    read -s -p "SMTP Password: " SMTP_PASSWORD
    echo
    
    read -p "Stripe Publishable Key: " STRIPE_PUBLISHABLE
    read -s -p "Stripe Secret Key: " STRIPE_SECRET
    echo
    read -s -p "Stripe Webhook Secret: " STRIPE_WEBHOOK
    echo
    
    read -p "PayPal Client ID: " PAYPAL_CLIENT_ID
    read -s -p "PayPal Client Secret: " PAYPAL_CLIENT_SECRET
    echo
    
    read -p "AWS Access Key ID: " AWS_ACCESS_KEY
    read -s -p "AWS Secret Access Key: " AWS_SECRET_KEY
    echo
    
    # Create remaining secrets
    create_secret "smtp-username" "$SMTP_USERNAME" "SMTP server username"
    create_secret "smtp-password" "$SMTP_PASSWORD" "SMTP server password"
    create_secret "stripe-publishable-key" "$STRIPE_PUBLISHABLE" "Stripe publishable key"
    create_secret "stripe-secret-key" "$STRIPE_SECRET" "Stripe secret key"
    create_secret "stripe-webhook-secret" "$STRIPE_WEBHOOK" "Stripe webhook secret"
    create_secret "paypal-client-id" "$PAYPAL_CLIENT_ID" "PayPal client ID"
    create_secret "paypal-client-secret" "$PAYPAL_CLIENT_SECRET" "PayPal client secret"
    create_secret "aws-access-key-id" "$AWS_ACCESS_KEY" "AWS access key ID"
    create_secret "aws-secret-access-key" "$AWS_SECRET_KEY" "AWS secret access key"
    
    print_success "All secrets have been created successfully!"
    print_status "You can now proceed with the deployment using the deployment script."
    
    # Save the database password for reference
    echo "Database Password: $DB_PASSWORD" > db-password.txt
    print_warning "Database password saved to db-password.txt (keep this secure!)"
}

# Run main function
main "$@"
