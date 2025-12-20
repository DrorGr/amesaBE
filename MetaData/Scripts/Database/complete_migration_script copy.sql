-- ============================================================================
-- Complete Database Migration Script for Lottery Component Fixes
-- ============================================================================
-- This script contains all database migrations required for the lottery
-- component critical fixes implementation.
--
-- IMPORTANT: Review and execute this script manually on your database.
-- Run each section in the appropriate database/schema.
--
-- Prerequisites:
--   1. PostgreSQL 12+ with UUID extension enabled
--   2. Appropriate database schemas already created
--   3. Sufficient privileges to create tables, indexes, and constraints
--
-- Execution Order:
--   1. Run Lottery Service migrations (Section 1) on amesa_lottery_db
--   2. Run Notification Service migrations (Section 2) on amesa_notification_db
--   3. Verify all constraints and indexes are created
-- ============================================================================

-- ============================================================================
-- SECTION 1: LOTTERY SERVICE MIGRATIONS
-- Database: amesa_lottery_db
-- Schema: amesa_lottery
-- ============================================================================
-- NOTE: Run this section while connected to amesa_lottery_db
-- Command: psql -h YOUR_HOST -U postgres -d amesa_lottery_db -f this_file.sql

-- Set search path to lottery schema
SET search_path TO amesa_lottery, public;

-- ----------------------------------------------------------------------------
-- 1.1: Create promotion_usage_audit table
-- Purpose: Track promotion usage when discount is applied but usage tracking fails
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS amesa_lottery.promotion_usage_audit (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id UUID NOT NULL,
    user_id UUID NOT NULL,
    promotion_code VARCHAR(50) NOT NULL,
    discount_amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMP,
    resolution_notes VARCHAR(1000),
    resolved_by_user_id UUID
);

-- Indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_promotion_audit_transaction 
    ON amesa_lottery.promotion_usage_audit(transaction_id);
    
CREATE INDEX IF NOT EXISTS idx_promotion_audit_user 
    ON amesa_lottery.promotion_usage_audit(user_id);
    
CREATE INDEX IF NOT EXISTS idx_promotion_audit_status 
    ON amesa_lottery.promotion_usage_audit(status);
    
CREATE INDEX IF NOT EXISTS idx_promotion_audit_created 
    ON amesa_lottery.promotion_usage_audit(created_at);
    
CREATE INDEX IF NOT EXISTS idx_promotion_audit_status_created 
    ON amesa_lottery.promotion_usage_audit(status, created_at);

-- Add comments
COMMENT ON TABLE amesa_lottery.promotion_usage_audit IS 
    'Audit log for promotion usage tracking failures. Created when discount is applied but usage cannot be recorded.';

COMMENT ON COLUMN amesa_lottery.promotion_usage_audit.status IS 
    'Status: Pending, Resolved, Reversed';

COMMENT ON COLUMN amesa_lottery.promotion_usage_audit.transaction_id IS 
    'Reference to payment transaction where promotion was used';

COMMENT ON COLUMN amesa_lottery.promotion_usage_audit.discount_amount IS 
    'Amount of discount applied (in currency units)';

-- ----------------------------------------------------------------------------
-- 1.2: Add unique constraint on ticket numbers per house
-- Purpose: Prevent duplicate ticket numbers within the same house
-- Note: This constraint is also defined in EF Core model configuration
-- ----------------------------------------------------------------------------

-- First, check for existing duplicates and report them
DO $$
DECLARE
    duplicate_count INTEGER;
    duplicate_records RECORD;
