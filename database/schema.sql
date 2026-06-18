CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
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
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    is_system_role BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(150) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    group_name VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
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
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMPTZ,
    revoked_reason TEXT,
    replaced_by_token TEXT,
    created_by_ip TEXT,
    user_agent TEXT,
    last_used_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS approval_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_type VARCHAR(100) NOT NULL,
    request_id UUID NOT NULL,
    approver_id UUID REFERENCES users(id),
    action VARCHAR(50) NOT NULL,
    remark TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    channel VARCHAR(50) NOT NULL DEFAULT 'InApp',
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    is_read BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
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
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
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
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS leave_balances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    leave_type_id UUID NOT NULL REFERENCES leave_types(id) ON DELETE CASCADE,
    year INTEGER NOT NULL,
    entitled_days NUMERIC(6,2) NOT NULL DEFAULT 0,
    used_days NUMERIC(6,2) NOT NULL DEFAULT 0,
    pending_days NUMERIC(6,2) NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
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
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    submitted_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS leave_attachments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    leave_request_id UUID NOT NULL REFERENCES leave_requests(id) ON DELETE CASCADE,
    file_name VARCHAR(255) NOT NULL,
    file_path TEXT NOT NULL,
    content_type VARCHAR(100),
    file_size_bytes BIGINT NOT NULL DEFAULT 0,
    uploaded_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS approval_chains (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) UNIQUE NOT NULL,
    description TEXT,
    department_id UUID REFERENCES departments(id),
    leave_type_id UUID REFERENCES leave_types(id),
    minimum_days NUMERIC(6,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS approval_chain_steps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    approval_chain_id UUID NOT NULL REFERENCES approval_chains(id) ON DELETE CASCADE,
    step_order INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    approver_role_id UUID REFERENCES roles(id),
    approver_user_id UUID REFERENCES users(id),
    required_permission_code VARCHAR(150) NOT NULL DEFAULT 'LeaveManagement.Approve',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    UNIQUE (approval_chain_id, step_order)
);

CREATE TABLE IF NOT EXISTS approval_delegations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    approver_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    delegate_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    reason TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS approval_escalation_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) UNIQUE NOT NULL,
    department_id UUID REFERENCES departments(id),
    leave_type_id UUID REFERENCES leave_types(id),
    escalate_after_hours INTEGER NOT NULL DEFAULT 24,
    escalate_to_user_id UUID REFERENCES users(id),
    escalate_to_role_id UUID REFERENCES roles(id),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS leave_approvals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    leave_request_id UUID NOT NULL REFERENCES leave_requests(id) ON DELETE CASCADE,
    approver_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    approval_chain_id UUID REFERENCES approval_chains(id),
    approval_chain_step_id UUID REFERENCES approval_chain_steps(id),
    step_order INTEGER NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    step_name VARCHAR(255),
    required_permission_code VARCHAR(150) NOT NULL DEFAULT 'LeaveManagement.Approve',
    remark TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    action_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS leave_balance_adjustments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    leave_type_id UUID NOT NULL REFERENCES leave_types(id) ON DELETE CASCADE,
    year INTEGER NOT NULL,
    adjustment_days NUMERIC(6,2) NOT NULL,
    reason TEXT NOT NULL,
    adjusted_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS leave_holidays (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    holiday_date DATE UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS line_delivery_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    leave_request_id UUID REFERENCES leave_requests(id),
    recipient_user_id UUID REFERENCES users(id),
    event_name VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Queued',
    payload TEXT NOT NULL,
    response_detail TEXT,
    attempt_count INTEGER NOT NULL DEFAULT 0,
    next_retry_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_departments_name ON departments(name);
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_employee_code ON users(employee_code) WHERE employee_code IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_revoked ON refresh_tokens(user_id, revoked_at);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON audit_logs(created_at);
CREATE UNIQUE INDEX IF NOT EXISTS idx_leave_types_code ON leave_types(code);
CREATE UNIQUE INDEX IF NOT EXISTS idx_leave_balances_user_type_year ON leave_balances(user_id, leave_type_id, year);
CREATE INDEX IF NOT EXISTS idx_leave_requests_user_status ON leave_requests(user_id, status);
CREATE INDEX IF NOT EXISTS idx_leave_requests_current_approver ON leave_requests(current_approver_id);
CREATE INDEX IF NOT EXISTS idx_leave_requests_status_dates ON leave_requests(status, start_date, end_date);
CREATE INDEX IF NOT EXISTS idx_leave_attachments_request ON leave_attachments(leave_request_id);
CREATE INDEX IF NOT EXISTS idx_approval_chains_department_type_days ON approval_chains(department_id, leave_type_id, minimum_days);
CREATE UNIQUE INDEX IF NOT EXISTS idx_approval_chain_steps_chain_order ON approval_chain_steps(approval_chain_id, step_order);
CREATE INDEX IF NOT EXISTS idx_approval_delegations_approver_dates ON approval_delegations(approver_user_id, start_date, end_date);
CREATE UNIQUE INDEX IF NOT EXISTS idx_approval_escalation_rules_name ON approval_escalation_rules(name);
CREATE INDEX IF NOT EXISTS idx_approval_escalation_rules_scope ON approval_escalation_rules(department_id, leave_type_id, is_active);
CREATE INDEX IF NOT EXISTS idx_leave_approvals_request_step ON leave_approvals(leave_request_id, step_order);
CREATE INDEX IF NOT EXISTS idx_leave_approvals_approver_status ON leave_approvals(approver_id, status);
CREATE INDEX IF NOT EXISTS idx_leave_balance_adjustments_user_type_year ON leave_balance_adjustments(user_id, leave_type_id, year);
CREATE UNIQUE INDEX IF NOT EXISTS idx_leave_holidays_date ON leave_holidays(holiday_date);
CREATE INDEX IF NOT EXISTS idx_line_delivery_logs_status_retry ON line_delivery_logs(status, next_retry_at);

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'hop_user') THEN
        GRANT USAGE ON SCHEMA public TO hop_user;
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO hop_user;
        GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO hop_user;
    END IF;
END $$;
