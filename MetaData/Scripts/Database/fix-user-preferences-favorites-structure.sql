-- ============================================================================
-- Fix User Preferences JSONB Structure for Favorites
-- ============================================================================
-- This script fixes the JSONB structure in user_preferences to match what
-- the code expects: lotteryPreferences.favoriteHouseIds
-- 
-- Issue: Existing preferences have "lottery" (lowercase) but code expects
--        "lotteryPreferences" (camelCase) with "favoriteHouseIds" array
-- ============================================================================

-- Step 1: Check current structure
SELECT 
    'BEFORE FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences'
        WHEN preferences_json::jsonb ? 'lottery' THEN 'HAS lottery (needs migration)'
        ELSE 'NO lottery structure'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds'
        ELSE 'MISSING favoriteHouseIds'
    END as favorites_status
FROM amesa_auth.user_preferences;

-- Step 2: Fix preferences that have "lottery" but not "lotteryPreferences"
-- This preserves the existing "lottery" preferences and adds "lotteryPreferences"
UPDATE amesa_auth.user_preferences
SET preferences_json = 
    CASE 
        -- If lotteryPreferences doesn't exist but lottery does, add lotteryPreferences
        WHEN preferences_json::jsonb ? 'lottery' 
             AND NOT (preferences_json::jsonb ? 'lotteryPreferences')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences', 
                jsonb_build_object(
                    'favoriteHouseIds', 
                    COALESCE(
                        preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
                        '[]'::jsonb
                    )
                )
            )
        -- If lotteryPreferences exists but doesn't have favoriteHouseIds, add it
        WHEN preferences_json::jsonb ? 'lotteryPreferences'
             AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                (preferences_json::jsonb->'lotteryPreferences')
                || jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- If neither exists, add lotteryPreferences with empty favoriteHouseIds
        WHEN NOT (preferences_json::jsonb ? 'lotteryPreferences')
             AND NOT (preferences_json::jsonb ? 'lottery')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- Otherwise, keep as is (already has correct structure)
        ELSE preferences_json::jsonb
    END,
    updated_at = NOW(),
    updated_by = 'system-migration'
WHERE 
    -- Only update rows that need fixing
    (
        -- Missing lotteryPreferences entirely
        NOT (preferences_json::jsonb ? 'lotteryPreferences')
        OR 
        -- Has lotteryPreferences but missing favoriteHouseIds
        (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        )
    );

-- Step 3: Verify the fix
SELECT 
    'AFTER FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences ✓'
        ELSE 'MISSING lotteryPreferences ✗'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds ✓'
        ELSE 'MISSING favoriteHouseIds ✗'
    END as favorites_status,
    jsonb_array_length(
        COALESCE(
            preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
            '[]'::jsonb
        )
    ) as favorite_count
FROM amesa_auth.user_preferences;

-- Step 4: Summary
SELECT 
    'SUMMARY' as report_type,
    COUNT(*) as total_users,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences'
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as users_with_correct_structure,
    COUNT(*) FILTER (
        WHERE NOT (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
        )
    ) as users_needing_fix
FROM amesa_auth.user_preferences;


-- ============================================================================
-- This script fixes the JSONB structure in user_preferences to match what
-- the code expects: lotteryPreferences.favoriteHouseIds
-- 
-- Issue: Existing preferences have "lottery" (lowercase) but code expects
--        "lotteryPreferences" (camelCase) with "favoriteHouseIds" array
-- ============================================================================

-- Step 1: Check current structure
SELECT 
    'BEFORE FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences'
        WHEN preferences_json::jsonb ? 'lottery' THEN 'HAS lottery (needs migration)'
        ELSE 'NO lottery structure'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds'
        ELSE 'MISSING favoriteHouseIds'
    END as favorites_status
FROM amesa_auth.user_preferences;

