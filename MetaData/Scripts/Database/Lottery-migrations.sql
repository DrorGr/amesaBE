CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'amesa_lottery') THEN
            CREATE SCHEMA amesa_lottery;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE TABLE amesa_lottery.houses (
        "Id" uuid NOT NULL,
        "Title" character varying(255) NOT NULL,
        "Description" text,
        "Price" numeric(15,2) NOT NULL,
        "Location" character varying(255) NOT NULL,
        "Address" text,
        "Bedrooms" integer NOT NULL,
        "Bathrooms" integer NOT NULL,
        "SquareFeet" integer,
        "PropertyType" character varying(50),
        "YearBuilt" integer,
        "LotSize" numeric(10,2),
        "Features" text[],
        "Status" character varying(50) NOT NULL,
        "TotalTickets" integer NOT NULL,
        "TicketPrice" numeric(10,2) NOT NULL,
        "LotteryStartDate" timestamp with time zone,
        "LotteryEndDate" timestamp with time zone NOT NULL,
        "DrawDate" timestamp with time zone,
        "MinimumParticipationPercentage" numeric(5,2) NOT NULL,
        "CreatedBy" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "DeletedAt" timestamp with time zone,
        CONSTRAINT "PK_houses" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE TABLE amesa_lottery.house_images (
        "Id" uuid NOT NULL,
        "HouseId" uuid NOT NULL,
        "ImageUrl" text NOT NULL,
        "AltText" character varying(255),
        "DisplayOrder" integer NOT NULL,
        "IsPrimary" boolean NOT NULL,
        "MediaType" character varying(50) NOT NULL,
        "FileSize" integer,
        "Width" integer,
        "Height" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_house_images" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_house_images_houses_HouseId" FOREIGN KEY ("HouseId") REFERENCES amesa_lottery.houses ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE TABLE amesa_lottery.lottery_draws (
        "Id" uuid NOT NULL,
        "HouseId" uuid NOT NULL,
        "DrawDate" timestamp with time zone NOT NULL,
        "TotalTicketsSold" integer NOT NULL,
        "TotalParticipationPercentage" numeric(5,2) NOT NULL,
        "WinningTicketNumber" character varying(20),
        "WinningTicketId" uuid,
        "WinnerUserId" uuid,
        "DrawStatus" character varying(50) NOT NULL,
        "DrawMethod" character varying(50) NOT NULL,
        "DrawSeed" character varying(255),
        "ConductedBy" uuid,
        "ConductedAt" timestamp with time zone,
        "VerificationHash" character varying(255),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_lottery_draws" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_lottery_draws_houses_HouseId" FOREIGN KEY ("HouseId") REFERENCES amesa_lottery.houses ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE TABLE amesa_lottery.lottery_tickets (
        "Id" uuid NOT NULL,
        "TicketNumber" character varying(20) NOT NULL,
        "HouseId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "PurchasePrice" numeric(10,2) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "PurchaseDate" timestamp with time zone NOT NULL,
        "PaymentId" uuid,
        "IsWinner" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_lottery_tickets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_lottery_tickets_houses_HouseId" FOREIGN KEY ("HouseId") REFERENCES amesa_lottery.houses ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE INDEX "IX_house_images_HouseId" ON amesa_lottery.house_images ("HouseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE INDEX "IX_houses_Status" ON amesa_lottery.houses ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE INDEX "IX_lottery_draws_DrawDate" ON amesa_lottery.lottery_draws ("DrawDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE INDEX "IX_lottery_draws_HouseId" ON amesa_lottery.lottery_draws ("HouseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE INDEX "IX_lottery_tickets_HouseId" ON amesa_lottery.lottery_tickets ("HouseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE INDEX "IX_lottery_tickets_TicketNumber" ON amesa_lottery.lottery_tickets ("TicketNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    CREATE INDEX "IX_lottery_tickets_UserId" ON amesa_lottery.lottery_tickets ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251116172217_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251116172217_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

