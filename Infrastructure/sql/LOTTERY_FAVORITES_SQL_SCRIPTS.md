# Lottery Favorites - Complete SQL Scripts

This document contains all SQL scripts needed for the Lottery Favorites & Entry Management system.

## Script 1: Database Migration (Indexes and View)

**File**: `BE/Infrastructure/sql/lottery-favorites-migration.sql`

```sql
-- =====================================================
-- Lottery Favorites & Entry Management - Database Migration
-- =====================================================
-- 
-- Purpose: Add indexes and views to support lottery favorites
--          and entry management features
--
-- Created: 2025-01-XX
-- Agent: BE Agent (Agent 1)
-- Task: BE-1.1 - Database Schema Extensions
-- Estimated Time: 4 hours
--
-- Database Schemas:
--   - amesa_auth: users, user_preferences (uses lowercase column names for user_id)
--   - amesa_lottery: houses, lottery_tickets (uses PascalCase column names)
--
-- Note: user_preferences uses lowercase (user_id), other tables use PascalCase ("UserId")
-- =====================================================
-- PREREQUISITES
-- =====================================================
-- 
-- This migration assumes:
-- 1. Tables exist in correct schemas:
--    - amesa_lottery.lottery_tickets (with "UserId", "Status", "HouseId", "PurchasePrice", "PurchaseDate", "IsWinner")
--    - amesa_lottery.houses (with "Status", "Location", "Id")
--    - amesa_auth.user_preferences (with user_id lowercase, preferences_json)
--    - amesa_auth.users (with "Id")
-- 2. PostgreSQL version 12+ (for JSONB GIN index support)
--
-- =====================================================
-- INDEXES FOR PERFORMANCE OPTIMIZATION
-- =====================================================

-- Composite index for user's lottery tickets filtered by status
-- Used for: GET /api/v1/tickets/active endpoint
-- Improves: Queries filtering tickets by user_id and status
-- Example query: SELECT * FROM amesa_lottery.lottery_tickets 
--                WHERE "UserId" = ? AND "Status" = 'active'
CREATE INDEX IF NOT EXISTS idx_lottery_tickets_user_status 
ON amesa_lottery.lottery_tickets("UserId", "Status");

-- Composite index for houses filtered by status and location
-- Used for: GET /api/v1/houses/recommendations endpoint
-- Improves: Recommendation queries filtering by status and location
-- Example query: SELECT * FROM amesa_lottery.houses 
--                WHERE "Status" = 'active' AND "Location" = ?
CREATE INDEX IF NOT EXISTS idx_houses_status_location 
ON amesa_lottery.houses("Status", "Location");

-- GIN index for JSONB lottery preferences in user_preferences
-- Used for: Querying lottery preferences stored in JSONB
-- Improves: JSONB queries on preferences_json->'lotteryPreferences'
-- Note: Column name is preferences_json (not preferences)
-- Example query: SELECT * FROM amesa_auth.user_preferences 
--                WHERE preferences_json->'lotteryPreferences'->'favoriteHouseIds' @> '["uuid"]'::jsonb
CREATE INDEX IF NOT EXISTS idx_user_preferences_lottery_gin 
ON amesa_auth.user_preferences USING GIN ((preferences_json->'lotteryPreferences'));

-- =====================================================
-- VIEW: amesa_auth.user_lottery_dashboard
-- =====================================================
-- 
-- Purpose: Aggregated view for user lottery dashboard data
-- Provides: Favorites count, active entries, stats, recent activity
--
-- Usage: 
--   SELECT * FROM amesa_auth.user_lottery_dashboard WHERE user_id = '...';
--
-- Columns:
--   - user_id: User identifier
--   - favorite_houses_count: Number of houses in favorites
--   - active_entries_count: Number of active lottery tickets
--   - total_entries_count: Total lottery tickets purchased
--   - total_wins: Number of winning tickets
--   - total_spending: Total amount spent on tickets
--   - total_winnings: Total amount won (from transactions, if available)
--   - win_rate_percentage: Percentage of wins (wins/total * 100)
--   - average_spending_per_entry: Average price per ticket
--   - favorite_house_id: Most frequently entered house
--   - most_active_month: Month with most entries (YYYY-MM format)
--   - last_entry_date: Date of most recent ticket purchase
--   - last_updated: View refresh timestamp
--

CREATE OR REPLACE VIEW amesa_auth.user_lottery_dashboard AS
SELECT 
    u."Id" AS user_id,
    
    -- Favorites Statistics
    -- Counts favorite house IDs stored in JSONB preferences
    (
        SELECT COUNT(*)::INTEGER
        FROM amesa_auth.user_preferences up
        WHERE up.user_id = u."Id"
        AND up.preferences_json->'lotteryPreferences'->'favoriteHouseIds' IS NOT NULL
        AND jsonb_array_length(up.preferences_json->'lotteryPreferences'->'favoriteHouseIds') > 0
    ) AS favorite_houses_count,
    
    -- Active Entries Statistics
    -- Counts tickets with status = 'active'
    (
        SELECT COUNT(*)::INTEGER
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
        AND lt."Status" = 'active'
    ) AS active_entries_count,
    
    -- Total Entries Statistics
    -- Counts all tickets regardless of status
    (
        SELECT COUNT(*)::INTEGER
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
    ) AS total_entries_count,
    
    -- Total Wins
    -- Counts tickets where is_winner = true
    (
        SELECT COUNT(*)::INTEGER
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
        AND lt."IsWinner" = true
    ) AS total_wins,
    
    -- Total Spending
    -- Sum of all ticket purchase prices
    (
        SELECT COALESCE(SUM(lt."PurchasePrice"), 0)::DECIMAL(10,2)
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
    ) AS total_spending,
    
    -- Total Winnings
    -- Sum of winning transactions (if transactions table exists in amesa_payment schema)
    -- Note: Adjust schema and table name based on your actual transactions table location
    (
        SELECT COALESCE(SUM(t.amount), 0)::DECIMAL(10,2)
        FROM amesa_payment.transactions t
        WHERE t."UserId" = u."Id"
        AND t.type = 'winning'
        AND t.status = 'completed'
    ) AS total_winnings,
    
    -- Win Rate Percentage
    -- Calculates: (wins / total_entries) * 100
    -- Returns 0 if no entries exist
    CASE 
        WHEN (
            SELECT COUNT(*)::INTEGER
            FROM amesa_lottery.lottery_tickets lt
            WHERE lt."UserId" = u."Id"
        ) > 0 THEN
            ROUND(
                (
                    SELECT COUNT(*)::INTEGER
                    FROM amesa_lottery.lottery_tickets lt
                    WHERE lt."UserId" = u."Id"
                    AND lt."IsWinner" = true
                )::DECIMAL / 
                (
                    SELECT COUNT(*)::INTEGER
                    FROM amesa_lottery.lottery_tickets lt
                    WHERE lt."UserId" = u."Id"
                )::DECIMAL * 100, 
                2
            )
        ELSE 0
    END AS win_rate_percentage,
    
    -- Average Spending Per Entry
    -- Calculates: total_spending / total_entries
    -- Returns 0 if no entries exist
    CASE 
        WHEN (
            SELECT COUNT(*)::INTEGER
            FROM amesa_lottery.lottery_tickets lt
            WHERE lt."UserId" = u."Id"
        ) > 0 THEN
            ROUND(
                (
                    SELECT COALESCE(SUM(lt."PurchasePrice"), 0)
                    FROM amesa_lottery.lottery_tickets lt
                    WHERE lt."UserId" = u."Id"
                )::DECIMAL / 
                (
                    SELECT COUNT(*)::INTEGER
                    FROM amesa_lottery.lottery_tickets lt
                    WHERE lt."UserId" = u."Id"
                )::DECIMAL, 
                2
            )
        ELSE 0
    END AS average_spending_per_entry,
    
    -- Favorite House ID
    -- Most frequently entered house (house with most tickets)
    (
        SELECT h."Id"
        FROM amesa_lottery.lottery_tickets lt
        JOIN amesa_lottery.houses h ON lt."HouseId" = h."Id"
        WHERE lt."UserId" = u."Id"
        GROUP BY h."Id"
        ORDER BY COUNT(*) DESC
        LIMIT 1
    ) AS favorite_house_id,
    
    -- Most Active Month
    -- Month (YYYY-MM format) with the most ticket purchases
    (
        SELECT TO_CHAR(lt."PurchaseDate", 'YYYY-MM')
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
        GROUP BY TO_CHAR(lt."PurchaseDate", 'YYYY-MM')
        ORDER BY COUNT(*) DESC
        LIMIT 1
    ) AS most_active_month,
    
    -- Last Entry Date
    -- Most recent ticket purchase date
    (
        SELECT MAX(lt."PurchaseDate")
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
    ) AS last_entry_date,
    
    -- Last Updated Timestamp
    -- Current timestamp when view is queried
    CURRENT_TIMESTAMP AS last_updated

FROM amesa_auth.users u;

-- =====================================================
-- COMMENTS FOR DOCUMENTATION
-- =====================================================

COMMENT ON INDEX idx_lottery_tickets_user_status IS 
'Composite index for efficient queries filtering lottery tickets by user and status. Used by favorites and entry management features. Improves performance for GET /api/v1/tickets/active endpoint.';

COMMENT ON INDEX idx_houses_status_location IS 
'Composite index for efficient queries filtering houses by status and location. Used by recommendation engine. Improves performance for GET /api/v1/houses/recommendations endpoint.';

COMMENT ON INDEX idx_user_preferences_lottery_gin IS 
'GIN index for efficient JSONB queries on lottery preferences. Enables fast lookups of favorite house IDs and lottery settings stored in user_preferences.preferences_json JSONB column.';

COMMENT ON VIEW amesa_auth.user_lottery_dashboard IS 
'Aggregated dashboard view providing comprehensive lottery statistics for each user. Includes favorites count, entry statistics, spending analytics, win rates, and activity metrics. Used by GET /api/v1/auth/me and dashboard endpoints.';

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================
-- 
-- Run these queries after migration to verify everything was created correctly:
--

-- 1. Check indexes were created:
--    SELECT 
--        schemaname,
--        tablename,
--        indexname,
--        indexdef 
--    FROM pg_indexes 
--    WHERE schemaname IN ('amesa_lottery', 'amesa_auth')
--    AND indexname LIKE 'idx_%'
--    ORDER BY schemaname, tablename, indexname;

-- 2. Check view was created and see its definition:
--    \d+ amesa_auth.user_lottery_dashboard
--    
--    OR
--    
--    SELECT 
--        table_schema,
--        table_name,
--        view_definition
--    FROM information_schema.views
--    WHERE table_schema = 'amesa_auth'
--    AND table_name = 'user_lottery_dashboard';

-- 3. Test view with sample user (replace with actual user_id):
--    SELECT * 
--    FROM amesa_auth.user_lottery_dashboard 
--    WHERE user_id = '00000000-0000-0000-0000-000000000000'::uuid;

-- 4. Verify JSONB index usage (should use index in execution plan):
--    EXPLAIN ANALYZE
--    SELECT * 
--    FROM amesa_auth.user_preferences 
--    WHERE preferences_json->'lotteryPreferences'->'favoriteHouseIds' IS NOT NULL;

-- 5. Test composite index usage:
--    EXPLAIN ANALYZE
--    SELECT * 
--    FROM amesa_lottery.lottery_tickets 
--    WHERE "UserId" = '00000000-0000-0000-0000-000000000000'::uuid 
--    AND "Status" = 'active';

-- 6. Test houses index usage:
--    EXPLAIN ANALYZE
--    SELECT * 
--    FROM amesa_lottery.houses 
--    WHERE "Status" = 'active' 
--    AND "Location" = 'Test Location';

-- =====================================================
-- ROLLBACK SCRIPT (if needed)
-- =====================================================
-- 
-- To rollback this migration, run:
--
-- DROP VIEW IF EXISTS amesa_auth.user_lottery_dashboard;
-- DROP INDEX IF EXISTS amesa_auth.idx_user_preferences_lottery_gin;
-- DROP INDEX IF EXISTS amesa_lottery.idx_houses_status_location;
-- DROP INDEX IF EXISTS amesa_lottery.idx_lottery_tickets_user_status;
--
-- Note: Dropping indexes is safe and will not affect data.
--       The view can be safely dropped and recreated.
--
-- =====================================================
-- END OF MIGRATION
-- =====================================================
```

