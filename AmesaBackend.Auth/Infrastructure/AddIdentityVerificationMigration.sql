-- Migration: Add Identity Verification Fields
-- Date: 2025-01-XX
-- Description: Adds identity verification fields to user_identity_documents table and creates system_configurations table

-- Add new columns to user_identity_documents table
ALTER TABLE amesa_auth.user_identity_documents
ADD COLUMN IF NOT EXISTS validation_key UUID UNIQUE DEFAULT gen_random_uuid(),
ADD COLUMN IF NOT EXISTS liveness_score DECIMAL(5,2),
ADD COLUMN IF NOT EXISTS face_match_score DECIMAL(5,2),
ADD COLUMN IF NOT EXISTS verification_provider VARCHAR(50) DEFAULT 'aws_rekognition',
ADD COLUMN IF NOT EXISTS verification_metadata JSONB,
ADD COLUMN IF NOT EXISTS verification_attempts INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS last_verification_attempt TIMESTAMPTZ;

-- Create unique index on validation_key if it doesn't exist
CREATE UNIQUE INDEX IF NOT EXISTS IX_user_identity_documents_ValidationKey 
ON amesa_auth.user_identity_documents(validation_key);

-- Set validation_key for existing records that don't have one
UPDATE amesa_auth.user_identity_documents
SET validation_key = gen_random_uuid()
WHERE validation_key IS NULL;

-- Make validation_key NOT NULL after setting values
ALTER TABLE amesa_auth.user_identity_documents
ALTER COLUMN validation_key SET NOT NULL;

-- Create system_configurations table
CREATE TABLE IF NOT EXISTS amesa_auth.system_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key VARCHAR(100) UNIQUE NOT NULL,
    value JSONB NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create index on key
CREATE UNIQUE INDEX IF NOT EXISTS IX_system_configurations_Key 
ON amesa_auth.system_configurations(key);

-- Insert initial configuration for ID verification (disabled by default)
INSERT INTO amesa_auth.system_configurations (key, value, description, is_active)
VALUES (
    'id_verification_required',
    '{"enabled": false, "enforced_at": null, "grace_period_days": 0}',
    'Requires ID verification with liveness for lottery ticket purchases',
    true
)
ON CONFLICT (key) DO NOTHING;

-- Note: FrontImageUrl, BackImageUrl, SelfieImageUrl columns remain for backward compatibility
-- but should be set to NULL for new verifications (images are not stored)