-- Step 2: Fix preferences that have "lottery" but not "lotteryPreferences"
-- This preserves the existing "lottery" preferences and adds "lotteryPreferences"
UPDATE amesa_auth.user_preferences
SET preferences_json = 
    CASE 
        -- If lotteryPreferences doesn't exist but lottery does, add lotteryPreferences
        WHEN preferences_json::jsonb ? 'lottery' 
             AND NOT (preferences_json::jsonb ? 'lotteryPreferences')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences', 
                jsonb_build_object(
                    'favoriteHouseIds', 
                    COALESCE(
                        preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
                        '[]'::jsonb
                    )
                )
            )
        -- If lotteryPreferences exists but doesn't have favoriteHouseIds, add it
        WHEN preferences_json::jsonb ? 'lotteryPreferences'
             AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                (preferences_json::jsonb->'lotteryPreferences')
                || jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- If neither exists, add lotteryPreferences with empty favoriteHouseIds
        WHEN NOT (preferences_json::jsonb ? 'lotteryPreferences')
             AND NOT (preferences_json::jsonb ? 'lottery')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- Otherwise, keep as is (already has correct structure)
        ELSE preferences_json::jsonb
    END,
    updated_at = NOW(),
    updated_by = 'system-migration'
WHERE 
    -- Only update rows that need fixing
    (
        -- Missing lotteryPreferences entirely
        NOT (preferences_json::jsonb ? 'lotteryPreferences')
        OR 
        -- Has lotteryPreferences but missing favoriteHouseIds
        (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        )
    );

-- Step 3: Verify the fix
SELECT 
    'AFTER FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences ✓'
        ELSE 'MISSING lotteryPreferences ✗'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds ✓'
        ELSE 'MISSING favoriteHouseIds ✗'
    END as favorites_status,
    jsonb_array_length(
        COALESCE(
            preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
            '[]'::jsonb
        )
    ) as favorite_count
FROM amesa_auth.user_preferences;

-- Step 4: Summary
SELECT 
    'SUMMARY' as report_type,
    COUNT(*) as total_users,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences'
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as users_with_correct_structure,
    COUNT(*) FILTER (
        WHERE NOT (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
        )
    ) as users_needing_fix
FROM amesa_auth.user_preferences;


-- ============================================================================
-- This script fixes the JSONB structure in user_preferences to match what
-- the code expects: lotteryPreferences.favoriteHouseIds
-- 
-- Issue: Existing preferences have "lottery" (lowercase) but code expects
--        "lotteryPreferences" (camelCase) with "favoriteHouseIds" array
-- ============================================================================

-- Step 1: Check current structure
SELECT 
    'BEFORE FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences'
        WHEN preferences_json::jsonb ? 'lottery' THEN 'HAS lottery (needs migration)'
        ELSE 'NO lottery structure'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds'
        ELSE 'MISSING favoriteHouseIds'
    END as favorites_status
FROM amesa_auth.user_preferences;

-- Step 2: Fix preferences that have "lottery" but not "lotteryPreferences"
-- This preserves the existing "lottery" preferences and adds "lotteryPreferences"
UPDATE amesa_auth.user_preferences
SET preferences_json = 
    CASE 
        -- If lotteryPreferences doesn't exist but lottery does, add lotteryPreferences
        WHEN preferences_json::jsonb ? 'lottery' 
             AND NOT (preferences_json::jsonb ? 'lotteryPreferences')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences', 
                jsonb_build_object(
                    'favoriteHouseIds', 
                    COALESCE(
                        preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
                        '[]'::jsonb
                    )
                )
            )
        -- If lotteryPreferences exists but doesn't have favoriteHouseIds, add it
        WHEN preferences_json::jsonb ? 'lotteryPreferences'
             AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                (preferences_json::jsonb->'lotteryPreferences')
                || jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- If neither exists, add lotteryPreferences with empty favoriteHouseIds
        WHEN NOT (preferences_json::jsonb ? 'lotteryPreferences')
             AND NOT (preferences_json::jsonb ? 'lottery')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- Otherwise, keep as is (already has correct structure)
        ELSE preferences_json::jsonb
    END,
    updated_at = NOW(),
    updated_by = 'system-migration'
WHERE 
    -- Only update rows that need fixing
    (
        -- Missing lotteryPreferences entirely
        NOT (preferences_json::jsonb ? 'lotteryPreferences')
        OR 
        -- Has lotteryPreferences but missing favoriteHouseIds
        (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        )
    );

-- Step 3: Verify the fix
SELECT 
    'AFTER FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences ✓'
        ELSE 'MISSING lotteryPreferences ✗'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds ✓'
        ELSE 'MISSING favoriteHouseIds ✗'
    END as favorites_status,
    jsonb_array_length(
        COALESCE(
            preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
            '[]'::jsonb
        )
    ) as favorite_count
