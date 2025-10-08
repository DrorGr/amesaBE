-- =====================================================
-- AMESA LOTTERY PLATFORM - POSTGRESQL DATABASE SCHEMA
-- =====================================================
-- Designed for .NET Core backend integration
-- Supports: User management, Lottery system, Payments, Analytics
-- =====================================================

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =====================================================
-- ENUMS
-- =====================================================

-- User related enums
CREATE TYPE user_status AS ENUM ('pending', 'active', 'suspended', 'banned', 'deleted');
CREATE TYPE user_verification_status AS ENUM ('unverified', 'email_verified', 'phone_verified', 'identity_verified', 'fully_verified');
CREATE TYPE auth_provider AS ENUM ('email', 'google', 'meta', 'apple', 'twitter');
CREATE TYPE gender_type AS ENUM ('male', 'female', 'other', 'prefer_not_to_say');

-- Lottery related enums
CREATE TYPE lottery_status AS ENUM ('upcoming', 'active', 'paused', 'ended', 'cancelled', 'completed');
CREATE TYPE ticket_status AS ENUM ('active', 'winner', 'refunded', 'cancelled');
CREATE TYPE draw_status AS ENUM ('pending', 'in_progress', 'completed', 'failed');

-- Payment related enums
CREATE TYPE payment_status AS ENUM ('pending', 'processing', 'completed', 'failed', 'refunded', 'cancelled');
CREATE TYPE payment_method AS ENUM ('credit_card', 'debit_card', 'paypal', 'apple_pay', 'google_pay', 'bank_transfer', 'crypto');
CREATE TYPE transaction_type AS ENUM ('ticket_purchase', 'refund', 'withdrawal', 'bonus', 'fee');

-- Content and media enums
CREATE TYPE media_type AS ENUM ('image', 'video', 'document', 'audio');
CREATE TYPE content_status AS ENUM ('draft', 'published', 'archived', 'deleted');

-- =====================================================
-- CORE TABLES
-- =====================================================

-- Users table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    email_verified BOOLEAN DEFAULT FALSE,
    phone VARCHAR(20),
    phone_verified BOOLEAN DEFAULT FALSE,
    password_hash VARCHAR(255), -- NULL for OAuth users
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    date_of_birth DATE,
    gender gender_type,
    id_number VARCHAR(50), -- National ID/Passport
    status user_status DEFAULT 'pending',
    verification_status user_verification_status DEFAULT 'unverified',
    auth_provider auth_provider DEFAULT 'email',
    provider_id VARCHAR(255), -- OAuth provider user ID
    profile_image_url TEXT,
    preferred_language VARCHAR(10) DEFAULT 'en',
    timezone VARCHAR(50) DEFAULT 'UTC',
    last_login_at TIMESTAMP WITH TIME ZONE,
    email_verification_token VARCHAR(255),
    phone_verification_token VARCHAR(10),
    password_reset_token VARCHAR(255),
    password_reset_expires_at TIMESTAMP WITH TIME ZONE,
    two_factor_enabled BOOLEAN DEFAULT FALSE,
    two_factor_secret VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- User addresses
CREATE TABLE user_addresses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type VARCHAR(20) DEFAULT 'home', -- home, work, billing
    country VARCHAR(100),
    city VARCHAR(100),
    street VARCHAR(255),
    house_number VARCHAR(20),
    zip_code VARCHAR(20),
    is_primary BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User phone numbers (multiple phones per user)