BEGIN
    -- Count duplicates
    SELECT COUNT(*) INTO duplicate_count
    FROM (
        SELECT "HouseId", "TicketNumber", COUNT(*) as cnt
        FROM amesa_lottery.lottery_tickets
        GROUP BY "HouseId", "TicketNumber"
        HAVING COUNT(*) > 1
    ) duplicates;
    
    IF duplicate_count > 0 THEN
        RAISE NOTICE '⚠️  WARNING: Found % duplicate ticket number groups. Please resolve before applying constraint.', duplicate_count;
        RAISE NOTICE '   Review the following duplicates:';
        
        FOR duplicate_records IN
            SELECT house_id, ticket_number, COUNT(*) as cnt
            FROM amesa_lottery.lottery_tickets
            GROUP BY house_id, ticket_number
            HAVING COUNT(*) > 1
            LIMIT 10
        LOOP
            RAISE NOTICE '   House ID: %, Ticket Number: %, Count: %', 
                duplicate_records.house_id, 
                duplicate_records.ticket_number, 
                duplicate_records.cnt;
        END LOOP;
        
        RAISE EXCEPTION 'Cannot proceed: Duplicate ticket numbers exist. Please resolve duplicates first.';
    ELSE
        RAISE NOTICE '✅ No duplicate ticket numbers found. Safe to proceed with constraint.';
    END IF;
END $$;

-- Add unique constraint (only if no duplicates exist)
-- This will be created automatically by EF Core migrations, but can be applied manually:
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_constraint 
        WHERE conname = 'uq_house_ticket_number' 
        AND conrelid = 'amesa_lottery.lottery_tickets'::regclass
    ) THEN
        ALTER TABLE amesa_lottery.lottery_tickets
        ADD CONSTRAINT uq_house_ticket_number UNIQUE ("HouseId", "TicketNumber");
        
        RAISE NOTICE '✅ Unique constraint uq_house_ticket_number created successfully.';
    ELSE
        RAISE NOTICE 'ℹ️  Unique constraint uq_house_ticket_number already exists.';
    END IF;
END $$;

-- Add comment on constraint
COMMENT ON CONSTRAINT uq_house_ticket_number ON amesa_lottery.lottery_tickets IS 
    'Ensures ticket numbers are unique within each house to prevent race conditions in ticket generation.';

-- ============================================================================
-- SECTION 2: NOTIFICATION SERVICE MIGRATIONS
-- Database: amesa_notification_db
-- Schema: amesa_notification
-- ============================================================================
-- NOTE: Run this section while connected to amesa_notification_db
-- Command: psql -h YOUR_HOST -U postgres -d amesa_notification_db -f this_file.sql

-- Set search path to notification schema
SET search_path TO amesa_notification, public;

-- ----------------------------------------------------------------------------
-- 2.1: Create device_registrations table
-- Purpose: Store device tokens for push notifications (iOS, Android, Web)
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS amesa_notification.device_registrations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    device_token VARCHAR(255) NOT NULL,
    platform VARCHAR(50) NOT NULL,
    device_id VARCHAR(255),
    device_name VARCHAR(255),
    app_version VARCHAR(50),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_used_at TIMESTAMP,
    CONSTRAINT uq_user_device_token UNIQUE (user_id, device_token)
);

-- Indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_device_reg_user 
    ON amesa_notification.device_registrations(user_id);
    
CREATE INDEX IF NOT EXISTS idx_device_reg_token 
    ON amesa_notification.device_registrations(device_token);
    
CREATE INDEX IF NOT EXISTS idx_device_reg_user_platform 
    ON amesa_notification.device_registrations(user_id, platform);
    
CREATE INDEX IF NOT EXISTS idx_device_reg_active 
    ON amesa_notification.device_registrations(user_id, is_active) 
    WHERE is_active = true;

-- Add comments
COMMENT ON TABLE amesa_notification.device_registrations IS 
    'Device registration for push notifications. Stores device tokens for iOS, Android, and Web platforms.';

COMMENT ON COLUMN amesa_notification.device_registrations.platform IS 
    'Platform: iOS, Android, Web';

COMMENT ON COLUMN amesa_notification.device_registrations.device_token IS 
    'Platform-specific device token (APNs token for iOS, FCM token for Android, etc.)';