FROM amesa_auth.user_preferences;

-- Step 4: Summary
SELECT 
    'SUMMARY' as report_type,
    COUNT(*) as total_users,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences'
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as users_with_correct_structure,
    COUNT(*) FILTER (
        WHERE NOT (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
        )
    ) as users_needing_fix
FROM amesa_auth.user_preferences;


-- ============================================================================
-- This script fixes the JSONB structure in user_preferences to match what
-- the code expects: lotteryPreferences.favoriteHouseIds
-- 
-- Issue: Existing preferences have "lottery" (lowercase) but code expects
--        "lotteryPreferences" (camelCase) with "favoriteHouseIds" array
-- ============================================================================

-- Step 1: Check current structure
SELECT 
    'BEFORE FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences'
        WHEN preferences_json::jsonb ? 'lottery' THEN 'HAS lottery (needs migration)'
        ELSE 'NO lottery structure'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds'
        ELSE 'MISSING favoriteHouseIds'
    END as favorites_status
FROM amesa_auth.user_preferences;

-- Step 2: Fix preferences that have "lottery" but not "lotteryPreferences"
-- This preserves the existing "lottery" preferences and adds "lotteryPreferences"
UPDATE amesa_auth.user_preferences
SET preferences_json = 
    CASE 
        -- If lotteryPreferences doesn't exist but lottery does, add lotteryPreferences
        WHEN preferences_json::jsonb ? 'lottery' 
             AND NOT (preferences_json::jsonb ? 'lotteryPreferences')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences', 
                jsonb_build_object(
                    'favoriteHouseIds', 
                    COALESCE(
                        preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
                        '[]'::jsonb
                    )
                )
            )
        -- If lotteryPreferences exists but doesn't have favoriteHouseIds, add it
        WHEN preferences_json::jsonb ? 'lotteryPreferences'
             AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                (preferences_json::jsonb->'lotteryPreferences')
                || jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- If neither exists, add lotteryPreferences with empty favoriteHouseIds
        WHEN NOT (preferences_json::jsonb ? 'lotteryPreferences')
             AND NOT (preferences_json::jsonb ? 'lottery')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- Otherwise, keep as is (already has correct structure)
        ELSE preferences_json::jsonb
    END,
    updated_at = NOW(),
    updated_by = 'system-migration'
WHERE 
    -- Only update rows that need fixing
    (
        -- Missing lotteryPreferences entirely
        NOT (preferences_json::jsonb ? 'lotteryPreferences')
        OR 
        -- Has lotteryPreferences but missing favoriteHouseIds
        (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        )
    );

-- Step 3: Verify the fix
SELECT 
    'AFTER FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences ✓'
        ELSE 'MISSING lotteryPreferences ✗'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds ✓'
        ELSE 'MISSING favoriteHouseIds ✗'
    END as favorites_status,
    jsonb_array_length(
        COALESCE(
            preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
            '[]'::jsonb
        )
    ) as favorite_count
FROM amesa_auth.user_preferences;

-- Step 4: Summary
SELECT 
    'SUMMARY' as report_type,
    COUNT(*) as total_users,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences'
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as users_with_correct_structure,
    COUNT(*) FILTER (
        WHERE NOT (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
        )
    ) as users_needing_fix
FROM amesa_auth.user_preferences;


-- ============================================================================
-- This script fixes the JSONB structure in user_preferences to match what
-- the code expects: lotteryPreferences.favoriteHouseIds
-- 
-- Issue: Existing preferences have "lottery" (lowercase) but code expects
--        "lotteryPreferences" (camelCase) with "favoriteHouseIds" array
-- ============================================================================

-- Step 1: Check current structure
SELECT 
    'BEFORE FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences'
        WHEN preferences_json::jsonb ? 'lottery' THEN 'HAS lottery (needs migration)'
        ELSE 'NO lottery structure'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds'
        ELSE 'MISSING favoriteHouseIds'
    END as favorites_status
FROM amesa_auth.user_preferences;