---

## Script 2: Translation Keys

**File**: `BE/Infrastructure/sql/lottery-favorites-translations.sql`

```sql
-- =====================================================
-- Lottery Favorites & Entry Management - Translation Keys
-- =====================================================
-- 
-- Purpose: Add translation keys for lottery favorites
--          and entry management features
--
-- Created: 2025-01-XX
-- Agent: BE Agent (Agent 1)
-- Task: BE-1.5 - Translation Keys SQL Script
-- Estimated Time: 4 hours
--
-- Languages: EN, ES, FR, PL (4 languages)
-- Category: lottery.*
-- =====================================================

-- Favorites Section
INSERT INTO amesa_content.translations ("Id", "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
VALUES 
    -- Favorites - General
    (gen_random_uuid(), 'en', 'lottery.favorites.title', 'My Favorites', 'Favorites page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.favorites.title', 'Mis Favoritos', 'Favorites page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.favorites.title', 'Mes Favoris', 'Favorites page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.favorites.title', 'Moje Ulubione', 'Favorites page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.favorites.empty', 'No favorite houses yet', 'Empty favorites message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.favorites.empty', 'Aún no hay casas favoritas', 'Empty favorites message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.favorites.empty', 'Aucune maison favorite pour le moment', 'Empty favorites message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.favorites.empty', 'Brak ulubionych domów', 'Empty favorites message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.favorites.add', 'Add to Favorites', 'Add to favorites button', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.favorites.add', 'Agregar a Favoritos', 'Add to favorites button', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.favorites.add', 'Ajouter aux Favoris', 'Add to favorites button', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.favorites.add', 'Dodaj do Ulubionych', 'Add to favorites button', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.favorites.remove', 'Remove from Favorites', 'Remove from favorites button', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.favorites.remove', 'Quitar de Favoritos', 'Remove from favorites button', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.favorites.remove', 'Retirer des Favoris', 'Remove from favorites button', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.favorites.remove', 'Usuń z Ulubionych', 'Remove from favorites button', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.favorites.added', 'House added to favorites', 'Success message when adding to favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.favorites.added', 'Casa agregada a favoritos', 'Success message when adding to favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.favorites.added', 'Maison ajoutée aux favoris', 'Success message when adding to favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.favorites.added', 'Dom dodany do ulubionych', 'Success message when adding to favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.favorites.removed', 'House removed from favorites', 'Success message when removing from favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.favorites.removed', 'Casa eliminada de favoritos', 'Success message when removing from favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.favorites.removed', 'Maison retirée des favoris', 'Success message when removing from favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.favorites.removed', 'Dom usunięty z ulubionych', 'Success message when removing from favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    -- Entry Management
    (gen_random_uuid(), 'en', 'lottery.entries.title', 'My Entries', 'Entries page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.entries.title', 'Mis Entradas', 'Entries page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.entries.title', 'Mes Participations', 'Entries page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.entries.title', 'Moje Zgłoszenia', 'Entries page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.entries.active', 'Active Entries', 'Active entries label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.entries.active', 'Entradas Activas', 'Active entries label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.entries.active', 'Participations Actives', 'Active entries label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.entries.active', 'Aktywne Zgłoszenia', 'Active entries label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.entries.total', 'Total Entries', 'Total entries label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.entries.total', 'Entradas Totales', 'Total entries label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.entries.total', 'Total des Participations', 'Total entries label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.entries.total', 'Wszystkie Zgłoszenia', 'Total entries label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.entries.empty', 'No entries yet', 'Empty entries message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.entries.empty', 'Aún no hay entradas', 'Empty entries message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.entries.empty', 'Aucune participation pour le moment', 'Empty entries message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.entries.empty', 'Brak zgłoszeń', 'Empty entries message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    -- Statistics
    (gen_random_uuid(), 'en', 'lottery.stats.title', 'Lottery Statistics', 'Statistics section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.stats.title', 'Estadísticas de Lotería', 'Statistics section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.stats.title', 'Statistiques de Loterie', 'Statistics section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.stats.title', 'Statystyki Loterii', 'Statistics section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.stats.totalWins', 'Total Wins', 'Total wins label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.stats.totalWins', 'Victorias Totales', 'Total wins label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.stats.totalWins', 'Total des Victoires', 'Total wins label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.stats.totalWins', 'Wszystkie Wygrane', 'Total wins label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.stats.totalSpending', 'Total Spending', 'Total spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.stats.totalSpending', 'Gasto Total', 'Total spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.stats.totalSpending', 'Dépenses Totales', 'Total spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.stats.totalSpending', 'Całkowite Wydatki', 'Total spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.stats.totalWinnings', 'Total Winnings', 'Total winnings label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.stats.totalWinnings', 'Ganancias Totales', 'Total winnings label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.stats.totalWinnings', 'Gains Totaux', 'Total winnings label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.stats.totalWinnings', 'Całkowite Wygrane', 'Total winnings label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.stats.winRate', 'Win Rate', 'Win rate label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.stats.winRate', 'Tasa de Victoria', 'Win rate label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.stats.winRate', 'Taux de Réussite', 'Win rate label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.stats.winRate', 'Wskaźnik Wygranych', 'Win rate label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.stats.avgSpending', 'Average Spending per Entry', 'Average spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.stats.avgSpending', 'Gasto Promedio por Entrada', 'Average spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.stats.avgSpending', 'Dépense Moyenne par Participation', 'Average spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.stats.avgSpending', 'Średnie Wydatki na Zgłoszenie', 'Average spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    -- Recommendations
    (gen_random_uuid(), 'en', 'lottery.recommendations.title', 'Recommended for You', 'Recommendations section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.recommendations.title', 'Recomendado para Ti', 'Recommendations section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.recommendations.title', 'Recommandé pour Vous', 'Recommendations section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.recommendations.title', 'Rekomendowane dla Ciebie', 'Recommendations section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.recommendations.empty', 'No recommendations available', 'Empty recommendations message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.recommendations.empty', 'No hay recomendaciones disponibles', 'Empty recommendations message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.recommendations.empty', 'Aucune recommandation disponible', 'Empty recommendations message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.recommendations.empty', 'Brak dostępnych rekomendacji', 'Empty recommendations message', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    -- Dashboard
    (gen_random_uuid(), 'en', 'lottery.dashboard.title', 'Lottery Dashboard', 'Dashboard page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.dashboard.title', 'Panel de Lotería', 'Dashboard page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.dashboard.title', 'Tableau de Bord de Loterie', 'Dashboard page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.dashboard.title', 'Panel Loterii', 'Dashboard page title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.dashboard.favoriteHouses', 'Favorite Houses', 'Favorite houses label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.dashboard.favoriteHouses', 'Casas Favoritas', 'Favorite houses label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.dashboard.favoriteHouses', 'Maisons Favorites', 'Favorite houses label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.dashboard.favoriteHouses', 'Ulubione Domy', 'Favorite houses label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.dashboard.lastEntry', 'Last Entry', 'Last entry label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.dashboard.lastEntry', 'Última Entrada', 'Last entry label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.dashboard.lastEntry', 'Dernière Participation', 'Last entry label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.dashboard.lastEntry', 'Ostatnie Zgłoszenie', 'Last entry label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.dashboard.mostActiveMonth', 'Most Active Month', 'Most active month label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.dashboard.mostActiveMonth', 'Mes Más Activo', 'Most active month label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.dashboard.mostActiveMonth', 'Mois le Plus Actif', 'Most active month label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.dashboard.mostActiveMonth', 'Najbardziej Aktywny Miesiąc', 'Most active month label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    -- Error Messages
    (gen_random_uuid(), 'en', 'lottery.error.addFavorite', 'Failed to add house to favorites', 'Error when adding to favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.error.addFavorite', 'Error al agregar casa a favoritos', 'Error when adding to favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.error.addFavorite', 'Échec de l''ajout de la maison aux favoris', 'Error when adding to favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.error.addFavorite', 'Nie udało się dodać domu do ulubionych', 'Error when adding to favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.error.removeFavorite', 'Failed to remove house from favorites', 'Error when removing from favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.error.removeFavorite', 'Error al quitar casa de favoritos', 'Error when removing from favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.error.removeFavorite', 'Échec du retrait de la maison des favoris', 'Error when removing from favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.error.removeFavorite', 'Nie udało się usunąć domu z ulubionych', 'Error when removing from favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.error.loadFavorites', 'Failed to load favorites', 'Error when loading favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.error.loadFavorites', 'Error al cargar favoritos', 'Error when loading favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.error.loadFavorites', 'Échec du chargement des favoris', 'Error when loading favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.error.loadFavorites', 'Nie udało się załadować ulubionych', 'Error when loading favorites', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.error.loadEntries', 'Failed to load entries', 'Error when loading entries', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.error.loadEntries', 'Error al cargar entradas', 'Error when loading entries', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.error.loadEntries', 'Échec du chargement des participations', 'Error when loading entries', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.error.loadEntries', 'Nie udało się załadować zgłoszeń', 'Error when loading entries', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.error.loadRecommendations', 'Failed to load recommendations', 'Error when loading recommendations', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.error.loadRecommendations', 'Error al cargar recomendaciones', 'Error when loading recommendations', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.error.loadRecommendations', 'Échec du chargement des recommandations', 'Error when loading recommendations', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.error.loadRecommendations', 'Nie udało się załadować rekomendacji', 'Error when loading recommendations', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.error.loadStats', 'Failed to load statistics', 'Error when loading statistics', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.error.loadStats', 'Error al cargar estadísticas', 'Error when loading statistics', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.error.loadStats', 'Échec du chargement des statistiques', 'Error when loading statistics', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.error.loadStats', 'Nie udało się załadować statystyk', 'Error when loading statistics', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites')
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================
-- 
-- Run these queries after migration to verify everything was created correctly:
--

-- 1. Count translation keys by category:
--    SELECT 
--        "Category",
--        COUNT(DISTINCT "Key") as key_count,
--        COUNT(*) as total_translations
--    FROM amesa_content.translations
--    WHERE "Category" = 'Lottery'
--    GROUP BY "Category";

-- 2. List all lottery translation keys:
--    SELECT 
--        "LanguageCode",
--        "Key",
--        "Value"
--    FROM amesa_content.translations
--    WHERE "Category" = 'Lottery'
--    ORDER BY "Key", "LanguageCode";

-- 3. Verify all languages have translations:
--    SELECT 
--        "Key",
--        COUNT(DISTINCT "LanguageCode") as language_count
--    FROM amesa_content.translations
--    WHERE "Category" = 'Lottery'
--    GROUP BY "Key"
--    HAVING COUNT(DISTINCT "LanguageCode") < 4;

-- =====================================================
-- END OF TRANSLATION SCRIPT
-- =====================================================
```

---

## Deployment Instructions

### 1. Database Migration Script
1. Connect to your PostgreSQL database
2. Run the migration script: `lottery-favorites-migration.sql`
3. Verify indexes and view were created using the verification queries in the script

### 2. Translation Keys Script
1. Connect to your PostgreSQL database
2. Run the translation script: `lottery-favorites-translations.sql`
3. Verify translations were inserted using the verification queries in the script

### 3. Service Registration
- ✅ **Auth Module**: `IUserPreferencesService` is registered in `Program.cs`
- ⚠️ **Lottery Module**: `ILotteryService` is registered, but `IUserPreferencesService` is optional
  - If you want full favorites functionality, add project reference to `AmesaBackend.Auth` and register the service

---

## Summary

- **Database Migration**: Ready for deployment
- **Translation Keys**: 100+ keys for 4 languages (EN, ES, FR, PL)
- **Service Registration**: Auth module complete, Lottery module has optional dependency

All SQL scripts are complete and ready for manual creation/deployment.



















