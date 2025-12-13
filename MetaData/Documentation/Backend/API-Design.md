# Amesa Lottery Platform - API Design

## Overview
This document outlines the RESTful API design for the Amesa Lottery Platform backend built with .NET Core and PostgreSQL.

## Base URL
```
Production: https://api.amesa.com/v1
Development: https://localhost:5001/api/v1
```

## Authentication
- **JWT Bearer Token** authentication
- **OAuth 2.0** integration for social logins (Google, Meta, Apple)
- **Refresh Token** mechanism for long-term sessions

## API Endpoints

### 1. Authentication & User Management

#### POST /auth/register
Register a new user account
```json
{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "dateOfBirth": "1990-01-01",
  "gender": "male",
  "phone": "+1234567890",
  "authProvider": "email"
}
```

#### POST /auth/login
Authenticate user
```json
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

#### POST /auth/refresh
Refresh access token
```json
{
  "refreshToken": "refresh_token_here"
}
```

#### POST /auth/logout
Logout user (invalidate tokens)

#### POST /auth/forgot-password
Request password reset
```json
{
  "email": "john@example.com"
}
```

#### POST /auth/reset-password
Reset password with token
```json
{
  "token": "reset_token_here",
  "newPassword": "NewSecurePass123!"
}
```

#### POST /auth/verify-email
Verify email address
```json
{
  "token": "verification_token_here"
}
```

#### POST /auth/verify-phone
Verify phone number
```json
{
  "phone": "+1234567890",
  "code": "123456"
}
```

### 2. User Profile Management

#### GET /users/profile
Get current user profile

#### PUT /users/profile
Update user profile
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "dateOfBirth": "1990-01-01",
  "gender": "male",
  "preferredLanguage": "en",
  "timezone": "UTC"
}
```

#### GET /users/addresses
Get user addresses

#### POST /users/addresses
Add new address
```json
{
  "type": "home",
  "country": "United States",
  "city": "New York",
  "street": "123 Main St",
  "houseNumber": "123",
  "zipCode": "10001",
  "isPrimary": true
}
```

#### PUT /users/addresses/{id}
Update address

#### DELETE /users/addresses/{id}
Delete address

#### GET /users/phones
Get user phone numbers

#### POST /users/phones
Add phone number
```json
{
  "phoneNumber": "+1234567890",
  "isPrimary": true
}
```

#### POST /users/phones/{id}/verify
Verify phone number

#### DELETE /users/phones/{id}
Delete phone number

### 3. Identity Verification

#### POST /users/identity/upload
Upload identity documents
```json
{
  "documentType": "passport",
  "documentNumber": "A1234567",
  "frontImage": "base64_encoded_image",
  "backImage": "base64_encoded_image",
  "selfieImage": "base64_encoded_image"
}
```

#### GET /users/identity/status
Get verification status

### 4. Lottery System

#### GET /houses
Get all houses with pagination and filtering
```
Query Parameters:
- page: 1
- limit: 20
- status: active|upcoming|ended
- minPrice: 100000
- maxPrice: 1000000
- location: "New York"
- bedrooms: 2
- bathrooms: 2
```

#### GET /houses/{id}
Get house details by ID

#### GET /houses/{id}/images
Get house images

#### GET /houses/{id}/tickets
Get available tickets for house

#### POST /houses/{id}/tickets/purchase
Purchase lottery ticket
```json
{
  "quantity": 1,
  "paymentMethodId": "payment_method_uuid",
  "promotionCode": "SAVE10"
}
```

#### GET /users/tickets
Get user's lottery tickets
```
Query Parameters:
- page: 1
- limit: 20
- status: active|winner|refunded
- houseId: uuid
```

#### GET /users/tickets/{id}
Get specific ticket details

### 5. Payment System

#### GET /payments/methods
Get user payment methods

#### POST /payments/methods
Add payment method
```json
{
  "type": "credit_card",
  "provider": "stripe",
  "cardNumber": "4111111111111111",
  "expMonth": 12,
  "expYear": 2025,
  "cvv": "123",
  "cardholderName": "John Doe"
}
```

#### PUT /payments/methods/{id}
Update payment method

#### DELETE /payments/methods/{id}
Delete payment method

#### GET /payments/transactions
Get user transactions
```
Query Parameters:
- page: 1
- limit: 20
- type: ticket_purchase|refund|withdrawal
- status: completed|pending|failed
- startDate: 2024-01-01
- endDate: 2024-12-31
```

#### GET /payments/transactions/{id}
Get transaction details

### 6. Lottery Draws & Results

#### GET /draws
Get lottery draws
```
Query Parameters:
- page: 1
- limit: 20
- status: pending|completed|failed
- houseId: uuid
```

#### GET /draws/{id}
Get draw details

#### GET /houses/{id}/draw
Get house draw information

#### POST /admin/draws/{id}/conduct
Conduct lottery draw (Admin only)

### 7. Promotions & Rewards

#### GET /promotions
Get active promotions
```
Query Parameters:
- type: discount|bonus|free_tickets
- applicable: true
```

#### POST /promotions/validate
Validate promotion code
```json
{
  "code": "SAVE10",
  "houseId": "uuid",
  "amount": 100.00
}
```

#### GET /users/promotions
Get user's used promotions

### 8. Content Management

#### GET /content
Get content pages
```
Query Parameters:
- category: general|lottery-rules|help-support
- status: published
- language: en
```

