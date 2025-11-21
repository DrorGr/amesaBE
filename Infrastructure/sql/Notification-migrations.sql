-- Amesa EF Core migration script for Notification
-- 2025-11-16T18:54:49.5742289+02:00
SET search_path TO amesa_notification;
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115220359_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'amesa_notification') THEN
            CREATE SCHEMA amesa_notification;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115220359_InitialCreate') THEN
    CREATE TABLE amesa_notification.email_templates (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Subject" character varying(255) NOT NULL,
        "BodyHtml" text,
        "BodyText" text,
        "Variables" text[],
        "Language" character varying(10) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_email_templates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115220359_InitialCreate') THEN
    CREATE TABLE amesa_notification.notification_templates (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Title" character varying(255) NOT NULL,
        "Message" text NOT NULL,
        "Type" character varying(50) NOT NULL,
        "Variables" text[],
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_notification_templates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115220359_InitialCreate') THEN
    CREATE TABLE amesa_notification.user_notifications (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TemplateId" uuid,
        "Title" character varying(255) NOT NULL,
        "Message" text NOT NULL,
        "Type" character varying(50) NOT NULL,
        "IsRead" boolean NOT NULL,
        "ReadAt" timestamp with time zone,
        "Data" jsonb,
        "CreatedAt" timestamp with time zone NOT NULL,
        "NotificationTemplateId" uuid,
        CONSTRAINT "PK_user_notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_notifications_notification_templates_NotificationTempl~" FOREIGN KEY ("NotificationTemplateId") REFERENCES amesa_notification.notification_templates ("Id"),
        CONSTRAINT "FK_user_notifications_notification_templates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES amesa_notification.notification_templates ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115220359_InitialCreate') THEN
    CREATE INDEX "IX_user_notifications_NotificationTemplateId" ON amesa_notification.user_notifications ("NotificationTemplateId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115220359_InitialCreate') THEN
    CREATE INDEX "IX_user_notifications_TemplateId" ON amesa_notification.user_notifications ("TemplateId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115220359_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251115220359_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

