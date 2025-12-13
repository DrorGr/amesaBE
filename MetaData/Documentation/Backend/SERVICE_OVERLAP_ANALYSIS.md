# Service Overlap Analysis

**Date**: 2025-01-27

## Summary

After analyzing all services in the monolith, here are the findings:

## ✅ No True Duplicates Found

### 1. **AuthService vs AdminAuthService** - NOT Duplicates
- **AuthService**: Full user authentication system
  - User registration, login, OAuth (Google/Meta)
  - JWT token generation and refresh
  - Password reset, email/phone verification
  - User session management
  - **Purpose**: End-user authentication

- **AdminAuthService**: Admin panel authentication
  - Simple email/password authentication
  - Session management for Blazor Server admin panel
  - Static in-memory storage for admin sessions
  - **Purpose**: Admin-only authentication

**Conclusion**: These serve different purposes and should remain separate.

---

### 2. **NotificationService vs EmailService** - Architectural Overlap (Not Duplicates)

- **EmailService**: Low-level email sending service
  - Sends emails via SMTP (MailKit)
  - Handles email templates and formatting
  - Methods: `SendEmailVerificationAsync`, `SendPasswordResetAsync`, `SendLotteryWinnerNotificationAsync`, etc.

- **NotificationService**: Higher-level notification orchestration
  - Creates in-app notifications (saves to database)
  - **Calls EmailService** to send emails
  - Methods: `SendNotificationAsync`, `SendLotteryWinnerNotificationAsync`, etc.

**Current Architecture**:
```
NotificationService
  ├── Creates UserNotification (database record)
  └── Calls EmailService.SendLotteryWinnerNotificationAsync()
```

**Overlap Issue**:
- Both services have methods with similar names (`SendLotteryWinnerNotificationAsync`)
- NotificationService is a wrapper around EmailService
- This creates confusion but is intentional (separation of concerns)

**Recommendation for Microservices**:
- **Option 1**: Keep as-is (Notification Service calls Email Service via HTTP/EventBridge)
- **Option 2**: Consolidate - Make EmailService part of Notification Service
- **Option 3**: Make EmailService a shared utility used by Notification Service

**Recommended**: **Option 1** - Keep separate services but have Notification Service call Email Service asynchronously via EventBridge events.

---

## Service Responsibilities Summary

| Service | Primary Responsibility | Overlaps With | Status |
|---------|----------------------|---------------|--------|
| **AuthService** | User authentication (register, login, OAuth, JWT) | None | ✅ Unique |
| **AdminAuthService** | Admin panel authentication | None | ✅ Unique |
| **UserService** | User profile management | None | ✅ Unique |
| **EmailService** | Email sending (SMTP) | NotificationService (used by it) | ✅ Unique |
| **NotificationService** | Notification orchestration (in-app + email) | EmailService (uses it) | ✅ Unique |
| **ContentService** | Content management (translations, articles) | None | ✅ Unique |
| **PaymentService** | Payment processing | None | ✅ Unique |
| **LotteryService** | Lottery operations (houses, tickets, draws) | None | ✅ Unique |
| **QRCodeService** | QR code generation | None | ✅ Unique |
| **AnalyticsService** | Analytics tracking | None | ✅ Unique |
| **FileService** | File upload/storage | None | ✅ Unique |
| **LotteryDrawService** | Background lottery draw processing | None | ✅ Unique |
| **AdminDatabaseService** | Admin database operations | None | ✅ Unique |

---

## Microservices Architecture Recommendations

### Current Plan (8 Services):
1. **Auth Service** - AuthService, UserService, AdminAuthService ✅
2. **Content Service** - ContentService ✅
3. **Notification Service** - NotificationService, EmailService ✅
4. **Payment Service** - PaymentService ✅
5. **Lottery Service** - LotteryService, LotteryDrawService, FileService ✅
6. **Lottery Results Service** - QRCodeService ✅
7. **Analytics Service** - AnalyticsService ✅
8. **Admin Service** - AdminDatabaseService, Blazor Server admin panel ✅

### Communication Pattern:
```
Notification Service
  └── Publishes EventBridge Event: "EmailRequested"
      └── Email Service (or Lambda)
          └── Sends Email via SMTP
```

**OR** (if EmailService is part of Notification Service):
```
Notification Service
  ├── Creates in-app notification
  └── Sends email directly (EmailService as internal component)
```

---

## Conclusion

✅ **No duplicate services found** - All services have distinct responsibilities.

⚠️ **Architectural consideration**: NotificationService and EmailService have a dependency relationship that should be handled via EventBridge in the microservices architecture.

**Recommendation**: Proceed with the current microservices plan. The Notification Service should publish events to EventBridge, and a separate Email Service (or Lambda function) should consume these events to send emails.

