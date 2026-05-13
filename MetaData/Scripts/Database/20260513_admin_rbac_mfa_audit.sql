CREATE SCHEMA IF NOT EXISTS amesa_admin;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

ALTER TABLE IF EXISTS amesa_admin.admin_users
    ADD COLUMN IF NOT EXISTS two_factor_enabled boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS two_factor_secret varchar(255),
    ADD COLUMN IF NOT EXISTS last_mfa_at timestamptz;

CREATE TABLE IF NOT EXISTS amesa_admin.admin_roles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100) NOT NULL UNIQUE,
    description varchar(500),
    is_system_role boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz
);

CREATE TABLE IF NOT EXISTS amesa_admin.admin_permissions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(150) NOT NULL UNIQUE,
    description varchar(500),
    category varchar(100) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS amesa_admin.admin_user_roles (
    admin_user_id uuid NOT NULL REFERENCES amesa_admin.admin_users(id) ON DELETE CASCADE,
    admin_role_id uuid NOT NULL REFERENCES amesa_admin.admin_roles(id) ON DELETE CASCADE,
    assigned_at timestamptz NOT NULL DEFAULT now(),
    assigned_by_admin_user_id uuid,
    PRIMARY KEY (admin_user_id, admin_role_id)
);

CREATE TABLE IF NOT EXISTS amesa_admin.admin_role_permissions (
    admin_role_id uuid NOT NULL REFERENCES amesa_admin.admin_roles(id) ON DELETE CASCADE,
    admin_permission_id uuid NOT NULL REFERENCES amesa_admin.admin_permissions(id) ON DELETE CASCADE,
    granted_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (admin_role_id, admin_permission_id)
);

CREATE TABLE IF NOT EXISTS amesa_admin.admin_sessions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_user_id uuid NOT NULL REFERENCES amesa_admin.admin_users(id) ON DELETE CASCADE,
    session_token varchar(255) NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    last_seen_at timestamptz,
    revoked_at timestamptz,
    ip_address varchar(64),
    user_agent varchar(512)
);

ALTER TABLE IF EXISTS amesa_admin.admin_sessions
    ADD COLUMN IF NOT EXISTS last_seen_at timestamptz,
    ADD COLUMN IF NOT EXISTS revoked_at timestamptz;

CREATE TABLE IF NOT EXISTS amesa_admin.audit_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    action varchar(100) NOT NULL,
    entity_type varchar(100) NOT NULL,
    entity_id uuid NOT NULL,
    admin_user_id uuid NOT NULL,
    action_details jsonb,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_admin_user_roles_role_id
    ON amesa_admin.admin_user_roles(admin_role_id);

CREATE INDEX IF NOT EXISTS ix_admin_role_permissions_permission_id
    ON amesa_admin.admin_role_permissions(admin_permission_id);

CREATE INDEX IF NOT EXISTS ix_admin_sessions_admin_user_id
    ON amesa_admin.admin_sessions(admin_user_id);

CREATE INDEX IF NOT EXISTS ix_audit_logs_entity
    ON amesa_admin.audit_logs(entity_type, entity_id);

CREATE INDEX IF NOT EXISTS ix_audit_logs_admin_created
    ON amesa_admin.audit_logs(admin_user_id, created_at DESC);

INSERT INTO amesa_admin.admin_roles (name, description, is_system_role)
VALUES ('SuperAdmin', 'Full access to every admin capability', true)
ON CONFLICT (name) DO UPDATE
SET description = EXCLUDED.description,
    is_system_role = true,
    updated_at = now();

INSERT INTO amesa_admin.admin_permissions (name, description, category)
VALUES
    ('dashboard.read', 'View admin dashboard metrics', 'dashboard'),
    ('houses.read', 'View houses', 'houses'),
    ('houses.write', 'Create and update houses', 'houses'),
    ('houses.publish', 'Activate and deactivate houses', 'houses'),
    ('houses.delete', 'Delete houses', 'houses'),
    ('users.read', 'View users', 'users'),
    ('users.write', 'Update users', 'users'),
    ('users.suspend', 'Suspend and activate users', 'users'),
    ('tickets.read', 'View lottery tickets', 'tickets'),
    ('draws.read', 'View lottery draws', 'draws'),
    ('draws.conduct', 'Conduct lottery draws', 'draws'),
    ('payments.read', 'View payment transactions', 'payments'),
    ('payments.refund', 'Refund payment transactions', 'payments'),
    ('translations.read', 'View translations', 'translations'),
    ('translations.write', 'Create, update, and delete translations', 'translations'),
    ('audit.read', 'View admin audit logs', 'audit'),
    ('admin_users.manage', 'Manage admin users and roles', 'admin_users'),
    ('settings.manage', 'Manage admin settings', 'settings')
ON CONFLICT (name) DO UPDATE
SET description = EXCLUDED.description,
    category = EXCLUDED.category;

INSERT INTO amesa_admin.admin_role_permissions (admin_role_id, admin_permission_id)
SELECT r.id, p.id
FROM amesa_admin.admin_roles r
CROSS JOIN amesa_admin.admin_permissions p
WHERE r.name = 'SuperAdmin'
ON CONFLICT DO NOTHING;

INSERT INTO amesa_admin.admin_user_roles (admin_user_id, admin_role_id)
SELECT u.id, r.id
FROM amesa_admin.admin_users u
CROSS JOIN amesa_admin.admin_roles r
WHERE r.name = 'SuperAdmin'
  AND u.is_active = true
  AND NOT EXISTS (SELECT 1 FROM amesa_admin.admin_user_roles)
ON CONFLICT DO NOTHING;
