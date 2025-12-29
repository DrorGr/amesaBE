-- ============================================================================
-- Check actual stored column names in ticket_reservations table
-- Date: 2025-01-25
-- Description: Verifies the actual column names as stored in PostgreSQL
--              (information_schema may show different case than what's stored)
-- ============================================================================

-- Method 1: Query pg_attribute directly (shows actual stored names)
SELECT 
    attname as actual_column_name,
    attnum as position,
    typname as data_type
FROM pg_attribute a
JOIN pg_class c ON a.attrelid = c.oid
JOIN pg_namespace n ON c.relnamespace = n.oid
JOIN pg_type t ON a.atttypid = t.oid
WHERE n.nspname = 'amesa_lottery'
  AND c.relname = 'ticket_reservations'
  AND a.attnum > 0  -- Exclude system columns
  AND NOT a.attisdropped
ORDER BY a.attnum;

-- Method 2: Test query with quoted PascalCase (what EF Core uses)
-- This will fail if columns are lowercase
SELECT 
    t."Id", 
    t."HouseId", 
    t."UserId"
FROM amesa_lottery.ticket_reservations t
LIMIT 1;

-- Method 3: Test query with lowercase (if Method 2 fails)
-- This will work if columns are stored as lowercase
SELECT 
    t.id, 
    t.house_id, 
    t.user_id
FROM amesa_lottery.ticket_reservations t
LIMIT 1;


