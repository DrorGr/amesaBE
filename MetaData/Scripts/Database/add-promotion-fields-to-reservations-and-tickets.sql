-- Migration: Add promotion support to ticket reservations and lottery tickets
-- Date: 2025-01-25
-- Description: Adds promotion_code and discount_amount fields to ticket_reservations and lottery_tickets tables
--              to support promotion tracking in reservation flow and ticket history

-- Add promotion fields to ticket_reservations table
ALTER TABLE amesa_lottery.ticket_reservations
    ADD COLUMN IF NOT EXISTS promotion_code VARCHAR(50),
    ADD COLUMN IF NOT EXISTS discount_amount DECIMAL(10,2);

-- Add promotion fields to lottery_tickets table
ALTER TABLE amesa_lottery.lottery_tickets
    ADD COLUMN IF NOT EXISTS promotion_code VARCHAR(50),
    ADD COLUMN IF NOT EXISTS discount_amount DECIMAL(10,2);

-- Create indexes for promotion code lookups
CREATE INDEX IF NOT EXISTS idx_reservation_promotion_code 
    ON amesa_lottery.ticket_reservations(promotion_code)
    WHERE promotion_code IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_ticket_promotion_code 
    ON amesa_lottery.lottery_tickets(promotion_code)
    WHERE promotion_code IS NOT NULL;

-- Add comments for documentation
COMMENT ON COLUMN amesa_lottery.ticket_reservations.promotion_code IS 'Promotion code applied to this reservation';
COMMENT ON COLUMN amesa_lottery.ticket_reservations.discount_amount IS 'Discount amount applied to this reservation';
COMMENT ON COLUMN amesa_lottery.lottery_tickets.promotion_code IS 'Promotion code used when purchasing this ticket';
COMMENT ON COLUMN amesa_lottery.lottery_tickets.discount_amount IS 'Discount amount applied when purchasing this ticket';






