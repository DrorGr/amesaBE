-- ============================================================================
-- Verify ReservationCleanupService query works correctly
-- Date: 2025-01-25
-- Description: Tests the exact query that ReservationCleanupService uses
-- ============================================================================

-- This is the exact query from ReservationCleanupService.cs line 67-69
-- It queries expired reservations with Status = 'pending'
SELECT 
    t."Id", 
    t."CreatedAt", 
    t."DiscountAmount", 
    t."ErrorMessage", 
    t."ExpiresAt", 
    t."HouseId", 
    t."PaymentMethodId", 
    t."PaymentTransactionId", 
    t."ProcessedAt", 
    t."PromotionCode", 
    t."Quantity", 
    t."ReservationToken", 
    t."Status", 
    t."TotalPrice", 
    t."UpdatedAt", 
    t."UserId"
FROM amesa_lottery.ticket_reservations t
WHERE t."Status" = 'pending' 
  AND t."ExpiresAt" <= NOW()
LIMIT 10;

-- Verify indexes exist (used by the query)
SELECT 
    indexname, 
    indexdef
FROM pg_indexes 
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'ticket_reservations'
ORDER BY indexname;


