-- Migration: Add unique constraint on ticket numbers per house
-- Schema: amesa_lottery
-- Purpose: Prevent duplicate ticket numbers within the same house

-- Note: This constraint is defined in EF Core model configuration
-- This SQL script is for manual application if needed

-- First, check for existing duplicates and resolve them
DO $$
DECLARE
    duplicate_count INTEGER;
BEGIN
    -- Count duplicates
    SELECT COUNT(*) INTO duplicate_count
    FROM (
        SELECT house_id, ticket_number, COUNT(*) as cnt
        FROM amesa_lottery.lottery_tickets
        GROUP BY house_id, ticket_number
        HAVING COUNT(*) > 1
    ) duplicates;
    
    IF duplicate_count > 0 THEN
        RAISE NOTICE 'Found % duplicate ticket numbers. Please resolve before applying constraint.', duplicate_count;
        -- In production, you would need to resolve duplicates first
    END IF;
END $$;

-- Add unique constraint (if no duplicates exist)
-- This will be created automatically by EF Core migrations
-- But can be applied manually if needed:
-- ALTER TABLE amesa_lottery.lottery_tickets
-- ADD CONSTRAINT uq_house_ticket_number UNIQUE (house_id, ticket_number);


