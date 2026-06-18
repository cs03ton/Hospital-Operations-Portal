INSERT INTO departments (name, description)
VALUES ('Information Technology', 'Default IT department')
ON CONFLICT DO NOTHING;

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
    VALUES
        ('Dashboard', 'View'), ('Dashboard', 'Create'), ('Dashboard', 'Edit'), ('Dashboard', 'Delete'), ('Dashboard', 'Approve'), ('Dashboard', 'Export'), ('Dashboard', 'Manage'),
        ('UserManagement', 'View'), ('UserManagement', 'Create'), ('UserManagement', 'Edit'), ('UserManagement', 'Delete'), ('UserManagement', 'Approve'), ('UserManagement', 'Export'), ('UserManagement', 'Manage'),
        ('DepartmentManagement', 'View'), ('DepartmentManagement', 'Create'), ('DepartmentManagement', 'Edit'), ('DepartmentManagement', 'Delete'), ('DepartmentManagement', 'Approve'), ('DepartmentManagement', 'Export'), ('DepartmentManagement', 'Manage'),
        ('RoleManagement', 'View'), ('RoleManagement', 'Create'), ('RoleManagement', 'Edit'), ('RoleManagement', 'Delete'), ('RoleManagement', 'Approve'), ('RoleManagement', 'Export'), ('RoleManagement', 'Manage'),
        ('LeaveManagement', 'View'), ('LeaveManagement', 'Create'), ('LeaveManagement', 'Edit'), ('LeaveManagement', 'Delete'), ('LeaveManagement', 'Approve'), ('LeaveManagement', 'Export'), ('LeaveManagement', 'Manage'),
        ('RepairManagement', 'View'), ('RepairManagement', 'Create'), ('RepairManagement', 'Edit'), ('RepairManagement', 'Delete'), ('RepairManagement', 'Approve'), ('RepairManagement', 'Export'), ('RepairManagement', 'Manage'),
        ('BorrowManagement', 'View'), ('BorrowManagement', 'Create'), ('BorrowManagement', 'Edit'), ('BorrowManagement', 'Delete'), ('BorrowManagement', 'Approve'), ('BorrowManagement', 'Export'), ('BorrowManagement', 'Manage'),
        ('InventoryManagement', 'View'), ('InventoryManagement', 'Create'), ('InventoryManagement', 'Edit'), ('InventoryManagement', 'Delete'), ('InventoryManagement', 'Approve'), ('InventoryManagement', 'Export'), ('InventoryManagement', 'Manage'),
        ('ReportManagement', 'View'), ('ReportManagement', 'Create'), ('ReportManagement', 'Edit'), ('ReportManagement', 'Delete'), ('ReportManagement', 'Approve'), ('ReportManagement', 'Export'), ('ReportManagement', 'Manage'),
        ('SystemSettings', 'View'), ('SystemSettings', 'Create'), ('SystemSettings', 'Edit'), ('SystemSettings', 'Delete'), ('SystemSettings', 'Approve'), ('SystemSettings', 'Export'), ('SystemSettings', 'Manage')
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
