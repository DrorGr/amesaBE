-- Amesa EF Core migration script for Analytics
-- 2025-11-16T18:55:44.8316955+02:00
SET search_path TO amesa_analytics;
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215737_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'amesa_analytics') THEN
            CREATE SCHEMA amesa_analytics;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215737_InitialCreate') THEN
    CREATE TABLE amesa_analytics.user_sessions (
        "Id" uuid NOT NULL,
        "UserId" uuid,
        "SessionToken" character varying(255) NOT NULL,
        "IpAddress" text,
        "UserAgent" text,
        "DeviceType" character varying(50),
        "Browser" character varying(100),
        "Os" character varying(100),
        "Country" character varying(100),
        "City" character varying(100),
        "IsActive" boolean NOT NULL,
        "LastActivity" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_sessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215737_InitialCreate') THEN
    CREATE TABLE amesa_analytics.user_activity_logs (
        "Id" uuid NOT NULL,
        "UserId" uuid,
        "SessionId" uuid,
        "Action" character varying(100) NOT NULL,
        "ResourceType" character varying(50),
        "ResourceId" uuid,
        "Details" jsonb,
        "IpAddress" text,
        "UserAgent" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_activity_logs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_activity_logs_user_sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES amesa_analytics.user_sessions ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215737_InitialCreate') THEN
    CREATE INDEX "IX_user_activity_logs_CreatedAt" ON amesa_analytics.user_activity_logs ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215737_InitialCreate') THEN
    CREATE INDEX "IX_user_activity_logs_SessionId" ON amesa_analytics.user_activity_logs ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215737_InitialCreate') THEN
    CREATE INDEX "IX_user_activity_logs_UserId" ON amesa_analytics.user_activity_logs ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215737_InitialCreate') THEN
    CREATE INDEX "IX_user_sessions_SessionToken" ON amesa_analytics.user_sessions ("SessionToken");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215737_InitialCreate') THEN
    CREATE INDEX "IX_user_sessions_UserId" ON amesa_analytics.user_sessions ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215737_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251115215737_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

