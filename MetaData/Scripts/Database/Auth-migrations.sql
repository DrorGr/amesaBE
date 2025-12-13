-- Amesa EF Core migration script for Auth
-- 2025-11-16T18:54:39.1884389+02:00
SET search_path TO amesa_auth;
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'amesa_auth') THEN
            CREATE SCHEMA amesa_auth;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE TABLE amesa_auth.users (
        "Id" uuid NOT NULL,
        "Username" character varying(50) NOT NULL,
        "Email" character varying(255) NOT NULL,
        "EmailVerified" boolean NOT NULL,
        "Phone" character varying(20),
        "PhoneVerified" boolean NOT NULL,
        "PasswordHash" character varying(255) NOT NULL,
        "FirstName" character varying(100) NOT NULL,
        "LastName" character varying(100) NOT NULL,
        "DateOfBirth" timestamp with time zone,
        "Gender" text,
        "IdNumber" character varying(50),
        "Status" text NOT NULL,
        "VerificationStatus" text NOT NULL,
        "AuthProvider" text NOT NULL,
        "ProviderId" character varying(255),
        "ProfileImageUrl" text,
        "PreferredLanguage" character varying(10) NOT NULL,
        "Timezone" character varying(50) NOT NULL,
        "LastLoginAt" timestamp with time zone,
        "EmailVerificationToken" character varying(255),
        "PhoneVerificationToken" character varying(10),
        "PasswordResetToken" character varying(255),
        "PasswordResetExpiresAt" timestamp with time zone,
        "TwoFactorEnabled" boolean NOT NULL,
        "TwoFactorSecret" character varying(255),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "DeletedAt" timestamp with time zone,
        CONSTRAINT "PK_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE TABLE amesa_auth.user_addresses (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Type" character varying(20) NOT NULL,
        "Country" character varying(100),
        "City" character varying(100),
        "Street" character varying(255),
        "HouseNumber" character varying(20),
        "ZipCode" character varying(20),
        "IsPrimary" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UserId1" uuid,
        CONSTRAINT "PK_user_addresses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_addresses_users_UserId" FOREIGN KEY ("UserId") REFERENCES amesa_auth.users ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_user_addresses_users_UserId1" FOREIGN KEY ("UserId1") REFERENCES amesa_auth.users ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE TABLE amesa_auth.user_identity_documents (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "DocumentType" character varying(50) NOT NULL,
        "DocumentNumber" character varying(100) NOT NULL,
        "FrontImageUrl" text,
        "BackImageUrl" text,
        "SelfieImageUrl" text,
        "VerificationStatus" character varying(20) NOT NULL,
        "VerifiedAt" timestamp with time zone,
        "VerifiedBy" uuid,
        "RejectionReason" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UserId1" uuid,
        CONSTRAINT "PK_user_identity_documents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_identity_documents_users_UserId" FOREIGN KEY ("UserId") REFERENCES amesa_auth.users ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_user_identity_documents_users_UserId1" FOREIGN KEY ("UserId1") REFERENCES amesa_auth.users ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE TABLE amesa_auth.user_phones (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "PhoneNumber" character varying(20) NOT NULL,
        "IsPrimary" boolean NOT NULL,
        "IsVerified" boolean NOT NULL,
        "VerificationCode" character varying(10),
        "VerificationExpiresAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UserId1" uuid,
        CONSTRAINT "PK_user_phones" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_phones_users_UserId" FOREIGN KEY ("UserId") REFERENCES amesa_auth.users ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_user_phones_users_UserId1" FOREIGN KEY ("UserId1") REFERENCES amesa_auth.users ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE TABLE amesa_auth.user_sessions (
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
        "UserId1" uuid,
        CONSTRAINT "PK_user_sessions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_sessions_users_UserId" FOREIGN KEY ("UserId") REFERENCES amesa_auth.users ("Id"),
        CONSTRAINT "FK_user_sessions_users_UserId1" FOREIGN KEY ("UserId1") REFERENCES amesa_auth.users ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE TABLE amesa_auth.user_activity_logs (
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
        "UserId1" uuid,
        "UserSessionId" uuid,
        CONSTRAINT "PK_user_activity_logs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_activity_logs_user_sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES amesa_auth.user_sessions ("Id"),
        CONSTRAINT "FK_user_activity_logs_user_sessions_UserSessionId" FOREIGN KEY ("UserSessionId") REFERENCES amesa_auth.user_sessions ("Id"),
        CONSTRAINT "FK_user_activity_logs_users_UserId" FOREIGN KEY ("UserId") REFERENCES amesa_auth.users ("Id"),
        CONSTRAINT "FK_user_activity_logs_users_UserId1" FOREIGN KEY ("UserId1") REFERENCES amesa_auth.users ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_activity_logs_SessionId" ON amesa_auth.user_activity_logs ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_activity_logs_UserId" ON amesa_auth.user_activity_logs ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_activity_logs_UserId1" ON amesa_auth.user_activity_logs ("UserId1");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_activity_logs_UserSessionId" ON amesa_auth.user_activity_logs ("UserSessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_addresses_UserId" ON amesa_auth.user_addresses ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_addresses_UserId1" ON amesa_auth.user_addresses ("UserId1");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_identity_documents_UserId" ON amesa_auth.user_identity_documents ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_identity_documents_UserId1" ON amesa_auth.user_identity_documents ("UserId1");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_phones_UserId" ON amesa_auth.user_phones ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_phones_UserId1" ON amesa_auth.user_phones ("UserId1");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_sessions_UserId" ON amesa_auth.user_sessions ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE INDEX "IX_user_sessions_UserId1" ON amesa_auth.user_sessions ("UserId1");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_Email" ON amesa_auth.users ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_Username" ON amesa_auth.users ("Username");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215123_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251115215123_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