COMMENT ON COLUMN amesa_notification.device_registrations.is_active IS 
    'Whether the device registration is currently active. Inactive devices will not receive push notifications.';

COMMENT ON COLUMN amesa_notification.device_registrations.last_used_at IS 
    'Timestamp when the device token was last successfully used to send a notification.';

-- ============================================================================
-- SECTION 3: VERIFICATION QUERIES
-- Run these queries to verify the migrations were successful
-- ============================================================================

-- Verify Lottery Service migrations
\c amesa_lottery_db;
SET search_path TO amesa_lottery, public;

DO $$
BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'LOTTERY SERVICE MIGRATION VERIFICATION';
    RAISE NOTICE '============================================================================';
    
    -- Check promotion_usage_audit table
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'amesa_lottery' AND table_name = 'promotion_usage_audit') THEN
        RAISE NOTICE '✅ promotion_usage_audit table exists';
    ELSE
        RAISE NOTICE '❌ promotion_usage_audit table NOT found';
    END IF;
    
    -- Check unique constraint on lottery_tickets
    IF EXISTS (
        SELECT 1 
        FROM pg_constraint 
        WHERE conname = 'uq_house_ticket_number' 
        AND conrelid = 'amesa_lottery.lottery_tickets'::regclass
    ) THEN
        RAISE NOTICE '✅ Unique constraint uq_house_ticket_number exists on lottery_tickets';
    ELSE
        RAISE NOTICE '❌ Unique constraint uq_house_ticket_number NOT found';
    END IF;
    
    -- Count indexes on promotion_usage_audit
    DECLARE
        idx_count INTEGER;
    BEGIN
        SELECT COUNT(*) INTO idx_count
        FROM pg_indexes
        WHERE schemaname = 'amesa_lottery'
        AND tablename = 'promotion_usage_audit';
        
        RAISE NOTICE '✅ Found % indexes on promotion_usage_audit table', idx_count;
    END;
END $$;

-- Verify Notification Service migrations
-- NOTE: Run verification while connected to amesa_notification_db
SET search_path TO amesa_notification, public;

DO $$
BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'NOTIFICATION SERVICE MIGRATION VERIFICATION';
    RAISE NOTICE '============================================================================';
    
    -- Check device_registrations table
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'amesa_notification' AND table_name = 'device_registrations') THEN
        RAISE NOTICE '✅ device_registrations table exists';
    ELSE
        RAISE NOTICE '❌ device_registrations table NOT found';
    END IF;
    
    -- Check unique constraint on device_registrations
    IF EXISTS (
        SELECT 1 
        FROM pg_constraint 
        WHERE conname = 'uq_user_device_token' 
        AND conrelid = 'amesa_notification.device_registrations'::regclass
    ) THEN
        RAISE NOTICE '✅ Unique constraint uq_user_device_token exists on device_registrations';
    ELSE
        RAISE NOTICE '❌ Unique constraint uq_user_device_token NOT found';
    END IF;
    
    -- Count indexes on device_registrations
    DECLARE
        idx_count INTEGER;
    BEGIN
        SELECT COUNT(*) INTO idx_count
        FROM pg_indexes
        WHERE schemaname = 'amesa_notification'
        AND tablename = 'device_registrations';
        
        RAISE NOTICE '✅ Found % indexes on device_registrations table', idx_count;
    END;
END $$;

-- ============================================================================
-- MIGRATION COMPLETE
-- ============================================================================

DO $$
BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE '✅ Database migrations completed successfully!';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE '';
    RAISE NOTICE 'Next steps:';
    RAISE NOTICE '  1. Verify all tables and constraints were created correctly';
    RAISE NOTICE '  2. Update application connection strings if needed';
    RAISE NOTICE '  3. Run EF Core migrations to sync model with database (optional)';
    RAISE NOTICE '  4. Test the application to ensure everything works correctly';
    RAISE NOTICE '';
END $$;


