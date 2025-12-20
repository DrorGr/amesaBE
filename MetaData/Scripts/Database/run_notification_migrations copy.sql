-- ============================================================================
-- NOTIFICATION SERVICE MIGRATIONS ONLY
-- Run this on amesa_notification_db database
-- Command: psql -h YOUR_HOST -U postgres -d amesa_notification_db -f run_notification_migrations.sql
-- ============================================================================

SET search_path TO amesa_notification, public;

-- ----------------------------------------------------------------------------
-- 2.1: Create device_registrations table
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS amesa_notification.device_registrations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    device_token VARCHAR(255) NOT NULL,
    platform VARCHAR(50) NOT NULL,
    device_id VARCHAR(255),
    device_name VARCHAR(255),
    app_version VARCHAR(50),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_used_at TIMESTAMP,
    CONSTRAINT uq_user_device_token UNIQUE (user_id, device_token)
);

CREATE INDEX IF NOT EXISTS idx_device_reg_user 
    ON amesa_notification.device_registrations(user_id);
    
CREATE INDEX IF NOT EXISTS idx_device_reg_token 
    ON amesa_notification.device_registrations(device_token);
    
CREATE INDEX IF NOT EXISTS idx_device_reg_user_platform 
    ON amesa_notification.device_registrations(user_id, platform);
    
CREATE INDEX IF NOT EXISTS idx_device_reg_active 
    ON amesa_notification.device_registrations(user_id, is_active) 
    WHERE is_active = true;

COMMENT ON TABLE amesa_notification.device_registrations IS 
    'Device registration for push notifications. Stores device tokens for iOS, Android, and Web platforms.';

COMMENT ON COLUMN amesa_notification.device_registrations.platform IS 
    'Platform: iOS, Android, Web';


