-- Amesa EF Core migration script for LotteryResults
-- 2025-11-16T18:55:29.3261825+02:00
SET search_path TO amesa_lottery_results;
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'amesa_lottery_results') THEN
            CREATE SCHEMA amesa_lottery_results;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
    CREATE TABLE amesa_lottery_results.lottery_results (
        "Id" uuid NOT NULL,
        "LotteryId" uuid NOT NULL,
        "DrawId" uuid NOT NULL,
        "WinnerTicketNumber" character varying(20) NOT NULL,
        "WinnerUserId" uuid NOT NULL,
        "PrizePosition" integer NOT NULL,
        "PrizeType" character varying(100) NOT NULL,
        "PrizeValue" numeric(18,2) NOT NULL,
        "PrizeDescription" character varying(500),
        "QRCodeData" character varying(500) NOT NULL,
        "QRCodeImageUrl" character varying(255),
        "IsVerified" boolean NOT NULL,
        "IsClaimed" boolean NOT NULL,
        "ClaimedAt" timestamp with time zone,
        "ClaimNotes" character varying(1000),
        "ResultDate" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_lottery_results" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
    CREATE TABLE amesa_lottery_results.lottery_result_history (
        "Id" uuid NOT NULL,
        "LotteryResultId" uuid NOT NULL,
        "Action" character varying(50) NOT NULL,
        "Details" character varying(1000) NOT NULL,
        "PerformedBy" character varying(100),
        "Timestamp" timestamp with time zone NOT NULL,
        "IpAddress" character varying(45),
        "UserAgent" character varying(500),
        CONSTRAINT "PK_lottery_result_history" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_lottery_result_history_lottery_results_LotteryResultId" FOREIGN KEY ("LotteryResultId") REFERENCES amesa_lottery_results.lottery_results ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
    CREATE TABLE amesa_lottery_results.prize_deliveries (
        "Id" uuid NOT NULL,
        "LotteryResultId" uuid NOT NULL,
        "WinnerUserId" uuid NOT NULL,
        "RecipientName" character varying(100) NOT NULL,
        "AddressLine1" character varying(200) NOT NULL,
        "AddressLine2" character varying(200),
        "City" character varying(100) NOT NULL,
        "State" character varying(50) NOT NULL,
        "PostalCode" character varying(20) NOT NULL,
        "Country" character varying(100) NOT NULL,
        "Phone" character varying(20),
        "Email" character varying(100),
        "DeliveryMethod" character varying(50) NOT NULL,
        "TrackingNumber" character varying(100),
        "DeliveryStatus" character varying(50) NOT NULL,
        "EstimatedDeliveryDate" timestamp with time zone,
        "ActualDeliveryDate" timestamp with time zone,
        "ShippingCost" numeric(10,2) NOT NULL,
        "DeliveryNotes" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_prize_deliveries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_prize_deliveries_lottery_results_LotteryResultId" FOREIGN KEY ("LotteryResultId") REFERENCES amesa_lottery_results.lottery_results ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
    CREATE INDEX "IX_lottery_result_history_LotteryResultId" ON amesa_lottery_results.lottery_result_history ("LotteryResultId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
    CREATE INDEX "IX_lottery_results_DrawId" ON amesa_lottery_results.lottery_results ("DrawId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
    CREATE INDEX "IX_lottery_results_LotteryId" ON amesa_lottery_results.lottery_results ("LotteryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
    CREATE INDEX "IX_lottery_results_WinnerUserId" ON amesa_lottery_results.lottery_results ("WinnerUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
    CREATE INDEX "IX_prize_deliveries_LotteryResultId" ON amesa_lottery_results.prize_deliveries ("LotteryResultId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215653_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251115215653_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

