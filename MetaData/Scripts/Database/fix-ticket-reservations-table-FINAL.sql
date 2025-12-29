-- ============================================================================
-- Fix ticket_reservations table structure (FINAL VERSION - NO BACKUP SCRIPT)
-- Date: 2025-01-25
-- Description: Creates ticket_reservations table with correct PascalCase columns
--              This version does NOT include the backup/drop section
-- ============================================================================

-- Drop existing table if it exists (this will delete all data!)
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

-- Verify table was created
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'amesa_lottery' 
    AND table_name = 'ticket_reservations'
) as table_exists;

-- Verify columns are PascalCase
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'ticket_reservations' 
ORDER BY ordinal_position;


