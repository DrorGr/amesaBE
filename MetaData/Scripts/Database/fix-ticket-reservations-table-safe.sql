-- ============================================================================
-- Fix ticket_reservations table structure (Safe Version)
-- Date: 2025-01-25
-- Description: Safely fixes ticket_reservations table to match EF Core expectations
--              This version checks current structure and provides migration path
-- ============================================================================

-- STEP 1: Check current table structure
-- Run this first to see what columns exist:
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'ticket_reservations' 
ORDER BY ordinal_position;

-- STEP 2: Check if table has data
SELECT COUNT(*) as row_count FROM amesa_lottery.ticket_reservations;

-- STEP 3: If table is empty or you can afford to lose data, use this:
-- Drop and recreate with correct structure
BEGIN;

-- Drop existing table (this will delete all data!)
DROP TABLE IF EXISTS amesa_lottery.ticket_reservations CASCADE;

-- Create table with correct PascalCase column names (quoted identifiers)
-- This matches EF Core's expectations from the TicketReservation model
CREATE TABLE amesa_lottery.ticket_reservations (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "HouseId" UUID NOT NULL,
    "UserId" UUID NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "TotalPrice" DECIMAL(10,2) NOT NULL,
    "PaymentMethodId" UUID,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'pending',
    "ReservationToken" VARCHAR(255) NOT NULL,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "ProcessedAt" TIMESTAMP WITH TIME ZONE,
    "PaymentTransactionId" UUID,
    "PromotionCode" VARCHAR(50),
    "DiscountAmount" DECIMAL(10,2),
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_ticket_reservations_houses" FOREIGN KEY ("HouseId") 
        REFERENCES amesa_lottery.houses("Id") ON DELETE CASCADE
);

-- Create indexes with quoted column names
CREATE INDEX "IX_ticket_reservations_HouseId_Status" 
    ON amesa_lottery.ticket_reservations("HouseId", "Status");

CREATE INDEX "IX_ticket_reservations_UserId_Status" 
    ON amesa_lottery.ticket_reservations("UserId", "Status");

CREATE INDEX "IX_ticket_reservations_ExpiresAt" 
    ON amesa_lottery.ticket_reservations("ExpiresAt") 
    WHERE "Status" = 'pending';

CREATE UNIQUE INDEX "IX_ticket_reservations_ReservationToken" 
    ON amesa_lottery.ticket_reservations("ReservationToken");

CREATE INDEX "idx_reservation_promotion_code" 
    ON amesa_lottery.ticket_reservations("PromotionCode") 
    WHERE "PromotionCode" IS NOT NULL;

-- Add comments for documentation
COMMENT ON TABLE amesa_lottery.ticket_reservations IS 'Stores ticket reservations for lottery houses';
COMMENT ON COLUMN amesa_lottery.ticket_reservations."Id" IS 'Primary key';
COMMENT ON COLUMN amesa_lottery.ticket_reservations."Status" IS 'Reservation status: pending, completed, expired, cancelled';
COMMENT ON COLUMN amesa_lottery.ticket_reservations."ReservationToken" IS 'Unique token for reservation tracking';
COMMENT ON COLUMN amesa_lottery.ticket_reservations."PromotionCode" IS 'Promotion code applied to this reservation';
COMMENT ON COLUMN amesa_lottery.ticket_reservations."DiscountAmount" IS 'Discount amount applied to this reservation';

COMMIT;

-- ============================================================================
-- ALTERNATIVE: If you need to preserve data, use this migration approach:
-- ============================================================================
-- This is more complex and requires manual column mapping
-- Only use if you have important data in the table

/*
BEGIN;

-- Create backup table
CREATE TABLE amesa_lottery.ticket_reservations_backup AS 
SELECT * FROM amesa_lottery.ticket_reservations;

-- Drop old table
DROP TABLE amesa_lottery.ticket_reservations CASCADE;

-- Create new table with correct structure (see above)

-- Migrate data (adjust column names based on actual structure)
INSERT INTO amesa_lottery.ticket_reservations (
    "Id", "HouseId", "UserId", "Quantity", "TotalPrice", "PaymentMethodId",
    "Status", "ReservationToken", "ExpiresAt", "ProcessedAt", 
    "PaymentTransactionId", "PromotionCode", "DiscountAmount", "ErrorMessage",
    "CreatedAt", "UpdatedAt"
)
SELECT 
    id, house_id, user_id, quantity, total_price, payment_method_id,
    status, reservation_token, expires_at, processed_at,
    payment_transaction_id, promotion_code, discount_amount, error_message,
    created_at, updated_at
FROM amesa_lottery.ticket_reservations_backup;

-- Drop backup table after verification
-- DROP TABLE amesa_lottery.ticket_reservations_backup;

COMMIT;
*/

