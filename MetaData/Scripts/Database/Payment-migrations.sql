-- Amesa EF Core migration script for Payment
-- 2025-11-16T18:54:58.6814490+02:00
SET search_path TO amesa_payment;
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215543_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'amesa_payment') THEN
            CREATE SCHEMA amesa_payment;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215543_InitialCreate') THEN
    CREATE TABLE amesa_payment.user_payment_methods (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Type" character varying(50) NOT NULL,
        "Provider" character varying(50),
        "ProviderPaymentMethodId" character varying(255),
        "CardLastFour" character varying(4),
        "CardBrand" character varying(50),
        "CardExpMonth" integer,
        "CardExpYear" integer,
        "IsDefault" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_payment_methods" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215543_InitialCreate') THEN
    CREATE TABLE amesa_payment.transactions (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Type" character varying(50) NOT NULL,
        "Amount" numeric(15,2) NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "Description" text,
        "ReferenceId" character varying(255),
        "PaymentMethodId" uuid,
        "ProviderTransactionId" character varying(255),
        "ProviderResponse" jsonb,
        "Metadata" jsonb,
        "ProcessedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_transactions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_transactions_user_payment_methods_PaymentMethodId" FOREIGN KEY ("PaymentMethodId") REFERENCES amesa_payment.user_payment_methods ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215543_InitialCreate') THEN
    CREATE INDEX "IX_transactions_PaymentMethodId" ON amesa_payment.transactions ("PaymentMethodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215543_InitialCreate') THEN
    CREATE INDEX "IX_transactions_ReferenceId" ON amesa_payment.transactions ("ReferenceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215543_InitialCreate') THEN
    CREATE INDEX "IX_transactions_UserId" ON amesa_payment.transactions ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215543_InitialCreate') THEN
    CREATE INDEX "IX_user_payment_methods_UserId" ON amesa_payment.user_payment_methods ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215543_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251115215543_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