-- Step 2: Fix preferences that have "lottery" but not "lotteryPreferences"
-- This preserves the existing "lottery" preferences and adds "lotteryPreferences"
UPDATE amesa_auth.user_preferences
SET preferences_json = 
    CASE 
        -- If lotteryPreferences doesn't exist but lottery does, add lotteryPreferences
        WHEN preferences_json::jsonb ? 'lottery' 
             AND NOT (preferences_json::jsonb ? 'lotteryPreferences')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences', 
                jsonb_build_object(
                    'favoriteHouseIds', 
                    COALESCE(
                        preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
                        '[]'::jsonb
                    )
                )
            )
        -- If lotteryPreferences exists but doesn't have favoriteHouseIds, add it
        WHEN preferences_json::jsonb ? 'lotteryPreferences'
             AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                (preferences_json::jsonb->'lotteryPreferences')
                || jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- If neither exists, add lotteryPreferences with empty favoriteHouseIds
        WHEN NOT (preferences_json::jsonb ? 'lotteryPreferences')
             AND NOT (preferences_json::jsonb ? 'lottery')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- Otherwise, keep as is (already has correct structure)
        ELSE preferences_json::jsonb
    END,
    updated_at = NOW(),
    updated_by = 'system-migration'
WHERE 
    -- Only update rows that need fixing
    (
        -- Missing lotteryPreferences entirely
        NOT (preferences_json::jsonb ? 'lotteryPreferences')
        OR 
        -- Has lotteryPreferences but missing favoriteHouseIds
        (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        )
    );

-- Step 3: Verify the fix
SELECT 
    'AFTER FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences ✓'
        ELSE 'MISSING lotteryPreferences ✗'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds ✓'
        ELSE 'MISSING favoriteHouseIds ✗'
    END as favorites_status,
    jsonb_array_length(
        COALESCE(
            preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
            '[]'::jsonb
        )
    ) as favorite_count
FROM amesa_auth.user_preferences;

-- Step 4: Summary
SELECT 
    'SUMMARY' as report_type,
    COUNT(*) as total_users,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences'
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as users_with_correct_structure,
    COUNT(*) FILTER (
        WHERE NOT (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
        )
    ) as users_needing_fix
FROM amesa_auth.user_preferences;


-- ============================================================================
-- This script fixes the JSONB structure in user_preferences to match what
-- the code expects: lotteryPreferences.favoriteHouseIds
-- 
-- Issue: Existing preferences have "lottery" (lowercase) but code expects
--        "lotteryPreferences" (camelCase) with "favoriteHouseIds" array
-- ============================================================================

-- Step 1: Check current structure
SELECT 
    'BEFORE FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences'
        WHEN preferences_json::jsonb ? 'lottery' THEN 'HAS lottery (needs migration)'
        ELSE 'NO lottery structure'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds'
        ELSE 'MISSING favoriteHouseIds'
    END as favorites_status
FROM amesa_auth.user_preferences;

-- Step 2: Fix preferences that have "lottery" but not "lotteryPreferences"
-- This preserves the existing "lottery" preferences and adds "lotteryPreferences"
UPDATE amesa_auth.user_preferences
SET preferences_json = 
    CASE 
        -- If lotteryPreferences doesn't exist but lottery does, add lotteryPreferences
        WHEN preferences_json::jsonb ? 'lottery' 
             AND NOT (preferences_json::jsonb ? 'lotteryPreferences')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences', 
                jsonb_build_object(
                    'favoriteHouseIds', 
                    COALESCE(
                        preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
                        '[]'::jsonb
                    )
                )
            )
        -- If lotteryPreferences exists but doesn't have favoriteHouseIds, add it
        WHEN preferences_json::jsonb ? 'lotteryPreferences'
             AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                (preferences_json::jsonb->'lotteryPreferences')
                || jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- If neither exists, add lotteryPreferences with empty favoriteHouseIds
        WHEN NOT (preferences_json::jsonb ? 'lotteryPreferences')
             AND NOT (preferences_json::jsonb ? 'lottery')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- Otherwise, keep as is (already has correct structure)
        ELSE preferences_json::jsonb
    END,
    updated_at = NOW(),
    updated_by = 'system-migration'
WHERE 
    -- Only update rows that need fixing
    (
        -- Missing lotteryPreferences entirely
        NOT (preferences_json::jsonb ? 'lotteryPreferences')
        OR 
        -- Has lotteryPreferences but missing favoriteHouseIds
        (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        )
    );

