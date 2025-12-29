-- ============================================================================
-- Test actual column names in ticket_reservations table
-- Date: 2025-01-25
-- Description: Tests if columns are stored as PascalCase (quoted) or lowercase
-- ============================================================================

-- Test 1: Query with quoted PascalCase (what EF Core uses)
-- This will work if columns are stored as PascalCase with quotes
SELECT 
    t."Id", 
    t."HouseId", 
    t."UserId",
    t."Status",
    t."ExpiresAt"
FROM amesa_lottery.ticket_reservations t
LIMIT 1;

-- If Test 1 fails, the columns are likely stored as lowercase
-- Test 2: Query with lowercase (unquoted)
-- This will work if columns are stored as lowercase
SELECT 
    t.id, 
    t.house_id, 
    t.user_id,
    t.status,
    t.expires_at
FROM amesa_lottery.ticket_reservations t
LIMIT 1;

-- Test 3: Check actual stored column names using pg_attribute
-- This shows the exact names as stored in PostgreSQL
SELECT 
    a.attname as actual_stored_name,
    a.attnum as position,
    pg_catalog.format_type(a.atttypid, a.atttypmod) as data_type
FROM pg_catalog.pg_attribute a
JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
WHERE n.nspname = 'amesa_lottery'
  AND c.relname = 'ticket_reservations'
  AND a.attnum > 0  -- Exclude system columns
  AND NOT a.attisdropped
ORDER BY a.attnum;


