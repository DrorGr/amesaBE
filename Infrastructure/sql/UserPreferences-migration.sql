-- User Preferences Migration Script
-- Creates tables for storing user preferences, history, and sync logs
-- Schema: amesa_auth

-- Create user_preferences table
CREATE TABLE IF NOT EXISTS amesa_auth.user_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES amesa_auth.users(id) ON DELETE CASCADE,
    preferences_json JSONB NOT NULL DEFAULT '{}',
    version VARCHAR(20) NOT NULL DEFAULT '1.0.0',
    sync_enabled BOOLEAN NOT NULL DEFAULT true,
    last_sync_at TIMESTAMP WITH TIME ZONE,
    preferences_hash VARCHAR(64),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    
    -- Constraints
    CONSTRAINT uk_user_preferences_user_id UNIQUE (user_id),
    CONSTRAINT ck_user_preferences_version CHECK (version ~ '^[0-9]+\.[0-9]+\.[0-9]+$')
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
    user_preferences_id UUID NOT NULL REFERENCES amesa_auth.user_preferences(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES amesa_auth.users(id) ON DELETE CASCADE,
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
    updated_by VARCHAR(255),
    
    -- Constraints
    CONSTRAINT ck_preference_history_category CHECK (category IN (
        'appearance', 'localization', 'accessibility', 'notifications', 
        'interaction', 'lottery', 'privacy', 'performance'
    ))
);

-- Create indexes for user_preference_history
CREATE INDEX IF NOT EXISTS idx_user_preference_history_user_preferences_id ON amesa_auth.user_preference_history(user_preferences_id);
CREATE INDEX IF NOT EXISTS idx_user_preference_history_user_id ON amesa_auth.user_preference_history(user_id);
CREATE INDEX IF NOT EXISTS idx_user_preference_history_category ON amesa_auth.user_preference_history(category);
CREATE INDEX IF NOT EXISTS idx_user_preference_history_created_at ON amesa_auth.user_preference_history(created_at);
CREATE INDEX IF NOT EXISTS idx_user_preference_history_property ON amesa_auth.user_preference_history(category, property_name);

-- Create user_preference_sync_log table for tracking synchronization
CREATE TABLE IF NOT EXISTS amesa_auth.user_preference_sync_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_preferences_id UUID NOT NULL REFERENCES amesa_auth.user_preferences(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES amesa_auth.users(id) ON DELETE CASCADE,
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
    updated_by VARCHAR(255),
    
    -- Constraints
    CONSTRAINT ck_sync_log_sync_type CHECK (sync_type IN ('upload', 'download', 'conflict', 'reset')),
    CONSTRAINT ck_sync_log_sync_status CHECK (sync_status IN ('success', 'failed', 'partial', 'cancelled')),
    CONSTRAINT ck_sync_log_conflict_resolution CHECK (conflict_resolution IN ('local', 'remote', 'merge', 'manual')),
    CONSTRAINT ck_sync_log_preferences_count CHECK (preferences_count >= 0),
    CONSTRAINT ck_sync_log_data_size CHECK (data_size_bytes >= 0)
);

-- Create indexes for user_preference_sync_log
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_user_preferences_id ON amesa_auth.user_preference_sync_log(user_preferences_id);
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_user_id ON amesa_auth.user_preference_sync_log(user_id);
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_sync_type ON amesa_auth.user_preference_sync_log(sync_type);
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_sync_status ON amesa_auth.user_preference_sync_log(sync_status);
CREATE INDEX IF NOT EXISTS idx_user_preference_sync_log_created_at ON amesa_auth.user_preference_sync_log(created_at);

-- Create function to update updated_at timestamp
CREATE OR REPLACE FUNCTION amesa_auth.update_user_preferences_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create triggers for updated_at
CREATE TRIGGER trigger_user_preferences_updated_at
    BEFORE UPDATE ON amesa_auth.user_preferences
    FOR EACH ROW
    EXECUTE FUNCTION amesa_auth.update_user_preferences_updated_at();

CREATE TRIGGER trigger_user_preference_history_updated_at
    BEFORE UPDATE ON amesa_auth.user_preference_history
    FOR EACH ROW
    EXECUTE FUNCTION amesa_auth.update_user_preferences_updated_at();

