-- Simple User Preferences Migration Script
-- Creates basic tables for storing user preferences
-- Schema: amesa_auth

-- Create user_preferences table
CREATE TABLE IF NOT EXISTS amesa_auth.user_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    preferences_json JSONB NOT NULL DEFAULT '{}',
    version VARCHAR(20) NOT NULL DEFAULT '1.0.0',
    sync_enabled BOOLEAN NOT NULL DEFAULT true,
    last_sync_at TIMESTAMP WITH TIME ZONE,
    preferences_hash VARCHAR(64),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255)
);

-- Create indexes for user_preferences
CREATE INDEX IF NOT EXISTS idx_user_preferences_user_id ON amesa_auth.user_preferences(user_id);
CREATE INDEX IF NOT EXISTS idx_user_preferences_updated_at ON amesa_auth.user_preferences(updated_at);
CREATE INDEX IF NOT EXISTS idx_user_preferences_sync_enabled ON amesa_auth.user_preferences(sync_enabled);
CREATE INDEX IF NOT EXISTS idx_user_preferences_version ON amesa_auth.user_preferences(version);

-- Create GIN index for JSONB preferences for efficient querying
CREATE INDEX IF NOT EXISTS idx_user_preferences_json ON amesa_auth.user_preferences USING GIN (preferences_json);

-- Create user_preference_history table for audit trail
CREATE TABLE IF NOT EXISTS amesa_auth.user_preference_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_preferences_id UUID NOT NULL,
    user_id UUID NOT NULL,
    category VARCHAR(50) NOT NULL,
    property_name VARCHAR(100) NOT NULL,
    old_value TEXT,
    new_value TEXT,
    change_reason VARCHAR(255),
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255)
);

-- Create indexes for user_preference_history
CREATE INDEX IF NOT EXISTS idx_user_preference_history_user_preferences_id ON amesa_auth.user_preference_history(user_preferences_id);
CREATE INDEX IF NOT EXISTS idx_user_preference_history_user_id ON amesa_auth.user_preference_history(user_id);
CREATE INDEX IF NOT EXISTS idx_user_preference_history_category ON amesa_auth.user_preference_history(category);
CREATE INDEX IF NOT EXISTS idx_user_preference_history_created_at ON amesa_auth.user_preference_history(created_at);

-- Create user_preference_sync_log table for tracking synchronization
CREATE TABLE IF NOT EXISTS amesa_auth.user_preference_sync_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_preferences_id UUID NOT NULL,
    user_id UUID NOT NULL,
    sync_type VARCHAR(20) NOT NULL,
    sync_status VARCHAR(20) NOT NULL,
    client_version VARCHAR(20),
    server_version VARCHAR(20),
    sync_duration_ms INTEGER,
    error_message TEXT,
    conflict_resolution VARCHAR(20),
    preferences_count INTEGER NOT NULL DEFAULT 0,
    data_size_bytes BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255)
);

-- Create indexes for user_preference_sync_log
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_user_preferences_id ON amesa_auth.user_preference_sync_log(user_preferences_id);
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_user_id ON amesa_auth.user_preference_sync_log(user_id);
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_sync_type ON amesa_auth.user_preference_sync_log(sync_type);
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_sync_status ON amesa_auth.user_preference_sync_log(sync_status);
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_created_at ON amesa_auth.user_preference_sync_log(created_at);

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'User Preferences migration completed successfully!';
    RAISE NOTICE 'Created tables: user_preferences, user_preference_history, user_preference_sync_log';
    RAISE NOTICE 'Created indexes for optimal performance';
END $$;
