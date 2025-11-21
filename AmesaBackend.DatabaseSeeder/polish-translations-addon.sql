-- Polish translations addon for all existing translation keys
-- This adds Polish (pl) translations to match all existing English translations

INSERT INTO amesa_content.translations ("Id", "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
VALUES 
    -- Navigation (Polish)
    (uuid_generate_v4(), 'pl', 'nav.lotteries', 'Loterie', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'nav.promotions', 'Promocje', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'nav.winners', 'Zwycięzcy', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'nav.signIn', 'Zaloguj się', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'nav.logout', 'Wyloguj się', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'nav.welcome', 'Witamy', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'nav.memberSettings', 'Ustawienia Członka', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'nav.home', 'Strona Główna', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),

    -- Hero Section (Polish)
    (uuid_generate_v4(), 'pl', 'hero.title', 'Wygraj Dom Swoich Marzeń', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'hero.browseLotteries', 'Przeglądaj Loterie', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'hero.howItWorks', 'Jak To Działa', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),

    -- Authentication (Polish)
    (uuid_generate_v4(), 'pl', 'auth.signIn', 'Zaloguj się', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.signUp', 'Zarejestruj się', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.createAccount', 'Utwórz Konto', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.email', 'Email', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.password', 'Hasło', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.fullName', 'Imię i Nazwisko', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.forgotPassword', 'Zapomniałeś hasła?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.processing', 'Przetwarzanie...', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.dontHaveAccount', 'Nie masz konta?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.alreadyHaveAccount', 'Masz już konto?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.continueWithGoogle', 'Kontynuuj z Google', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.continueWithMeta', 'Kontynuuj z Meta', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.continueWithApple', 'Kontynuuj z Apple', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'auth.or', 'LUB', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),

    -- House/Property (Polish)
    (uuid_generate_v4(), 'pl', 'house.currentlyViewing', 'Obecnie Oglądasz', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.propertyOfYourOwn', 'Twoja własna nieruchomość', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.buyTicket', 'Kup Bilet', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.bed', 'łóżko', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.bath', 'łazienka', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.sqft', 'm²', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.city', 'Miasto', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.address', 'Adres', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.odds', 'Szanse', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.lotteryCountdown', 'Odliczanie do Loterii', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.processing', 'Przetwarzanie...', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.signInToParticipate', 'Zaloguj się, aby uczestniczyć', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.perTicket', 'za bilet', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.active', 'Aktywny', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.ended', 'Zakończony', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.upcoming', 'Nadchodzący', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'house.onlyTicketsAvailable', 'Tylko %d biletów dostępnych', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),

    -- Carousel (Polish)
    (uuid_generate_v4(), 'pl', 'carousel.propertyValue', 'Wartość Nieruchomości', 'Carousel', 'Carousel', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'carousel.ticketPrice', 'Cena Biletu', 'Carousel', 'Carousel', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'carousel.buyTicket', 'Kup Bilet', 'Carousel', 'Carousel', true, NOW(), NOW(), 'System', 'System'),

    -- Common (Polish)
    (uuid_generate_v4(), 'pl', 'common.loading', 'Ładowanie...', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'common.error', 'Błąd', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'common.success', 'Sukces', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'common.save', 'Zapisz', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'common.cancel', 'Anuluj', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'common.confirm', 'Potwierdź', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'common.close', 'Zamknij', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),

    -- Footer (Polish)
    (uuid_generate_v4(), 'pl', 'footer.description', 'Wygraj dom swoich marzeń dzięki naszym ekskluzywnym loteriom nieruchomości.', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.community', 'Społeczność', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.about', 'O Nas', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.makeSponsorship', 'Zostań Sponsorem', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.responsibleGaming', 'Odpowiedzialna Gra', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.support', 'Wsparcie', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.helpCenter', 'Centrum Pomocy', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.liveChat', 'Czat na Żywo', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.contactUs', 'Skontaktuj się z Nami', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.faq', 'FAQ', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.drawCalendar', 'Kalendarz Losowań', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.branchMap', 'Mapa Oddziałów', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.legal', 'Prawne', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.regulations', 'Regulamin', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.termsConditions', 'Warunki Użytkowania', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.privacyPolicy', 'Polityka Prywatności', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.gdprInfo', 'Informacje RODO', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.news', 'Aktualności', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.legalPartners', 'Partnerzy Prawni', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'footer.comingSoon', 'Wkrótce', 'Footer', 'Footer', true, NOW(), NOW(), 'System', 'System'),

    -- Partners (Polish)
    (uuid_generate_v4(), 'pl', 'partners.legalPartner', 'Partner Prawny', 'Partners', 'Partners', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'partners.accountingPartner', 'Partner Księgowy', 'Partners', 'Partners', true, NOW(), NOW(), 'System', 'System'),

    -- Houses (Polish)
    (uuid_generate_v4(), 'pl', 'houses.title', 'Dostępne Loterie', 'Houses', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'houses.noLotteries', 'Brak dostępnych loterii', 'Houses', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'pl', 'houses.checkBack', 'Sprawdź ponownie wkrótce', 'Houses', 'Houses', true, NOW(), NOW(), 'System', 'System');
