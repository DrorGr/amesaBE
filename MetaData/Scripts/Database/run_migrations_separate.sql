-- ============================================================================
-- LOTTERY SERVICE MIGRATIONS ONLY
-- Run this on amesa_lottery_db database
-- Command: psql -h YOUR_HOST -U postgres -d amesa_lottery_db -f run_migrations_separate.sql
-- ============================================================================

SET search_path TO amesa_lottery, public;

-- ----------------------------------------------------------------------------
-- 1.1: Create promotion_usage_audit table
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

COMMENT ON TABLE amesa_lottery.promotion_usage_audit IS 
    'Audit log for promotion usage tracking failures. Created when discount is applied but usage cannot be recorded.';

COMMENT ON COLUMN amesa_lottery.promotion_usage_audit.status IS 
    'Status: Pending, Resolved, Reversed';

-- ----------------------------------------------------------------------------
-- 1.2: Add unique constraint on ticket numbers per house
-- ----------------------------------------------------------------------------

DO $$
DECLARE
    duplicate_count INTEGER;
    duplicate_records RECORD;
BEGIN
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
            SELECT "HouseId", "TicketNumber", COUNT(*) as cnt
            FROM amesa_lottery.lottery_tickets
            GROUP BY "HouseId", "TicketNumber"
            HAVING COUNT(*) > 1
            LIMIT 10
        LOOP
            RAISE NOTICE '   House ID: %, Ticket Number: %, Count: %', 
                duplicate_records."HouseId", 
                duplicate_records."TicketNumber", 
                duplicate_records.cnt;
        END LOOP;
        
        RAISE EXCEPTION 'Cannot proceed: Duplicate ticket numbers exist. Please resolve duplicates first.';
    ELSE
        RAISE NOTICE '✅ No duplicate ticket numbers found. Safe to proceed with constraint.';
    END IF;
END $$;

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

COMMENT ON CONSTRAINT uq_house_ticket_number ON amesa_lottery.lottery_tickets IS 
    'Ensures ticket numbers are unique within each house to prevent race conditions in ticket generation.';


