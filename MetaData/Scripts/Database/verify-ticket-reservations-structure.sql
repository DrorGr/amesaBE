-- ============================================================================
-- Verify ticket_reservations table structure
-- Date: 2025-01-25
-- Description: Verifies the table exists and has correct PascalCase column names
-- ============================================================================

-- Step 1: Verify table exists
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'amesa_lottery' 
    AND table_name = 'ticket_reservations'
) as table_exists;

-- Step 2: Check column names (should be PascalCase with quotes)
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'ticket_reservations' 
ORDER BY ordinal_position;

-- Step 3: Test query with PascalCase columns (this is what EF Core generates)
SELECT 
    t."Id", 
    t."HouseId", 
    t."UserId", 
    t."Quantity", 
    t."Status", 
    t."ExpiresAt"
FROM amesa_lottery.ticket_reservations t
WHERE t."Status" = 'pending'
LIMIT 1;

-- Step 4: Check indexes exist
SELECT 
    indexname, 
    indexdef
FROM pg_indexes 
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'ticket_reservations'
ORDER BY indexname;


