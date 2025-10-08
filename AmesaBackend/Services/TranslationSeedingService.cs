using Microsoft.EntityFrameworkCore;
using AmesaBackend.Data;
using AmesaBackend.Models;

namespace AmesaBackend.Services
{
    public static class TranslationSeedingService
    {
        public static async Task SeedTranslationsAsync(AmesaDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if translations already exist
            if (await context.Translations.AnyAsync())
            {
                return; // Data already seeded
            }

            // Seed Languages first
            await SeedLanguagesAsync(context);

            // Seed English translations
            await SeedEnglishTranslationsAsync(context);

            // Seed Polish translations
            await SeedPolishTranslationsAsync(context);

            // Save all changes
            await context.SaveChangesAsync();
        }

        private static async Task SeedLanguagesAsync(AmesaDbContext context)
        {
            var languages = new List<Language>
            {
                new Language
                {
                    Code = "en",
                    Name = "English",
                    NativeName = "English",
                    FlagUrl = "https://flagcdn.com/w40/us.png",
                    IsActive = true,
                    IsDefault = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Language
                {
                    Code = "pl",
                    Name = "Polish",
                    NativeName = "Polski",
                    FlagUrl = "https://flagcdn.com/w40/pl.png",
                    IsActive = true,
                    IsDefault = false,
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Languages.AddRangeAsync(languages);
        }

        private static async Task SeedEnglishTranslationsAsync(AmesaDbContext context)
        {
            var englishTranslations = new List<Translation>
            {
                // Navigation
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.lotteries", Value = "Lotteries", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.promotions", Value = "Promotions", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.howItWorks", Value = "How It Works", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.winners", Value = "Winners", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.about", Value = "About", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.sponsorship", Value = "Sponsorship", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.faq", Value = "FAQ", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.help", Value = "Help", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.partners", Value = "Partners", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.responsibleGambling", Value = "Responsible Gambling", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.register", Value = "Register", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.memberSettings", Value = "My Account", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.signIn", Value = "Sign In", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.getStarted", Value = "Get Started", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.welcome", Value = "Welcome", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "nav.logout", Value = "Logout", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Hero Section
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.title", Value = "Win Your Dream Home", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.subtitle", Value = "Enter exclusive house lotteries and get the chance to win amazing properties at a fraction of their market value.", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.browseLotteries", Value = "Browse Lotteries", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.howItWorks", Value = "How It Works", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // How It Works Page
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.heroTitle", Value = "How Amesa Lottery Works", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.heroSubtitle", Value = "Your path to winning a dream home is simple and transparent.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.simpleProcess", Value = "Simple Process", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.introduction", Value = "Participating in our house lotteries is straightforward and secure. Follow these simple steps to get started on your journey to homeownership.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.step1Title", Value = "Choose Your Lottery", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.step1Desc", Value = "Browse our exclusive selection of luxury homes. Each property is a separate lottery with a limited number of tickets.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.step2Title", Value = "Buy Tickets", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.step2Desc", Value = "Purchase tickets for your chosen lottery. The more tickets you buy, the higher your chances of winning. All transactions are secure and transparent.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.step3Title", Value = "Win & Own", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.step3Desc", Value = "If you win, you become the proud owner of your dream property with all legal fees covered.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.readyToStart", Value = "Ready to Get Started?", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.ctaDescription", Value = "Join thousands of participants who are already on their way to winning their dream homes.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "howItWorks.browseLotteries", Value = "Browse Available Lotteries", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Lottery Results
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.title", Value = "Lottery Results", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.subtitle", Value = "View all lottery results, winners, and prize information.", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.filters", Value = "Filters", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.fromDate", Value = "From Date", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.toDate", Value = "To Date", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.address", Value = "Address", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.addressPlaceholder", Value = "Enter address...", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.prizePosition", Value = "Prize Position", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.allPositions", Value = "All Positions", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.firstPlace", Value = "First Place", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.secondPlace", Value = "Second Place", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.thirdPlace", Value = "Third Place", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.clearFilters", Value = "Clear Filters", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.firstPlaceWinners", Value = "First Place Winners", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.secondPlaceWinners", Value = "Second Place Winners", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.thirdPlaceWinners", Value = "Third Place Winners", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.loading", Value = "Loading results...", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.error", Value = "Error loading results", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.winner", Value = "Winner", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.ticketNumber", Value = "Ticket Number", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.claimed", Value = "Claimed", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.unclaimed", Value = "Unclaimed", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.viewQR", Value = "View QR Code", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.previous", Value = "Previous", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.next", Value = "Next", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.noResults", Value = "No results found", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.noResultsDescription", Value = "Try adjusting your filters to find more results.", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.qrCodeTitle", Value = "Winner QR Code", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.qrCode", Value = "QR Code", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.prize", Value = "Prize", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.close", Value = "Close", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.download", Value = "Download", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.backToResults", Value = "Back to Results", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.resultDetails", Value = "Result Details", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.resultDetailsSubtitle", Value = "Complete information about this lottery result", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.drawDate", Value = "Draw Date", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.winnerInformation", Value = "Winner Information", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.email", Value = "Email", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.prizeInformation", Value = "Prize Information", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.prizeValue", Value = "Prize Value", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.verificationQR", Value = "Verification QR Code", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.clickToReveal", Value = "Click to Reveal", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.clickToRevealDescription", Value = "Click to see the winner QR code", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.spinning", Value = "Spinning...", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.spinningDescription", Value = "Please wait while we reveal the result", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.qrCodeDescription", Value = "Winner verification QR code", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.qrCodeInstructions", Value = "Scan this QR code to verify the winner and claim your prize", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.claimStatus", Value = "Claim Status", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.claimedOn", Value = "Claimed on", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.downloadQR", Value = "Download QR Code", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "lotteryResults.share", Value = "Share Result", Category = "LotteryResults", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Footer
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.description", Value = "Your trusted partner in making homeownership dreams come true through transparent and fair lottery systems.", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.supportCause", Value = "Supporting a Good Cause", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.supportDescription", Value = "Every ticket you purchase contributes to charitable causes and community development projects. Together, we're building a better future.", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.community", Value = "Community", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.about", Value = "About Us", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.makeSponsorship", Value = "Make Sponsorship", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.partners", Value = "Partners", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.responsibleGaming", Value = "Responsible Gaming", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.support", Value = "Support", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.helpCenter", Value = "Help Center", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.liveChat", Value = "Live Chat", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.contactUs", Value = "Contact Us", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.faq", Value = "FAQ", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.drawCalendar", Value = "Draw Calendar", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.branchMap", Value = "Branch Map", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.legal", Value = "Legal", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.regulations", Value = "Regulations", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.termsConditions", Value = "Terms & Conditions", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.privacyPolicy", Value = "Privacy Policy", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.gdprInfo", Value = "GDPR Info", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.news", Value = "News", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.legalPartners", Value = "Legal Partners", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.comingSoon", Value = "Coming Soon..", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                
                // Partners Section
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "partners.legalPartner", Value = "Legal Office", Category = "Partners", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "partners.accountingPartner", Value = "Accounting Partner", Category = "Partners", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "footer.copyright", Value = "© 2024 Amesa Group. All rights reserved. Licensed and regulated lottery operator.", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Stats Section
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "stats.oddsToWin", Value = "Odds to Win", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "stats.currentPrizes", Value = "Current Prizes", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "stats.activeLotteries", Value = "Active Lotteries", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "stats.satisfaction", Value = "Satisfaction Rate", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "stats.happyWinners", Value = "Happy Winners", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "stats.totalPrizes", Value = "Total Prizes Won", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "stats.firstPrizeWinners", Value = "1st Prize Winners", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "stats.secondPrizeWinners", Value = "2nd Prize Winners", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "stats.thirdPrizeWinners", Value = "3rd Prize Winners", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Carousel Section
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "carousel.propertyValue", Value = "Property Value", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "carousel.ticketPrice", Value = "Ticket Price", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "carousel.ticketsSold", Value = "Tickets Sold", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "carousel.drawDate", Value = "Draw Date", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "carousel.progress", Value = "Progress", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "carousel.buyTicket", Value = "Buy Ticket", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.happyWinners", Value = "Happy Winners", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.propertiesWon", Value = "Properties Won", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.satisfaction", Value = "Satisfaction", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.winnerKeys", Value = "Winner Keys", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.dreamHome", Value = "Dream Home", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.totalSlots", Value = "Total Slots", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "hero.totalPrize", Value = "Total Prize", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Authentication
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.signIn", Value = "Sign In", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.createAccount", Value = "Create Account", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.processing", Value = "Processing...", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.resetPassword", Value = "Reset Password", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.resetPasswordDescription", Value = "Enter your email address and we'll send you a link to reset your password.", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.enterEmail", Value = "Enter your email", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.emailSent", Value = "Email Sent", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.emailSentTo", Value = "We've sent a password reset link to", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.checkEmailInstructions", Value = "Check your email and follow the instructions to reset your password.", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.backToLogin", Value = "Back to Login", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.resendEmail", Value = "Resend Email", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.resending", Value = "Resending...", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.fullName", Value = "Full Name", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.email", Value = "Email", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.password", Value = "Password", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.confirmPassword", Value = "Confirm Password", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.forgotPassword", Value = "Forgot Password?", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.continueWithGoogle", Value = "Continue with Google", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.continueWithMeta", Value = "Continue with Meta", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.continueWithApple", Value = "Continue with Apple", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.continueWithTwitter", Value = "Continue with Twitter", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.or", Value = "OR", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.alreadyHaveAccount", Value = "Already have an account?", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.dontHaveAccount", Value = "Don't have an account?", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.signUpHere", Value = "Sign up here", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "auth.signInHere", Value = "Sign in here", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // House/Property
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.title", Value = "Active Lotteries", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.noLotteries", Value = "No Active Lotteries", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.checkBack", Value = "Check back soon for new lottery opportunities!", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.bedrooms", Value = "Bedrooms", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.bathrooms", Value = "Bathrooms", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.sqft", Value = "Sq Ft", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.ticketPrice", Value = "Ticket Price", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.totalTickets", Value = "Total Tickets", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.soldTickets", Value = "Sold Tickets", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.remainingTickets", Value = "Remaining Tickets", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.lotteryEnds", Value = "Lottery Ends", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.purchaseTicket", Value = "Purchase Ticket", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "houses.viewDetails", Value = "View Details", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // House Card (individual house display)
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.bed", Value = "bed", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.bath", Value = "bath", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.sqft", Value = "sq ft", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.ticketsSold", Value = "tickets sold", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.lotteryEnds", Value = "Lottery ends", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.processing", Value = "Processing...", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.buyTicket", Value = "Buy Ticket", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.signInToParticipate", Value = "Buy Ticket", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.perTicket", Value = "per ticket", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.active", Value = "Active", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.ended", Value = "Ended", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.upcoming", Value = "Upcoming", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.odds", Value = "Odds", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.onlyTicketsAvailable", Value = "Only {count} tickets available", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.lotteryDate", Value = "Lottery Date", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.city", Value = "City", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.address", Value = "Address", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.currentlyViewing", Value = "Currently Viewing", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.lotteryCountdown", Value = "Time Remaining", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "house.propertyOfYourOwn", Value = "Property of your own with a price of obiad", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Chatbot
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.title", Value = "Amesa Assistant", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.subtitle", Value = "Online now", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.welcomeMessage", Value = "Hi! How can I help you with your lottery questions?", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.placeholder", Value = "Type your message...", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.response.help", Value = "I'm here to help! You can ask me about lotteries, tickets, prizes, or how our platform works.", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.response.lottery", Value = "Our lotteries offer amazing prizes including luxury properties! Each lottery has different odds and ticket prices.", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.response.tickets", Value = "You can purchase tickets directly from any active lottery. Just click 'Buy Ticket' on the lottery you're interested in!", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.response.winners", Value = "We've had hundreds of happy winners! Check out our statistics section to see our latest prize winners.", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.response.contact", Value = "For more detailed assistance, you can contact our support team through the Help Center or FAQ section.", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.hideWidget", Value = "Hide chatbot", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "chatbot.close", Value = "Close chat", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Accessibility
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.title", Value = "Accessibility Settings", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.subtitle", Value = "Customize your experience", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.close", Value = "Close accessibility settings", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.toggleWidget", Value = "Toggle accessibility settings", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.quickActions", Value = "Quick Actions", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.reset", Value = "Reset", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.save", Value = "Save", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.fontSize", Value = "Font Size", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.contrast", Value = "Contrast", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.contrastNormal", Value = "Normal", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.contrastHigh", Value = "High Contrast", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.contrastInverted", Value = "Inverted Colors", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.colorBlind", Value = "Color Blind Support", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.colorBlindNone", Value = "None", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.colorBlindProtanopia", Value = "Protanopia (Red-Green)", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.colorBlindDeuteranopia", Value = "Deuteranopia (Red-Green)", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.colorBlindTritanopia", Value = "Tritanopia (Blue-Yellow)", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.cursorSize", Value = "Cursor Size", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.cursorNormal", Value = "Normal", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.cursorLarge", Value = "Large", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.cursorExtraLarge", Value = "Extra Large", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.toggleSettings", Value = "Toggle Settings", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.reduceMotion", Value = "Reduce Motion", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.focusIndicator", Value = "Enhanced Focus", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.textSpacing", Value = "Text Spacing", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.linkHighlight", Value = "Link Highlight", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.readingGuide", Value = "Reading Guide", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "en", Key = "accessibility.hideWidget", Value = "Hide accessibility widget", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
            };

            await context.Translations.AddRangeAsync(englishTranslations);
        }

        private static async Task SeedPolishTranslationsAsync(AmesaDbContext context)
        {
            var polishTranslations = new List<Translation>
            {
                // Navigation
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.lotteries", Value = "Loterie", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.promotions", Value = "Promocje", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.howItWorks", Value = "Jak to działa", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.winners", Value = "Zwycięzcy", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.about", Value = "O nas", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.sponsorship", Value = "Sponsoring", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.faq", Value = "FAQ", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.help", Value = "Pomoc", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.partners", Value = "Partnerzy", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.responsibleGambling", Value = "Odpowiedzialna Gra", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.register", Value = "Zarejestruj się", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.memberSettings", Value = "Moje Konto", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.signIn", Value = "Zaloguj się", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.getStarted", Value = "Rozpocznij", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.welcome", Value = "Witaj", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "nav.logout", Value = "Wyloguj", Category = "Navigation", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Hero Section
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.title", Value = "Wygraj Dom Swoich Marzeń", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.subtitle", Value = "Weź udział w ekskluzywnych loteriach domów i miej szansę wygrać niesamowite nieruchomości za ułamek ich wartości rynkowej.", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.browseLotteries", Value = "Przeglądaj Loterie", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.howItWorks", Value = "Jak to działa", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // How It Works Page (Polish)
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.heroTitle", Value = "Jak działa Loteria Amesa", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.heroSubtitle", Value = "Twoja droga do wygrania wymarzonego domu jest prosta i przejrzysta.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.simpleProcess", Value = "Prosty Proces", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.introduction", Value = "Udział w naszych loteriach domów jest prosty i bezpieczny. Wykonaj te proste kroki, aby rozpocząć swoją podróż do posiadania domu.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.step1Title", Value = "Wybierz swoją loterię", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.step1Desc", Value = "Przeglądaj naszą ekskluzywną ofertę luksusowych domów. Każda nieruchomość to osobna loteria z ograniczoną liczbą biletów.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.step2Title", Value = "Kup bilety", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.step2Desc", Value = "Kup bilety na wybraną loterię. Im więcej biletów kupisz, tym większe masz szanse na wygraną. Wszystkie transakcje są bezpieczne i przejrzyste.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.step3Title", Value = "Wygraj i Posiadaj", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.step3Desc", Value = "Jeśli wygrasz, stajesz się dumnym właścicielem wymarzonej nieruchomości ze wszystkimi opłatami prawnymi pokrytymi.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.readyToStart", Value = "Gotowy, aby zacząć?", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.ctaDescription", Value = "Dołącz do tysięcy uczestników, którzy już są na drodze do wygrania swoich wymarzonych domów.", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "howItWorks.browseLotteries", Value = "Przeglądaj dostępne loterie", Category = "HowItWorks", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Footer (Polish)
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.description", Value = "Twój zaufany partner w realizacji marzeń o własnym domu poprzez przejrzyste i uczciwe systemy loterii.", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.supportCause", Value = "Wspieranie Dobrej Sprawy", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.supportDescription", Value = "Każdy zakupiony bilet przyczynia się do celów charytatywnych i projektów rozwoju społeczności. Razem budujemy lepszą przyszłość.", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.community", Value = "Społeczność", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.about", Value = "O nas", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.makeSponsorship", Value = "Zostań Sponsorem", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.partners", Value = "Partnerzy", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.responsibleGaming", Value = "Odpowiedzialna Gra", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.support", Value = "Wsparcie", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.helpCenter", Value = "Centrum Pomocy", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.liveChat", Value = "Czat na Żywo", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.contactUs", Value = "Skontaktuj się z nami", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.faq", Value = "FAQ", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.drawCalendar", Value = "Kalendarz Losowań", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.branchMap", Value = "Mapa Oddziałów", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.legal", Value = "Prawne", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.regulations", Value = "Regulaminy", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.termsConditions", Value = "Warunki i Zasady", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.privacyPolicy", Value = "Polityka Prywatności", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.gdprInfo", Value = "Informacje RODO", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.news", Value = "Aktualności", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.legalPartners", Value = "Partnerzy Prawni", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.comingSoon", Value = "Wkrótce..", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                
                // Partners Section (Polish)
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "partners.legalPartner", Value = "Biuro Prawne", Category = "Partners", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "partners.accountingPartner", Value = "Partner Księgowy", Category = "Partners", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "footer.copyright", Value = "© 2024 Grupa Amesa. Wszelkie prawa zastrzeżone. Licencjonowany i regulowany operator loterii.", Category = "Footer", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Stats Section (Polish)
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "stats.oddsToWin", Value = "Szanse na Wygraną", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "stats.currentPrizes", Value = "Aktualne Nagrody", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "stats.activeLotteries", Value = "Aktywne Loterie", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "stats.satisfaction", Value = "Wskaźnik Zadowolenia", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "stats.happyWinners", Value = "Szczęśliwi Zwycięzcy", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "stats.totalPrizes", Value = "Łączne Wygrane Nagrody", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "stats.firstPrizeWinners", Value = "Zwycięzcy 1. Nagrody", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "stats.secondPrizeWinners", Value = "Zwycięzcy 2. Nagrody", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "stats.thirdPrizeWinners", Value = "Zwycięzcy 3. Nagrody", Category = "Stats", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Carousel Section (Polish)
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "carousel.propertyValue", Value = "Wartość Nieruchomości", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "carousel.ticketPrice", Value = "Cena Biletu", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "carousel.ticketsSold", Value = "Sprzedane Bilety", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "carousel.drawDate", Value = "Data Losowania", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "carousel.progress", Value = "Postęp", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "carousel.buyTicket", Value = "Kup Bilet", Category = "Carousel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.happyWinners", Value = "Szczęśliwi Zwycięzcy", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.propertiesWon", Value = "Wygrane Nieruchomości", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.satisfaction", Value = "Zadowolenie", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.winnerKeys", Value = "Klucze Zwycięzcy", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.dreamHome", Value = "Dom Marzeń", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.totalSlots", Value = "Łączne Sloty", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "hero.totalPrize", Value = "Łączna Nagroda", Category = "Hero", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Authentication
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.signIn", Value = "Zaloguj się", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.createAccount", Value = "Utwórz Konto", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.fullName", Value = "Imię i Nazwisko", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.email", Value = "Email", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.password", Value = "Hasło", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.confirmPassword", Value = "Potwierdź Hasło", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.forgotPassword", Value = "Zapomniałeś hasła?", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.continueWithGoogle", Value = "Kontynuuj z Google", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.continueWithMeta", Value = "Kontynuuj z Meta", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.continueWithApple", Value = "Kontynuuj z Apple", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.continueWithTwitter", Value = "Kontynuuj z Twitter", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.or", Value = "LUB", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.alreadyHaveAccount", Value = "Masz już konto?", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.dontHaveAccount", Value = "Nie masz konta?", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.signUpHere", Value = "Zarejestruj się tutaj", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "auth.signInHere", Value = "Zaloguj się tutaj", Category = "Authentication", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // House/Property
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.title", Value = "Aktywne Loterie", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.noLotteries", Value = "Brak Aktywnych Loterii", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.checkBack", Value = "Sprawdź wkrótce nowe możliwości loterii!", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.bedrooms", Value = "Sypialnie", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.bathrooms", Value = "Łazienki", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.sqft", Value = "M²", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.ticketPrice", Value = "Cena Biletu", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.totalTickets", Value = "Łączne Bilety", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.soldTickets", Value = "Sprzedane Bilety", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.remainingTickets", Value = "Pozostałe Bilety", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.lotteryEnds", Value = "Loterie Kończy Się", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.purchaseTicket", Value = "Kup Bilet", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "houses.viewDetails", Value = "Zobacz Szczegóły", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // House Card (individual house display) - Polish
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.bed", Value = "sypialnia", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.bath", Value = "łazienka", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.sqft", Value = "m²", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.ticketsSold", Value = "biletów sprzedanych", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.lotteryEnds", Value = "Loteria kończy się", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.processing", Value = "Przetwarzanie...", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.buyTicket", Value = "Kup Bilet", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.signInToParticipate", Value = "Kup Bilet", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.perTicket", Value = "za bilet", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.active", Value = "Aktywny", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.ended", Value = "Zakończony", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.upcoming", Value = "Nadchodzący", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.odds", Value = "Szanse", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.onlyTicketsAvailable", Value = "Tylko {count} biletów dostępnych", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.lotteryDate", Value = "Data Loterii", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.city", Value = "Miasto", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.address", Value = "Adres", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.currentlyViewing", Value = "Obecnie Oglądają", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.lotteryCountdown", Value = "Pozostały Czas", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "house.propertyOfYourOwn", Value = "Nieruchomość na własność w cenie obiadu", Category = "Houses", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Chatbot (Polish)
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.title", Value = "Asystent Amesa", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.subtitle", Value = "Online teraz", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.welcomeMessage", Value = "Cześć! Jak mogę pomóc z pytaniami o loterie?", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.placeholder", Value = "Napisz swoją wiadomość...", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.response.help", Value = "Jestem tutaj, aby pomóc! Możesz zapytać mnie o loterie, bilety, nagrody lub jak działa nasza platforma.", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.response.lottery", Value = "Nasze loterie oferują niesamowite nagrody, w tym luksusowe nieruchomości! Każda loteria ma różne szanse i ceny biletów.", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.response.tickets", Value = "Możesz kupić bilety bezpośrednio z dowolnej aktywnej loterii. Po prostu kliknij 'Kup Bilet' przy loterii, która Cię interesuje!", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.response.winners", Value = "Mieliśmy setki szczęśliwych zwycięzców! Sprawdź naszą sekcję statystyk, aby zobaczyć najnowszych zwycięzców nagród.", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.response.contact", Value = "Aby uzyskać bardziej szczegółową pomoc, możesz skontaktować się z naszym zespołem wsparcia przez Centrum Pomocy lub sekcję FAQ.", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.hideWidget", Value = "Ukryj chatbota", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "chatbot.close", Value = "Zamknij czat", Category = "Chatbot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },

                // Accessibility (Polish)
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.title", Value = "Ustawienia Dostępności", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.subtitle", Value = "Dostosuj swoje doświadczenie", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.close", Value = "Zamknij ustawienia dostępności", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.toggleWidget", Value = "Przełącz ustawienia dostępności", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.quickActions", Value = "Szybkie Akcje", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.reset", Value = "Resetuj", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.save", Value = "Zapisz", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.fontSize", Value = "Rozmiar Czcionki", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.contrast", Value = "Kontrast", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.contrastNormal", Value = "Normalny", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.contrastHigh", Value = "Wysoki Kontrast", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.contrastInverted", Value = "Odwrócone Kolory", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.colorBlind", Value = "Wsparcie Daltonizmu", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.colorBlindNone", Value = "Brak", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.colorBlindProtanopia", Value = "Protanopia (Czerwono-Zielona)", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.colorBlindDeuteranopia", Value = "Deuteranopia (Czerwono-Zielona)", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.colorBlindTritanopia", Value = "Tritanopia (Niebiesko-Żółta)", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.cursorSize", Value = "Rozmiar Kursora", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.cursorNormal", Value = "Normalny", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.cursorLarge", Value = "Duży", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.cursorExtraLarge", Value = "Bardzo Duży", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.toggleSettings", Value = "Ustawienia Przełączników", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.reduceMotion", Value = "Ogranicz Animacje", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.focusIndicator", Value = "Wzmocniony Fokus", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.textSpacing", Value = "Odstępy w Tekście", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.linkHighlight", Value = "Podświetlenie Linków", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.readingGuide", Value = "Przewodnik Czytania", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), LanguageCode = "pl", Key = "accessibility.hideWidget", Value = "Ukryj widget dostępności", Category = "Accessibility", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "System" },
            };

            await context.Translations.AddRangeAsync(polishTranslations);
        }
    }
}
