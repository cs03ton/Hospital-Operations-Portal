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
    VALUES ('20260616133013_InitialAdminFoundation', '9.0.0');
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
    VALUES ('20260616135914_Phase12SecurityGovernanceBranding', '9.0.0');
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
    VALUES ('20260616235228_LeaveWorkflowSessionsAuditRetention', '9.0.0');
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
    VALUES ('20260617010548_Phase21LeaveApprovalAdvanced', '9.0.0');
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
    VALUES ('20260618075042_LeaveOperationsReliability', '9.0.0');
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
    VALUES ('20260621143719_LeaveSupportDelegationOverride', '9.0.0');
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
    VALUES ('20260622023940_AddLeaveRequestNumber', '9.0.0');
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
    VALUES ('20260622070245_UserApprovalRule', '9.0.0');
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
    VALUES ('20260623014426_AddLeaveDurationType', '9.0.0');
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
    VALUES ('20260623015631_AddLeaveTypeRequiresBalance', '9.0.0');
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
    VALUES ('20260623083453_AddUserSelfProfileFields', '9.0.0');
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
    VALUES ('20260624032034_AddUserEmail', '9.0.0');
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
    VALUES ('20260624090000_AddLeaveHolidayNameIndex', '9.0.0');
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
    VALUES ('20260625042341_AddLeaveBalanceAdjustedDaysAndNotes', '9.0.0');
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
    VALUES ('20260625090000_AddFiscalYearLeaveBalance', '9.0.0');
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
    VALUES ('20260629090000_AddRoleBasedNotifications', '9.0.0');
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
    VALUES ('20260629093000_AddLineTestSendPermission', '9.0.0');
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
    VALUES ('20260630043133_AddUserProfileImageFields', '9.0.0');
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
    VALUES ('20260701015112_AddLineUserBinding', '9.0.0');
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
    VALUES ('20260701020552_UpdateLineBindingHistoryIndex', '9.0.0');
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
    VALUES ('20260701023110_BackfillLegacyLineUserBindings', '9.0.0');
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
    VALUES ('20260701025742_AddLineConnectTokens', '9.0.0');
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
    VALUES ('20260701042150_AddEmploymentTypeLeavePolicyRules', '9.0.0');
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
    VALUES ('20260701072241_AddGenderEligibilityValidation', '9.0.0');
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
    VALUES ('20260701091139_AddProductionReadyLeaveRollover', '9.0.0');
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
    VALUES ('20260709120000_AddDocumentationCenterPermissions', '9.0.0');
    END IF;
END $EF$;
COMMIT;