#### GET /content/{slug}
Get content by slug

#### GET /content/categories
Get content categories

### 9. Notifications

#### GET /notifications
Get user notifications
```
Query Parameters:
- page: 1
- limit: 20
- type: email|sms|push|in_app
- isRead: true|false
```

#### PUT /notifications/{id}/read
Mark notification as read

#### PUT /notifications/read-all
Mark all notifications as read

#### GET /notifications/preferences
Get notification preferences

#### PUT /notifications/preferences
Update notification preferences
```json
{
  "email": {
    "lotteryUpdates": true,
    "promotions": true,
    "accountActivity": false
  },
  "sms": {
    "lotteryUpdates": false,
    "promotions": false
  },
  "push": {
    "lotteryUpdates": true,
    "promotions": true
  }
}
```

### 10. Analytics & Reporting

#### GET /analytics/dashboard
Get user dashboard analytics
```json
{
  "totalTicketsPurchased": 25,
  "totalSpent": 1250.00,
  "winnings": 0.00,
  "activeLotteries": 3,
  "favoriteLocations": ["New York", "Los Angeles"],
  "monthlySpending": [
    {"month": "2024-01", "amount": 500.00},
    {"month": "2024-02", "amount": 750.00}
  ]
}
```

#### GET /analytics/lottery-stats
Get lottery statistics
```
Query Parameters:
- houseId: uuid
- period: 30d|90d|1y
```

### 11. Admin Endpoints

#### GET /admin/users
Get all users (Admin only)
```
Query Parameters:
- page: 1
- limit: 50
- status: active|suspended|banned
- verificationStatus: fully_verified|unverified
- search: "john"
```

#### PUT /admin/users/{id}/status
Update user status (Admin only)
```json
{
  "status": "suspended",
  "reason": "Violation of terms"
}
```

#### GET /admin/houses
Get all houses (Admin only)

#### POST /admin/houses
Create new house (Admin only)
```json
{
  "title": "Luxury Downtown Condo",
  "description": "Beautiful 3-bedroom condo...",
  "price": 750000,
  "location": "Downtown, City Center",
  "bedrooms": 3,
  "bathrooms": 2,
  "squareFeet": 1800,
  "totalTickets": 2000,
  "ticketPrice": 75.00,
  "lotteryEndDate": "2024-12-31T23:59:59Z",
  "minimumParticipationPercentage": 75.00
}
```

#### PUT /admin/houses/{id}
Update house (Admin only)

#### DELETE /admin/houses/{id}
Delete house (Admin only)

#### GET /admin/transactions
Get all transactions (Admin only)

#### GET /admin/analytics/overview
Get platform analytics (Admin only)

## Response Format

### Success Response
```json
{
  "success": true,
  "data": {
    // Response data
  },
  "message": "Operation completed successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Error Response
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input data",
    "details": [
      {
        "field": "email",
        "message": "Email is required"
      }
    ]
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Paginated Response
```json
{
  "success": true,
  "data": {
    "items": [
      // Array of items
    ],
    "pagination": {
      "page": 1,
      "limit": 20,
      "total": 150,
      "totalPages": 8,
      "hasNext": true,
      "hasPrevious": false
    }
  }
}
```

## HTTP Status Codes

- `200 OK` - Successful GET, PUT, PATCH
- `201 Created` - Successful POST
- `204 No Content` - Successful DELETE
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource conflict (e.g., duplicate email)
- `422 Unprocessable Entity` - Validation errors
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error

## Rate Limiting

- **Authentication endpoints**: 5 requests per minute
- **General API**: 100 requests per minute per user
- **File uploads**: 10 requests per minute
- **Admin endpoints**: 200 requests per minute

## WebSocket Events

### Real-time Updates
```
ws://api.amesa.com/ws
```

#### Events:
- `lottery.update` - Lottery status changes
- `ticket.purchased` - New ticket purchased
- `draw.completed` - Lottery draw completed
- `notification.new` - New notification
- `user.status` - User status changes

## Security Considerations

1. **HTTPS Only** - All API calls must use HTTPS
2. **JWT Expiration** - Access tokens expire in 15 minutes
3. **Refresh Tokens** - Expire in 7 days
4. **Input Validation** - All inputs are validated and sanitized
5. **SQL Injection Prevention** - Using parameterized queries
6. **XSS Protection** - Output encoding and CSP headers
7. **Rate Limiting** - Prevent abuse and DoS attacks
8. **CORS Configuration** - Restricted to allowed origins
9. **Audit Logging** - All sensitive operations are logged
10. **Data Encryption** - Sensitive data encrypted at rest

## API Versioning

- **URL Versioning**: `/api/v1/`
- **Header Versioning**: `Accept: application/vnd.amesa.v1+json`
- **Backward Compatibility**: Maintained for at least 12 months
- **Deprecation Notice**: 6 months advance notice

## Testing

### Test Environment
```
Base URL: https://api-test.amesa.com/v1
Test Database: Separate test database
Test Cards: Use Stripe test card numbers
```

### Postman Collection
- Complete API collection available
- Environment variables for different stages
- Automated tests for critical endpoints

## Documentation

- **Swagger/OpenAPI**: Available at `/swagger`
- **Postman Collection**: Import from repository
- **SDK Libraries**: Available for JavaScript, Python, PHP
- **Integration Guides**: Step-by-step integration examples
