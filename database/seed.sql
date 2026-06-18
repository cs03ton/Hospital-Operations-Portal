INSERT INTO departments (name, description, is_active)
VALUES ('Information Technology', 'Default IT department', TRUE)
ON CONFLICT (name) DO UPDATE
SET description = EXCLUDED.description,
    is_active = TRUE;

INSERT INTO roles (name, description, is_system_role, is_active)
VALUES
    ('SuperAdmin', 'Full system administration access', TRUE, TRUE),
    ('Admin', 'Operational administration access', TRUE, TRUE),
    ('Director', 'Executive approval and reporting access', TRUE, TRUE),
    ('DepartmentHead', 'Department approval access', TRUE, TRUE),
    ('Staff', 'Standard user access', TRUE, TRUE)
ON CONFLICT (name) DO UPDATE
SET description = EXCLUDED.description,
    is_system_role = TRUE,
    is_active = TRUE;

WITH permission_seed(group_name, action) AS (
    SELECT permission_group, permission_action
    FROM (
        VALUES
            ('Dashboard'),
            ('UserManagement'),
            ('DepartmentManagement'),
            ('RoleManagement'),
            ('LeaveManagement'),
            ('ApprovalChain'),
            ('ApprovalDelegation'),
            ('LeaveBalance'),
            ('LeaveHoliday'),
            ('LeaveAttachment'),
            ('ReportManagement'),
            ('SystemSettings')
    ) AS permission_groups(permission_group)
    CROSS JOIN (
        VALUES
            ('View'),
            ('Create'),
            ('Edit'),
            ('Delete'),
            ('Approve'),
            ('Export'),
            ('Manage')
    ) AS permission_actions(permission_action)
)
INSERT INTO permissions (code, name, group_name, action, is_active)
SELECT group_name || '.' || action, group_name || '.' || action, group_name, action, TRUE
FROM permission_seed
ON CONFLICT (code) DO UPDATE
SET name = EXCLUDED.name,
    group_name = EXCLUDED.group_name,
    action = EXCLUDED.action,
    is_active = TRUE;

DELETE FROM permissions
WHERE code IN ('dashboard.view', 'users.manage', 'departments.manage', 'approvals.manage', 'roles.view');

DELETE FROM permissions
WHERE group_name IN ('RepairManagement', 'BorrowManagement', 'InventoryManagement');

INSERT INTO users (
    employee_code,
    fullname,
    username,
    password_hash,
    department_id,
    is_active
)
SELECT
    'ADMIN',
    'Default Administrator',
    'admin',
    crypt('Admin@1234', gen_salt('bf')),
    departments.id,
    TRUE
FROM departments
WHERE departments.name = 'Information Technology'
ON CONFLICT (username) DO NOTHING;

INSERT INTO user_roles (user_id, role_id)
SELECT users.id, roles.id
FROM users
CROSS JOIN roles
WHERE users.username = 'admin'
  AND roles.name = 'SuperAdmin'
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT roles.id, permissions.id
FROM roles
CROSS JOIN permissions
WHERE roles.name IN ('SuperAdmin', 'Admin')
ON CONFLICT DO NOTHING;

INSERT INTO leave_types (code, name, description, default_days_per_year, requires_attachment, is_paid, is_active)
VALUES
    ('AnnualLeave', 'Annual Leave', 'Annual vacation leave', 10, FALSE, TRUE, TRUE),
    ('SickLeave', 'Sick Leave', 'Medical sick leave', 30, TRUE, TRUE, TRUE),
    ('PersonalLeave', 'Personal Leave', 'Personal business leave', 3, FALSE, TRUE, TRUE),
    ('MaternityLeave', 'Maternity Leave', 'Maternity leave', 98, TRUE, TRUE, TRUE)
ON CONFLICT (code) DO UPDATE
SET name = EXCLUDED.name,
    description = EXCLUDED.description,
    default_days_per_year = EXCLUDED.default_days_per_year,
    requires_attachment = EXCLUDED.requires_attachment,
    is_paid = EXCLUDED.is_paid,
    is_active = TRUE;