-- Step 3: Verify the fix
SELECT 
    'AFTER FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences ✓'
        ELSE 'MISSING lotteryPreferences ✗'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds ✓'
        ELSE 'MISSING favoriteHouseIds ✗'
    END as favorites_status,
    jsonb_array_length(
        COALESCE(
            preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
            '[]'::jsonb
        )
    ) as favorite_count
FROM amesa_auth.user_preferences;

-- Step 4: Summary
SELECT 
    'SUMMARY' as report_type,
    COUNT(*) as total_users,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences'
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as users_with_correct_structure,
    COUNT(*) FILTER (
        WHERE NOT (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
        )
    ) as users_needing_fix
FROM amesa_auth.user_preferences;


-- ============================================================================
-- This script fixes the JSONB structure in user_preferences to match what
-- the code expects: lotteryPreferences.favoriteHouseIds
-- 
-- Issue: Existing preferences have "lottery" (lowercase) but code expects
--        "lotteryPreferences" (camelCase) with "favoriteHouseIds" array
-- ============================================================================

-- Step 1: Check current structure
SELECT 
    'BEFORE FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences'
        WHEN preferences_json::jsonb ? 'lottery' THEN 'HAS lottery (needs migration)'
        ELSE 'NO lottery structure'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds'
        ELSE 'MISSING favoriteHouseIds'
    END as favorites_status
FROM amesa_auth.user_preferences;

-- Step 2: Fix preferences that have "lottery" but not "lotteryPreferences"
-- This preserves the existing "lottery" preferences and adds "lotteryPreferences"
UPDATE amesa_auth.user_preferences
SET preferences_json = 
    CASE 
        -- If lotteryPreferences doesn't exist but lottery does, add lotteryPreferences
        WHEN preferences_json::jsonb ? 'lottery' 
             AND NOT (preferences_json::jsonb ? 'lotteryPreferences')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences', 
                jsonb_build_object(
                    'favoriteHouseIds', 
                    COALESCE(
                        preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
                        '[]'::jsonb
                    )
                )
            )
        -- If lotteryPreferences exists but doesn't have favoriteHouseIds, add it
        WHEN preferences_json::jsonb ? 'lotteryPreferences'
             AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                (preferences_json::jsonb->'lotteryPreferences')
                || jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- If neither exists, add lotteryPreferences with empty favoriteHouseIds
        WHEN NOT (preferences_json::jsonb ? 'lotteryPreferences')
             AND NOT (preferences_json::jsonb ? 'lottery')
        THEN 
            preferences_json::jsonb 
            || jsonb_build_object(
                'lotteryPreferences',
                jsonb_build_object('favoriteHouseIds', '[]'::jsonb)
            )
        -- Otherwise, keep as is (already has correct structure)
        ELSE preferences_json::jsonb
    END,
    updated_at = NOW(),
    updated_by = 'system-migration'
WHERE 
    -- Only update rows that need fixing
    (
        -- Missing lotteryPreferences entirely
        NOT (preferences_json::jsonb ? 'lotteryPreferences')
        OR 
        -- Has lotteryPreferences but missing favoriteHouseIds
        (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND NOT (preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds')
        )
    );

-- Step 3: Verify the fix
SELECT 
    'AFTER FIX' as check_type,
    user_id,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' THEN 'HAS lotteryPreferences ✓'
        ELSE 'MISSING lotteryPreferences ✗'
    END as structure_status,
    CASE 
        WHEN preferences_json::jsonb ? 'lotteryPreferences' 
             AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds' 
        THEN 'HAS favoriteHouseIds ✓'
        ELSE 'MISSING favoriteHouseIds ✗'
    END as favorites_status,
    jsonb_array_length(
        COALESCE(
            preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
            '[]'::jsonb
        )
    ) as favorite_count
FROM amesa_auth.user_preferences;

-- Step 4: Summary
SELECT 
    'SUMMARY' as report_type,
    COUNT(*) as total_users,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences'
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as users_with_correct_structure,
    COUNT(*) FILTER (
        WHERE NOT (
            preferences_json::jsonb ? 'lotteryPreferences'
            AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
        )
    ) as users_needing_fix
FROM amesa_auth.user_preferences;

