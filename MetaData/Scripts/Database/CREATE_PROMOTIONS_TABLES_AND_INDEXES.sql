-- ============================================================
-- Create Promotions Tables and Indexes
-- ============================================================
-- Created: 2025-01-25
-- Purpose: Create promotions and user_promotions tables, then add performance indexes
-- 
-- INSTRUCTIONS:
-- 1. Connect to your production database
-- 2. Copy and paste this entire script
-- 3. Execute the script
-- 4. Verify tables and indexes were created using the verification queries at the end
-- ============================================================

-- ============================================================
-- STEP 1: CREATE TABLES
-- ============================================================

-- ============================================================
-- IMPORTANT: Database Schema Information
-- ============================================================
-- - users table is in amesa_auth schema with PascalCase columns (Id, Username, etc.)
-- - promotions table should be in amesa_admin schema (matches C# model)
-- - transactions table is in amesa_payment schema
-- - Foreign keys must be schema-qualified
-- ============================================================

-- Create schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS amesa_admin;

-- Promotions table (in amesa_admin schema)
CREATE TABLE IF NOT EXISTS amesa_admin.promotions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    type VARCHAR(50) NOT NULL, -- discount, bonus, free_tickets
    value DECIMAL(10,2),
    value_type VARCHAR(20), -- percentage, fixed_amount
    code VARCHAR(50) UNIQUE,
    is_active BOOLEAN DEFAULT TRUE,
    start_date TIMESTAMP WITH TIME ZONE,
    end_date TIMESTAMP WITH TIME ZONE,
    usage_limit INTEGER,
    usage_count INTEGER DEFAULT 0,
    min_purchase_amount DECIMAL(10,2),
    max_discount_amount DECIMAL(10,2),
    applicable_houses UUID[], -- Array of house IDs
    created_by UUID REFERENCES amesa_auth.users("Id"), -- Schema-qualified reference to amesa_auth.users.Id
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User promotions (tracking usage) - in amesa_admin schema
CREATE TABLE IF NOT EXISTS amesa_admin.user_promotions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES amesa_auth.users("Id") ON DELETE CASCADE, -- Schema-qualified reference
    promotion_id UUID NOT NULL REFERENCES amesa_admin.promotions(id) ON DELETE CASCADE,
    used_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    transaction_id UUID, -- Note: Foreign key to transactions table (schema depends on payment service setup)
    discount_amount DECIMAL(10,2),
    UNIQUE(user_id, promotion_id)
);

-- ============================================================
-- STEP 2: CREATE INDEXES
-- ============================================================

-- Index for promotion code lookups (case-insensitive searches)
CREATE INDEX IF NOT EXISTS idx_promotions_code ON amesa_admin.promotions(UPPER(code)) WHERE code IS NOT NULL;

-- Index for active promotions with date range filtering
CREATE INDEX IF NOT EXISTS idx_promotions_active_dates ON amesa_admin.promotions(is_active, start_date, end_date) 
WHERE is_active = true;

-- Index for user promotion lookups
CREATE INDEX IF NOT EXISTS idx_user_promotions_user ON amesa_admin.user_promotions(user_id);

-- Index for promotion usage tracking
CREATE INDEX IF NOT EXISTS idx_user_promotions_promotion ON amesa_admin.user_promotions(promotion_id);

-- Index for transaction linking
CREATE INDEX IF NOT EXISTS idx_user_promotions_transaction ON amesa_admin.user_promotions(transaction_id) 
WHERE transaction_id IS NOT NULL;

-- Composite index for user promotion history queries
CREATE INDEX IF NOT EXISTS idx_user_promotions_user_used ON amesa_admin.user_promotions(user_id, used_at DESC);

-- Index for promotion type filtering
CREATE INDEX IF NOT EXISTS idx_promotions_type_active ON amesa_admin.promotions(type, is_active) 
WHERE is_active = true;

-- ============================================================
-- VERIFICATION QUERIES
-- ============================================================
-- Run these queries after executing the script to verify everything was created:
-- ============================================================

-- Verify tables exist
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'amesa_admin' AND table_name IN ('promotions', 'user_promotions')
ORDER BY table_name;

-- Verify indexes were created
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' AND tablename IN ('promotions', 'user_promotions')
ORDER BY tablename, indexname;

-- Expected Results:
-- Tables: 2 tables (promotions, user_promotions)
-- Indexes: 7 indexes (plus primary keys)
--   - idx_promotions_code
--   - idx_promotions_active_dates
--   - idx_promotions_type_active
--   - idx_user_promotions_user
--   - idx_user_promotions_promotion
--   - idx_user_promotions_transaction
--   - idx_user_promotions_user_used

