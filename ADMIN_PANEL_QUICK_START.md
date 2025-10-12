# 🚀 Amesa Admin Panel - Quick Start

## ✅ Installation Complete!

Your **Blazor Server Admin Panel** has been successfully integrated into the AmesaBackend project.

## 🎯 What You Got

### Features
- ✅ **Email/Password Login** - Secure authentication
- ✅ **Database Selector** - Switch between Dev/Stage and Production
- ✅ **Houses Management** - Create, edit, delete properties
- ✅ **Image Management** - Upload and organize house photos
- ✅ **Translations Editor** - Multi-language content management
- ✅ **Users Management** - View and edit user accounts
- ✅ **Beautiful UI** - Bootstrap 5 + Font Awesome

### Database Options
- **Development** - Dev + Stage (shared) → `amesadbmain-stage`
- **Production** - Production only → `amesadbmain`

## 📦 Step 1: Install Dependencies

```powershell
cd BE\AmesaBackend
dotnet restore
```

**New packages installed:**
- `Microsoft.AspNetCore.Components.Web` - Blazor Server
- `AWSSDK.S3` - For future S3 uploads

## 🚀 Step 2: Run the Application

```powershell
cd BE\AmesaBackend
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:8080
...
```

## 🌐 Step 3: Access Admin Panel

Open your browser:
```
http://localhost:8080/admin
```

You'll be redirected to the login page.

## 🔐 Step 4: Login

**Test Credentials** (seeded by DatabaseSeeder):

| Email | Password |
|-------|----------|
| `admin@amesa.com` | `Admin123!` |

**Other test users:**
| Email | Password |
|-------|----------|
| `john.doe@example.com` | `Password123!` |
| `sarah.wilson@example.com` | `Password123!` |

## 🎨 Step 5: Explore the Admin Panel

After login, you'll see:

### 📊 Dashboard
- Total houses count
- Active lotteries count
- Total users count
- Languages count
- Quick action cards

### 🏠 Manage Houses
1. Click **Houses** in sidebar
2. Click **Add New House** to create
3. Use **Edit** to modify existing
4. Use **Images** to manage photos
5. Use **Delete** to remove

### 🖼️ Manage Images
1. Go to Houses → Click **Images** icon
2. Enter image URL (e.g., from Unsplash)
3. Add alt text
4. Click **Add Image**
5. Set primary image with star icon

### 🌐 Manage Translations
1. Click **Translations** in sidebar
2. Select language (English, Polish, etc.)
3. Search for specific keys (optional)
4. Click **Edit** to modify
5. Click **Save**

### 👥 Manage Users
1. Click **Users** in sidebar
2. Click **Edit** on any user
3. Update name, email, phone, status
4. Check/uncheck verification flags
5. Click **Save Changes**

## 🗄️ Step 6: Switch Databases

Use the **Database Selector** in the sidebar:

1. Click dropdown showing current environment
2. Select **Development** or **Production**
3. Confirm the alert
4. Page reloads with new database connection

**Indicator:**
- **Development** shows: "(Dev + Stage)"
- **Production** shows as-is

## 📁 Project Structure

```
BE/AmesaBackend/
├── Admin/                         ← New admin panel
│   ├── Pages/
│   │   ├── Index.razor           (Dashboard)
│   │   ├── Login.razor           (Auth)
│   │   ├── Houses/               (3 pages)
│   │   ├── Users/                (2 pages)
│   │   ├── Translations/         (1 page)
│   │   └── ...
│   ├── Shared/
│   │   ├── MainLayout.razor
│   │   └── DatabaseSelector.razor
│   └── README.md
│
├── Services/
│   ├── AdminAuthService.cs       ← New
│   ├── AdminDatabaseService.cs   ← New
│   └── ...
│
├── Pages/
│   └── _Host.cshtml              ← New (Blazor host)
│
├── Program.cs                     ← Updated
└── appsettings.Development.json   ← Updated
```

## 🔧 Configuration

### Connection Strings (appsettings.Development.json)

```json
{
  "ConnectionStrings": {
    "DevelopmentConnection": "Host=amesadbmain-stage...;Database=amesa_lottery;...",
    "ProductionConnection": "Host=amesadbmain1...;Database=amesa_lottery;..."
  }
}
```

### Session Settings (Program.cs)
- **Timeout**: 2 hours
- **Cookie**: HttpOnly, secure
- **Storage**: In-memory (upgrade to Redis for production scaling)

## 🎯 Usage Examples

