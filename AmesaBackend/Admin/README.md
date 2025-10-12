# Amesa Admin Panel

**Blazor Server Admin Panel** for managing the Amesa Lottery Platform.

## 🎯 Features

- ✅ **Email/Password Authentication** - Secure admin login
- ✅ **Database Selector** - Switch between Development/Staging and Production databases
- ✅ **Houses Management** - Create, edit, and manage lottery properties
- ✅ **Image Management** - Upload and manage house images
- ✅ **Translations Editor** - Edit multi-language content
- ✅ **Users Management** - View and edit user accounts
- ✅ **Dashboard** - Overview stats and quick actions

## 📦 Technology Stack

- **Blazor Server** (.NET 8.0) - Server-side rendering
- **Bootstrap 5** - Modern UI framework
- **Font Awesome 6** - Icons
- **Entity Framework Core** - Database access
- **Session State** - Authentication and database selection

## 🚀 Getting Started

### 1. Access the Admin Panel

Navigate to: `http://localhost:8080/admin`

### 2. Login

**Default Test Credentials:**
- **Email**: `admin@amesa.com`
- **Password**: `Admin123!`

These credentials are seeded in the database by `DatabaseSeeder`.

### 3. Select Database

After login, use the **Database Selector** in the sidebar to choose:
- **Development** (Dev + Stage shared database)
- **Production** (Production database)

The selection is stored in your session and persists across page navigation.

## 📁 Project Structure

```
BE/AmesaBackend/Admin/
├── _Imports.razor           # Global imports
├── App.razor                # Root component
├── README.md                # This file
│
├── Pages/
│   ├── Index.razor          # Dashboard
│   ├── Login.razor          # Login page
│   ├── Logout.razor         # Logout handler
│   │
│   ├── Houses/
│   │   ├── Index.razor      # Houses list
│   │   ├── Create.razor     # Create house
│   │   ├── Edit.razor       # Edit house
│   │   └── Images.razor     # Manage house images
│   │
│   ├── Users/
│   │   ├── Index.razor      # Users list
│   │   └── Edit.razor       # Edit user
│   │
│   ├── Translations/
│   │   └── Index.razor      # Edit translations
│   │
│   ├── Content/
│   │   └── Index.razor      # Content (coming soon)
│   │
│   └── Promotions/
│       └── Index.razor      # Promotions (coming soon)
│
└── Shared/
    ├── MainLayout.razor     # Main layout with sidebar
    └── DatabaseSelector.razor  # Database selector component
```

## 🔧 Services

### AdminAuthService
Handles authentication using email/password:
- `AuthenticateAsync(email, password)` - Login
- `IsAuthenticated()` - Check authentication status
- `GetCurrentAdminEmail()` - Get logged-in admin email
- `SignOutAsync()` - Logout

### AdminDatabaseService
Manages database selection:
- `GetCurrentEnvironment()` - Get selected environment
- `SetEnvironment(environment)` - Set environment
- `GetConnectionString(environment)` - Get connection string
- `GetAvailableEnvironments()` - List available environments

## 🔐 Security

- **Session-based authentication** - Secure cookie-based sessions
- **2-hour timeout** - Sessions expire after 2 hours of inactivity
- **Direct database access** - No API layer overhead
- **Password verification** - BCrypt password hashing

## 🎨 UI Components

### Sidebar Navigation
- Dashboard
- Houses (with sub-pages)
- Translations
- Users
- Content (placeholder)
- Promotions (placeholder)
- Logout

### Database Selector
Dropdown in sidebar showing:
- Available environments
- Current selection
- Visual indicator (Dev + Stage) or (Production)

### Cards & Forms
- Bootstrap 5 cards for content containers
- Responsive forms with validation
- Action buttons with icons
- Status badges

## 📝 Usage Examples

### Managing Houses

1. Navigate to **Houses** in sidebar
2. Click **Add New House** to create
3. Fill in property details:
   - Title, location, description
   - Bedrooms, bathrooms, size
   - Price and lottery configuration
4. Click **Create House**
5. Manage images via **Images** button

### Editing Translations

1. Navigate to **Translations**
2. Select language from top buttons
3. Search for specific keys (optional)
4. Click **Edit** on any translation
5. Modify the text
6. Click **Save**

### Managing Users

1. Navigate to **Users**
2. View all registered users
3. Click **Edit** on any user
4. Update user details, status, verification
5. Click **Save Changes**

## 🔄 Database Switching

The database selector allows switching between:

1. **Development** - Points to `amesadbmain-stage` (shared with staging)
2. **Production** - Points to `amesadbmain` (production only)

**Important**: Page reloads after switching to apply new connection.

## 🚀 Deployment

The admin panel is **bundled with the backend**:

```bash
cd BE/AmesaBackend
dotnet build
dotnet run
```

Access at: `http://localhost:8080/admin`

### Production Deployment

The admin panel is deployed automatically with the backend:

1. **ECS Deployment** - Included in Docker container
2. **URL**: `http://amesa-backend-alb.../admin`
3. **Authentication Required** - Must login to access

## 🛠️ Customization

### Add New Page

1. Create `Admin/Pages/MyPage.razor`
2. Add `@page "/admin/mypage"`
3. Add link in `Shared/MainLayout.razor`

### Add New Service

1. Create interface `Services/IMyService.cs`
2. Create implementation `Services/MyService.cs`
3. Register in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<IMyService, MyService>();
   ```

## 📊 Future Enhancements

- [ ] **Content Management** - CMS for About Us, FAQ pages
- [ ] **Promotions** - Manage discount codes
- [ ] **Analytics Dashboard** - Charts and metrics
- [ ] **Bulk Import/Export** - CSV upload for translations
- [ ] **S3 Image Upload** - Direct file upload to S3
- [ ] **Role-Based Access** - Different admin permission levels
- [ ] **Audit Log** - Track all admin actions
- [ ] **Real-time Updates** - SignalR for live data

## 🐛 Troubleshooting

### Can't Login
- Check database connection
- Verify admin user exists (run seeder)
- Check session cookie is enabled

### Database Selector Not Working
- Session must be enabled in `Program.cs`
- Check connection strings in `appsettings.json`

### Pages Not Loading
- Ensure Blazor Server is registered in `Program.cs`
- Check `_Host.cshtml` exists in `Pages/`
- Verify routes in `MapFallbackToPage()`

## 📞 Support

For issues or questions:
- Check BE documentation: `BE/README.md`
- Review context: `BE/CONTEXT_QUICK_REFERENCE.md`
- Check logs in `BE/logs/`

---

**Built with** ❤️ **using Blazor Server**

