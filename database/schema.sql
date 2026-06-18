CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_code VARCHAR(50),
    fullname VARCHAR(255) NOT NULL,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    department_id UUID REFERENCES departments(id),
    line_user_id VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    is_system_role BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(150) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    group_name VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS role_permissions (
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token TEXT UNIQUE NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMP,
    revoked_reason TEXT,
    replaced_by_token TEXT,
    created_by_ip TEXT,
    user_agent TEXT,
    last_used_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS approval_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_type VARCHAR(100) NOT NULL,
    request_id UUID NOT NULL,
    approver_id UUID REFERENCES users(id),
    action VARCHAR(50) NOT NULL,
    remark TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    channel VARCHAR(50) NOT NULL DEFAULT 'InApp',
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    is_read BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    action VARCHAR(100) NOT NULL,
    entity_name VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100),
    detail TEXT,
    ip_address VARCHAR(100),
    result VARCHAR(50) NOT NULL DEFAULT 'Success',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS leave_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    default_days_per_year NUMERIC(6,2) NOT NULL DEFAULT 0,
    requires_attachment BOOLEAN NOT NULL DEFAULT FALSE,
    is_paid BOOLEAN NOT NULL DEFAULT TRUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS leave_balances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    leave_type_id UUID NOT NULL REFERENCES leave_types(id) ON DELETE CASCADE,
    year INTEGER NOT NULL,
    entitled_days NUMERIC(6,2) NOT NULL DEFAULT 0,
    used_days NUMERIC(6,2) NOT NULL DEFAULT 0,
    pending_days NUMERIC(6,2) NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    UNIQUE (user_id, leave_type_id, year)
);

CREATE TABLE IF NOT EXISTS leave_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    leave_type_id UUID NOT NULL REFERENCES leave_types(id) ON DELETE CASCADE,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    total_days NUMERIC(6,2) NOT NULL,
    reason TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    current_approver_id UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    submitted_at TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS leave_attachments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    leave_request_id UUID NOT NULL REFERENCES leave_requests(id) ON DELETE CASCADE,
    file_name VARCHAR(255) NOT NULL,
    file_path TEXT NOT NULL,
    content_type VARCHAR(100),
    file_size_bytes BIGINT NOT NULL DEFAULT 0,
    uploaded_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS leave_approvals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    leave_request_id UUID NOT NULL REFERENCES leave_requests(id) ON DELETE CASCADE,
    approver_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    step_order INTEGER NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    remark TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    action_at TIMESTAMP
);

ALTER TABLE departments ADD COLUMN IF NOT EXISTS description TEXT;
ALTER TABLE departments ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;
ALTER TABLE departments ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;

ALTER TABLE roles ADD COLUMN IF NOT EXISTS is_system_role BOOLEAN DEFAULT FALSE;
ALTER TABLE roles ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;
ALTER TABLE roles ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;

ALTER TABLE permissions ADD COLUMN IF NOT EXISTS group_name VARCHAR(100);
ALTER TABLE permissions ADD COLUMN IF NOT EXISTS action VARCHAR(50);
ALTER TABLE permissions ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;
ALTER TABLE permissions ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;

UPDATE permissions
SET group_name = split_part(code, '.', 1),
    action = split_part(code, '.', 2)
WHERE group_name IS NULL OR action IS NULL;

ALTER TABLE permissions ALTER COLUMN group_name SET NOT NULL;
ALTER TABLE permissions ALTER COLUMN action SET NOT NULL;

ALTER TABLE users ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;

ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS revoked_reason TEXT;
ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS replaced_by_token TEXT;
ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS created_by_ip TEXT;
ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS user_agent TEXT;
ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS last_used_at TIMESTAMP;

ALTER TABLE audit_logs ADD COLUMN IF NOT EXISTS ip_address VARCHAR(100);
ALTER TABLE audit_logs ADD COLUMN IF NOT EXISTS result VARCHAR(50) NOT NULL DEFAULT 'Success';

CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE UNIQUE INDEX IF NOT EXISTS idx_departments_name ON departments(name);
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_employee_code ON users(employee_code);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_revoked ON refresh_tokens(user_id, revoked_at);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON audit_logs(created_at);
CREATE UNIQUE INDEX IF NOT EXISTS idx_leave_types_code ON leave_types(code);
CREATE UNIQUE INDEX IF NOT EXISTS idx_leave_balances_user_type_year ON leave_balances(user_id, leave_type_id, year);
CREATE INDEX IF NOT EXISTS idx_leave_requests_user_status ON leave_requests(user_id, status);
CREATE INDEX IF NOT EXISTS idx_leave_requests_current_approver ON leave_requests(current_approver_id);
CREATE INDEX IF NOT EXISTS idx_leave_attachments_request ON leave_attachments(leave_request_id);
CREATE INDEX IF NOT EXISTS idx_leave_approvals_request_step ON leave_approvals(leave_request_id, step_order);

GRANT USAGE ON SCHEMA public TO hop_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO hop_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO hop_user;
