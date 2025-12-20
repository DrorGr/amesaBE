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
--
-- Languages: EN, ES, FR, PL (4 languages)
-- Category: lottery.*
-- =====================================================

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
    (gen_random_uuid(), 'en', 'lottery.statistics.title', 'Lottery Statistics', 'Statistics section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.statistics.title', 'Estadísticas de Lotería', 'Statistics section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.statistics.title', 'Statistiques de Loterie', 'Statistics section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.statistics.title', 'Statystyki Loterii', 'Statistics section title', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.statistics.totalWins', 'Total Wins', 'Total wins label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.statistics.totalWins', 'Victorias Totales', 'Total wins label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.statistics.totalWins', 'Total des Victoires', 'Total wins label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.statistics.totalWins', 'Wszystkie Wygrane', 'Total wins label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.statistics.totalSpending', 'Total Spending', 'Total spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.statistics.totalSpending', 'Gasto Total', 'Total spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.statistics.totalSpending', 'Dépenses Totales', 'Total spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.statistics.totalSpending', 'Całkowite Wydatki', 'Total spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.statistics.totalWinnings', 'Total Winnings', 'Total winnings label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.statistics.totalWinnings', 'Ganancias Totales', 'Total winnings label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.statistics.totalWinnings', 'Gains Totaux', 'Total winnings label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.statistics.totalWinnings', 'Całkowite Wygrane', 'Total winnings label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.statistics.winRate', 'Win Rate', 'Win rate label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.statistics.winRate', 'Tasa de Victoria', 'Win rate label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.statistics.winRate', 'Taux de Réussite', 'Win rate label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.statistics.winRate', 'Wskaźnik Wygranych', 'Win rate label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

    (gen_random_uuid(), 'en', 'lottery.statistics.averageSpending', 'Average Spending per Entry', 'Average spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'es', 'lottery.statistics.averageSpending', 'Gasto Promedio por Entrada', 'Average spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'fr', 'lottery.statistics.averageSpending', 'Dépense Moyenne par Participation', 'Average spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),
    (gen_random_uuid(), 'pl', 'lottery.statistics.averageSpending', 'Średnie Wydatki na Zgłoszenie', 'Average spending label', 'Lottery', true, NOW(), NOW(), 'lottery-favorites', 'lottery-favorites'),

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
-- VERIFICATION QUERIES (Run these after script)
-- =====================================================

-- 1. Count translation keys by category:
-- SELECT 
--     "Category",
--     COUNT(DISTINCT "Key") as key_count,
--     COUNT(*) as total_translations
-- FROM amesa_content.translations
-- WHERE "Category" = 'Lottery'
-- GROUP BY "Category";

-- 2. List all lottery translation keys:
-- SELECT 
--     "LanguageCode",
--     "Key",
--     "Value"
-- FROM amesa_content.translations
-- WHERE "Category" = 'Lottery'
-- ORDER BY "Key", "LanguageCode";

-- 3. Verify all languages have translations:
-- SELECT 
--     "Key",
--     COUNT(DISTINCT "LanguageCode") as language_count
-- FROM amesa_content.translations
-- WHERE "Category" = 'Lottery'
-- GROUP BY "Key"
-- HAVING COUNT(DISTINCT "LanguageCode") < 4;

-- =====================================================
-- END OF TRANSLATION SCRIPT
-- =====================================================


