CREATE TABLE user_phones (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    phone_number VARCHAR(20) NOT NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    is_verified BOOLEAN DEFAULT FALSE,
    verification_code VARCHAR(10),
    verification_expires_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Identity verification documents
CREATE TABLE user_identity_documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    document_type VARCHAR(50) NOT NULL, -- passport, driver_license, national_id
    document_number VARCHAR(100) NOT NULL,
    front_image_url TEXT,
    back_image_url TEXT,
    selfie_image_url TEXT,
    verification_status VARCHAR(20) DEFAULT 'pending', -- pending, verified, rejected
    verified_at TIMESTAMP WITH TIME ZONE,
    verified_by UUID REFERENCES users(id),
    rejection_reason TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- LOTTERY SYSTEM
-- =====================================================

-- Houses/Properties
CREATE TABLE houses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    price DECIMAL(15,2) NOT NULL,
    location VARCHAR(255) NOT NULL,
    address TEXT,
    bedrooms INTEGER NOT NULL,
    bathrooms INTEGER NOT NULL,
    square_feet INTEGER,
    property_type VARCHAR(50), -- condo, house, villa, apartment
    year_built INTEGER,
    lot_size DECIMAL(10,2),
    features TEXT[], -- Array of features
    coordinates POINT, -- PostGIS point for location
    status lottery_status DEFAULT 'upcoming',
    total_tickets INTEGER NOT NULL,
    ticket_price DECIMAL(10,2) NOT NULL,
    lottery_start_date TIMESTAMP WITH TIME ZONE,
    lottery_end_date TIMESTAMP WITH TIME ZONE NOT NULL,
    draw_date TIMESTAMP WITH TIME ZONE,
    minimum_participation_percentage DECIMAL(5,2) DEFAULT 75.00,
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- House images
CREATE TABLE house_images (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    house_id UUID NOT NULL REFERENCES houses(id) ON DELETE CASCADE,
    image_url TEXT NOT NULL,
    alt_text VARCHAR(255),
    display_order INTEGER DEFAULT 0,
    is_primary BOOLEAN DEFAULT FALSE,
    media_type media_type DEFAULT 'image',
    file_size INTEGER,
    width INTEGER,
    height INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Lottery tickets
CREATE TABLE lottery_tickets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_number VARCHAR(20) UNIQUE NOT NULL,
    house_id UUID NOT NULL REFERENCES houses(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    purchase_price DECIMAL(10,2) NOT NULL,
    status ticket_status DEFAULT 'active',
    purchase_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    payment_id UUID, -- Reference to payment
    is_winner BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Lottery draws
CREATE TABLE lottery_draws (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    house_id UUID NOT NULL REFERENCES houses(id) ON DELETE CASCADE,
    draw_date TIMESTAMP WITH TIME ZONE NOT NULL,
    total_tickets_sold INTEGER NOT NULL,
    total_participation_percentage DECIMAL(5,2) NOT NULL,
    winning_ticket_number VARCHAR(20),
    winning_ticket_id UUID REFERENCES lottery_tickets(id),
    winner_user_id UUID REFERENCES users(id),
    draw_status draw_status DEFAULT 'pending',
    draw_method VARCHAR(50) DEFAULT 'random', -- random, weighted, etc.
    draw_seed VARCHAR(255), -- For reproducible draws
    conducted_by UUID REFERENCES users(id),
    conducted_at TIMESTAMP WITH TIME ZONE,
    verification_hash VARCHAR(255), -- For draw integrity
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- PAYMENT SYSTEM
-- =====================================================

-- Payment methods
CREATE TABLE user_payment_methods (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type payment_method NOT NULL,
    provider VARCHAR(50), -- stripe, paypal, etc.
    provider_payment_method_id VARCHAR(255),
    card_last_four VARCHAR(4),
    card_brand VARCHAR(50),
    card_exp_month INTEGER,
    card_exp_year INTEGER,
    is_default BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Transactions
CREATE TABLE transactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type transaction_type NOT NULL,
    amount DECIMAL(15,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    status payment_status DEFAULT 'pending',
    description TEXT,
    reference_id VARCHAR(255), -- External reference (ticket_id, etc.)
    payment_method_id UUID REFERENCES user_payment_methods(id),
    provider_transaction_id VARCHAR(255),
    provider_response JSONB,
    metadata JSONB,
    processed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- CONTENT MANAGEMENT
-- =====================================================

-- Content categories
CREATE TABLE content_categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    parent_id UUID REFERENCES content_categories(id),
    display_order INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Content (pages, articles, etc.)
CREATE TABLE content (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    slug VARCHAR(255) UNIQUE NOT NULL,
    content TEXT,
    excerpt TEXT,
    category_id UUID REFERENCES content_categories(id),
    status content_status DEFAULT 'draft',
    author_id UUID REFERENCES users(id),
    published_at TIMESTAMP WITH TIME ZONE,
    meta_title VARCHAR(255),
    meta_description TEXT,
    featured_image_url TEXT,
    language VARCHAR(10) DEFAULT 'en',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- Content media
CREATE TABLE content_media (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    content_id UUID REFERENCES content(id) ON DELETE CASCADE,
    media_url TEXT NOT NULL,
    alt_text VARCHAR(255),
    caption TEXT,
    media_type media_type NOT NULL,
    display_order INTEGER DEFAULT 0,
    file_size INTEGER,
    width INTEGER,
    height INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- PROMOTIONS & REWARDS
-- =====================================================

-- Promotions
CREATE TABLE promotions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    type VARCHAR(50) NOT NULL, -- discount, bonus, free_tickets
    value DECIMAL(10,2),
    value_type VARCHAR(20), -- percentage, fixed_amount
    code VARCHAR(50) UNIQUE,
    is_active BOOLEAN DEFAULT TRUE,
    start_date TIMESTAMP WITH TIME ZONE,
    end_date TIMESTAMP WITH TIME ZONE,
    usage_limit INTEGER,
    usage_count INTEGER DEFAULT 0,
    min_purchase_amount DECIMAL(10,2),
    max_discount_amount DECIMAL(10,2),
    applicable_houses UUID[], -- Array of house IDs
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User promotions (tracking usage)
CREATE TABLE user_promotions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    promotion_id UUID NOT NULL REFERENCES promotions(id) ON DELETE CASCADE,
    used_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    transaction_id UUID REFERENCES transactions(id),
    discount_amount DECIMAL(10,2),
    UNIQUE(user_id, promotion_id)
);

-- =====================================================
-- ANALYTICS & TRACKING
-- =====================================================

-- User sessions
CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    session_token VARCHAR(255) UNIQUE NOT NULL,
    ip_address INET,
    user_agent TEXT,
    device_type VARCHAR(50),
    browser VARCHAR(100),
    os VARCHAR(100),
    country VARCHAR(100),
    city VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    last_activity TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User activity logs
CREATE TABLE user_activity_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    session_id UUID REFERENCES user_sessions(id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    resource_type VARCHAR(50),
    resource_id UUID,
    details JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- SYSTEM CONFIGURATION
-- =====================================================

-- System settings
CREATE TABLE system_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(100) UNIQUE NOT NULL,
    value TEXT,
    type VARCHAR(50) DEFAULT 'string', -- string, number, boolean, json
    description TEXT,
    is_public BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Email templates
CREATE TABLE email_templates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) UNIQUE NOT NULL,
    subject VARCHAR(255) NOT NULL,
    body_html TEXT,
    body_text TEXT,
    variables TEXT[], -- Array of available variables
    language VARCHAR(10) DEFAULT 'en',
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- NOTIFICATIONS
-- =====================================================

-- Notification templates
CREATE TABLE notification_templates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) UNIQUE NOT NULL,
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    type VARCHAR(50) NOT NULL, -- email, sms, push, in_app
    variables TEXT[],
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User notifications
CREATE TABLE user_notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    template_id UUID REFERENCES notification_templates(id),
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    type VARCHAR(50) NOT NULL,
    is_read BOOLEAN DEFAULT FALSE,
    read_at TIMESTAMP WITH TIME ZONE,
    data JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- INDEXES FOR PERFORMANCE
-- =====================================================

-- User indexes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_status ON users(status);
CREATE INDEX idx_users_verification_status ON users(verification_status);
CREATE INDEX idx_users_created_at ON users(created_at);

-- House indexes
CREATE INDEX idx_houses_status ON houses(status);
CREATE INDEX idx_houses_lottery_end_date ON houses(lottery_end_date);
CREATE INDEX idx_houses_created_at ON houses(created_at);
CREATE INDEX idx_houses_location ON houses(location);

-- Ticket indexes
CREATE INDEX idx_lottery_tickets_house_id ON lottery_tickets(house_id);
CREATE INDEX idx_lottery_tickets_user_id ON lottery_tickets(user_id);
CREATE INDEX idx_lottery_tickets_status ON lottery_tickets(status);
CREATE INDEX idx_lottery_tickets_ticket_number ON lottery_tickets(ticket_number);
CREATE INDEX idx_lottery_tickets_purchase_date ON lottery_tickets(purchase_date);

-- Transaction indexes
CREATE INDEX idx_transactions_user_id ON transactions(user_id);
CREATE INDEX idx_transactions_type ON transactions(type);
CREATE INDEX idx_transactions_status ON transactions(status);
CREATE INDEX idx_transactions_created_at ON transactions(created_at);

-- Session indexes
CREATE INDEX idx_user_sessions_user_id ON user_sessions(user_id);
CREATE INDEX idx_user_sessions_token ON user_sessions(session_token);
CREATE INDEX idx_user_sessions_expires_at ON user_sessions(expires_at);

-- Activity log indexes
CREATE INDEX idx_user_activity_logs_user_id ON user_activity_logs(user_id);
CREATE INDEX idx_user_activity_logs_action ON user_activity_logs(action);
CREATE INDEX idx_user_activity_logs_created_at ON user_activity_logs(created_at);

-- =====================================================
-- TRIGGERS FOR UPDATED_AT
-- =====================================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply trigger to all tables with updated_at column
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_houses_updated_at BEFORE UPDATE ON houses FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_lottery_tickets_updated_at BEFORE UPDATE ON lottery_tickets FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_transactions_updated_at BEFORE UPDATE ON transactions FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_content_updated_at BEFORE UPDATE ON content FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_promotions_updated_at BEFORE UPDATE ON promotions FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- INITIAL DATA
-- =====================================================

-- Insert default content categories
INSERT INTO content_categories (name, slug, description) VALUES
('General', 'general', 'General information and updates'),
('Lottery Rules', 'lottery-rules', 'Rules and regulations for lotteries'),
('Help & Support', 'help-support', 'Help articles and support information'),
('Legal', 'legal', 'Legal documents and terms'),
('Promotions', 'promotions', 'Promotional content and offers');

-- Insert default system settings
INSERT INTO system_settings (key, value, type, description, is_public) VALUES
('site_name', 'Amesa Lottery', 'string', 'Site name', true),
('site_description', 'Innovative lottery platform supporting charitable causes', 'string', 'Site description', true),
('default_currency', 'USD', 'string', 'Default currency', true),
('min_ticket_price', '10.00', 'number', 'Minimum ticket price', true),
('max_ticket_price', '1000.00', 'number', 'Maximum ticket price', true),
('lottery_min_participation', '75.00', 'number', 'Minimum participation percentage for lottery execution', true),
('email_verification_required', 'true', 'boolean', 'Require email verification for registration', false),
('phone_verification_required', 'false', 'boolean', 'Require phone verification for registration', false),
('identity_verification_required', 'false', 'boolean', 'Require identity verification for registration', false);

-- Insert default email templates
INSERT INTO email_templates (name, subject, body_html, variables) VALUES
('welcome', 'Welcome to Amesa Lottery!', '<h1>Welcome {{user_name}}!</h1><p>Thank you for joining Amesa Lottery.</p>', ARRAY['user_name', 'user_email']),
('email_verification', 'Verify Your Email Address', '<h1>Verify Your Email</h1><p>Click the link to verify: <a href="{{verification_link}}">Verify Email</a></p>', ARRAY['user_name', 'verification_link']),
('lottery_winner', 'Congratulations! You Won!', '<h1>Congratulations {{user_name}}!</h1><p>You won the lottery for {{house_title}}!</p>', ARRAY['user_name', 'house_title', 'ticket_number']),
('lottery_ended', 'Lottery Ended - {{house_title}}', '<h1>Lottery Ended</h1><p>The lottery for {{house_title}} has ended.</p>', ARRAY['house_title', 'winner_name']);

-- =====================================================
-- VIEWS FOR COMMON QUERIES
-- =====================================================

-- Active lotteries view
CREATE VIEW active_lotteries AS
SELECT 
    h.*,
    COUNT(lt.id) as tickets_sold,
    ROUND((COUNT(lt.id)::decimal / h.total_tickets * 100), 2) as participation_percentage,
    CASE 
        WHEN COUNT(lt.id) >= (h.total_tickets * h.minimum_participation_percentage / 100) THEN true 
        ELSE false 
    END as can_execute
FROM houses h
LEFT JOIN lottery_tickets lt ON h.id = lt.house_id AND lt.status = 'active'
WHERE h.status IN ('upcoming', 'active')
GROUP BY h.id;

-- User lottery history view
CREATE VIEW user_lottery_history AS
SELECT 
    u.id as user_id,
    u.username,
    h.title as house_title,
    lt.ticket_number,
    lt.purchase_date,
    lt.purchase_price,
    lt.status as ticket_status,
    h.status as lottery_status,
    ld.winning_ticket_number,
    CASE WHEN lt.is_winner THEN true ELSE false END as is_winner
FROM users u
JOIN lottery_tickets lt ON u.id = lt.user_id
JOIN houses h ON lt.house_id = h.id
LEFT JOIN lottery_draws ld ON h.id = ld.house_id
ORDER BY lt.purchase_date DESC;

-- =====================================================
-- SECURITY & RLS (Row Level Security)
-- =====================================================

-- Enable RLS on sensitive tables
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_addresses ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_phones ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_identity_documents ENABLE ROW LEVEL SECURITY;
ALTER TABLE lottery_tickets ENABLE ROW LEVEL SECURITY;
ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_payment_methods ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_activity_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_notifications ENABLE ROW LEVEL SECURITY;

-- Create policies (example - adjust based on your security requirements)
CREATE POLICY user_own_data ON users FOR ALL TO authenticated USING (id = current_setting('app.current_user_id')::uuid);
CREATE POLICY user_own_addresses ON user_addresses FOR ALL TO authenticated USING (user_id = current_setting('app.current_user_id')::uuid);
CREATE POLICY user_own_tickets ON lottery_tickets FOR ALL TO authenticated USING (user_id = current_setting('app.current_user_id')::uuid);

-- =====================================================
-- COMMENTS FOR DOCUMENTATION
-- =====================================================

COMMENT ON TABLE users IS 'Core user accounts with authentication and profile information';
COMMENT ON TABLE houses IS 'Lottery properties with details and lottery configuration';
COMMENT ON TABLE lottery_tickets IS 'Individual lottery tickets purchased by users';
COMMENT ON TABLE transactions IS 'Financial transactions including ticket purchases and refunds';
COMMENT ON TABLE lottery_draws IS 'Lottery draw results and winner information';
COMMENT ON TABLE user_sessions IS 'User session tracking for security and analytics';
COMMENT ON TABLE content IS 'CMS content for pages, articles, and help documentation';

-- =====================================================
-- END OF SCHEMA
-- =====================================================