CREATE TRIGGER trigger_user_preference_sync_log_updated_at
    BEFORE UPDATE ON amesa_auth.user_preference_sync_log
    FOR EACH ROW
    EXECUTE FUNCTION amesa_auth.update_user_preferences_updated_at();

-- Create function to automatically log preference changes
CREATE OR REPLACE FUNCTION amesa_auth.log_preference_change()
RETURNS TRIGGER AS $$
DECLARE
    old_json JSONB;
    new_json JSONB;
    key TEXT;
    old_val TEXT;
    new_val TEXT;
BEGIN
    -- Only log changes, not inserts
    IF TG_OP = 'UPDATE' AND OLD.preferences_json IS DISTINCT FROM NEW.preferences_json THEN
        old_json := OLD.preferences_json;
        new_json := NEW.preferences_json;
        
        -- Log each changed property
        FOR key IN SELECT jsonb_object_keys(new_json) LOOP
            old_val := old_json ->> key;
            new_val := new_json ->> key;
            
            IF old_val IS DISTINCT FROM new_val THEN
                INSERT INTO amesa_auth.user_preference_history (
                    user_preferences_id,
                    user_id,
                    category,
                    property_name,
                    old_value,
                    new_value,
                    created_by,
                    updated_by
                ) VALUES (
                    NEW.id,
                    NEW.user_id,
                    'general', -- This could be enhanced to detect category from key
                    key,
                    old_val,
                    new_val,
                    NEW.updated_by,
                    NEW.updated_by
                );
            END IF;
        END LOOP;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for automatic preference change logging
CREATE TRIGGER trigger_log_preference_changes
    AFTER UPDATE ON amesa_auth.user_preferences
    FOR EACH ROW
    EXECUTE FUNCTION amesa_auth.log_preference_change();

-- Create function to clean up old preference history (optional)
CREATE OR REPLACE FUNCTION amesa_auth.cleanup_old_preference_history(days_to_keep INTEGER DEFAULT 365)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM amesa_auth.user_preference_history
    WHERE created_at < CURRENT_TIMESTAMP - INTERVAL '1 day' * days_to_keep;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- Create function to get user preferences with fallback to defaults
CREATE OR REPLACE FUNCTION amesa_auth.get_user_preferences_with_defaults(p_user_id UUID)
RETURNS JSONB AS $$
DECLARE
    user_prefs JSONB;
    default_prefs JSONB;
BEGIN
    -- Get user preferences
    SELECT preferences_json INTO user_prefs
    FROM amesa_auth.user_preferences
    WHERE user_id = p_user_id;
    
    -- Default preferences JSON
    default_prefs := '{
        "version": "1.0.0",
        "appearance": {
            "theme": "auto",
            "primaryColor": "#3B82F6",
            "accentColor": "#10B981",
            "fontSize": "medium",
            "fontFamily": "Inter, system-ui, sans-serif",
            "uiDensity": "comfortable",
            "borderRadius": 8,
            "showAnimations": true,
            "animationLevel": "normal",
            "reducedMotion": false
        },
        "localization": {
            "language": "en",
            "dateFormat": "MM/DD/YYYY",
            "timeFormat": "12h",
            "numberFormat": "US",
            "currency": "USD",
            "timezone": "UTC",
            "rtlSupport": false
        },
        "accessibility": {
            "highContrast": false,
            "colorBlindAssist": false,
            "colorBlindType": "none",
            "screenReaderOptimized": false,
            "keyboardNavigation": true,
            "focusIndicators": true,
            "skipLinks": true,
            "altTextVerbosity": "standard",
            "captionsEnabled": false,
            "audioDescriptions": false,
            "largeClickTargets": false,
            "reducedFlashing": false
        },
        "notifications": {
            "emailNotifications": true,
            "pushNotifications": false,
            "browserNotifications": false,
            "smsNotifications": false,
            "lotteryResults": true,
            "newLotteries": true,
            "promotions": false,
            "accountUpdates": true,
            "securityAlerts": true,
            "quietHours": {
                "enabled": false,
                "startTime": "22:00",
                "endTime": "08:00"
            },
            "soundEnabled": true,
            "soundVolume": 50,
            "customSounds": false
        }
    }'::JSONB;
    
    -- Return user preferences merged with defaults
    IF user_prefs IS NOT NULL THEN
        RETURN default_prefs || user_prefs;
    ELSE
        RETURN default_prefs;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Grant permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON amesa_auth.user_preferences TO amesa_backend;