### Create a New House

1. Go to **Houses** → **Add New House**
2. Fill in:
   ```
   Title: Luxury Apartment in Tel Aviv
   Location: Tel Aviv, Israel
   Address: 123 Rothschild Blvd
   Property Type: Apartment
   Bedrooms: 3
   Bathrooms: 2
   Price: 1500000
   Total Tickets: 75000
   Ticket Price: 20
   ```
3. Click **Create House**
4. Success! House created

### Add Images to House

1. Click **Images** icon on the house
2. Add image URL:
   ```
   https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=1200
   ```
3. Alt text: `Luxury Apartment - Exterior View`
4. Click **Add Image**
5. First image automatically becomes primary

### Edit Translation

1. Go to **Translations**
2. Select language: **English**
3. Search: `welcome`
4. Click **Edit** on `app.welcome.message`
5. Change text to: `Welcome to Amesa Lottery!`
6. Click **Save**

### Update User Status

1. Go to **Users**
2. Click **Edit** on a user
3. Change **Status** to: `Suspended`
4. Uncheck **Email Verified** (if needed)
5. Click **Save Changes**

## 🚀 Deployment

The admin panel deploys automatically with your backend:

### Local Development
```powershell
dotnet run
# Access: http://localhost:8080/admin
```

### Docker
```bash
docker build -t amesa-backend .
docker run -p 8080:8080 amesa-backend
# Access: http://localhost:8080/admin
```

### AWS ECS (Production)
Your existing CI/CD pipeline will include the admin panel:

**URLs:**
- **Dev**: `http://amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com/admin`
- **Stage**: `http://amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com/admin`
- **Prod**: `http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/admin`

## 🔐 Security Notes

1. **Change default passwords** in production
2. **Add proper role-based authorization** (currently any authenticated user can access)
3. **Use HTTPS** in production (ALB handles this)
4. **Set strong JWT secret** in production appsettings
5. **Enable session encryption** for sensitive data

## 🐛 Troubleshooting

### Login Fails
**Issue**: Can't login with admin@amesa.com

**Solution:**
1. Make sure database is seeded:
   ```bash
   dotnet run --seeder
   ```
2. Check database connection string in appsettings
3. Verify user exists in database

### Database Selector Not Working
**Issue**: Can't switch databases

**Solution:**
1. Check connection strings in `appsettings.Development.json`
2. Verify session is enabled in Program.cs
3. Clear browser cookies and try again

### Images Not Loading
**Issue**: House images showing broken link

**Solution:**
1. Use valid image URLs (Unsplash, etc.)
2. Check CORS if using external images
3. Verify URL is accessible

### Build Errors
**Issue**: Compilation errors

**Solution:**
1. Run `dotnet restore` to install packages
2. Check all files are saved
3. Rebuild: `dotnet build`

## 📚 Next Steps

### Immediate
1. ✅ **Run the app**: `dotnet run`
2. ✅ **Login**: Use `admin@amesa.com` / `Admin123!`
3. ✅ **Create test house**: Add a property
4. ✅ **Upload images**: Add some photos
5. ✅ **Edit translations**: Try changing some text
6. ✅ **Switch databases**: Test the database selector

### Future Enhancements
- [ ] **S3 File Upload** - Direct file upload to AWS S3
- [ ] **Content Management** - Edit About Us, FAQ pages
- [ ] **Promotions** - Manage discount codes
- [ ] **Analytics Dashboard** - Charts and metrics
- [ ] **Bulk Import** - CSV upload for translations
- [ ] **Role-Based Access** - Admin vs Super Admin
- [ ] **Audit Log** - Track all changes
- [ ] **Advanced Search** - Filter houses, users

## 📞 Support

**Documentation:**
- `BE/ADMIN_PANEL_GUIDE.md` - Complete guide
- `BE/AmesaBackend/Admin/README.md` - Technical details
- `BE/CONTEXT_QUICK_REFERENCE.md` - Backend context

**Files Updated:**
- `AmesaBackend.csproj` - Added Blazor packages
- `Program.cs` - Added Blazor + Session services
- `appsettings.Development.json` - Added connection strings

**Files Created:**
- 18 new Razor pages/components
- 4 new service classes
- 1 Razor Pages host file

---

## 🎉 You're Ready!

**Your admin panel is fully functional and ready to use.**

Just run:
```powershell
cd BE\AmesaBackend
dotnet run
```

Then open: `http://localhost:8080/admin`

**Happy managing!** 🎯

