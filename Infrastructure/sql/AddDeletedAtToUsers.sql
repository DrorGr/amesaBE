-- Migration: Add deleted_at column to users table
-- This column was defined in the initial migration but is missing in production
-- Date: 2025-11-23

-- Add deleted_at column to amesa_auth.users table
ALTER TABLE amesa_auth.users
ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

-- Add comment to document the column
COMMENT ON COLUMN amesa_auth.users.deleted_at IS 'Timestamp when the user was soft deleted (nullable)';