GRANT SELECT, INSERT, UPDATE, DELETE ON amesa_auth.user_preference_history TO amesa_backend;
GRANT SELECT, INSERT, UPDATE, DELETE ON amesa_auth.user_preference_sync_log TO amesa_backend;
GRANT EXECUTE ON FUNCTION amesa_auth.get_user_preferences_with_defaults(UUID) TO amesa_backend;
GRANT EXECUTE ON FUNCTION amesa_auth.cleanup_old_preference_history(INTEGER) TO amesa_backend;

-- Add comments for documentation
COMMENT ON TABLE amesa_auth.user_preferences IS 'Stores user-specific preferences and settings';
COMMENT ON COLUMN amesa_auth.user_preferences.preferences_json IS 'JSONB containing all user preferences organized by category';
COMMENT ON COLUMN amesa_auth.user_preferences.version IS 'Schema version for preference migration purposes';
COMMENT ON COLUMN amesa_auth.user_preferences.sync_enabled IS 'Whether preferences should sync between devices';
COMMENT ON COLUMN amesa_auth.user_preferences.preferences_hash IS 'Hash for conflict detection during sync';

COMMENT ON TABLE amesa_auth.user_preference_history IS 'Audit trail for user preference changes';
COMMENT ON COLUMN amesa_auth.user_preference_history.category IS 'Preference category (appearance, localization, etc.)';
COMMENT ON COLUMN amesa_auth.user_preference_history.property_name IS 'Specific property that changed';

COMMENT ON TABLE amesa_auth.user_preference_sync_log IS 'Log of preference synchronization operations';
COMMENT ON COLUMN amesa_auth.user_preference_sync_log.sync_type IS 'Type of sync operation (upload, download, conflict)';
COMMENT ON COLUMN amesa_auth.user_preference_sync_log.conflict_resolution IS 'How conflicts were resolved (local, remote, merge)';

-- Insert initial system settings for preference management
INSERT INTO amesa_auth.system_settings (key, value, description, category, created_by, updated_by)
VALUES 
    ('preferences.max_history_days', '365', 'Maximum days to keep preference history', 'preferences', 'system', 'system'),
    ('preferences.sync_enabled', 'true', 'Global preference sync enabled', 'preferences', 'system', 'system'),
    ('preferences.max_sync_size_mb', '10', 'Maximum sync payload size in MB', 'preferences', 'system', 'system'),
    ('preferences.auto_cleanup_enabled', 'true', 'Automatically cleanup old preference history', 'preferences', 'system', 'system')
ON CONFLICT (key) DO NOTHING;

-- Create a view for easier preference querying
CREATE OR REPLACE VIEW amesa_auth.v_user_preferences_summary AS
SELECT 
    up.id,
    up.user_id,
    u.email,
    u.first_name,
    u.last_name,
    up.preferences_json ->> 'appearance' ->> 'theme' as theme,
    up.preferences_json ->> 'localization' ->> 'language' as language,
    up.preferences_json ->> 'accessibility' ->> 'highContrast' as high_contrast,
    up.version,
    up.sync_enabled,
    up.last_sync_at,
    up.created_at,
    up.updated_at,
    (SELECT COUNT(*) FROM amesa_auth.user_preference_history uph WHERE uph.user_preferences_id = up.id) as history_count,
    (SELECT MAX(created_at) FROM amesa_auth.user_preference_sync_log upsl WHERE upsl.user_preferences_id = up.id) as last_sync_log
FROM amesa_auth.user_preferences up
JOIN amesa_auth.users u ON up.user_id = u.id;

GRANT SELECT ON amesa_auth.v_user_preferences_summary TO amesa_backend;

COMMENT ON VIEW amesa_auth.v_user_preferences_summary IS 'Summary view of user preferences with key extracted fields';

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'User Preferences migration completed successfully!';
    RAISE NOTICE 'Created tables: user_preferences, user_preference_history, user_preference_sync_log';
    RAISE NOTICE 'Created functions: update_user_preferences_updated_at, log_preference_change, cleanup_old_preference_history, get_user_preferences_with_defaults';
    RAISE NOTICE 'Created view: v_user_preferences_summary';
    RAISE NOTICE 'Created indexes and triggers for optimal performance';
END $$;
