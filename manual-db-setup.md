# Manual Database Setup Instructions

## Step 1: Create the Database

Connect to your PostgreSQL server and create the `amesa_lottery` database:

### Option 1: Using psql command line
```bash
psql -h amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com -U dror -d postgres
```

Then run:
```sql
CREATE DATABASE amesa_lottery;
\q
```

### Option 2: Using any PostgreSQL client (pgAdmin, DBeaver, etc.)
Connect to your PostgreSQL server and run:
```sql
CREATE DATABASE amesa_lottery;
```

## Step 2: Run the Database Seeder

Once the database is created, run the seeder:

```powershell
cd backend
$env:DB_CONNECTION_STRING = "Host=amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=dror;Password=aAXa406L6qdqfTU6o8vr;Port=5432;"
dotnet run --project AmesaBackend/AmesaBackend.csproj --configuration Release -- --seeder
```

## What the Seeder Will Create

The database seeder will populate your database with:

- **5 Languages**: English, Hebrew, Arabic, Spanish, French
- **5 Users**: Admin user and 4 sample users with different verification statuses
- **4 Houses**: Luxury properties with images and lottery details
- **Multiple Lottery Tickets**: Distributed across users and houses
- **Lottery Draws & Results**: Complete lottery history with winners
- **18 Translations**: Multi-language support (3 languages × 6 keys)
- **3 Content Articles**: How it works, About us, FAQ
- **3 Promotional Campaigns**: Welcome bonus, bulk discounts, house-specific offers
- **8 System Settings**: Configuration for the application

## Verification

After seeding, you can verify the data by connecting to the database and running:

```sql
-- Check users
SELECT COUNT(*) FROM users;

-- Check houses
SELECT COUNT(*) FROM houses;

-- Check lottery tickets
SELECT COUNT(*) FROM lottery_tickets;

-- Check translations
SELECT COUNT(*) FROM translations;
```

Your Amesa Lottery database will be fully populated and ready to use!
