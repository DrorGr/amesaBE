-- Amesa EF Core migration script for Content
-- 2025-11-16T18:54:44.2066281+02:00
SET search_path TO amesa_content;
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'amesa_content') THEN
            CREATE SCHEMA amesa_content;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE TABLE amesa_content.content_categories (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Slug" character varying(100) NOT NULL,
        "Description" text,
        "ParentId" uuid,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_content_categories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_content_categories_content_categories_ParentId" FOREIGN KEY ("ParentId") REFERENCES amesa_content.content_categories ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE TABLE amesa_content.languages (
        "Code" character varying(10) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "NativeName" character varying(255),
        "FlagUrl" character varying(10),
        "IsActive" boolean NOT NULL,
        "IsDefault" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_languages" PRIMARY KEY ("Code")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE TABLE amesa_content.content (
        "Id" uuid NOT NULL,
        "Title" character varying(255) NOT NULL,
        "Slug" character varying(255) NOT NULL,
        "ContentBody" text,
        "Excerpt" text,
        "CategoryId" uuid,
        "Status" text NOT NULL,
        "AuthorId" uuid,
        "PublishedAt" timestamp with time zone,
        "MetaTitle" character varying(255),
        "MetaDescription" text,
        "FeaturedImageUrl" text,
        "Language" character varying(10) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "DeletedAt" timestamp with time zone,
        "ContentCategoryId" uuid,
        CONSTRAINT "PK_content" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_content_content_categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES amesa_content.content_categories ("Id"),
        CONSTRAINT "FK_content_content_categories_ContentCategoryId" FOREIGN KEY ("ContentCategoryId") REFERENCES amesa_content.content_categories ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE TABLE amesa_content.translations (
        "Id" uuid NOT NULL,
        "LanguageCode" character varying(10) NOT NULL,
        "Key" character varying(255) NOT NULL,
        "Value" text NOT NULL,
        "Description" text,
        "Category" text,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(100),
        "UpdatedBy" character varying(100),
        CONSTRAINT "PK_translations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_translations_languages_LanguageCode" FOREIGN KEY ("LanguageCode") REFERENCES amesa_content.languages ("Code") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE TABLE amesa_content.content_media (
        "Id" uuid NOT NULL,
        "ContentId" uuid,
        "MediaUrl" text NOT NULL,
        "AltText" character varying(255),
        "Caption" text,
        "MediaType" text NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "FileSize" integer,
        "Width" integer,
        "Height" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_content_media" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_content_media_content_ContentId" FOREIGN KEY ("ContentId") REFERENCES amesa_content.content ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE INDEX "IX_content_CategoryId" ON amesa_content.content ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE INDEX "IX_content_ContentCategoryId" ON amesa_content.content ("ContentCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE INDEX "IX_content_categories_ParentId" ON amesa_content.content_categories ("ParentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE INDEX "IX_content_media_ContentId" ON amesa_content.content_media ("ContentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_languages_Code" ON amesa_content.languages ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_translations_LanguageCode_Key" ON amesa_content.translations ("LanguageCode", "Key");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115215443_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251115215443_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

