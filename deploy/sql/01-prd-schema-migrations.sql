-- HOP production schema migration bundle generated from EF Core migrations.
-- Safe for an existing database: every migration is guarded by __EFMigrationsHistory.
-- DBeaver: enable "Stop on error", run the whole script, and keep auto-commit enabled.

DO $precheck$
BEGIN
    IF current_setting('server_version_num')::integer < 140000 THEN
        RAISE EXCEPTION 'PostgreSQL 14 or newer is required. Current version: %', version();
    END IF;
END
$precheck$;

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE approval_logs (
        id uuid NOT NULL,
        request_type text NOT NULL,
        request_id uuid NOT NULL,
        approver_id uuid,
        action text NOT NULL,
        remark text,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_approval_logs" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE departments (
        id uuid NOT NULL,
        name text NOT NULL,
        description text,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_departments" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE notifications (
        id uuid NOT NULL,
        user_id uuid,
        channel text NOT NULL,
        title text NOT NULL,
        message text NOT NULL,
        is_read boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_notifications" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE permissions (
        id uuid NOT NULL,
        code text NOT NULL,
        name text NOT NULL,
        group_name text NOT NULL,
        action text NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_permissions" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE roles (
        id uuid NOT NULL,
        name text NOT NULL,
        description text,
        is_system_role boolean NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_roles" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        employee_code text,
        fullname text NOT NULL,
        username text NOT NULL,
        password_hash text NOT NULL,
        department_id uuid,
        line_user_id text,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_users" PRIMARY KEY (id),
        CONSTRAINT "FK_users_departments_department_id" FOREIGN KEY (department_id) REFERENCES departments (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE role_permissions (
        role_id uuid NOT NULL,
        permission_id uuid NOT NULL,
        CONSTRAINT "PK_role_permissions" PRIMARY KEY (role_id, permission_id),
        CONSTRAINT "FK_role_permissions_permissions_permission_id" FOREIGN KEY (permission_id) REFERENCES permissions (id) ON DELETE CASCADE,
        CONSTRAINT "FK_role_permissions_roles_role_id" FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE audit_logs (
        id uuid NOT NULL,
        user_id uuid,
        action text NOT NULL,
        entity_name text NOT NULL,
        entity_id text,
        detail text,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_audit_logs" PRIMARY KEY (id),
        CONSTRAINT "FK_audit_logs_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token text NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        CONSTRAINT "PK_refresh_tokens" PRIMARY KEY (id),
        CONSTRAINT "FK_refresh_tokens_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE TABLE user_roles (
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        CONSTRAINT "PK_user_roles" PRIMARY KEY (user_id, role_id),
        CONSTRAINT "FK_user_roles_roles_role_id" FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE,
        CONSTRAINT "FK_user_roles_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE INDEX "IX_audit_logs_created_at" ON audit_logs (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE INDEX "IX_audit_logs_user_id" ON audit_logs (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE UNIQUE INDEX "IX_departments_name" ON departments (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE UNIQUE INDEX "IX_permissions_code" ON permissions (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE UNIQUE INDEX "IX_refresh_tokens_token" ON refresh_tokens (token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE INDEX "IX_refresh_tokens_user_id" ON refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE INDEX "IX_role_permissions_permission_id" ON role_permissions (permission_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE UNIQUE INDEX "IX_roles_name" ON roles (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE INDEX "IX_user_roles_role_id" ON user_roles (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE INDEX "IX_users_department_id" ON users (department_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE UNIQUE INDEX "IX_users_employee_code" ON users (employee_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    CREATE UNIQUE INDEX "IX_users_username" ON users (username);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616133013_InitialAdminFoundation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260616133013_InitialAdminFoundation', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    ALTER TABLE audit_logs ADD ip_address text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    ALTER TABLE audit_logs ADD result text NOT NULL DEFAULT 'Success';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE TABLE leave_types (
        id uuid NOT NULL,
        code text NOT NULL,
        name text NOT NULL,
        description text,
        default_days_per_year numeric NOT NULL,
        requires_attachment boolean NOT NULL,
        is_paid boolean NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_leave_types" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE TABLE leave_balances (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        leave_type_id uuid NOT NULL,
        year integer NOT NULL,
        entitled_days numeric NOT NULL,
        used_days numeric NOT NULL,
        pending_days numeric NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_leave_balances" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_balances_leave_types_leave_type_id" FOREIGN KEY (leave_type_id) REFERENCES leave_types (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_balances_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE TABLE leave_requests (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        leave_type_id uuid NOT NULL,
        start_date date NOT NULL,
        end_date date NOT NULL,
        total_days numeric NOT NULL,
        reason text NOT NULL,
        status text NOT NULL,
        current_approver_id uuid,
        created_at timestamp with time zone NOT NULL,
        submitted_at timestamp with time zone,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_leave_requests" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_requests_leave_types_leave_type_id" FOREIGN KEY (leave_type_id) REFERENCES leave_types (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_requests_users_current_approver_id" FOREIGN KEY (current_approver_id) REFERENCES users (id),
        CONSTRAINT "FK_leave_requests_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE TABLE leave_approvals (
        id uuid NOT NULL,
        leave_request_id uuid NOT NULL,
        approver_id uuid NOT NULL,
        step_order integer NOT NULL,
        status text NOT NULL,
        remark text,
        created_at timestamp with time zone NOT NULL,
        action_at timestamp with time zone,
        CONSTRAINT "PK_leave_approvals" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_approvals_leave_requests_leave_request_id" FOREIGN KEY (leave_request_id) REFERENCES leave_requests (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_approvals_users_approver_id" FOREIGN KEY (approver_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE TABLE leave_attachments (
        id uuid NOT NULL,
        leave_request_id uuid NOT NULL,
        file_name text NOT NULL,
        file_path text NOT NULL,
        content_type text,
        file_size_bytes bigint NOT NULL,
        uploaded_by_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_leave_attachments" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_attachments_leave_requests_leave_request_id" FOREIGN KEY (leave_request_id) REFERENCES leave_requests (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_attachments_users_uploaded_by_user_id" FOREIGN KEY (uploaded_by_user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE INDEX "IX_leave_approvals_approver_id" ON leave_approvals (approver_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE INDEX "IX_leave_approvals_leave_request_id_step_order" ON leave_approvals (leave_request_id, step_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE INDEX "IX_leave_attachments_leave_request_id" ON leave_attachments (leave_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE INDEX "IX_leave_attachments_uploaded_by_user_id" ON leave_attachments (uploaded_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE INDEX "IX_leave_balances_leave_type_id" ON leave_balances (leave_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE UNIQUE INDEX "IX_leave_balances_user_id_leave_type_id_year" ON leave_balances (user_id, leave_type_id, year);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE INDEX "IX_leave_requests_current_approver_id" ON leave_requests (current_approver_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE INDEX "IX_leave_requests_leave_type_id" ON leave_requests (leave_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE INDEX "IX_leave_requests_user_id_status" ON leave_requests (user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    CREATE UNIQUE INDEX "IX_leave_types_code" ON leave_types (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616135914_Phase12SecurityGovernanceBranding') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260616135914_Phase12SecurityGovernanceBranding', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616235228_LeaveWorkflowSessionsAuditRetention') THEN
    DROP INDEX IF EXISTS "IX_refresh_tokens_user_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616235228_LeaveWorkflowSessionsAuditRetention') THEN
    ALTER TABLE refresh_tokens ADD created_by_ip text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616235228_LeaveWorkflowSessionsAuditRetention') THEN
    ALTER TABLE refresh_tokens ADD last_used_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616235228_LeaveWorkflowSessionsAuditRetention') THEN
    ALTER TABLE refresh_tokens ADD replaced_by_token text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616235228_LeaveWorkflowSessionsAuditRetention') THEN
    ALTER TABLE refresh_tokens ADD revoked_reason text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616235228_LeaveWorkflowSessionsAuditRetention') THEN
    ALTER TABLE refresh_tokens ADD user_agent text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616235228_LeaveWorkflowSessionsAuditRetention') THEN
    CREATE INDEX "IX_refresh_tokens_user_id_revoked_at" ON refresh_tokens (user_id, revoked_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616235228_LeaveWorkflowSessionsAuditRetention') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260616235228_LeaveWorkflowSessionsAuditRetention', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    ALTER TABLE leave_approvals ADD approval_chain_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    ALTER TABLE leave_approvals ADD approval_chain_step_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    ALTER TABLE leave_approvals ADD required_permission_code text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    ALTER TABLE leave_approvals ADD step_name text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE TABLE approval_chains (
        id uuid NOT NULL,
        name text NOT NULL,
        description text,
        department_id uuid,
        leave_type_id uuid,
        minimum_days numeric NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_approval_chains" PRIMARY KEY (id),
        CONSTRAINT "FK_approval_chains_departments_department_id" FOREIGN KEY (department_id) REFERENCES departments (id),
        CONSTRAINT "FK_approval_chains_leave_types_leave_type_id" FOREIGN KEY (leave_type_id) REFERENCES leave_types (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE TABLE leave_balance_adjustments (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        leave_type_id uuid NOT NULL,
        year integer NOT NULL,
        adjustment_days numeric NOT NULL,
        reason text NOT NULL,
        adjusted_by_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_leave_balance_adjustments" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_balance_adjustments_leave_types_leave_type_id" FOREIGN KEY (leave_type_id) REFERENCES leave_types (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_balance_adjustments_users_adjusted_by_user_id" FOREIGN KEY (adjusted_by_user_id) REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_balance_adjustments_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE TABLE leave_holidays (
        id uuid NOT NULL,
        holiday_date date NOT NULL,
        name text NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_leave_holidays" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE TABLE line_delivery_logs (
        id uuid NOT NULL,
        leave_request_id uuid,
        recipient_user_id uuid,
        event_name text NOT NULL,
        status text NOT NULL,
        payload text NOT NULL,
        response_detail text,
        attempt_count integer NOT NULL,
        next_retry_at timestamp with time zone,
        sent_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_line_delivery_logs" PRIMARY KEY (id),
        CONSTRAINT "FK_line_delivery_logs_leave_requests_leave_request_id" FOREIGN KEY (leave_request_id) REFERENCES leave_requests (id),
        CONSTRAINT "FK_line_delivery_logs_users_recipient_user_id" FOREIGN KEY (recipient_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE TABLE approval_chain_steps (
        id uuid NOT NULL,
        approval_chain_id uuid NOT NULL,
        step_order integer NOT NULL,
        name text NOT NULL,
        approver_role_id uuid,
        approver_user_id uuid,
        required_permission_code text NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_approval_chain_steps" PRIMARY KEY (id),
        CONSTRAINT "FK_approval_chain_steps_approval_chains_approval_chain_id" FOREIGN KEY (approval_chain_id) REFERENCES approval_chains (id) ON DELETE CASCADE,
        CONSTRAINT "FK_approval_chain_steps_roles_approver_role_id" FOREIGN KEY (approver_role_id) REFERENCES roles (id),
        CONSTRAINT "FK_approval_chain_steps_users_approver_user_id" FOREIGN KEY (approver_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_leave_approvals_approval_chain_id" ON leave_approvals (approval_chain_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_leave_approvals_approval_chain_step_id" ON leave_approvals (approval_chain_step_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE UNIQUE INDEX "IX_approval_chain_steps_approval_chain_id_step_order" ON approval_chain_steps (approval_chain_id, step_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_approval_chain_steps_approver_role_id" ON approval_chain_steps (approver_role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_approval_chain_steps_approver_user_id" ON approval_chain_steps (approver_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_approval_chains_department_id" ON approval_chains (department_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_approval_chains_leave_type_id" ON approval_chains (leave_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE UNIQUE INDEX "IX_approval_chains_name" ON approval_chains (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_leave_balance_adjustments_adjusted_by_user_id" ON leave_balance_adjustments (adjusted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_leave_balance_adjustments_leave_type_id" ON leave_balance_adjustments (leave_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_leave_balance_adjustments_user_id_leave_type_id_year" ON leave_balance_adjustments (user_id, leave_type_id, year);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE UNIQUE INDEX "IX_leave_holidays_holiday_date" ON leave_holidays (holiday_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_line_delivery_logs_leave_request_id" ON line_delivery_logs (leave_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_line_delivery_logs_recipient_user_id" ON line_delivery_logs (recipient_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    CREATE INDEX "IX_line_delivery_logs_status_next_retry_at" ON line_delivery_logs (status, next_retry_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    ALTER TABLE leave_approvals ADD CONSTRAINT "FK_leave_approvals_approval_chain_steps_approval_chain_step_id" FOREIGN KEY (approval_chain_step_id) REFERENCES approval_chain_steps (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    ALTER TABLE leave_approvals ADD CONSTRAINT "FK_leave_approvals_approval_chains_approval_chain_id" FOREIGN KEY (approval_chain_id) REFERENCES approval_chains (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260617010548_Phase21LeaveApprovalAdvanced') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617010548_Phase21LeaveApprovalAdvanced', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    CREATE TABLE approval_delegations (
        id uuid NOT NULL,
        approver_user_id uuid NOT NULL,
        delegate_user_id uuid NOT NULL,
        start_date date NOT NULL,
        end_date date NOT NULL,
        reason text NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_approval_delegations" PRIMARY KEY (id),
        CONSTRAINT "FK_approval_delegations_users_approver_user_id" FOREIGN KEY (approver_user_id) REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT "FK_approval_delegations_users_delegate_user_id" FOREIGN KEY (delegate_user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    CREATE TABLE approval_escalation_rules (
        id uuid NOT NULL,
        name text NOT NULL,
        department_id uuid,
        leave_type_id uuid,
        escalate_after_hours integer NOT NULL,
        escalate_to_user_id uuid,
        escalate_to_role_id uuid,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_approval_escalation_rules" PRIMARY KEY (id),
        CONSTRAINT "FK_approval_escalation_rules_departments_department_id" FOREIGN KEY (department_id) REFERENCES departments (id),
        CONSTRAINT "FK_approval_escalation_rules_leave_types_leave_type_id" FOREIGN KEY (leave_type_id) REFERENCES leave_types (id),
        CONSTRAINT "FK_approval_escalation_rules_roles_escalate_to_role_id" FOREIGN KEY (escalate_to_role_id) REFERENCES roles (id),
        CONSTRAINT "FK_approval_escalation_rules_users_escalate_to_user_id" FOREIGN KEY (escalate_to_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    CREATE INDEX "IX_approval_delegations_approver_user_id_start_date_end_date" ON approval_delegations (approver_user_id, start_date, end_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    CREATE INDEX "IX_approval_delegations_delegate_user_id" ON approval_delegations (delegate_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    CREATE INDEX "IX_approval_escalation_rules_department_id_leave_type_id_is_ac~" ON approval_escalation_rules (department_id, leave_type_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    CREATE INDEX "IX_approval_escalation_rules_escalate_to_role_id" ON approval_escalation_rules (escalate_to_role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    CREATE INDEX "IX_approval_escalation_rules_escalate_to_user_id" ON approval_escalation_rules (escalate_to_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    CREATE INDEX "IX_approval_escalation_rules_leave_type_id" ON approval_escalation_rules (leave_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    CREATE UNIQUE INDEX "IX_approval_escalation_rules_name" ON approval_escalation_rules (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618075042_LeaveOperationsReliability') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618075042_LeaveOperationsReliability', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260621143719_LeaveSupportDelegationOverride') THEN
    ALTER TABLE approval_delegations ADD cancelled_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260621143719_LeaveSupportDelegationOverride') THEN
    ALTER TABLE approval_delegations ADD created_by uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260621143719_LeaveSupportDelegationOverride') THEN
    CREATE TABLE approval_override_logs (
        id uuid NOT NULL,
        leave_request_id uuid NOT NULL,
        original_approver_id uuid,
        override_by_user_id uuid NOT NULL,
        action text NOT NULL,
        reason text NOT NULL,
        ip_address text,
        user_agent text,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_approval_override_logs" PRIMARY KEY (id),
        CONSTRAINT "FK_approval_override_logs_leave_requests_leave_request_id" FOREIGN KEY (leave_request_id) REFERENCES leave_requests (id) ON DELETE CASCADE,
        CONSTRAINT "FK_approval_override_logs_users_original_approver_id" FOREIGN KEY (original_approver_id) REFERENCES users (id),
        CONSTRAINT "FK_approval_override_logs_users_override_by_user_id" FOREIGN KEY (override_by_user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260621143719_LeaveSupportDelegationOverride') THEN
    CREATE INDEX "IX_approval_delegations_created_by" ON approval_delegations (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260621143719_LeaveSupportDelegationOverride') THEN
    CREATE INDEX "IX_approval_override_logs_leave_request_id" ON approval_override_logs (leave_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260621143719_LeaveSupportDelegationOverride') THEN
    CREATE INDEX "IX_approval_override_logs_original_approver_id" ON approval_override_logs (original_approver_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260621143719_LeaveSupportDelegationOverride') THEN
    CREATE INDEX "IX_approval_override_logs_override_by_user_id" ON approval_override_logs (override_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260621143719_LeaveSupportDelegationOverride') THEN
    ALTER TABLE approval_delegations ADD CONSTRAINT "FK_approval_delegations_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260621143719_LeaveSupportDelegationOverride') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260621143719_LeaveSupportDelegationOverride', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622023940_AddLeaveRequestNumber') THEN
    ALTER TABLE leave_requests ADD request_number character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622023940_AddLeaveRequestNumber') THEN
    WITH numbered AS (
        SELECT
            id,
            'LV-' || to_char(created_at, 'YYYYMM') || '-' ||
            lpad(row_number() OVER (
                PARTITION BY to_char(created_at, 'YYYYMM')
                ORDER BY created_at, id
            )::text, 3, '0') AS generated_request_number
        FROM leave_requests
        WHERE request_number IS NULL
    )
    UPDATE leave_requests AS request
    SET request_number = numbered.generated_request_number
    FROM numbered
    WHERE request.id = numbered.id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622023940_AddLeaveRequestNumber') THEN
    CREATE UNIQUE INDEX "IX_leave_requests_request_number" ON leave_requests (request_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622023940_AddLeaveRequestNumber') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260622023940_AddLeaveRequestNumber', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622070245_UserApprovalRule') THEN
    ALTER TABLE users ADD leave_approval_rule_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622070245_UserApprovalRule') THEN
    CREATE INDEX "IX_users_leave_approval_rule_id" ON users (leave_approval_rule_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622070245_UserApprovalRule') THEN
    ALTER TABLE users ADD CONSTRAINT "FK_users_approval_chains_leave_approval_rule_id" FOREIGN KEY (leave_approval_rule_id) REFERENCES approval_chains (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622070245_UserApprovalRule') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260622070245_UserApprovalRule', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623014426_AddLeaveDurationType') THEN
    ALTER TABLE leave_requests ADD duration_type character varying(20) NOT NULL DEFAULT 'FULL_DAY';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623014426_AddLeaveDurationType') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260623014426_AddLeaveDurationType', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623015631_AddLeaveTypeRequiresBalance') THEN
    ALTER TABLE leave_types ADD requires_balance boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623015631_AddLeaveTypeRequiresBalance') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260623015631_AddLeaveTypeRequiresBalance', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623083453_AddUserSelfProfileFields') THEN
    ALTER TABLE users ADD leave_contact_address text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623083453_AddUserSelfProfileFields') THEN
    ALTER TABLE users ADD phone_number text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623083453_AddUserSelfProfileFields') THEN
    ALTER TABLE users ADD position text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623083453_AddUserSelfProfileFields') THEN
    ALTER TABLE users ADD profile_image_url text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623083453_AddUserSelfProfileFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260623083453_AddUserSelfProfileFields', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260624032034_AddUserEmail') THEN
    ALTER TABLE users ADD email text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260624032034_AddUserEmail') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260624032034_AddUserEmail', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260624090000_AddLeaveHolidayNameIndex') THEN
    CREATE INDEX "IX_leave_holidays_name" ON leave_holidays (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260624090000_AddLeaveHolidayNameIndex') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260624090000_AddLeaveHolidayNameIndex', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260625042341_AddLeaveBalanceAdjustedDaysAndNotes') THEN
    ALTER TABLE leave_balances ADD adjusted_days numeric NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260625042341_AddLeaveBalanceAdjustedDaysAndNotes') THEN
    ALTER TABLE leave_balances ADD notes character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260625042341_AddLeaveBalanceAdjustedDaysAndNotes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260625042341_AddLeaveBalanceAdjustedDaysAndNotes', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260625090000_AddFiscalYearLeaveBalance') THEN
    ALTER TABLE leave_types ADD allow_carry_over boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260625090000_AddFiscalYearLeaveBalance') THEN
    ALTER TABLE leave_types ADD carry_over_max_days numeric NOT NULL DEFAULT 30.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260625090000_AddFiscalYearLeaveBalance') THEN
    ALTER TABLE leave_types ADD use_fiscal_year boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260625090000_AddFiscalYearLeaveBalance') THEN
    ALTER TABLE leave_balances ADD carried_over_days numeric NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260625090000_AddFiscalYearLeaveBalance') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260625090000_AddFiscalYearLeaveBalance', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD action_url text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD archived_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD category character varying(80) NOT NULL DEFAULT 'Leave';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD expires_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD notification_type character varying(40) NOT NULL DEFAULT 'Information';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD priority character varying(40) NOT NULL DEFAULT 'Information';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD read_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD reference_entity character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD reference_id character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    ALTER TABLE notifications ADD target_role character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    CREATE INDEX "IX_notifications_expires_at" ON notifications (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    CREATE INDEX "IX_notifications_target_role_category" ON notifications (target_role, category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    CREATE INDEX "IX_notifications_user_id_is_read_notification_type" ON notifications (user_id, is_read, notification_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629090000_AddRoleBasedNotifications') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260629090000_AddRoleBasedNotifications', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629093000_AddLineTestSendPermission') THEN
    INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
    SELECT gen_random_uuid(), 'System.Line.TestSend', 'ทดสอบส่งข้อความ LINE', 'System', 'LineTestSend', true, NOW()
    WHERE NOT EXISTS (
        SELECT 1 FROM permissions WHERE code = 'System.Line.TestSend'
    );

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.name IN ('Admin', 'SuperAdmin')
      AND p.code = 'System.Line.TestSend'
      AND NOT EXISTS (
          SELECT 1
          FROM role_permissions rp
          WHERE rp.role_id = r.id AND rp.permission_id = p.id
      );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260629093000_AddLineTestSendPermission') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260629093000_AddLineTestSendPermission', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260630043133_AddUserProfileImageFields') THEN
    ALTER TABLE users ADD profile_image_content_type text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260630043133_AddUserProfileImageFields') THEN
    ALTER TABLE users ADD profile_image_file_name text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260630043133_AddUserProfileImageFields') THEN
    ALTER TABLE users ADD profile_image_path text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260630043133_AddUserProfileImageFields') THEN
    ALTER TABLE users ADD profile_image_updated_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260630043133_AddUserProfileImageFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260630043133_AddUserProfileImageFields', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701015112_AddLineUserBinding') THEN
    CREATE TABLE line_pairing_codes (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        code character varying(20) NOT NULL,
        status character varying(40) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        used_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_line_pairing_codes" PRIMARY KEY (id),
        CONSTRAINT "FK_line_pairing_codes_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701015112_AddLineUserBinding') THEN
    CREATE TABLE line_user_bindings (
        id uuid NOT NULL,
        line_user_id character varying(80) NOT NULL,
        display_name character varying(200),
        picture_url character varying(1000),
        user_id uuid,
        status character varying(40) NOT NULL,
        last_event_type character varying(40),
        last_event_at timestamp with time zone,
        bound_at timestamp with time zone,
        unbound_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_line_user_bindings" PRIMARY KEY (id),
        CONSTRAINT "FK_line_user_bindings_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701015112_AddLineUserBinding') THEN
    CREATE UNIQUE INDEX "IX_line_pairing_codes_code" ON line_pairing_codes (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701015112_AddLineUserBinding') THEN
    CREATE INDEX "IX_line_pairing_codes_expires_at" ON line_pairing_codes (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701015112_AddLineUserBinding') THEN
    CREATE INDEX "IX_line_pairing_codes_user_id_status" ON line_pairing_codes (user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701015112_AddLineUserBinding') THEN
    CREATE UNIQUE INDEX "IX_line_user_bindings_line_user_id" ON line_user_bindings (line_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701015112_AddLineUserBinding') THEN
    CREATE INDEX "IX_line_user_bindings_status" ON line_user_bindings (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701015112_AddLineUserBinding') THEN
    CREATE UNIQUE INDEX "IX_line_user_bindings_user_id" ON line_user_bindings (user_id) WHERE "user_id" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701015112_AddLineUserBinding') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260701015112_AddLineUserBinding', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701020552_UpdateLineBindingHistoryIndex') THEN
    DROP INDEX "IX_line_user_bindings_user_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701020552_UpdateLineBindingHistoryIndex') THEN
    CREATE UNIQUE INDEX "IX_line_user_bindings_user_id" ON line_user_bindings (user_id) WHERE "user_id" IS NOT NULL AND "status" = 'Bound';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701020552_UpdateLineBindingHistoryIndex') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260701020552_UpdateLineBindingHistoryIndex', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701023110_BackfillLegacyLineUserBindings') THEN
    INSERT INTO line_user_bindings (
        id,
        line_user_id,
        user_id,
        status,
        bound_at,
        created_at,
        updated_at
    )
    SELECT
        md5('line-binding:' || ranked.line_user_id)::uuid,
        ranked.line_user_id,
        ranked.id,
        'Bound',
        COALESCE(ranked.updated_at, ranked.created_at, NOW()),
        COALESCE(ranked.created_at, NOW()),
        NOW()
    FROM (
        SELECT
            u.id,
            btrim(u.line_user_id) AS line_user_id,
            u.created_at,
            u.updated_at,
            ROW_NUMBER() OVER (
                PARTITION BY btrim(u.line_user_id)
                ORDER BY COALESCE(u.updated_at, u.created_at, NOW()) DESC, u.id
            ) AS row_number
        FROM users u
        WHERE u.line_user_id IS NOT NULL
            AND btrim(u.line_user_id) <> ''
    ) ranked
    WHERE ranked.row_number = 1
        AND NOT EXISTS (
            SELECT 1
            FROM line_user_bindings existing
            WHERE existing.line_user_id = ranked.line_user_id
        )
        AND NOT EXISTS (
            SELECT 1
            FROM line_user_bindings existing
            WHERE existing.user_id = ranked.id
                AND existing.status = 'Bound'
        );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701023110_BackfillLegacyLineUserBindings') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260701023110_BackfillLegacyLineUserBindings', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701025742_AddLineConnectTokens') THEN
    CREATE TABLE line_connect_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token character varying(120) NOT NULL,
        short_code character varying(20) NOT NULL,
        status character varying(40) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        used_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        created_by_ip character varying(80),
        line_user_id character varying(80),
        metadata jsonb,
        CONSTRAINT "PK_line_connect_tokens" PRIMARY KEY (id),
        CONSTRAINT "FK_line_connect_tokens_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701025742_AddLineConnectTokens') THEN
    CREATE INDEX "IX_line_connect_tokens_expires_at" ON line_connect_tokens (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701025742_AddLineConnectTokens') THEN
    CREATE UNIQUE INDEX "IX_line_connect_tokens_short_code" ON line_connect_tokens (short_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701025742_AddLineConnectTokens') THEN
    CREATE UNIQUE INDEX "IX_line_connect_tokens_token" ON line_connect_tokens (token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701025742_AddLineConnectTokens') THEN
    CREATE INDEX "IX_line_connect_tokens_user_id_status" ON line_connect_tokens (user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701025742_AddLineConnectTokens') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260701025742_AddLineConnectTokens', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701042150_AddEmploymentTypeLeavePolicyRules') THEN
    ALTER TABLE users ADD employment_start_date date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701042150_AddEmploymentTypeLeavePolicyRules') THEN
    ALTER TABLE users ADD employment_type character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701042150_AddEmploymentTypeLeavePolicyRules') THEN
    CREATE TABLE leave_policy_rules (
        id uuid NOT NULL,
        employment_type character varying(80) NOT NULL,
        leave_type_id uuid NOT NULL,
        fiscal_year integer,
        entitlement_days numeric NOT NULL,
        max_paid_days numeric,
        allow_carry_over boolean NOT NULL,
        carry_over_max_days numeric,
        max_accumulated_days numeric,
        min_service_months integer,
        min_service_years integer,
        prorate_if_service_less_than_year boolean NOT NULL,
        first_year_entitlement_days numeric,
        first_year_paid_days numeric,
        is_paid boolean NOT NULL,
        max_extended_days numeric,
        social_security_max_days numeric,
        notes character varying(1000),
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_leave_policy_rules" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_policy_rules_leave_types_leave_type_id" FOREIGN KEY (leave_type_id) REFERENCES leave_types (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701042150_AddEmploymentTypeLeavePolicyRules') THEN
    CREATE INDEX "IX_users_employment_type" ON users (employment_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701042150_AddEmploymentTypeLeavePolicyRules') THEN
    CREATE INDEX "IX_leave_policy_rules_employment_type_leave_type_id_fiscal_yea~" ON leave_policy_rules (employment_type, leave_type_id, fiscal_year, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701042150_AddEmploymentTypeLeavePolicyRules') THEN
    CREATE INDEX "IX_leave_policy_rules_leave_type_id" ON leave_policy_rules (leave_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701042150_AddEmploymentTypeLeavePolicyRules') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260701042150_AddEmploymentTypeLeavePolicyRules', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701072241_AddGenderEligibilityValidation') THEN
    ALTER TABLE users ADD gender character varying(20) NOT NULL DEFAULT 'Unknown';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701072241_AddGenderEligibilityValidation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260701072241_AddGenderEligibilityValidation', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701091139_AddProductionReadyLeaveRollover') THEN
    CREATE TABLE leave_balance_rollover_runs (
        id uuid NOT NULL,
        from_fiscal_year integer NOT NULL,
        to_fiscal_year integer NOT NULL,
        status character varying(40) NOT NULL,
        filters_json text,
        total integer NOT NULL,
        created_count integer NOT NULL,
        updated_count integer NOT NULL,
        skipped_count integer NOT NULL,
        blocked_count integer NOT NULL,
        reason text,
        started_at timestamp with time zone NOT NULL,
        completed_at timestamp with time zone,
        created_by_user_id uuid,
        CONSTRAINT "PK_leave_balance_rollover_runs" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_balance_rollover_runs_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701091139_AddProductionReadyLeaveRollover') THEN
    CREATE TABLE leave_balance_snapshots (
        id uuid NOT NULL,
        rollover_run_id uuid NOT NULL,
        user_id uuid NOT NULL,
        leave_type_id uuid NOT NULL,
        fiscal_year integer NOT NULL,
        entitlement_days numeric NOT NULL,
        carried_over_days numeric NOT NULL,
        adjusted_days numeric NOT NULL,
        used_days numeric NOT NULL,
        pending_days numeric NOT NULL,
        available_days numeric NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        CONSTRAINT "PK_leave_balance_snapshots" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_balance_snapshots_leave_balance_rollover_runs_rollove~" FOREIGN KEY (rollover_run_id) REFERENCES leave_balance_rollover_runs (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_balance_snapshots_leave_types_leave_type_id" FOREIGN KEY (leave_type_id) REFERENCES leave_types (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_balance_snapshots_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id),
        CONSTRAINT "FK_leave_balance_snapshots_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701091139_AddProductionReadyLeaveRollover') THEN
    CREATE INDEX "IX_leave_balance_rollover_runs_created_by_user_id" ON leave_balance_rollover_runs (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701091139_AddProductionReadyLeaveRollover') THEN
    CREATE INDEX "IX_leave_balance_rollover_runs_from_fiscal_year_to_fiscal_year~" ON leave_balance_rollover_runs (from_fiscal_year, to_fiscal_year, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701091139_AddProductionReadyLeaveRollover') THEN
    CREATE INDEX "IX_leave_balance_snapshots_created_by_user_id" ON leave_balance_snapshots (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701091139_AddProductionReadyLeaveRollover') THEN
    CREATE INDEX "IX_leave_balance_snapshots_leave_type_id" ON leave_balance_snapshots (leave_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701091139_AddProductionReadyLeaveRollover') THEN
    CREATE INDEX "IX_leave_balance_snapshots_rollover_run_id" ON leave_balance_snapshots (rollover_run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701091139_AddProductionReadyLeaveRollover') THEN
    CREATE INDEX "IX_leave_balance_snapshots_user_id_leave_type_id_fiscal_year" ON leave_balance_snapshots (user_id, leave_type_id, fiscal_year);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260701091139_AddProductionReadyLeaveRollover') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260701091139_AddProductionReadyLeaveRollover', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260709120000_AddDocumentationCenterPermissions') THEN
    INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
    SELECT gen_random_uuid(), item.code, item.name, item.group_name, item.action, true, NOW()
    FROM (VALUES
        ('Documentation.View', 'ดูศูนย์คู่มือการใช้งาน', 'Documentation', 'View'),
        ('Documentation.AdminView', 'ดูคู่มือสำหรับผู้ดูแลระบบ', 'Documentation', 'AdminView'),
        ('Documentation.Manage', 'จัดการคู่มือการใช้งาน', 'Documentation', 'Manage')
    ) AS item(code, name, group_name, action)
    WHERE NOT EXISTS (
        SELECT 1 FROM permissions p WHERE p.code = item.code
    );

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.name IN ('Staff', 'DepartmentHead', 'Director', 'LeaveAdmin', 'Admin', 'SuperAdmin')
      AND p.code = 'Documentation.View'
      AND NOT EXISTS (
          SELECT 1
          FROM role_permissions rp
          WHERE rp.role_id = r.id AND rp.permission_id = p.id
      );

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.name IN ('Admin', 'SuperAdmin')
      AND p.code IN ('Documentation.AdminView', 'Documentation.Manage')
      AND NOT EXISTS (
          SELECT 1
          FROM role_permissions rp
          WHERE rp.role_id = r.id AND rp.permission_id = p.id
      );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260709120000_AddDocumentationCenterPermissions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260709120000_AddDocumentationCenterPermissions', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260710035043_AddSelfServicePasswordChange') THEN
    ALTER TABLE users ADD COLUMN IF NOT EXISTS password_changed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260710035043_AddSelfServicePasswordChange') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260710035043_AddSelfServicePasswordChange', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    ALTER TABLE leave_requests ADD last_resubmitted_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    ALTER TABLE leave_requests ADD returned_for_revision_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    ALTER TABLE leave_requests ADD returned_for_revision_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    ALTER TABLE leave_requests ADD revision_count integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    ALTER TABLE leave_requests ADD revision_reason character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    ALTER TABLE leave_approvals ADD return_reason character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    ALTER TABLE leave_approvals ADD returned_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    CREATE INDEX "IX_leave_requests_returned_for_revision_by_user_id" ON leave_requests (returned_for_revision_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    ALTER TABLE leave_requests ADD CONSTRAINT "FK_leave_requests_users_returned_for_revision_by_user_id" FOREIGN KEY (returned_for_revision_by_user_id) REFERENCES users (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260711104616_AddLeaveRevisionWorkflow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260711104616_AddLeaveRevisionWorkflow', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE TABLE backup_runs (
        id uuid NOT NULL,
        backup_type character varying(40) NOT NULL,
        status character varying(40) NOT NULL,
        file_name character varying(260) NOT NULL,
        file_path character varying(1000) NOT NULL,
        file_size_bytes bigint NOT NULL,
        checksum character varying(128),
        started_at timestamp with time zone NOT NULL,
        completed_at timestamp with time zone,
        duration_ms bigint,
        error_message character varying(1000),
        created_by_user_id uuid,
        verified_at timestamp with time zone,
        verified_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_backup_runs" PRIMARY KEY (id),
        CONSTRAINT "FK_backup_runs_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id),
        CONSTRAINT "FK_backup_runs_users_deleted_by_user_id" FOREIGN KEY (deleted_by_user_id) REFERENCES users (id),
        CONSTRAINT "FK_backup_runs_users_verified_by_user_id" FOREIGN KEY (verified_by_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE TABLE restore_runs (
        id uuid NOT NULL,
        backup_run_id uuid NOT NULL,
        restore_type character varying(40) NOT NULL,
        target_environment character varying(80) NOT NULL,
        target_database character varying(200),
        status character varying(40) NOT NULL,
        reason character varying(1000) NOT NULL,
        started_at timestamp with time zone NOT NULL,
        completed_at timestamp with time zone,
        duration_ms bigint,
        error_message character varying(1000),
        created_by_user_id uuid,
        confirmation_method character varying(80) NOT NULL,
        pre_restore_backup_run_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_restore_runs" PRIMARY KEY (id),
        CONSTRAINT "FK_restore_runs_backup_runs_backup_run_id" FOREIGN KEY (backup_run_id) REFERENCES backup_runs (id) ON DELETE CASCADE,
        CONSTRAINT "FK_restore_runs_backup_runs_pre_restore_backup_run_id" FOREIGN KEY (pre_restore_backup_run_id) REFERENCES backup_runs (id),
        CONSTRAINT "FK_restore_runs_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE INDEX "IX_backup_runs_backup_type_status_started_at" ON backup_runs (backup_type, status, started_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE INDEX "IX_backup_runs_created_by_user_id" ON backup_runs (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE INDEX "IX_backup_runs_deleted_by_user_id" ON backup_runs (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE UNIQUE INDEX "IX_backup_runs_file_path" ON backup_runs (file_path);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE INDEX "IX_backup_runs_verified_by_user_id" ON backup_runs (verified_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE INDEX "IX_restore_runs_backup_run_id_status_started_at" ON restore_runs (backup_run_id, status, started_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE INDEX "IX_restore_runs_created_by_user_id" ON restore_runs (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    CREATE INDEX "IX_restore_runs_pre_restore_backup_run_id" ON restore_runs (pre_restore_backup_run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
    SELECT gen_random_uuid(), item.code, item.name, item.group_name, item.action, true, NOW()
    FROM (VALUES
        ('System.Backup.View', 'ดู Backup Center', 'SystemBackup', 'View'),
        ('System.Backup.Run', 'ตรวจสอบและบันทึก Backup', 'SystemBackup', 'Run'),
        ('System.Backup.Restore', 'ดำเนินการ Restore Backup', 'SystemBackup', 'Restore'),
        ('System.Backup.ManageRetention', 'จัดการ Retention Backup', 'SystemBackup', 'ManageRetention')
    ) AS item(code, name, group_name, action)
    WHERE NOT EXISTS (
        SELECT 1 FROM permissions p WHERE p.code = item.code
    );

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.name = 'SuperAdmin'
      AND p.code IN ('System.Backup.View', 'System.Backup.Run', 'System.Backup.Restore', 'System.Backup.ManageRetention')
      AND NOT EXISTS (
          SELECT 1
          FROM role_permissions rp
          WHERE rp.role_id = r.id AND rp.permission_id = p.id
      );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713140933_AddBackupRestoreHistoryAndRetention') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260713140933_AddBackupRestoreHistoryAndRetention', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717045456_AddLeaveBalanceTransactions') THEN
    CREATE TABLE leave_balance_transactions (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        leave_type_id uuid NOT NULL,
        fiscal_year integer NOT NULL,
        transaction_type character varying(80) NOT NULL,
        amount_days numeric NOT NULL,
        reference_type character varying(120) NOT NULL,
        reference_id uuid NOT NULL,
        reason character varying(1000) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        CONSTRAINT "PK_leave_balance_transactions" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_balance_transactions_leave_types_leave_type_id" FOREIGN KEY (leave_type_id) REFERENCES leave_types (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_balance_transactions_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id),
        CONSTRAINT "FK_leave_balance_transactions_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717045456_AddLeaveBalanceTransactions') THEN
    CREATE INDEX "IX_leave_balance_transactions_created_by_user_id" ON leave_balance_transactions (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717045456_AddLeaveBalanceTransactions') THEN
    CREATE INDEX "IX_leave_balance_transactions_leave_type_id" ON leave_balance_transactions (leave_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717045456_AddLeaveBalanceTransactions') THEN
    CREATE UNIQUE INDEX "IX_leave_balance_transactions_reference_type_reference_id_tran~" ON leave_balance_transactions (reference_type, reference_id, transaction_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717045456_AddLeaveBalanceTransactions') THEN
    CREATE INDEX "IX_leave_balance_transactions_user_id_leave_type_id_fiscal_year" ON leave_balance_transactions (user_id, leave_type_id, fiscal_year);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717045456_AddLeaveBalanceTransactions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260717045456_AddLeaveBalanceTransactions', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE TABLE leave_cancellation_requests (
        id uuid NOT NULL,
        cancellation_request_number character varying(24) NOT NULL,
        original_leave_request_id uuid NOT NULL,
        requester_user_id uuid NOT NULL,
        leave_type_id uuid NOT NULL,
        original_leave_days numeric NOT NULL,
        reason character varying(1000) NOT NULL,
        status character varying(40) NOT NULL,
        approval_chain_id uuid,
        current_approver_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        created_by_user_id uuid,
        submitted_at timestamp with time zone,
        approved_at timestamp with time zone,
        rejected_at timestamp with time zone,
        cancelled_at timestamp with time zone,
        returned_for_revision_at timestamp with time zone,
        returned_for_revision_by_user_id uuid,
        revision_reason character varying(1000),
        revision_count integer NOT NULL DEFAULT 0,
        last_resubmitted_at timestamp with time zone,
        balance_restored_at timestamp with time zone,
        CONSTRAINT "PK_leave_cancellation_requests" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_cancellation_requests_approval_chains_approval_chain_~" FOREIGN KEY (approval_chain_id) REFERENCES approval_chains (id),
        CONSTRAINT "FK_leave_cancellation_requests_leave_requests_original_leave_r~" FOREIGN KEY (original_leave_request_id) REFERENCES leave_requests (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_cancellation_requests_leave_types_leave_type_id" FOREIGN KEY (leave_type_id) REFERENCES leave_types (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_cancellation_requests_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id),
        CONSTRAINT "FK_leave_cancellation_requests_users_current_approver_id" FOREIGN KEY (current_approver_id) REFERENCES users (id),
        CONSTRAINT "FK_leave_cancellation_requests_users_requester_user_id" FOREIGN KEY (requester_user_id) REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_cancellation_requests_users_returned_for_revision_by_~" FOREIGN KEY (returned_for_revision_by_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE TABLE leave_cancellation_approvals (
        id uuid NOT NULL,
        leave_cancellation_request_id uuid NOT NULL,
        approver_id uuid NOT NULL,
        approval_chain_id uuid,
        approval_chain_step_id uuid,
        step_order integer NOT NULL,
        status character varying(40) NOT NULL,
        step_name character varying(200),
        required_permission_code character varying(120) NOT NULL,
        remark character varying(1000),
        created_at timestamp with time zone NOT NULL,
        action_at timestamp with time zone,
        returned_at timestamp with time zone,
        return_reason character varying(1000),
        CONSTRAINT "PK_leave_cancellation_approvals" PRIMARY KEY (id),
        CONSTRAINT "FK_leave_cancellation_approvals_approval_chain_steps_approval_~" FOREIGN KEY (approval_chain_step_id) REFERENCES approval_chain_steps (id),
        CONSTRAINT "FK_leave_cancellation_approvals_approval_chains_approval_chain~" FOREIGN KEY (approval_chain_id) REFERENCES approval_chains (id),
        CONSTRAINT "FK_leave_cancellation_approvals_leave_cancellation_requests_le~" FOREIGN KEY (leave_cancellation_request_id) REFERENCES leave_cancellation_requests (id) ON DELETE CASCADE,
        CONSTRAINT "FK_leave_cancellation_approvals_users_approver_id" FOREIGN KEY (approver_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_approvals_approval_chain_id" ON leave_cancellation_approvals (approval_chain_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_approvals_approval_chain_step_id" ON leave_cancellation_approvals (approval_chain_step_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_approvals_approver_id" ON leave_cancellation_approvals (approver_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_leave_cancellation_approvals_leave_cancellation_request_id_~" ON leave_cancellation_approvals (leave_cancellation_request_id, step_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_requests_approval_chain_id" ON leave_cancellation_requests (approval_chain_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_leave_cancellation_requests_cancellation_request_number" ON leave_cancellation_requests (cancellation_request_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_requests_created_by_user_id" ON leave_cancellation_requests (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_requests_current_approver_id" ON leave_cancellation_requests (current_approver_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_requests_leave_type_id" ON leave_cancellation_requests (leave_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_leave_cancellation_requests_original_leave_request_id" ON leave_cancellation_requests (original_leave_request_id) WHERE "status" IN ('Draft', 'Pending', 'ReturnedForRevision');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_requests_requester_user_id_status" ON leave_cancellation_requests (requester_user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_requests_returned_for_revision_by_user_id" ON leave_cancellation_requests (returned_for_revision_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    CREATE INDEX "IX_leave_cancellation_requests_status" ON leave_cancellation_requests (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717062834_AddLeaveCancellationWorkflow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260717062834_AddLeaveCancellationWorkflow', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
    SELECT gen_random_uuid(), item.code, item.name, 'SystemDiagnostics', item.action, true, NOW()
    FROM (VALUES
        ('System.Diagnostics.View', 'ดู Diagnostics Center', 'View'),
        ('System.Diagnostics.Run', 'รัน Diagnostics Test', 'Run'),
        ('System.Diagnostics.Export', 'สร้างและดาวน์โหลด Support Bundle', 'Export')
    ) AS item(code, name, action)
    WHERE NOT EXISTS (
        SELECT 1 FROM permissions p WHERE p.code = item.code
    );

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.name IN ('Admin', 'SuperAdmin')
      AND p.code IN ('System.Diagnostics.View', 'System.Diagnostics.Run', 'System.Diagnostics.Export')
      AND NOT EXISTS (
          SELECT 1 FROM role_permissions rp
          WHERE rp.role_id = r.id AND rp.permission_id = p.id
      );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    CREATE TABLE diagnostic_runs (
        id uuid NOT NULL,
        diagnostic_type character varying(80) NOT NULL,
        status character varying(40) NOT NULL,
        started_at timestamp with time zone NOT NULL,
        completed_at timestamp with time zone,
        duration_ms bigint,
        result_summary character varying(2000),
        reference_id character varying(120),
        created_by_user_id uuid,
        error_message character varying(1000),
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_diagnostic_runs" PRIMARY KEY (id),
        CONSTRAINT "FK_diagnostic_runs_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    CREATE TABLE support_bundles (
        id uuid NOT NULL,
        file_name character varying(260) NOT NULL,
        file_path character varying(1000) NOT NULL,
        file_size_bytes bigint NOT NULL,
        checksum character varying(128),
        expires_at timestamp with time zone NOT NULL,
        reason character varying(1000) NOT NULL,
        status character varying(40) NOT NULL,
        created_by_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        downloaded_at timestamp with time zone,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_support_bundles" PRIMARY KEY (id),
        CONSTRAINT "FK_support_bundles_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    CREATE INDEX "IX_diagnostic_runs_created_by_user_id" ON diagnostic_runs (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    CREATE INDEX "IX_diagnostic_runs_diagnostic_type_started_at" ON diagnostic_runs (diagnostic_type, started_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    CREATE INDEX "IX_diagnostic_runs_reference_id" ON diagnostic_runs (reference_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    CREATE INDEX "IX_support_bundles_created_at" ON support_bundles (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    CREATE INDEX "IX_support_bundles_created_by_user_id" ON support_bundles (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    CREATE INDEX "IX_support_bundles_status_expires_at" ON support_bundles (status, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260717160121_AddDiagnosticsCenterAndSupportBundles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260717160121_AddDiagnosticsCenterAndSupportBundles', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE TABLE announcement_categories (
        id uuid NOT NULL,
        name character varying(160) NOT NULL,
        description character varying(1000),
        color character varying(20),
        is_active boolean NOT NULL,
        display_order integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_announcement_categories" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE TABLE announcements (
        id uuid NOT NULL,
        title character varying(240) NOT NULL,
        summary character varying(1000) NOT NULL,
        body text NOT NULL,
        status character varying(40) NOT NULL,
        priority character varying(40) NOT NULL,
        category_id uuid,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        published_by_user_id uuid,
        publish_at timestamp with time zone,
        expires_at timestamp with time zone,
        published_at timestamp with time zone,
        archived_at timestamp with time zone,
        cancelled_at timestamp with time zone,
        is_featured boolean NOT NULL,
        show_as_popup boolean NOT NULL,
        show_as_banner boolean NOT NULL,
        requires_acknowledgement boolean NOT NULL,
        cover_image_url character varying(1000),
        tags character varying(1000),
        view_count integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_announcements" PRIMARY KEY (id),
        CONSTRAINT "FK_announcements_announcement_categories_category_id" FOREIGN KEY (category_id) REFERENCES announcement_categories (id),
        CONSTRAINT "FK_announcements_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_announcements_users_published_by_user_id" FOREIGN KEY (published_by_user_id) REFERENCES users (id),
        CONSTRAINT "FK_announcements_users_updated_by_user_id" FOREIGN KEY (updated_by_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE TABLE announcement_files (
        id uuid NOT NULL,
        announcement_id uuid NOT NULL,
        file_name character varying(260) NOT NULL,
        original_file_name character varying(260) NOT NULL,
        content_type character varying(120) NOT NULL,
        file_path character varying(1000) NOT NULL,
        file_size bigint NOT NULL,
        file_role character varying(40) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_announcement_files" PRIMARY KEY (id),
        CONSTRAINT "FK_announcement_files_announcements_announcement_id" FOREIGN KEY (announcement_id) REFERENCES announcements (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE TABLE announcement_reads (
        id uuid NOT NULL,
        announcement_id uuid NOT NULL,
        user_id uuid NOT NULL,
        read_at timestamp with time zone NOT NULL,
        acknowledged_at timestamp with time zone,
        CONSTRAINT "PK_announcement_reads" PRIMARY KEY (id),
        CONSTRAINT "FK_announcement_reads_announcements_announcement_id" FOREIGN KEY (announcement_id) REFERENCES announcements (id) ON DELETE CASCADE,
        CONSTRAINT "FK_announcement_reads_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE TABLE announcement_targets (
        id uuid NOT NULL,
        announcement_id uuid NOT NULL,
        target_type character varying(40) NOT NULL,
        target_value character varying(160),
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_announcement_targets" PRIMARY KEY (id),
        CONSTRAINT "FK_announcement_targets_announcements_announcement_id" FOREIGN KEY (announcement_id) REFERENCES announcements (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcement_categories_is_active_display_order" ON announcement_categories (is_active, display_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE UNIQUE INDEX "IX_announcement_categories_name" ON announcement_categories (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcement_files_announcement_id_file_role" ON announcement_files (announcement_id, file_role);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE UNIQUE INDEX "IX_announcement_reads_announcement_id_user_id" ON announcement_reads (announcement_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcement_reads_user_id_read_at" ON announcement_reads (user_id, read_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcements_category_id" ON announcements (category_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcements_created_by_user_id" ON announcements (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcements_is_featured_status" ON announcements (is_featured, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcements_published_by_user_id" ON announcements (published_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcements_show_as_banner_status" ON announcements (show_as_banner, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcements_show_as_popup_status" ON announcements (show_as_popup, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcements_status_publish_at_expires_at" ON announcements (status, publish_at, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcements_updated_by_user_id" ON announcements (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    CREATE INDEX "IX_announcement_targets_announcement_id_target_type_target_val~" ON announcement_targets (announcement_id, target_type, target_value);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260720032005_AddAnnouncementCenter') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260720032005_AddAnnouncementCenter', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721024102_UpdateEmploymentLeavePolicyMatrix') THEN
    UPDATE leave_policy_rules rule
    SET min_service_months = 12,
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type = 'GOVERNMENT_EMPLOYEE'
      AND leave_type.code = 'PERSONAL_LEAVE';

    UPDATE leave_policy_rules rule
    SET carry_over_max_days = 5,
        max_accumulated_days = 15,
        first_year_entitlement_days = NULL,
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type IN ('GOVERNMENT_EMPLOYEE', 'MOPH_EMPLOYEE')
      AND leave_type.code = 'VACATION_LEAVE';

    UPDATE leave_policy_rules rule
    SET first_year_entitlement_days = 6,
        first_year_paid_days = 6,
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type = 'MOPH_EMPLOYEE'
      AND leave_type.code = 'PERSONAL_LEAVE';

    UPDATE leave_policy_rules rule
    SET min_service_months = NULL,
        first_year_entitlement_days = 8,
        first_year_paid_days = 8,
        notes = 'ผู้ปฏิบัติงานยังไม่ครบ 6 เดือนรองรับวงเงินสิทธิ 8 วันทำการ',
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type = 'TEMPORARY_EMPLOYEE_MONTHLY'
      AND leave_type.code = 'SICK_LEAVE';

    UPDATE leave_policy_rules rule
    SET first_year_entitlement_days = NULL,
        notes = 'ไม่มีสิทธิสะสมวันลาพักผ่อน',
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type = 'TEMPORARY_EMPLOYEE_MONTHLY'
      AND leave_type.code = 'VACATION_LEAVE';

    UPDATE leave_policy_rules rule
    SET first_year_entitlement_days = NULL,
        notes = 'ไม่สะสมวันลาพักผ่อน',
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type = 'TEMPORARY_EMPLOYEE_DAILY'
      AND leave_type.code = 'VACATION_LEAVE';

    UPDATE leave_policy_rules rule
    SET entitlement_days = 90,
        is_paid = false,
        social_security_max_days = 90,
        notes = 'ลาได้ 90 วัน แต่ไม่ได้รับค่าจ้างระหว่างลา ใช้สิทธิประกันสังคมตามเงื่อนไข',
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type = 'TEMPORARY_EMPLOYEE_DAILY'
      AND leave_type.code = 'MATERNITY_LEAVE';

    WITH policy_values AS (
        SELECT *
        FROM (VALUES
            ('PERMANENT_EMPLOYEE', 'SICK_LEAVE', 60.0, 60.0, false, NULL::numeric, NULL::numeric, NULL::integer, NULL::integer, false, NULL::numeric, NULL::numeric, true, 120.0, NULL::numeric, 'กรณีเกิน 60 วัน ผู้อำนวยการอาจพิจารณาได้รวมไม่เกิน 120 วัน'),
            ('PERMANENT_EMPLOYEE', 'PERSONAL_LEAVE', 45.0, 45.0, false, NULL::numeric, NULL::numeric, NULL::integer, NULL::integer, false, 15.0, NULL::numeric, true, NULL::numeric, NULL::numeric, NULL::text),
            ('PERMANENT_EMPLOYEE', 'VACATION_LEAVE', 10.0, 10.0, true, 30.0, 30.0, 6, NULL::integer, false, NULL::numeric, NULL::numeric, true, NULL::numeric, NULL::numeric, NULL::text),
            ('PERMANENT_EMPLOYEE', 'MATERNITY_LEAVE', 90.0, 90.0, false, NULL::numeric, NULL::numeric, NULL::integer, NULL::integer, false, NULL::numeric, NULL::numeric, true, NULL::numeric, NULL::numeric, NULL::text),
            ('PERMANENT_EMPLOYEE', 'ORDINATION_LEAVE', 120.0, 120.0, false, NULL::numeric, NULL::numeric, 12, NULL::integer, false, NULL::numeric, NULL::numeric, true, NULL::numeric, NULL::numeric, 'ใช้ตามระเบียบราชการและเงื่อนไขหน่วยงาน')
        ) AS data(
            employment_type,
            leave_type_code,
            entitlement_days,
            max_paid_days,
            allow_carry_over,
            carry_over_max_days,
            max_accumulated_days,
            min_service_months,
            min_service_years,
            prorate_if_service_less_than_year,
            first_year_entitlement_days,
            first_year_paid_days,
            is_paid,
            max_extended_days,
            social_security_max_days,
            notes
        )
    )
    INSERT INTO leave_policy_rules (
        id,
        employment_type,
        leave_type_id,
        fiscal_year,
        entitlement_days,
        max_paid_days,
        allow_carry_over,
        carry_over_max_days,
        max_accumulated_days,
        min_service_months,
        min_service_years,
        prorate_if_service_less_than_year,
        first_year_entitlement_days,
        first_year_paid_days,
        is_paid,
        max_extended_days,
        social_security_max_days,
        notes,
        is_active,
        created_at,
        updated_at
    )
    SELECT
        (
            substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 1, 8) || '-' ||
            substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 9, 4) || '-' ||
            substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 13, 4) || '-' ||
            substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 17, 4) || '-' ||
            substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 21, 12)
        )::uuid,
        policy_values.employment_type,
        leave_types.id,
        NULL,
        policy_values.entitlement_days,
        policy_values.max_paid_days,
        policy_values.allow_carry_over,
        policy_values.carry_over_max_days,
        policy_values.max_accumulated_days,
        policy_values.min_service_months,
        policy_values.min_service_years,
        policy_values.prorate_if_service_less_than_year,
        policy_values.first_year_entitlement_days,
        policy_values.first_year_paid_days,
        policy_values.is_paid,
        policy_values.max_extended_days,
        policy_values.social_security_max_days,
        policy_values.notes,
        true,
        now(),
        now()
    FROM policy_values
    JOIN leave_types ON leave_types.code = policy_values.leave_type_code
    WHERE NOT EXISTS (
        SELECT 1
        FROM leave_policy_rules existing
        WHERE existing.employment_type = policy_values.employment_type
          AND existing.leave_type_id = leave_types.id
          AND existing.fiscal_year IS NULL
          AND existing.is_active
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721024102_UpdateEmploymentLeavePolicyMatrix') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260721024102_UpdateEmploymentLeavePolicyMatrix', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD allow_request boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD annual_entitlement_days numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD carry_forward_limit_days numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD day_counting_type character varying(40) NOT NULL DEFAULT 'BusinessDays';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD employer_paid_limit_days numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD maximum_leave_days numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD maximum_total_available_days numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD payment_rule_type character varying(80) NOT NULL DEFAULT 'EmployerPaid';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD probation_entitlement_days numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD requires_special_approval_after_days numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    ALTER TABLE leave_policy_rules ADD uses_social_security boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    UPDATE leave_policy_rules
    SET annual_entitlement_days = entitlement_days,
        employer_paid_limit_days = max_paid_days,
        carry_forward_limit_days = carry_over_max_days,
        maximum_total_available_days = max_accumulated_days,
        maximum_leave_days = COALESCE(max_extended_days, entitlement_days),
        allow_request = true,
        payment_rule_type = CASE WHEN is_paid THEN 'EmployerPaid' ELSE 'Unpaid' END,
        day_counting_type = 'BusinessDays',
        updated_at = now();

    UPDATE leave_policy_rules rule
    SET requires_special_approval_after_days = 60,
        maximum_leave_days = 120,
        payment_rule_type = 'EmployerPaidThenSpecialApproval',
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type IN ('CIVIL_SERVANT', 'PERMANENT_EMPLOYEE')
      AND leave_type.code = 'SICK_LEAVE';

    UPDATE leave_policy_rules rule
    SET carry_forward_limit_days = 5,
        maximum_total_available_days = 15,
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type IN ('GOVERNMENT_EMPLOYEE', 'MOPH_EMPLOYEE')
      AND leave_type.code = 'VACATION_LEAVE';

    UPDATE leave_policy_rules rule
    SET uses_social_security = true,
        social_security_max_days = COALESCE(social_security_max_days, 90),
        payment_rule_type = 'EmployerPaidThenSocialSecurity',
        notes = COALESCE(notes, 'ส่วนที่เกินสิทธิ์ได้รับค่าจ้างจากหน่วยงานให้ตรวจสิทธิประกันสังคมตามเงื่อนไข'),
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type IN ('GOVERNMENT_EMPLOYEE', 'MOPH_EMPLOYEE')
      AND leave_type.code = 'SICK_LEAVE';

    UPDATE leave_policy_rules rule
    SET uses_social_security = true,
        social_security_max_days = 45,
        payment_rule_type = 'EmployerPaidThenSocialSecurity',
        day_counting_type = 'CalendarDays',
        notes = COALESCE(notes, 'ได้รับค่าจ้างจากหน่วยงานไม่เกิน 45 วัน ส่วนที่เหลือใช้สิทธิประกันสังคมตามเงื่อนไข'),
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type IN ('GOVERNMENT_EMPLOYEE', 'MOPH_EMPLOYEE', 'TEMPORARY_EMPLOYEE_MONTHLY')
      AND leave_type.code = 'MATERNITY_LEAVE';

    UPDATE leave_policy_rules rule
    SET uses_social_security = true,
        social_security_max_days = 90,
        payment_rule_type = 'UnpaidThenSocialSecurity',
        day_counting_type = CASE WHEN leave_type.code = 'MATERNITY_LEAVE' THEN 'CalendarDays' ELSE day_counting_type END,
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type = 'TEMPORARY_EMPLOYEE_DAILY'
      AND leave_type.code IN ('SICK_LEAVE', 'MATERNITY_LEAVE');

    UPDATE leave_policy_rules rule
    SET probation_entitlement_days = 8,
        updated_at = now()
    FROM leave_types leave_type
    WHERE rule.leave_type_id = leave_type.id
      AND rule.employment_type IN ('TEMPORARY_EMPLOYEE_MONTHLY', 'TEMPORARY_EMPLOYEE_DAILY')
      AND leave_type.code = 'SICK_LEAVE';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721030258_AddDetailedLeavePolicyMatrix') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260721030258_AddDetailedLeavePolicyMatrix', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    ALTER TABLE announcement_files ADD created_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    CREATE TABLE announcement_images (
        id uuid NOT NULL,
        announcement_id uuid NOT NULL,
        original_file_name character varying(260) NOT NULL,
        stored_file_name character varying(260) NOT NULL,
        relative_path character varying(1000) NOT NULL,
        large_path character varying(1000),
        medium_path character varying(1000),
        thumbnail_path character varying(1000),
        mime_type character varying(120) NOT NULL,
        file_size bigint NOT NULL,
        width integer,
        height integer,
        display_order integer NOT NULL,
        is_cover boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_announcement_images" PRIMARY KEY (id),
        CONSTRAINT "FK_announcement_images_announcements_announcement_id" FOREIGN KEY (announcement_id) REFERENCES announcements (id) ON DELETE CASCADE,
        CONSTRAINT "FK_announcement_images_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id),
        CONSTRAINT "FK_announcement_images_users_updated_by_user_id" FOREIGN KEY (updated_by_user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    CREATE INDEX "IX_announcement_files_created_by_user_id" ON announcement_files (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    CREATE UNIQUE INDEX "IX_announcement_images_announcement_id" ON announcement_images (announcement_id) WHERE is_cover = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    CREATE INDEX "IX_announcement_images_announcement_id_display_order" ON announcement_images (announcement_id, display_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    CREATE INDEX "IX_announcement_images_announcement_id_is_cover" ON announcement_images (announcement_id, is_cover);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    CREATE INDEX "IX_announcement_images_created_by_user_id" ON announcement_images (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    CREATE INDEX "IX_announcement_images_updated_by_user_id" ON announcement_images (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    ALTER TABLE announcement_files ADD CONSTRAINT "FK_announcement_files_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260721144138_AddAnnouncementMediaSupport') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260721144138_AddAnnouncementMediaSupport', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    ALTER TABLE announcements ADD line_notification_queued_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    ALTER TABLE announcements ADD notification_config_version integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    ALTER TABLE announcements ADD notification_dispatch_error character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    ALTER TABLE announcements ADD notification_dispatch_status character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    ALTER TABLE announcements ADD notification_sent_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    ALTER TABLE announcements ADD notify_in_app boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    ALTER TABLE announcements ADD notify_via_line boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    CREATE TABLE announcement_notification_deliveries (
        id uuid NOT NULL,
        announcement_id uuid NOT NULL,
        user_id uuid NOT NULL,
        channel character varying(40) NOT NULL,
        status character varying(40) NOT NULL,
        idempotency_key character varying(240) NOT NULL,
        queued_at timestamp with time zone NOT NULL,
        sent_at timestamp with time zone,
        failed_at timestamp with time zone,
        retry_count integer NOT NULL,
        last_error_code character varying(120),
        last_error_message_sanitized character varying(1000),
        notification_id uuid,
        line_queue_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_announcement_notification_deliveries" PRIMARY KEY (id),
        CONSTRAINT "FK_announcement_notification_deliveries_announcements_announce~" FOREIGN KEY (announcement_id) REFERENCES announcements (id) ON DELETE CASCADE,
        CONSTRAINT "FK_announcement_notification_deliveries_line_delivery_logs_lin~" FOREIGN KEY (line_queue_id) REFERENCES line_delivery_logs (id) ON DELETE SET NULL,
        CONSTRAINT "FK_announcement_notification_deliveries_notifications_notifica~" FOREIGN KEY (notification_id) REFERENCES notifications (id) ON DELETE SET NULL,
        CONSTRAINT "FK_announcement_notification_deliveries_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    CREATE INDEX "IX_announcements_notify_in_app_notify_via_line" ON announcements (notify_in_app, notify_via_line);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    CREATE INDEX "IX_announcement_notification_deliveries_announcement_id_channe~" ON announcement_notification_deliveries (announcement_id, channel, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    CREATE UNIQUE INDEX "IX_announcement_notification_deliveries_idempotency_key" ON announcement_notification_deliveries (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    CREATE INDEX "IX_announcement_notification_deliveries_line_queue_id" ON announcement_notification_deliveries (line_queue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    CREATE INDEX "IX_announcement_notification_deliveries_notification_id" ON announcement_notification_deliveries (notification_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    CREATE INDEX "IX_announcement_notification_deliveries_user_id_channel" ON announcement_notification_deliveries (user_id, channel);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
    VALUES
        (gen_random_uuid(), 'Announcement.Notification.Configure', 'กำหนดช่องทางแจ้งเตือนประกาศ', 'AnnouncementNotification', 'Configure', true, NOW()),
        (gen_random_uuid(), 'Announcement.Notification.Preview', 'ดูตัวอย่างผู้รับแจ้งเตือนประกาศ', 'AnnouncementNotification', 'Preview', true, NOW()),
        (gen_random_uuid(), 'Announcement.Notification.SendInApp', 'ส่ง Notification Bell สำหรับประกาศ', 'AnnouncementNotification', 'SendInApp', true, NOW()),
        (gen_random_uuid(), 'Announcement.Notification.SendLine', 'ส่ง LINE สำหรับประกาศ', 'AnnouncementNotification', 'SendLine', true, NOW()),
        (gen_random_uuid(), 'Announcement.Notification.ViewDelivery', 'ดูสถานะการส่งแจ้งเตือนประกาศ', 'AnnouncementNotification', 'ViewDelivery', true, NOW()),
        (gen_random_uuid(), 'Announcement.Notification.RetryFailed', 'ส่งแจ้งเตือนประกาศที่ล้มเหลวซ้ำ', 'AnnouncementNotification', 'RetryFailed', true, NOW())
    ON CONFLICT (code) DO UPDATE
    SET name = EXCLUDED.name,
        group_name = EXCLUDED.group_name,
        action = EXCLUDED.action,
        is_active = true;

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT roles.id, permissions.id
    FROM roles
    CROSS JOIN permissions
    WHERE roles.name IN ('Admin', 'SuperAdmin', 'LeaveAdmin')
      AND permissions.code IN (
        'Announcement.Notification.Configure',
        'Announcement.Notification.Preview',
        'Announcement.Notification.SendInApp',
        'Announcement.Notification.SendLine',
        'Announcement.Notification.ViewDelivery',
        'Announcement.Notification.RetryFailed'
      )
    ON CONFLICT DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260722030007_AddAnnouncementNotificationChannels') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260722030007_AddAnnouncementNotificationChannels', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260723145513_AddRefreshTokenHashSecurity') THEN
    ALTER TABLE refresh_tokens ADD replaced_by_token_hash character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260723145513_AddRefreshTokenHashSecurity') THEN
    ALTER TABLE refresh_tokens ADD token_hash character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260723145513_AddRefreshTokenHashSecurity') THEN
    CREATE UNIQUE INDEX "IX_refresh_tokens_token_hash" ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260723145513_AddRefreshTokenHashSecurity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260723145513_AddRefreshTokenHashSecurity', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260724090000_AddLineLiffSupport') THEN
    ALTER TABLE line_user_bindings ADD last_login_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260724090000_AddLineLiffSupport') THEN
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_line_user_bindings_user_id_active_bound"
    ON line_user_bindings (user_id)
    WHERE user_id IS NOT NULL AND status = 'Bound';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260724090000_AddLineLiffSupport') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260724090000_AddLineLiffSupport', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE TABLE fleet_driver_profiles (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        license_number character varying(100) NOT NULL,
        license_type character varying(100) NOT NULL,
        license_issue_date date,
        license_expiry_date date,
        can_drive_sedan boolean NOT NULL,
        can_drive_pickup boolean NOT NULL,
        can_drive_van boolean NOT NULL,
        can_drive_ambulance boolean NOT NULL,
        can_drive_other boolean NOT NULL,
        driver_status character varying(40) NOT NULL,
        is_active boolean NOT NULL,
        note character varying(2000),
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_fleet_driver_profiles" PRIMARY KEY (id),
        CONSTRAINT "FK_fleet_driver_profiles_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_driver_profiles_users_updated_by_user_id" FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_driver_profiles_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE TABLE fleet_driver_unavailability (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        start_at timestamp with time zone NOT NULL,
        end_at timestamp with time zone NOT NULL,
        reason character varying(1000) NOT NULL,
        created_by_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_fleet_driver_unavailability" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_driver_unavailability_range CHECK (end_at > start_at),
        CONSTRAINT "FK_fleet_driver_unavailability_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_driver_unavailability_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE TABLE fleet_vehicle_types (
        id uuid NOT NULL,
        code character varying(50) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(1000),
        sort_order integer NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_fleet_vehicle_types" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_vehicle_types_sort_order CHECK (sort_order >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE TABLE fleet_vehicles (
        id uuid NOT NULL,
        vehicle_code character varying(50) NOT NULL,
        registration_number character varying(50) NOT NULL,
        registration_province character varying(100),
        vehicle_type_id uuid NOT NULL,
        brand character varying(100),
        model character varying(100),
        manufacture_year integer,
        seat_capacity_total integer NOT NULL,
        passenger_capacity integer NOT NULL,
        fuel_type character varying(50),
        current_mileage numeric(12,2) NOT NULL,
        owning_department_id uuid,
        responsible_user_id uuid,
        status character varying(40) NOT NULL,
        is_active boolean NOT NULL,
        note character varying(2000),
        image_path character varying(1000),
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_fleet_vehicles" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_vehicles_capacity CHECK (seat_capacity_total > 0 AND passenger_capacity > 0 AND passenger_capacity <= seat_capacity_total),
        CONSTRAINT ck_fleet_vehicles_mileage CHECK (current_mileage >= 0),
        CONSTRAINT "FK_fleet_vehicles_departments_owning_department_id" FOREIGN KEY (owning_department_id) REFERENCES departments (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_vehicles_fleet_vehicle_types_vehicle_type_id" FOREIGN KEY (vehicle_type_id) REFERENCES fleet_vehicle_types (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_vehicles_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_vehicles_users_responsible_user_id" FOREIGN KEY (responsible_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_vehicles_users_updated_by_user_id" FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE TABLE fleet_vehicle_unavailability (
        id uuid NOT NULL,
        vehicle_id uuid NOT NULL,
        start_at timestamp with time zone NOT NULL,
        end_at timestamp with time zone NOT NULL,
        reason character varying(1000) NOT NULL,
        type character varying(50) NOT NULL,
        created_by_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_fleet_vehicle_unavailability" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_vehicle_unavailability_range CHECK (end_at > start_at),
        CONSTRAINT "FK_fleet_vehicle_unavailability_fleet_vehicles_vehicle_id" FOREIGN KEY (vehicle_id) REFERENCES fleet_vehicles (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_vehicle_unavailability_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_driver_profiles_created_by_user_id" ON fleet_driver_profiles (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_driver_profiles_is_active_driver_status" ON fleet_driver_profiles (is_active, driver_status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_driver_profiles_updated_by_user_id" ON fleet_driver_profiles (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE UNIQUE INDEX "IX_fleet_driver_profiles_user_id" ON fleet_driver_profiles (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_driver_unavailability_created_by_user_id" ON fleet_driver_unavailability (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_driver_unavailability_user_id_start_at_end_at" ON fleet_driver_unavailability (user_id, start_at, end_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_vehicles_created_by_user_id" ON fleet_vehicles (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_vehicles_is_active_status_vehicle_type_id" ON fleet_vehicles (is_active, status, vehicle_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_vehicles_owning_department_id" ON fleet_vehicles (owning_department_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE UNIQUE INDEX "IX_fleet_vehicles_registration_number" ON fleet_vehicles (registration_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_vehicles_responsible_user_id" ON fleet_vehicles (responsible_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_vehicles_updated_by_user_id" ON fleet_vehicles (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE UNIQUE INDEX "IX_fleet_vehicles_vehicle_code" ON fleet_vehicles (vehicle_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_vehicles_vehicle_type_id" ON fleet_vehicles (vehicle_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE UNIQUE INDEX "IX_fleet_vehicle_types_code" ON fleet_vehicle_types (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_vehicle_types_is_active_sort_order" ON fleet_vehicle_types (is_active, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_vehicle_unavailability_created_by_user_id" ON fleet_vehicle_unavailability (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    CREATE INDEX "IX_fleet_vehicle_unavailability_vehicle_id_start_at_end_at" ON fleet_vehicle_unavailability (vehicle_id, start_at, end_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801150401_AddFleetFoundation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260801150401_AddFleetFoundation', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE TABLE fleet_requests (
        id uuid NOT NULL,
        request_no character varying(30) NOT NULL,
        requester_user_id uuid NOT NULL,
        requester_department_id uuid,
        request_date date NOT NULL,
        purpose character varying(2000) NOT NULL,
        mission_type character varying(100) NOT NULL,
        requested_vehicle_type_id uuid,
        destination character varying(1000) NOT NULL,
        contact_person_name character varying(200) NOT NULL,
        contact_phone character varying(50) NOT NULL,
        departure_at timestamp with time zone NOT NULL,
        expected_return_at timestamp with time zone NOT NULL,
        passenger_count integer NOT NULL,
        special_requirement character varying(2000),
        is_urgent boolean NOT NULL,
        urgent_reason character varying(1000),
        status character varying(50) NOT NULL,
        return_target character varying(30),
        submitted_at timestamp with time zone,
        cancelled_at timestamp with time zone,
        cancellation_reason character varying(1000),
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        concurrency_token uuid NOT NULL,
        CONSTRAINT "PK_fleet_requests" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_requests_passenger_count CHECK (passenger_count > 0),
        CONSTRAINT ck_fleet_requests_time_range CHECK (expected_return_at > departure_at),
        CONSTRAINT "FK_fleet_requests_departments_requester_department_id" FOREIGN KEY (requester_department_id) REFERENCES departments (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_requests_fleet_vehicle_types_requested_vehicle_type_id" FOREIGN KEY (requested_vehicle_type_id) REFERENCES fleet_vehicle_types (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_requests_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_requests_users_requester_user_id" FOREIGN KEY (requester_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_requests_users_updated_by_user_id" FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE TABLE fleet_assignments (
        id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        vehicle_id uuid NOT NULL,
        driver_user_id uuid NOT NULL,
        assigned_by_user_id uuid NOT NULL,
        assigned_at timestamp with time zone NOT NULL,
        assignment_status character varying(30) NOT NULL,
        assignment_reason character varying(1000),
        replaced_assignment_id uuid,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_fleet_assignments" PRIMARY KEY (id),
        CONSTRAINT "FK_fleet_assignments_fleet_assignments_replaced_assignment_id" FOREIGN KEY (replaced_assignment_id) REFERENCES fleet_assignments (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_assignments_fleet_requests_fleet_request_id" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_assignments_fleet_vehicles_vehicle_id" FOREIGN KEY (vehicle_id) REFERENCES fleet_vehicles (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_assignments_users_assigned_by_user_id" FOREIGN KEY (assigned_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_assignments_users_driver_user_id" FOREIGN KEY (driver_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE TABLE fleet_cancellation_requests (
        id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        requested_by_user_id uuid NOT NULL,
        previous_status character varying(50) NOT NULL,
        reason character varying(1000) NOT NULL,
        status character varying(30) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        completed_at timestamp with time zone,
        CONSTRAINT "PK_fleet_cancellation_requests" PRIMARY KEY (id),
        CONSTRAINT "FK_fleet_cancellation_requests_fleet_requests_fleet_request_id" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_cancellation_requests_users_requested_by_user_id" FOREIGN KEY (requested_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE TABLE fleet_request_passengers (
        id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        user_id uuid,
        full_name character varying(200) NOT NULL,
        position_or_organization character varying(300),
        phone character varying(50),
        passenger_type character varying(20) NOT NULL,
        is_requester boolean NOT NULL,
        sort_order integer NOT NULL,
        CONSTRAINT "PK_fleet_request_passengers" PRIMARY KEY (id),
        CONSTRAINT "FK_fleet_request_passengers_fleet_requests_fleet_request_id" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE CASCADE,
        CONSTRAINT "FK_fleet_request_passengers_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE TABLE fleet_request_status_histories (
        id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        from_status character varying(50),
        to_status character varying(50) NOT NULL,
        action character varying(100) NOT NULL,
        return_target character varying(30),
        reason character varying(2000),
        actor_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        correlation_id character varying(100),
        CONSTRAINT "PK_fleet_request_status_histories" PRIMARY KEY (id),
        CONSTRAINT "FK_fleet_request_status_histories_fleet_requests_fleet_request~" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE CASCADE,
        CONSTRAINT "FK_fleet_request_status_histories_users_actor_user_id" FOREIGN KEY (actor_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_assignments_assigned_by_user_id" ON fleet_assignments (assigned_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_assignments_driver_user_id_is_active" ON fleet_assignments (driver_user_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE UNIQUE INDEX "IX_fleet_assignments_fleet_request_id" ON fleet_assignments (fleet_request_id) WHERE is_active = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_assignments_replaced_assignment_id" ON fleet_assignments (replaced_assignment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_assignments_vehicle_id_is_active" ON fleet_assignments (vehicle_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_cancellation_requests_fleet_request_id_created_at" ON fleet_cancellation_requests (fleet_request_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_cancellation_requests_requested_by_user_id" ON fleet_cancellation_requests (requested_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_request_passengers_fleet_request_id_sort_order" ON fleet_request_passengers (fleet_request_id, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_request_passengers_user_id" ON fleet_request_passengers (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_requests_created_by_user_id" ON fleet_requests (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_requests_requested_vehicle_type_id" ON fleet_requests (requested_vehicle_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_requests_requester_department_id" ON fleet_requests (requester_department_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_requests_requester_user_id_status" ON fleet_requests (requester_user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE UNIQUE INDEX "IX_fleet_requests_request_no" ON fleet_requests (request_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_requests_status_departure_at" ON fleet_requests (status, departure_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_requests_updated_by_user_id" ON fleet_requests (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_request_status_histories_actor_user_id" ON fleet_request_status_histories (actor_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    CREATE INDEX "IX_fleet_request_status_histories_fleet_request_id_created_at" ON fleet_request_status_histories (fleet_request_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801155138_AddFleetRequesterDispatcherWorkflow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260801155138_AddFleetRequesterDispatcherWorkflow', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE fleet_cancellation_requests ADD concurrency_token uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE fleet_cancellation_requests ADD review_reason character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE fleet_cancellation_requests ADD reviewed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE fleet_cancellation_requests ADD reviewed_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE audit_logs ADD correlation_id character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE audit_logs ADD delegation_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE audit_logs ADD delegator_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE audit_logs ADD effective_actor_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE audit_logs ADD new_value text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE audit_logs ADD old_value text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE audit_logs ADD reason text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE audit_logs ADD user_agent character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE approval_delegations ADD concurrency_token uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE approval_delegations ADD end_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE approval_delegations ADD required_permission_code character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE approval_delegations ADD scope character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE approval_delegations ADD start_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    UPDATE approval_delegations
    SET scope = 'LEAVE',
        required_permission_code = 'LeaveApproval.ApproveCurrentStep',
        start_at = start_date::timestamp AT TIME ZONE 'Asia/Bangkok',
        end_at = (end_date + 1)::timestamp AT TIME ZONE 'Asia/Bangkok',
        concurrency_token = md5(id::text || created_at::text)::uuid
    WHERE scope IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE approval_delegations ADD CONSTRAINT ck_approval_delegations_scope CHECK (scope IS NULL OR scope IN ('LEAVE', 'FLEET'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE approval_delegations ADD CONSTRAINT ck_approval_delegations_interval CHECK (start_at IS NULL OR end_at IS NULL OR end_at > start_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE TABLE domain_events (
        event_id uuid NOT NULL,
        event_type character varying(160) NOT NULL,
        scope character varying(30) NOT NULL,
        aggregate_type character varying(100) NOT NULL,
        aggregate_id uuid NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        actor_user_id uuid,
        correlation_id character varying(100) NOT NULL,
        payload jsonb NOT NULL,
        CONSTRAINT "PK_domain_events" PRIMARY KEY (event_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE TABLE fleet_trip_records (
        id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        assignment_id uuid NOT NULL,
        driver_user_id uuid NOT NULL,
        actual_start_at timestamp with time zone,
        actual_end_at timestamp with time zone,
        start_mileage numeric(12,2),
        end_mileage numeric(12,2),
        fuel_amount numeric(10,2),
        fuel_cost numeric(12,2),
        trip_notes character varying(2000),
        completion_notes character varying(2000),
        completed_by_user_id uuid,
        override_reason character varying(1000),
        concurrency_token uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_fleet_trip_records" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_trip_mileage CHECK (start_mileage IS NULL OR start_mileage >= 0 AND (end_mileage IS NULL OR end_mileage >= start_mileage)),
        CONSTRAINT ck_fleet_trip_time CHECK (actual_start_at IS NULL OR actual_end_at IS NULL OR actual_end_at >= actual_start_at),
        CONSTRAINT "FK_fleet_trip_records_fleet_assignments_assignment_id" FOREIGN KEY (assignment_id) REFERENCES fleet_assignments (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_records_fleet_requests_fleet_request_id" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_records_users_completed_by_user_id" FOREIGN KEY (completed_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_records_users_driver_user_id" FOREIGN KEY (driver_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE TABLE outbox_messages (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        event_type character varying(160) NOT NULL,
        scope character varying(30) NOT NULL,
        payload jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL,
        available_at timestamp with time zone NOT NULL,
        processed_at timestamp with time zone,
        attempt_count integer NOT NULL,
        last_error character varying(2000),
        status character varying(30) NOT NULL,
        concurrency_token uuid NOT NULL,
        CONSTRAINT "PK_outbox_messages" PRIMARY KEY (id),
        CONSTRAINT "FK_outbox_messages_domain_events_event_id" FOREIGN KEY (event_id) REFERENCES domain_events (event_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE TABLE notification_deliveries (
        id uuid NOT NULL,
        outbox_message_id uuid NOT NULL,
        recipient_user_id uuid NOT NULL,
        channel character varying(30) NOT NULL,
        status character varying(30) NOT NULL,
        idempotency_key character varying(300) NOT NULL,
        attempt_count integer NOT NULL,
        last_error character varying(2000),
        created_at timestamp with time zone NOT NULL,
        processed_at timestamp with time zone,
        CONSTRAINT "PK_notification_deliveries" PRIMARY KEY (id),
        CONSTRAINT "FK_notification_deliveries_outbox_messages_outbox_message_id" FOREIGN KEY (outbox_message_id) REFERENCES outbox_messages (id) ON DELETE CASCADE,
        CONSTRAINT "FK_notification_deliveries_users_recipient_user_id" FOREIGN KEY (recipient_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE INDEX "IX_fleet_cancellation_requests_reviewed_by_user_id" ON fleet_cancellation_requests (reviewed_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE INDEX "IX_approval_delegations_approver_user_id_scope_required_permis~" ON approval_delegations (approver_user_id, scope, required_permission_code, start_at, end_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE INDEX "IX_domain_events_scope_event_type_occurred_at" ON domain_events (scope, event_type, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE UNIQUE INDEX "IX_fleet_trip_records_assignment_id" ON fleet_trip_records (assignment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE INDEX "IX_fleet_trip_records_completed_by_user_id" ON fleet_trip_records (completed_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE INDEX "IX_fleet_trip_records_driver_user_id" ON fleet_trip_records (driver_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE UNIQUE INDEX "IX_fleet_trip_records_fleet_request_id" ON fleet_trip_records (fleet_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE UNIQUE INDEX "IX_notification_deliveries_idempotency_key" ON notification_deliveries (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE INDEX "IX_notification_deliveries_outbox_message_id" ON notification_deliveries (outbox_message_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE INDEX "IX_notification_deliveries_recipient_user_id" ON notification_deliveries (recipient_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE INDEX "IX_notification_deliveries_status_created_at" ON notification_deliveries (status, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE UNIQUE INDEX "IX_outbox_messages_event_id" ON outbox_messages (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    CREATE INDEX "IX_outbox_messages_status_available_at" ON outbox_messages (status, available_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    ALTER TABLE fleet_cancellation_requests ADD CONSTRAINT "FK_fleet_cancellation_requests_users_reviewed_by_user_id" FOREIGN KEY (reviewed_by_user_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801162044_AddFleetApprovalExecutionOutbox') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260801162044_AddFleetApprovalExecutionOutbox', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    ALTER TABLE fleet_trip_records ADD abort_reason character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    ALTER TABLE fleet_trip_records ADD aborted_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    ALTER TABLE fleet_trip_records ADD aborted_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    ALTER TABLE fleet_trip_records ADD is_aborted boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    ALTER TABLE fleet_assignments ADD concurrency_token uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    UPDATE fleet_assignments
    SET concurrency_token = md5(id::text || created_at::text)::uuid
    WHERE concurrency_token = '00000000-0000-0000-0000-000000000000';

    CREATE OR REPLACE FUNCTION protect_fleet_assignment_immutable_fields()
    RETURNS trigger LANGUAGE plpgsql AS $$
    BEGIN
        IF NEW.fleet_request_id IS DISTINCT FROM OLD.fleet_request_id
           OR NEW.vehicle_id IS DISTINCT FROM OLD.vehicle_id
           OR NEW.driver_user_id IS DISTINCT FROM OLD.driver_user_id
           OR NEW.assigned_by_user_id IS DISTINCT FROM OLD.assigned_by_user_id
           OR NEW.assigned_at IS DISTINCT FROM OLD.assigned_at
           OR NEW.assignment_reason IS DISTINCT FROM OLD.assignment_reason
           OR NEW.replaced_assignment_id IS DISTINCT FROM OLD.replaced_assignment_id
           OR NEW.created_at IS DISTINCT FROM OLD.created_at THEN
            RAISE EXCEPTION 'Fleet assignment business fields are immutable';
        END IF;
        RETURN NEW;
    END;
    $$;

    CREATE TRIGGER trg_fleet_assignments_immutable
    BEFORE UPDATE ON fleet_assignments
    FOR EACH ROW EXECUTE FUNCTION protect_fleet_assignment_immutable_fields();
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    ALTER TABLE fleet_trip_records ADD CONSTRAINT ck_fleet_trip_abort_details CHECK (NOT is_aborted OR (aborted_at IS NOT NULL AND aborted_by_user_id IS NOT NULL AND length(trim(abort_reason)) > 0));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    CREATE INDEX "IX_fleet_trip_records_aborted_by_user_id" ON fleet_trip_records (aborted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    ALTER TABLE fleet_trip_records ADD CONSTRAINT "FK_fleet_trip_records_users_aborted_by_user_id" FOREIGN KEY (aborted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260801165322_FleetProductionHardeningUatReadiness') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260801165322_FleetProductionHardeningUatReadiness', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802041012_AddFleetControlledRolloutAndHealth') THEN
    CREATE TABLE fleet_rollout_settings (
        id uuid NOT NULL,
        mode character varying(20) NOT NULL,
        uat_user_ids uuid[] NOT NULL,
        uat_role_codes text[] NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        concurrency_token uuid NOT NULL,
        CONSTRAINT "PK_fleet_rollout_settings" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_rollout_mode CHECK (mode IN ('Disabled', 'UATOnly', 'Enabled')),
        CONSTRAINT "FK_fleet_rollout_settings_users_updated_by_user_id" FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802041012_AddFleetControlledRolloutAndHealth') THEN
    CREATE INDEX "IX_fleet_rollout_settings_created_at" ON fleet_rollout_settings (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802041012_AddFleetControlledRolloutAndHealth') THEN
    CREATE INDEX "IX_fleet_rollout_settings_updated_by_user_id" ON fleet_rollout_settings (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802041012_AddFleetControlledRolloutAndHealth') THEN
    CREATE UNIQUE INDEX ux_fleet_rollout_settings_singleton ON fleet_rollout_settings ((true));

    INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
    SELECT gen_random_uuid(), item.code, item.name, 'FleetHealth', item.action, true, NOW()
    FROM (VALUES
        ('FleetHealth.View', 'ดู Fleet Health Center', 'View'),
        ('FleetHealth.Manage', 'จัดการ Fleet Health และ Rollout', 'Manage')
    ) AS item(code, name, action)
    WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.code = item.code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802041012_AddFleetControlledRolloutAndHealth') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260802041012_AddFleetControlledRolloutAndHealth', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE TABLE fleet_maintenance_types (
        id uuid NOT NULL,
        code character varying(50) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(1000),
        category character varying(50) NOT NULL,
        is_date_based boolean NOT NULL,
        is_mileage_based boolean NOT NULL,
        blocks_availability_when_overdue boolean NOT NULL,
        default_reminder_days integer,
        default_reminder_mileage numeric(12,2),
        is_active boolean NOT NULL,
        sort_order integer NOT NULL,
        concurrency_token uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_fleet_maintenance_types" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_maintenance_types_basis CHECK (is_date_based OR is_mileage_based),
        CONSTRAINT ck_fleet_maintenance_types_reminders CHECK (default_reminder_days IS NULL OR default_reminder_days >= 0 AND (default_reminder_mileage IS NULL OR default_reminder_mileage >= 0))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE TABLE fleet_vehicle_documents (
        id uuid NOT NULL,
        vehicle_id uuid NOT NULL,
        document_type character varying(50) NOT NULL,
        document_number character varying(200),
        issued_at timestamp with time zone,
        expires_at timestamp with time zone,
        provider character varying(300),
        notes character varying(2000),
        is_required boolean NOT NULL,
        is_active boolean NOT NULL,
        concurrency_token uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_fleet_vehicle_documents" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_vehicle_document_dates CHECK (issued_at IS NULL OR expires_at IS NULL OR expires_at >= issued_at),
        CONSTRAINT "FK_fleet_vehicle_documents_fleet_vehicles_vehicle_id" FOREIGN KEY (vehicle_id) REFERENCES fleet_vehicles (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE TABLE fleet_vehicle_maintenance_schedules (
        id uuid NOT NULL,
        vehicle_id uuid NOT NULL,
        maintenance_type_id uuid NOT NULL,
        due_date timestamp with time zone,
        due_mileage numeric(12,2),
        reminder_days integer,
        reminder_mileage numeric(12,2),
        recurrence_days integer,
        recurrence_mileage numeric(12,2),
        status character varying(30) NOT NULL,
        is_active boolean NOT NULL,
        notes character varying(2000),
        last_completed_record_id uuid,
        concurrency_token uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_fleet_vehicle_maintenance_schedules" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_maintenance_schedule_values CHECK ((due_mileage IS NULL OR due_mileage >= 0) AND (reminder_days IS NULL OR reminder_days >= 0) AND (reminder_mileage IS NULL OR reminder_mileage >= 0) AND (recurrence_days IS NULL OR recurrence_days > 0) AND (recurrence_mileage IS NULL OR recurrence_mileage > 0)),
        CONSTRAINT "FK_fleet_vehicle_maintenance_schedules_fleet_maintenance_types~" FOREIGN KEY (maintenance_type_id) REFERENCES fleet_maintenance_types (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_vehicle_maintenance_schedules_fleet_vehicles_vehicle_~" FOREIGN KEY (vehicle_id) REFERENCES fleet_vehicles (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE TABLE fleet_vehicle_maintenance_records (
        id uuid NOT NULL,
        vehicle_id uuid NOT NULL,
        maintenance_schedule_id uuid NOT NULL,
        maintenance_type_id uuid NOT NULL,
        started_at timestamp with time zone NOT NULL,
        completed_at timestamp with time zone,
        completed_mileage numeric(12,2),
        cost numeric(14,2),
        vendor character varying(300),
        invoice_number character varying(100),
        description character varying(2000),
        result character varying(2000),
        performed_by character varying(300),
        next_due_date timestamp with time zone,
        next_due_mileage numeric(12,2),
        is_cancelled boolean NOT NULL,
        cancellation_reason character varying(1000),
        concurrency_token uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_fleet_vehicle_maintenance_records" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_maintenance_record_cost CHECK (cost IS NULL OR cost >= 0),
        CONSTRAINT ck_fleet_maintenance_record_dates CHECK (completed_at IS NULL OR completed_at >= started_at),
        CONSTRAINT ck_fleet_maintenance_record_mileage CHECK (completed_mileage IS NULL OR completed_mileage >= 0),
        CONSTRAINT "FK_fleet_vehicle_maintenance_records_fleet_vehicle_maintenance~" FOREIGN KEY (maintenance_schedule_id) REFERENCES fleet_vehicle_maintenance_schedules (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE TABLE fleet_maintenance_attachments (
        id uuid NOT NULL,
        maintenance_record_id uuid NOT NULL,
        original_file_name character varying(260) NOT NULL,
        stored_file_name character varying(100) NOT NULL,
        content_type character varying(100) NOT NULL,
        file_path character varying(1000) NOT NULL,
        file_size bigint NOT NULL,
        is_deleted boolean NOT NULL,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_fleet_maintenance_attachments" PRIMARY KEY (id),
        CONSTRAINT "FK_fleet_maintenance_attachments_fleet_vehicle_maintenance_rec~" FOREIGN KEY (maintenance_record_id) REFERENCES fleet_vehicle_maintenance_records (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_maintenance_attachments_maintenance_record_id_is_dele~" ON fleet_maintenance_attachments (maintenance_record_id, is_deleted);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE UNIQUE INDEX "IX_fleet_maintenance_types_code" ON fleet_maintenance_types (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_maintenance_types_is_active_sort_order" ON fleet_maintenance_types (is_active, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_vehicle_documents_expires_at" ON fleet_vehicle_documents (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_vehicle_documents_vehicle_id_is_active_document_type" ON fleet_vehicle_documents (vehicle_id, is_active, document_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_vehicle_maintenance_records_maintenance_schedule_id" ON fleet_vehicle_maintenance_records (maintenance_schedule_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_vehicle_maintenance_records_vehicle_id_started_at" ON fleet_vehicle_maintenance_records (vehicle_id, started_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_vehicle_maintenance_schedules_due_date" ON fleet_vehicle_maintenance_schedules (due_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_vehicle_maintenance_schedules_due_mileage" ON fleet_vehicle_maintenance_schedules (due_mileage);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_vehicle_maintenance_schedules_maintenance_type_id" ON fleet_vehicle_maintenance_schedules (maintenance_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    CREATE INDEX "IX_fleet_vehicle_maintenance_schedules_vehicle_id_is_active_st~" ON fleet_vehicle_maintenance_schedules (vehicle_id, is_active, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802043455_AddFleetKpiMaintenanceCalendar') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260802043455_AddFleetKpiMaintenanceCalendar', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802050545_AddFleetMaintenanceStartMileage') THEN
    ALTER TABLE fleet_vehicle_maintenance_records ADD start_mileage numeric(12,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802050545_AddFleetMaintenanceStartMileage') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260802050545_AddFleetMaintenanceStartMileage', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802051021_AddFleetMaintenanceStartMileageConstraint') THEN
    ALTER TABLE fleet_vehicle_maintenance_records ADD CONSTRAINT ck_fleet_maintenance_record_start_mileage CHECK (start_mileage IS NULL OR start_mileage >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802051021_AddFleetMaintenanceStartMileageConstraint') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260802051021_AddFleetMaintenanceStartMileageConstraint', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_trip_records ADD completion_idempotency_key character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_trip_records ADD start_idempotency_key character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD emergency_declared_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD emergency_declared_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD emergency_policy_code character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD emergency_reason character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD incident_location character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD priority character varying(20) NOT NULL DEFAULT 'NORMAL';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD reported_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD reported_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD requested_departure_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD requires_post_review boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE TABLE fleet_capabilities (
        id uuid NOT NULL,
        code character varying(80) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(1000),
        category character varying(80) NOT NULL,
        data_type character varying(20) NOT NULL,
        unit character varying(50),
        is_required_safety_capability boolean NOT NULL,
        is_active boolean NOT NULL,
        sort_order integer NOT NULL,
        concurrency_token uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_fleet_capabilities" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_capability_type CHECK (data_type IN ('BOOLEAN','NUMBER','TEXT','ENUM'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    INSERT INTO fleet_capabilities
        (id, code, name, description, category, data_type, unit, is_required_safety_capability, is_active, sort_order, concurrency_token, created_at, created_by_user_id)
    SELECT gen_random_uuid(), seed.code, seed.name, seed.description, seed.category, seed.data_type, seed.unit, seed.is_safety, true, seed.sort_order, gen_random_uuid(), NOW(), NULL
    FROM (VALUES
        ('PassengerTransport','Passenger transport','รองรับการขนส่งผู้โดยสาร','TRANSPORT','BOOLEAN',NULL,false,10),
        ('PassengerCapacity','Passenger capacity','จำนวนผู้โดยสารที่รองรับ','CAPACITY','NUMBER','person',true,20),
        ('Wheelchair','Wheelchair','รองรับรถเข็น','ACCESSIBILITY','BOOLEAN',NULL,true,30),
        ('PatientTransport','Patient transport','รองรับการขนส่งผู้ป่วยภายใต้ Fleet policy','SAFETY','BOOLEAN',NULL,true,40),
        ('MedicalEquipment','Medical equipment','รองรับอุปกรณ์การแพทย์','SAFETY','BOOLEAN',NULL,true,50),
        ('Cargo','Cargo','รองรับสัมภาระ','CARGO','BOOLEAN',NULL,false,60),
        ('HeavyCargo','Heavy cargo','รองรับสัมภาระหนัก','CARGO','BOOLEAN',NULL,true,70),
        ('MaximumCargoWeight','Maximum cargo weight','น้ำหนักสัมภาระสูงสุด','CARGO','NUMBER','kg',true,80),
        ('VIP','VIP','รองรับภารกิจ VIP','SERVICE','BOOLEAN',NULL,false,90),
        ('LongDistance','Long distance','รองรับเส้นทางไกล','ROUTE','BOOLEAN',NULL,false,100),
        ('MountainRoute','Mountain route','รองรับเส้นทางภูเขา','ROUTE','BOOLEAN',NULL,true,110),
        ('RouteType','Route type','ประเภทเส้นทางที่รองรับ','ROUTE','ENUM',NULL,false,120),
        ('Overnight','Overnight','รองรับภารกิจค้างคืน','ROUTE','BOOLEAN',NULL,false,130),
        ('AirConditioning','Air conditioning','มีเครื่องปรับอากาศ','COMFORT','BOOLEAN',NULL,false,140),
        ('EmergencySupport','Emergency support','รองรับ Emergency Transport ตาม Fleet policy','SAFETY','BOOLEAN',NULL,true,150)
    ) AS seed(code,name,description,category,data_type,unit,is_safety,sort_order)
    WHERE NOT EXISTS (SELECT 1 FROM fleet_capabilities existing WHERE existing.code = seed.code);

    INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
    SELECT gen_random_uuid(), item.code, item.name, item.group_name, item.action, true, NOW()
    FROM (VALUES
        ('FleetCapability.View','ดู Capability Fleet','FleetCapability','View'),
        ('FleetCapability.Manage','จัดการ Capability Fleet','FleetCapability','Manage'),
        ('FleetVehicleCapability.Manage','กำหนด Capability ให้รถ','FleetVehicleCapability','Manage'),
        ('FleetRequestCapability.ManageOwn','กำหนด Capability ในคำขอตนเอง','FleetRequestCapability','ManageOwn'),
        ('FleetCompatibility.View','ดูผล Compatibility','FleetCompatibility','View'),
        ('FleetCompatibility.Override','Override Compatibility ที่ไม่ใช่ Safety','FleetCompatibility','Override'),
        ('FleetEmergency.Create','สร้างคำขอ Fleet Emergency','FleetEmergency','Create'),
        ('FleetEmergency.ViewOwn','ดูคำขอ Fleet Emergency ของตน','FleetEmergency','ViewOwn'),
        ('FleetEmergency.ViewQueue','ดูคิว Fleet Emergency','FleetEmergency','ViewQueue'),
        ('FleetEmergency.Dispatch','จัดรถ Fleet Emergency','FleetEmergency','Dispatch'),
        ('FleetEmergency.BypassApproval','ข้าม approval ตามนโยบาย Fleet Emergency','FleetEmergency','BypassApproval'),
        ('FleetEmergency.Review','ทบทวน Fleet Emergency หลังเหตุการณ์','FleetEmergency','Review'),
        ('FleetEmergency.ViewAudit','ดู Audit Fleet Emergency','FleetEmergency','ViewAudit'),
        ('FleetDriver.ViewJobs','ดูรายการงานขับรถบนมือถือ','FleetDriver','ViewJobs'),
        ('FleetDriver.AcceptJob','ตอบรับงานขับรถ','FleetDriver','AcceptJob'),
        ('FleetDriver.DeclineJob','ปฏิเสธงานขับรถ','FleetDriver','DeclineJob'),
        ('FleetTrip.Start','เริ่ม Trip','FleetTrip','Start'),
        ('FleetTrip.Complete','จบ Trip','FleetTrip','Complete'),
        ('FleetTrip.UploadAttachment','อัปโหลดไฟล์ Trip','FleetTrip','UploadAttachment')
    ) AS item(code,name,group_name,action)
    WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.code=item.code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE TABLE fleet_compatibility_overrides (
        id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        vehicle_id uuid NOT NULL,
        assignment_id uuid,
        mismatch_capability_ids jsonb NOT NULL,
        reason character varying(2000) NOT NULL,
        approved_by_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        concurrency_token uuid NOT NULL,
        CONSTRAINT "PK_fleet_compatibility_overrides" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_compatibility_override_mismatches CHECK (jsonb_typeof(mismatch_capability_ids) = 'array' AND jsonb_array_length(mismatch_capability_ids) > 0),
        CONSTRAINT "FK_fleet_compatibility_overrides_fleet_assignments_assignment_~" FOREIGN KEY (assignment_id) REFERENCES fleet_assignments (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_compatibility_overrides_fleet_requests_fleet_request_~" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_compatibility_overrides_fleet_vehicles_vehicle_id" FOREIGN KEY (vehicle_id) REFERENCES fleet_vehicles (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_compatibility_overrides_users_approved_by_user_id" FOREIGN KEY (approved_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE TABLE fleet_emergency_post_reviews (
        id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        reviewed_by_user_id uuid NOT NULL,
        reviewed_at timestamp with time zone NOT NULL,
        outcome character varying(40) NOT NULL,
        was_bypass_appropriate boolean NOT NULL,
        response_time_assessment character varying(2000),
        safety_issues character varying(4000),
        follow_up_actions character varying(4000),
        notes character varying(4000),
        concurrency_token uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_fleet_emergency_post_reviews" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_emergency_review_outcome CHECK (outcome IN ('ACCEPTABLE','NEEDS_IMPROVEMENT','POLICY_VIOLATION')),
        CONSTRAINT "FK_fleet_emergency_post_reviews_fleet_requests_fleet_request_id" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_emergency_post_reviews_users_reviewed_by_user_id" FOREIGN KEY (reviewed_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE TABLE fleet_trip_attachments (
        id uuid NOT NULL,
        trip_id uuid NOT NULL,
        original_file_name character varying(260) NOT NULL,
        stored_file_name character varying(100) NOT NULL,
        content_type character varying(100) NOT NULL,
        file_path character varying(1000) NOT NULL,
        file_size bigint NOT NULL,
        idempotency_key character varying(200) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_fleet_trip_attachments" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_trip_attachment_size CHECK (file_size > 0),
        CONSTRAINT "FK_fleet_trip_attachments_fleet_trip_records_trip_id" FOREIGN KEY (trip_id) REFERENCES fleet_trip_records (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_attachments_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE TABLE fleet_request_required_capabilities (
        id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        capability_id uuid NOT NULL,
        operator character varying(40) NOT NULL,
        required_boolean_value boolean,
        required_numeric_value numeric(14,2),
        required_text_value character varying(1000),
        required_enum_value character varying(200),
        is_mandatory boolean NOT NULL,
        notes character varying(1000),
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_fleet_request_required_capabilities" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_request_capability_operator CHECK (operator IN ('EQUALS','GREATER_THAN_OR_EQUAL','LESS_THAN_OR_EQUAL','CONTAINS','IN')),
        CONSTRAINT ck_fleet_request_capability_value CHECK (num_nonnulls(required_boolean_value,required_numeric_value,required_text_value,required_enum_value)=1),
        CONSTRAINT "FK_fleet_request_required_capabilities_fleet_capabilities_capa~" FOREIGN KEY (capability_id) REFERENCES fleet_capabilities (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_request_required_capabilities_fleet_requests_fleet_re~" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE TABLE fleet_vehicle_capabilities (
        id uuid NOT NULL,
        vehicle_id uuid NOT NULL,
        capability_id uuid NOT NULL,
        boolean_value boolean,
        numeric_value numeric(14,2),
        text_value character varying(1000),
        enum_value character varying(200),
        effective_from timestamp with time zone NOT NULL,
        effective_to timestamp with time zone,
        is_active boolean NOT NULL,
        concurrency_token uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_fleet_vehicle_capabilities" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_vehicle_capability_dates CHECK (effective_to IS NULL OR effective_to > effective_from),
        CONSTRAINT ck_fleet_vehicle_capability_value CHECK (num_nonnulls(boolean_value,numeric_value,text_value,enum_value)=1),
        CONSTRAINT "FK_fleet_vehicle_capabilities_fleet_capabilities_capability_id" FOREIGN KEY (capability_id) REFERENCES fleet_capabilities (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_vehicle_capabilities_fleet_vehicles_vehicle_id" FOREIGN KEY (vehicle_id) REFERENCES fleet_vehicles (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE UNIQUE INDEX "IX_fleet_trip_records_completion_idempotency_key" ON fleet_trip_records (completion_idempotency_key) WHERE completion_idempotency_key IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE UNIQUE INDEX "IX_fleet_trip_records_start_idempotency_key" ON fleet_trip_records (start_idempotency_key) WHERE start_idempotency_key IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_requests_priority_status_submitted_at" ON fleet_requests (priority, status, submitted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD CONSTRAINT ck_fleet_requests_emergency_reason CHECK (priority <> 'EMERGENCY' OR emergency_reason IS NOT NULL AND reported_by_user_id IS NOT NULL AND reported_at IS NOT NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    ALTER TABLE fleet_requests ADD CONSTRAINT ck_fleet_requests_priority CHECK (priority IN ('NORMAL','URGENT','EMERGENCY'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE UNIQUE INDEX "IX_fleet_capabilities_code" ON fleet_capabilities (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_capabilities_is_active_sort_order" ON fleet_capabilities (is_active, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_compatibility_overrides_approved_by_user_id" ON fleet_compatibility_overrides (approved_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_compatibility_overrides_assignment_id" ON fleet_compatibility_overrides (assignment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_compatibility_overrides_fleet_request_id_vehicle_id_c~" ON fleet_compatibility_overrides (fleet_request_id, vehicle_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_compatibility_overrides_vehicle_id" ON fleet_compatibility_overrides (vehicle_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE UNIQUE INDEX "IX_fleet_emergency_post_reviews_fleet_request_id" ON fleet_emergency_post_reviews (fleet_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_emergency_post_reviews_reviewed_by_user_id" ON fleet_emergency_post_reviews (reviewed_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_request_required_capabilities_capability_id" ON fleet_request_required_capabilities (capability_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE UNIQUE INDEX "IX_fleet_request_required_capabilities_fleet_request_id_capabi~" ON fleet_request_required_capabilities (fleet_request_id, capability_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_trip_attachments_created_by_user_id" ON fleet_trip_attachments (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE UNIQUE INDEX "IX_fleet_trip_attachments_idempotency_key" ON fleet_trip_attachments (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_trip_attachments_trip_id" ON fleet_trip_attachments (trip_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE INDEX "IX_fleet_vehicle_capabilities_capability_id" ON fleet_vehicle_capabilities (capability_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    CREATE UNIQUE INDEX "IX_fleet_vehicle_capabilities_vehicle_id_capability_id" ON fleet_vehicle_capabilities (vehicle_id, capability_id) WHERE is_active = true AND effective_to IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802082337_AddFleetCompatibilityEmergencyMobile') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260802082337_AddFleetCompatibilityEmergencyMobile', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_trip_attachments ADD deleted_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_trip_attachments ADD deleted_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_trip_attachments ADD is_deleted boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_requests ADD approval_bypass_allowed_snapshot boolean;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_requests ADD dispatch_target_minutes_snapshot integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_requests ADD driver_ack_target_minutes_snapshot integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_requests ADD emergency_policy_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_requests ADD post_review_required_snapshot boolean;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_requests ADD response_target_minutes_snapshot integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_capabilities ADD enum_options_json jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_capabilities ADD maximum_numeric_value numeric(14,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_capabilities ADD minimum_numeric_value numeric(14,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    CREATE TABLE fleet_emergency_policies (
        id uuid NOT NULL,
        code character varying(80) NOT NULL,
        name character varying(200) NOT NULL,
        priority character varying(20) NOT NULL,
        response_target_minutes integer NOT NULL,
        dispatch_target_minutes integer NOT NULL,
        driver_ack_target_minutes integer NOT NULL,
        approval_bypass_allowed boolean NOT NULL,
        post_review_required boolean NOT NULL,
        is_active boolean NOT NULL,
        effective_from timestamp with time zone NOT NULL,
        effective_to timestamp with time zone,
        concurrency_token uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_fleet_emergency_policies" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_emergency_policy_dates CHECK (effective_to IS NULL OR effective_to > effective_from),
        CONSTRAINT ck_fleet_emergency_policy_priority CHECK (priority IN ('URGENT','EMERGENCY')),
        CONSTRAINT ck_fleet_emergency_policy_targets CHECK (response_target_minutes > 0 AND dispatch_target_minutes > 0 AND driver_ack_target_minutes > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    CREATE INDEX "IX_fleet_requests_emergency_policy_id" ON fleet_requests (emergency_policy_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    CREATE UNIQUE INDEX "IX_fleet_emergency_policies_code" ON fleet_emergency_policies (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    CREATE INDEX "IX_fleet_emergency_policies_priority_is_active_effective_from" ON fleet_emergency_policies (priority, is_active, effective_from);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    ALTER TABLE fleet_requests ADD CONSTRAINT "FK_fleet_requests_fleet_emergency_policies_emergency_policy_id" FOREIGN KEY (emergency_policy_id) REFERENCES fleet_emergency_policies (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    INSERT INTO fleet_emergency_policies
        (id,code,name,priority,response_target_minutes,dispatch_target_minutes,driver_ack_target_minutes,approval_bypass_allowed,post_review_required,is_active,effective_from,concurrency_token,created_at,created_by_user_id)
    SELECT gen_random_uuid(),'INTERNAL_EMERGENCY_DEFAULT','Internal emergency transport default','EMERGENCY',15,15,15,true,true,true,TIMESTAMPTZ '2026-01-01 00:00:00+00',gen_random_uuid(),CURRENT_TIMESTAMP,NULL
    WHERE NOT EXISTS (SELECT 1 FROM fleet_emergency_policies WHERE code='INTERNAL_EMERGENCY_DEFAULT');

    UPDATE fleet_requests SET
        emergency_policy_code=COALESCE(NULLIF(emergency_policy_code,''),'CONFIG_FALLBACK'),
        response_target_minutes_snapshot=COALESCE(response_target_minutes_snapshot,15),
        dispatch_target_minutes_snapshot=COALESCE(dispatch_target_minutes_snapshot,15),
        driver_ack_target_minutes_snapshot=COALESCE(driver_ack_target_minutes_snapshot,15),
        approval_bypass_allowed_snapshot=COALESCE(approval_bypass_allowed_snapshot,true),
        post_review_required_snapshot=COALESCE(post_review_required_snapshot,requires_post_review)
    WHERE priority='EMERGENCY';

    INSERT INTO permissions (id,code,name,group_name,action,is_active,created_at)
    SELECT gen_random_uuid(),v.code,v.name,'FleetEmergencyPolicy',v.action,true,CURRENT_TIMESTAMP
    FROM (VALUES
        ('FleetEmergencyPolicy.View','ดูนโยบาย Fleet Emergency','View'),
        ('FleetEmergencyPolicy.Manage','จัดการนโยบาย Fleet Emergency','Manage')
    ) v(code,name,action)
    WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.code=v.code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260802090122_CompleteFleetM42Uat') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260802090122_CompleteFleetM42Uat', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805022643_AddFleetLineGroupRegistration') THEN
    CREATE TABLE line_group_destinations (
        id uuid NOT NULL,
        line_group_id character varying(100) NOT NULL,
        display_name character varying(300) NOT NULL,
        status character varying(30) NOT NULL,
        module character varying(30) NOT NULL,
        attention_required boolean NOT NULL,
        attention_reason character varying(500),
        first_detected_at timestamp with time zone NOT NULL,
        last_detected_at timestamp with time zone NOT NULL,
        confirmed_by_user_id uuid,
        confirmed_at timestamp with time zone,
        disabled_by_user_id uuid,
        disabled_at timestamp with time zone,
        concurrency_token uuid NOT NULL,
        CONSTRAINT "PK_line_group_destinations" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805022643_AddFleetLineGroupRegistration') THEN
    CREATE TABLE line_webhook_inbox (
        id uuid NOT NULL,
        webhook_event_id character varying(160) NOT NULL,
        event_type character varying(40) NOT NULL,
        source_type character varying(30) NOT NULL,
        source_group_id character varying(100),
        payload jsonb NOT NULL,
        status character varying(30) NOT NULL,
        attempt_count integer NOT NULL,
        available_at timestamp with time zone NOT NULL,
        processed_at timestamp with time zone,
        last_error character varying(1000),
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_line_webhook_inbox" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805022643_AddFleetLineGroupRegistration') THEN
    CREATE TABLE line_group_event_subscriptions (
        id uuid NOT NULL,
        destination_id uuid NOT NULL,
        event_type character varying(160) NOT NULL,
        is_enabled boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_line_group_event_subscriptions" PRIMARY KEY (id),
        CONSTRAINT "FK_line_group_event_subscriptions_line_group_destinations_dest~" FOREIGN KEY (destination_id) REFERENCES line_group_destinations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805022643_AddFleetLineGroupRegistration') THEN
    CREATE UNIQUE INDEX "IX_line_group_destinations_line_group_id" ON line_group_destinations (line_group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805022643_AddFleetLineGroupRegistration') THEN
    CREATE INDEX "IX_line_group_destinations_module_status" ON line_group_destinations (module, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805022643_AddFleetLineGroupRegistration') THEN
    CREATE UNIQUE INDEX "IX_line_group_event_subscriptions_destination_id_event_type" ON line_group_event_subscriptions (destination_id, event_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805022643_AddFleetLineGroupRegistration') THEN
    CREATE INDEX "IX_line_webhook_inbox_status_available_at" ON line_webhook_inbox (status, available_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805022643_AddFleetLineGroupRegistration') THEN
    CREATE UNIQUE INDEX "IX_line_webhook_inbox_webhook_event_id" ON line_webhook_inbox (webhook_event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805022643_AddFleetLineGroupRegistration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260805022643_AddFleetLineGroupRegistration', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805025628_AddFleetLineGroupDeliveryQueue') THEN
    CREATE TABLE line_group_delivery_logs (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        destination_id uuid NOT NULL,
        canonical_event_type character varying(160) NOT NULL,
        source_event_type character varying(160) NOT NULL,
        request_id uuid NOT NULL,
        status character varying(30) NOT NULL,
        deduplication_key character varying(400) NOT NULL,
        correlation_id character varying(100) NOT NULL,
        message_text character varying(5000),
        attempt_count integer NOT NULL,
        available_at timestamp with time zone NOT NULL,
        sent_at timestamp with time zone,
        failed_at timestamp with time zone,
        error_code character varying(80),
        error_message character varying(1000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_line_group_delivery_logs" PRIMARY KEY (id),
        CONSTRAINT "FK_line_group_delivery_logs_line_group_destinations_destinatio~" FOREIGN KEY (destination_id) REFERENCES line_group_destinations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805025628_AddFleetLineGroupDeliveryQueue') THEN
    CREATE UNIQUE INDEX "IX_line_group_delivery_logs_deduplication_key" ON line_group_delivery_logs (deduplication_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805025628_AddFleetLineGroupDeliveryQueue') THEN
    CREATE INDEX "IX_line_group_delivery_logs_destination_id" ON line_group_delivery_logs (destination_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805025628_AddFleetLineGroupDeliveryQueue') THEN
    CREATE UNIQUE INDEX "IX_line_group_delivery_logs_event_id_destination_id_canonical_~" ON line_group_delivery_logs (event_id, destination_id, canonical_event_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805025628_AddFleetLineGroupDeliveryQueue') THEN
    CREATE INDEX "IX_line_group_delivery_logs_status_available_at" ON line_group_delivery_logs (status, available_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805025628_AddFleetLineGroupDeliveryQueue') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260805025628_AddFleetLineGroupDeliveryQueue', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045224_AddFleetRequestStatusDefinitions') THEN
    CREATE TABLE fleet_status_definitions (
        id uuid NOT NULL,
        domain character varying(40) NOT NULL,
        code character varying(60) NOT NULL,
        thai_name character varying(200) NOT NULL,
        description character varying(1000),
        sort_order integer NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_fleet_status_definitions" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045224_AddFleetRequestStatusDefinitions') THEN
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000001', 'DRAFT', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 10, 'แบบร่าง', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000002', 'PENDING_DISPATCH', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 20, 'รอจัดรถและคนขับ', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000003', 'PENDING_ADMIN_REVIEW', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 30, 'รอหัวหน้าฝ่ายบริหารตรวจสอบ', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000004', 'PENDING_DIRECTOR', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 40, 'รอผู้อำนวยการอนุมัติ', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000005', 'APPROVED', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 50, 'อนุมัติแล้ว', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000006', 'PENDING_DRIVER_ACK', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 60, 'รอคนขับรับทราบ', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000007', 'READY', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 70, 'พร้อมเดินทาง', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000008', 'IN_PROGRESS', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 80, 'กำลังปฏิบัติงาน', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000009', 'COMPLETED', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 90, 'เสร็จสิ้น', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000010', 'CANCELLATION_PENDING', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 100, 'รอพิจารณายกเลิก', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000011', 'RETURNED', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 110, 'ส่งกลับแก้ไข', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000012', 'REJECTED', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 120, 'ไม่รับคำขอ', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000013', 'CANCELLED', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 130, 'ยกเลิก', NULL);
    INSERT INTO fleet_status_definitions (id, code, created_at, description, domain, is_active, sort_order, thai_name, updated_at)
    VALUES ('019fd100-0000-7000-8000-000000000014', 'ABORTED', TIMESTAMPTZ '2026-08-11T00:00:00Z', NULL, 'REQUEST', TRUE, 140, 'ยุติการเดินทาง', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045224_AddFleetRequestStatusDefinitions') THEN
    CREATE UNIQUE INDEX "IX_fleet_status_definitions_domain_code" ON fleet_status_definitions (domain, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045224_AddFleetRequestStatusDefinitions') THEN
    CREATE INDEX "IX_fleet_status_definitions_domain_is_active_sort_order" ON fleet_status_definitions (domain, is_active, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045224_AddFleetRequestStatusDefinitions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260811045224_AddFleetRequestStatusDefinitions', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811065119_AddLineGroupCustomEndpointConfiguration') THEN
    ALTER TABLE line_group_destinations ADD client_id character varying(300);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811065119_AddLineGroupCustomEndpointConfiguration') THEN
    ALTER TABLE line_group_destinations ADD client_secret_protected character varying(4000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811065119_AddLineGroupCustomEndpointConfiguration') THEN
    ALTER TABLE line_group_destinations ADD delivery_provider character varying(40) NOT NULL DEFAULT 'LINE_MESSAGING_API';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811065119_AddLineGroupCustomEndpointConfiguration') THEN
    ALTER TABLE line_group_destinations ADD endpoint_url character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811065119_AddLineGroupCustomEndpointConfiguration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260811065119_AddLineGroupCustomEndpointConfiguration', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    CREATE TABLE fleet_trip_participants (
        id uuid NOT NULL,
        trip_id uuid NOT NULL,
        user_id uuid,
        is_requester boolean NOT NULL,
        participant_type character varying(20) NOT NULL,
        is_actual_participant boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_fleet_trip_participants" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_trip_participants_type CHECK (participant_type IN ('EMPLOYEE','EXTERNAL')),
        CONSTRAINT "FK_fleet_trip_participants_fleet_trip_records_trip_id" FOREIGN KEY (trip_id) REFERENCES fleet_trip_records (id) ON DELETE CASCADE,
        CONSTRAINT "FK_fleet_trip_participants_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_participants_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    CREATE INDEX "IX_fleet_trip_participants_created_by_user_id" ON fleet_trip_participants (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    CREATE UNIQUE INDEX "IX_fleet_trip_participants_trip_id_user_id" ON fleet_trip_participants (trip_id, user_id) WHERE user_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    CREATE INDEX "IX_fleet_trip_participants_user_id_is_actual_participant" ON fleet_trip_participants (user_id, is_actual_participant);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
    VALUES
      ('019fd9a0-0000-7000-8000-000000000001', 'FleetFeedback.Create', 'ส่ง Feedback การเดินทาง', 'FleetFeedback', 'Create', TRUE, NOW()),
      ('019fd9a0-0000-7000-8000-000000000002', 'FleetFeedback.ViewOwn', 'ดู Feedback การเดินทางของตนเอง', 'FleetFeedback', 'ViewOwn', TRUE, NOW()),
      ('019fd9a0-0000-7000-8000-000000000003', 'FleetFeedback.ViewManagement', 'ดูรายงาน Feedback สำหรับผู้บริหาร', 'FleetFeedback', 'ViewManagement', TRUE, NOW()),
      ('019fd9a0-0000-7000-8000-000000000004', 'FleetFeedback.ViewIdentity', 'ดูตัวตนผู้ให้ Feedback', 'FleetFeedback', 'ViewIdentity', TRUE, NOW()),
      ('019fd9a0-0000-7000-8000-000000000005', 'FleetFeedback.Manage', 'จัดการ Feedback การเดินทาง', 'FleetFeedback', 'Manage', TRUE, NOW())
    ON CONFLICT (code) DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818062212_AddFleetTripParticipantFeedbackM1', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE TABLE fleet_trip_feedbacks (
        id uuid NOT NULL,
        trip_id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        vehicle_assignment_id uuid NOT NULL,
        vehicle_id uuid NOT NULL,
        driver_user_id uuid NOT NULL,
        submitted_by_user_id uuid NOT NULL,
        punctuality_rating integer NOT NULL,
        safety_rating integer NOT NULL,
        service_rating integer NOT NULL,
        overall_rating integer NOT NULL,
        vehicle_condition_rating integer NOT NULL,
        vehicle_cleanliness_rating integer NOT NULL,
        has_incident boolean NOT NULL,
        incident_category character varying(40),
        comment character varying(2000),
        submitted_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        concurrency_token uuid NOT NULL,
        CONSTRAINT "PK_fleet_trip_feedbacks" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_trip_feedback_incident CHECK ((has_incident = FALSE AND incident_category IS NULL) OR (has_incident = TRUE AND incident_category IS NOT NULL AND comment IS NOT NULL)),
        CONSTRAINT ck_fleet_trip_feedback_incident_category CHECK (incident_category IS NULL OR incident_category IN ('DRIVING','PUNCTUALITY','SERVICE','VEHICLE_CONDITION','CLEANLINESS','OTHER')),
        CONSTRAINT ck_fleet_trip_feedback_ratings CHECK (punctuality_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND service_rating BETWEEN 1 AND 5 AND overall_rating BETWEEN 1 AND 5 AND vehicle_condition_rating BETWEEN 1 AND 5 AND vehicle_cleanliness_rating BETWEEN 1 AND 5),
        CONSTRAINT "FK_fleet_trip_feedbacks_fleet_assignments_vehicle_assignment_id" FOREIGN KEY (vehicle_assignment_id) REFERENCES fleet_assignments (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_fleet_requests_fleet_request_id" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_fleet_trip_records_trip_id" FOREIGN KEY (trip_id) REFERENCES fleet_trip_records (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_fleet_vehicles_vehicle_id" FOREIGN KEY (vehicle_id) REFERENCES fleet_vehicles (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_users_driver_user_id" FOREIGN KEY (driver_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_users_submitted_by_user_id" FOREIGN KEY (submitted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_users_updated_by_user_id" FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_driver_user_id_submitted_at" ON fleet_trip_feedbacks (driver_user_id, submitted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_fleet_request_id" ON fleet_trip_feedbacks (fleet_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_submitted_by_user_id_submitted_at" ON fleet_trip_feedbacks (submitted_by_user_id, submitted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE UNIQUE INDEX "IX_fleet_trip_feedbacks_trip_id_submitted_by_user_id" ON fleet_trip_feedbacks (trip_id, submitted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_updated_by_user_id" ON fleet_trip_feedbacks (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_vehicle_assignment_id" ON fleet_trip_feedbacks (vehicle_assignment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_vehicle_id_submitted_at" ON fleet_trip_feedbacks (vehicle_id, submitted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.is_active = TRUE
      AND p.is_active = TRUE
      AND p.code IN ('FleetFeedback.Create', 'FleetFeedback.ViewOwn')
    ON CONFLICT (role_id, permission_id) DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818071449_AddFleetTripFeedbackM2', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818074425_AddFleetFeedbackManagementM3') THEN
    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.is_active = TRUE
      AND r.name IN ('Admin', 'SuperAdmin', 'Director')
      AND p.code = 'FleetFeedback.ViewManagement'
      AND p.is_active = TRUE
    ON CONFLICT (role_id, permission_id) DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818074425_AddFleetFeedbackManagementM3') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818074425_AddFleetFeedbackManagementM3', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260820090000_AddDocumentationViewForFleetRoles') THEN
    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.is_active
      AND p.code = 'Documentation.View'
      AND p.is_active
      AND NOT EXISTS (
          SELECT 1 FROM role_permissions rp
          WHERE rp.role_id = r.id AND rp.permission_id = p.id
      );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260820090000_AddDocumentationViewForFleetRoles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260820090000_AddDocumentationViewForFleetRoles', '9.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260909090000_CorrectFleetRolePermissions') THEN
    DELETE FROM role_permissions rp
    USING roles r, permissions p
    WHERE rp.role_id = r.id
      AND rp.permission_id = p.id
      AND r.name = 'DepartmentHead'
      AND p.code = 'FleetRequest.ViewAll';

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.name = 'DepartmentHead'
      AND r.is_active
      AND p.code = 'FleetRequest.ViewDepartment'
      AND p.is_active
      AND NOT EXISTS (
          SELECT 1 FROM role_permissions rp
          WHERE rp.role_id = r.id AND rp.permission_id = p.id
      );

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.name IN ('Staff', 'DepartmentHead', 'Director')
      AND r.is_active
      AND p.code = 'FleetCalendar.View'
      AND p.is_active
      AND NOT EXISTS (
          SELECT 1 FROM role_permissions rp
          WHERE rp.role_id = r.id AND rp.permission_id = p.id
      );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260909090000_CorrectFleetRolePermissions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260909090000_CorrectFleetRolePermissions', '9.0.15');
    END IF;
END $EF$;
COMMIT;

DO $postcheck$
DECLARE
    missing_tables text;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20260909090000_CorrectFleetRolePermissions'
    ) THEN
        RAISE EXCEPTION 'Latest HOP migration was not recorded.';
    END IF;

    SELECT string_agg(required.name, ', ' ORDER BY required.name)
    INTO missing_tables
    FROM (VALUES
        ('users'), ('roles'), ('permissions'), ('leave_requests'),
        ('announcements'), ('fleet_requests'), ('fleet_vehicles'),
        ('fleet_driver_profiles'), ('fleet_status_definitions')
    ) required(name)
    WHERE to_regclass('public.' || required.name) IS NULL;

    IF missing_tables IS NOT NULL THEN
        RAISE EXCEPTION 'Required tables are missing: %', missing_tables;
    END IF;
END
$postcheck$;

SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 10;

