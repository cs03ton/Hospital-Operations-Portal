-- HOP production master data and initial Fleet data.
-- Run 01-prd-schema-migrations.sql first. This file is safe to run repeatedly.
-- DBeaver: enable "Stop on error" before executing the whole script.

BEGIN;

DO $precheck$
BEGIN
    IF current_setting('server_version_num')::integer < 140000 THEN
        RAISE EXCEPTION 'PostgreSQL 14 or newer is required. Current version: %', version();
    END IF;

    IF to_regclass('public."__EFMigrationsHistory"') IS NULL OR
       NOT EXISTS (
           SELECT 1 FROM "__EFMigrationsHistory"
           WHERE "MigrationId" = '20260909090000_CorrectFleetRolePermissions'
       ) THEN
        RAISE EXCEPTION 'Schema migration 20260909090000_CorrectFleetRolePermissions is required. Run 01-prd-schema-migrations.sql first.';
    END IF;
END
$precheck$;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

INSERT INTO roles (id, name, description, is_system_role, is_active, created_at)
SELECT gen_random_uuid(), seed.name, seed.description, TRUE, TRUE, NOW()
FROM (VALUES
    ('SuperAdmin', 'Full system administration access'),
    ('Admin', 'Operational administration access'),
    ('Director', 'Executive approval and reporting access'),
    ('DepartmentHead', 'Department approval access'),
    ('Staff', 'Standard user access'),
    ('LeaveAdmin', 'Leave administration access'),
    ('FleetAdminReviewer', 'Fleet administration review access'),
    ('พนักงานขับรถ', 'พนักงานขับรถ: เข้าถึงเฉพาะงานที่ได้รับมอบหมาย')
) AS seed(name, description)
WHERE NOT EXISTS (SELECT 1 FROM roles r WHERE r.name = seed.name);

INSERT INTO fleet_vehicle_types (id, code, name, description, sort_order, is_active, created_at)
SELECT gen_random_uuid(), seed.code, seed.name, seed.description, seed.sort_order, TRUE, NOW()
FROM (VALUES
    ('SEDAN', 'รถเก๋ง', 'รถยนต์นั่งส่วนบุคคล', 10),
    ('PICKUP', 'รถกระบะ', 'รถกระบะ', 20),
    ('VAN', 'รถตู้', 'รถตู้โดยสาร', 30),
    ('AMBULANCE', 'รถพยาบาล', 'รถพยาบาล', 40),
    ('MOTORCYCLE', 'รถจักรยานยนต์', 'รถจักรยานยนต์', 50),
    ('OTHER', 'อื่น ๆ', 'ยานพาหนะประเภทอื่น', 99)
) AS seed(code, name, description, sort_order)
WHERE NOT EXISTS (SELECT 1 FROM fleet_vehicle_types type WHERE type.code = seed.code);

INSERT INTO leave_types (
    id, code, name, description, default_days_per_year, requires_attachment,
    is_paid, is_active, created_at, requires_balance, allow_carry_over,
    carry_over_max_days, use_fiscal_year
)
SELECT gen_random_uuid(), seed.code, seed.name, seed.description, seed.default_days,
       seed.requires_attachment, seed.is_paid, TRUE, NOW(), seed.requires_balance,
       seed.allow_carry_over, seed.carry_over_max_days, seed.use_fiscal_year
FROM (VALUES
    ('SICK_LEAVE', 'ลาป่วย', 'Medical sick leave', 30::numeric, TRUE, TRUE, TRUE, FALSE, 0::numeric, TRUE),
    ('PERSONAL_LEAVE', 'ลากิจส่วนตัว', 'Personal business leave', 45::numeric, FALSE, TRUE, TRUE, FALSE, 0::numeric, TRUE),
    ('VACATION_LEAVE', 'ลาพักผ่อน', 'Annual vacation leave', 10::numeric, FALSE, TRUE, TRUE, TRUE, 30::numeric, TRUE),
    ('MATERNITY_LEAVE', 'ลาคลอดบุตร', 'Maternity leave', 90::numeric, TRUE, TRUE, TRUE, FALSE, 0::numeric, TRUE),
    ('ORDINATION_LEAVE', 'ลาบวช', 'Ordination leave', 120::numeric, FALSE, TRUE, FALSE, FALSE, 0::numeric, TRUE),
    ('STUDY_LEAVE', 'ลาศึกษาต่อ', 'Study leave', 0::numeric, FALSE, FALSE, FALSE, FALSE, 0::numeric, TRUE),
    ('OTHER_LEAVE', 'อื่น ๆ', 'Other leave', 0::numeric, FALSE, FALSE, FALSE, FALSE, 0::numeric, TRUE)
) AS seed(code, name, description, default_days, requires_attachment, is_paid,
          requires_balance, allow_carry_over, carry_over_max_days, use_fiscal_year)
WHERE NOT EXISTS (SELECT 1 FROM leave_types type WHERE type.code = seed.code);

-- Canonical leave policy matrix. Existing active rules are preserved so this
-- deployment bundle never overwrites policy values maintained in production.
WITH policy_values (
    employment_type, leave_type_code, entitlement_days, max_paid_days,
    allow_carry_over, carry_over_max_days, max_accumulated_days,
    min_service_months, min_service_years, first_year_entitlement_days,
    probation_entitlement_days, first_year_paid_days, is_paid,
    max_extended_days, requires_special_approval_after_days,
    social_security_max_days, uses_social_security, payment_rule_type,
    day_counting_type, notes
) AS (
    VALUES
    ('CIVIL_SERVANT','SICK_LEAVE',60,60,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,TRUE,120,60,NULL,FALSE,'EmployerPaidThenSpecialApproval','BusinessDays','กรณีเกิน 60 วัน ผู้อำนวยการอาจพิจารณาได้รวมไม่เกิน 120 วัน'),
    ('CIVIL_SERVANT','PERSONAL_LEAVE',45,45,FALSE,NULL,NULL,NULL,NULL,15,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays',NULL),
    ('CIVIL_SERVANT','VACATION_LEAVE',10,10,TRUE,30,30,6,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays',NULL),
    ('CIVIL_SERVANT','MATERNITY_LEAVE',90,90,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays',NULL),
    ('CIVIL_SERVANT','ORDINATION_LEAVE',120,120,FALSE,NULL,NULL,12,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ใช้ตามระเบียบราชการและเงื่อนไขหน่วยงาน'),
    ('PERMANENT_EMPLOYEE','SICK_LEAVE',60,60,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,TRUE,120,60,NULL,FALSE,'EmployerPaidThenSpecialApproval','BusinessDays','กรณีเกิน 60 วัน ผู้อำนวยการอาจพิจารณาได้รวมไม่เกิน 120 วัน'),
    ('PERMANENT_EMPLOYEE','PERSONAL_LEAVE',45,45,FALSE,NULL,NULL,NULL,NULL,15,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays',NULL),
    ('PERMANENT_EMPLOYEE','VACATION_LEAVE',10,10,TRUE,30,30,6,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays',NULL),
    ('PERMANENT_EMPLOYEE','MATERNITY_LEAVE',90,90,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays',NULL),
    ('PERMANENT_EMPLOYEE','ORDINATION_LEAVE',120,120,FALSE,NULL,NULL,12,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ใช้ตามระเบียบราชการและเงื่อนไขหน่วยงาน'),
    ('GOVERNMENT_EMPLOYEE','SICK_LEAVE',30,30,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,90,TRUE,'EmployerPaidThenSocialSecurity','BusinessDays','ส่วนที่เกิน 30 วันให้ตรวจสิทธิประกันสังคมตามเงื่อนไข'),
    ('GOVERNMENT_EMPLOYEE','PERSONAL_LEAVE',10,10,FALSE,NULL,NULL,12,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays',NULL),
    ('GOVERNMENT_EMPLOYEE','VACATION_LEAVE',10,10,TRUE,5,15,6,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays',NULL),
    ('GOVERNMENT_EMPLOYEE','MATERNITY_LEAVE',90,45,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,45,TRUE,'EmployerPaidThenSocialSecurity','CalendarDays','ได้รับค่าจ้างจากหน่วยงานไม่เกิน 45 วัน ส่วนที่เหลือใช้สิทธิประกันสังคมตามเงื่อนไข'),
    ('GOVERNMENT_EMPLOYEE','ORDINATION_LEAVE',120,120,FALSE,NULL,NULL,NULL,4,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ต้องทำงานไม่น้อยกว่า 4 ปี'),
    ('MOPH_EMPLOYEE','SICK_LEAVE',45,45,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,90,TRUE,'EmployerPaidThenSocialSecurity','BusinessDays','ส่วนที่เกิน 45 วันให้ตรวจสิทธิประกันสังคมตามเงื่อนไข'),
    ('MOPH_EMPLOYEE','PERSONAL_LEAVE',15,15,FALSE,NULL,NULL,NULL,NULL,6,NULL,6,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ปีแรกได้รับค่าจ้างไม่เกิน 6 วัน'),
    ('MOPH_EMPLOYEE','VACATION_LEAVE',10,10,TRUE,5,15,6,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays',NULL),
    ('MOPH_EMPLOYEE','MATERNITY_LEAVE',90,45,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,45,TRUE,'EmployerPaidThenSocialSecurity','CalendarDays','ได้รับค่าจ้างจากหน่วยงานไม่เกิน 45 วัน ส่วนที่เหลือใช้สิทธิประกันสังคมตามเงื่อนไข'),
    ('MOPH_EMPLOYEE','ORDINATION_LEAVE',120,120,FALSE,NULL,NULL,NULL,4,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ต้องทำงานไม่น้อยกว่า 4 ปี'),
    ('TEMPORARY_EMPLOYEE_MONTHLY','SICK_LEAVE',15,15,FALSE,NULL,NULL,NULL,NULL,8,8,8,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ผู้ปฏิบัติงานยังไม่ครบ 6 เดือนรองรับวงเงินสิทธิ 8 วันทำการ'),
    ('TEMPORARY_EMPLOYEE_MONTHLY','PERSONAL_LEAVE',10,NULL,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,FALSE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ลาได้แต่ไม่ได้รับค่าจ้าง'),
    ('TEMPORARY_EMPLOYEE_MONTHLY','VACATION_LEAVE',10,10,FALSE,NULL,NULL,6,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ไม่มีสิทธิสะสมวันลาพักผ่อน'),
    ('TEMPORARY_EMPLOYEE_MONTHLY','MATERNITY_LEAVE',90,45,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,TRUE,NULL,NULL,45,TRUE,'EmployerPaidThenSocialSecurity','CalendarDays','การได้รับค่าจ้างให้เป็นไปตามสิทธิและประกันสังคม'),
    ('TEMPORARY_EMPLOYEE_MONTHLY','ORDINATION_LEAVE',120,NULL,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,FALSE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ไม่มีสิทธิได้รับค่าจ้างระหว่างลา'),
    ('TEMPORARY_EMPLOYEE_DAILY','SICK_LEAVE',15,NULL,FALSE,NULL,NULL,NULL,NULL,8,8,NULL,FALSE,30,NULL,90,TRUE,'UnpaidThenSocialSecurity','BusinessDays','รายวันไม่มีสิทธิได้รับค่าจ้างจากหน่วยงาน อาจใช้สิทธิประกันสังคมตามเงื่อนไข'),
    ('TEMPORARY_EMPLOYEE_DAILY','PERSONAL_LEAVE',10,NULL,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,FALSE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ลาได้แต่ไม่ได้รับค่าจ้าง'),
    ('TEMPORARY_EMPLOYEE_DAILY','VACATION_LEAVE',10,NULL,FALSE,NULL,NULL,6,NULL,NULL,NULL,NULL,FALSE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ไม่สะสมวันลาพักผ่อน'),
    ('TEMPORARY_EMPLOYEE_DAILY','MATERNITY_LEAVE',90,NULL,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,FALSE,NULL,NULL,90,TRUE,'UnpaidThenSocialSecurity','CalendarDays','ลาได้ 90 วัน แต่ไม่ได้รับค่าจ้างระหว่างลา ใช้สิทธิประกันสังคมตามเงื่อนไข'),
    ('TEMPORARY_EMPLOYEE_DAILY','ORDINATION_LEAVE',120,NULL,FALSE,NULL,NULL,NULL,NULL,NULL,NULL,NULL,FALSE,NULL,NULL,NULL,FALSE,'EmployerPaid','BusinessDays','ไม่มีสิทธิได้รับค่าจ้างระหว่างลา')
)
INSERT INTO leave_policy_rules (
    id, employment_type, leave_type_id, fiscal_year, entitlement_days,
    annual_entitlement_days, max_paid_days, employer_paid_limit_days,
    allow_carry_over, carry_over_max_days, carry_forward_limit_days,
    max_accumulated_days, maximum_total_available_days, min_service_months,
    min_service_years, prorate_if_service_less_than_year,
    first_year_entitlement_days, probation_entitlement_days,
    first_year_paid_days, is_paid, allow_request, max_extended_days,
    maximum_leave_days, requires_special_approval_after_days,
    social_security_max_days, uses_social_security, payment_rule_type,
    day_counting_type, notes, is_active, created_at, updated_at
)
SELECT gen_random_uuid(), policy.employment_type, leave_type.id, NULL,
       policy.entitlement_days, policy.entitlement_days, policy.max_paid_days,
       policy.max_paid_days, policy.allow_carry_over, policy.carry_over_max_days,
       policy.carry_over_max_days, policy.max_accumulated_days,
       policy.max_accumulated_days, policy.min_service_months,
       policy.min_service_years, FALSE, policy.first_year_entitlement_days,
       policy.probation_entitlement_days, policy.first_year_paid_days,
       policy.is_paid, TRUE, policy.max_extended_days, policy.max_extended_days,
       policy.requires_special_approval_after_days,
       policy.social_security_max_days, policy.uses_social_security,
       policy.payment_rule_type, policy.day_counting_type, policy.notes,
       TRUE, NOW(), NOW()
FROM policy_values policy
JOIN leave_types leave_type ON leave_type.code = policy.leave_type_code
WHERE NOT EXISTS (
    SELECT 1 FROM leave_policy_rules existing
    WHERE existing.employment_type = policy.employment_type
      AND existing.leave_type_id = leave_type.id
      AND existing.fiscal_year IS NULL
      AND existing.is_active
);

INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
SELECT gen_random_uuid(), seed.code, seed.name, seed.group_name, seed.action, TRUE, NOW()
FROM (VALUES
    ('FleetDriver.ViewOwn', 'ดูงานขับรถของตนเอง', 'FleetDriver', 'ViewOwn'),
    ('FleetDriver.ViewOwnJobs', 'ดูรายการงานขับรถของตนเอง', 'FleetDriver', 'ViewOwnJobs'),
    ('FleetDriver.Acknowledge', 'รับทราบงานขับรถ', 'FleetDriver', 'Acknowledge'),
    ('FleetDriver.AcceptJob', 'ตอบรับงานขับรถ', 'FleetDriver', 'AcceptJob'),
    ('FleetDriver.DeclineJob', 'ปฏิเสธงานขับรถ', 'FleetDriver', 'DeclineJob'),
    ('FleetDriver.Start', 'เริ่มงานขับรถ', 'FleetDriver', 'Start'),
    ('FleetDriver.StartTrip', 'เริ่มภารกิจรถ', 'FleetDriver', 'StartTrip'),
    ('FleetDriver.Complete', 'จบงานขับรถ', 'FleetDriver', 'Complete'),
    ('FleetDriver.CompleteTrip', 'ปิดภารกิจรถ', 'FleetDriver', 'CompleteTrip'),
    ('FleetTrip.Start', 'เริ่ม Trip', 'FleetTrip', 'Start'),
    ('FleetTrip.Complete', 'จบ Trip', 'FleetTrip', 'Complete'),
    ('FleetTrip.UploadAttachment', 'อัปโหลดไฟล์ Trip', 'FleetTrip', 'UploadAttachment'),
    ('FleetFeedback.Create', 'ให้ Feedback หลังจบทริป', 'FleetFeedback', 'Create'),
    ('FleetFeedback.ViewOwn', 'ดู Feedback ของตนเอง', 'FleetFeedback', 'ViewOwn'),
    ('FleetCalendar.View', 'ดูปฏิทิน Fleet', 'FleetCalendar', 'View')
) AS seed(code, name, group_name, action)
WHERE NOT EXISTS (SELECT 1 FROM permissions permission WHERE permission.code = seed.code);

WITH driver_permissions(code) AS (
    VALUES
        ('FleetDriver.ViewOwn'),
        ('FleetDriver.ViewOwnJobs'),
        ('FleetDriver.Acknowledge'),
        ('FleetDriver.AcceptJob'),
        ('FleetDriver.DeclineJob'),
        ('FleetDriver.Start'),
        ('FleetDriver.StartTrip'),
        ('FleetDriver.Complete'),
        ('FleetDriver.CompleteTrip'),
        ('FleetTrip.Start'),
        ('FleetTrip.Complete'),
        ('FleetTrip.UploadAttachment'),
        ('FleetFeedback.Create'),
        ('FleetFeedback.ViewOwn'),
        ('FleetCalendar.View')
)
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
JOIN driver_permissions wanted ON TRUE
JOIN permissions p ON p.code = wanted.code AND p.is_active
WHERE r.name = 'พนักงานขับรถ'
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Baseline role matrix for a database created only from migrations. These are
-- additive grants; custom production grants and revocations are not replaced.
WITH baseline(role_name, permission_code) AS (
    VALUES
    ('Staff','Dashboard.View'),('Staff','Documentation.View'),('Staff','Announcement.View'),('Staff','Announcement.Acknowledge'),
    ('Staff','LeaveRequest.ViewOwn'),('Staff','LeaveRequest.Create'),('Staff','LeaveRequest.EditOwn'),('Staff','LeaveRequest.CancelOwn'),
    ('Staff','LeaveCancellation.ViewOwn'),('Staff','LeaveCancellation.Create'),('Staff','LeaveCancellation.Submit'),('Staff','LeaveCancellation.CancelOwn'),
    ('Staff','FleetRequest.ViewOwn'),('Staff','FleetRequest.Create'),('Staff','FleetRequest.EditOwn'),('Staff','FleetRequest.Submit'),
    ('Staff','FleetRequest.Cancel'),('Staff','FleetRequest.Copy'),('Staff','FleetRequestCapability.ManageOwn'),('Staff','FleetCompatibility.View'),('Staff','FleetCalendar.View'),
    ('DepartmentHead','Dashboard.View'),('DepartmentHead','Documentation.View'),('DepartmentHead','Announcement.View'),('DepartmentHead','Announcement.Acknowledge'),
    ('DepartmentHead','LeaveRequest.ViewOwn'),('DepartmentHead','LeaveRequest.ViewPendingApproval'),('DepartmentHead','LeaveRequest.ViewDepartment'),
    ('DepartmentHead','LeaveRequest.Create'),('DepartmentHead','LeaveRequest.EditOwn'),('DepartmentHead','LeaveRequest.CancelOwn'),
    ('DepartmentHead','LeaveApproval.ApproveCurrentStep'),('DepartmentHead','LeaveCancellation.ViewOwn'),('DepartmentHead','LeaveCancellation.Create'),
    ('DepartmentHead','LeaveCancellation.Submit'),('DepartmentHead','LeaveCancellation.CancelOwn'),('DepartmentHead','LeaveCancellation.ApproveCurrentStep'),
    ('DepartmentHead','LeaveCancellation.ViewDepartment'),('DepartmentHead','FleetRequest.ViewOwn'),('DepartmentHead','FleetRequest.ViewDepartment'),
    ('DepartmentHead','FleetRequest.Create'),('DepartmentHead','FleetRequest.EditOwn'),('DepartmentHead','FleetRequest.Submit'),
    ('DepartmentHead','FleetRequest.Cancel'),('DepartmentHead','FleetRequest.Copy'),('DepartmentHead','FleetRequestCapability.ManageOwn'),
    ('DepartmentHead','FleetCompatibility.View'),('DepartmentHead','FleetCalendar.View'),
    ('Director','Dashboard.View'),('Director','Documentation.View'),('Director','Announcement.View'),('Director','Announcement.Acknowledge'),
    ('Director','AdminDashboard.View'),('Director','Dashboard.Executive.View'),('Director','LeaveDashboard.ViewExecutiveSummary'),('Director','LeaveAnalytics.View'),
    ('Director','LeaveRequest.ViewOwn'),('Director','LeaveRequest.ViewAll'),('Director','LeaveRequest.ViewPendingApproval'),('Director','LeaveRequest.Create'),
    ('Director','LeaveRequest.EditOwn'),('Director','LeaveRequest.CancelOwn'),('Director','LeaveApproval.ApproveCurrentStep'),
    ('Director','LeaveCancellation.ViewOwn'),('Director','LeaveCancellation.Create'),('Director','LeaveCancellation.Submit'),
    ('Director','LeaveCancellation.CancelOwn'),('Director','LeaveCancellation.ApproveCurrentStep'),
    ('Director','FleetRequest.ViewOwn'),('Director','FleetRequest.ViewAll'),('Director','FleetRequest.Create'),('Director','FleetRequest.EditOwn'),
    ('Director','FleetRequest.Submit'),('Director','FleetRequest.Cancel'),('Director','FleetRequest.Copy'),('Director','FleetRequestCapability.ManageOwn'),
    ('Director','FleetCompatibility.View'),('Director','FleetCalendar.View'),('Director','FleetDirector.Approve'),('Director','FleetDirector.Return'),('Director','FleetDirector.Reject'),
    ('LeaveAdmin','Dashboard.View'),('LeaveAdmin','Documentation.View'),('LeaveAdmin','Announcement.View'),('LeaveAdmin','Announcement.Acknowledge'),
    ('LeaveAdmin','Announcement.Manage'),('LeaveAdmin','Announcement.Create'),('LeaveAdmin','Announcement.EditAll'),('LeaveAdmin','Announcement.Publish'),
    ('LeaveAdmin','Announcement.Schedule'),('LeaveAdmin','Announcement.Archive'),('LeaveAdmin','Announcement.Cancel'),('LeaveAdmin','Announcement.DeleteDraft'),
    ('LeaveAdmin','Announcement.ManageTargets'),('LeaveAdmin','Announcement.Analytics.View'),('LeaveAdmin','Announcement.Notification.Configure'),
    ('LeaveAdmin','Announcement.Notification.Preview'),('LeaveAdmin','Announcement.Notification.SendInApp'),('LeaveAdmin','Announcement.Notification.SendLine'),
    ('LeaveAdmin','Announcement.Notification.ViewDelivery'),('LeaveAdmin','LeaveRequest.ViewDepartment'),('LeaveAdmin','LeaveCancellation.ViewDepartment'),
    ('LeaveAdmin','LeaveCancellation.Manage'),('LeaveAdmin','LeaveAdmin.ManageTypes'),('LeaveAdmin','LeaveAdmin.ManageBalances'),
    ('LeaveAdmin','LeaveBalance.Rollover'),('LeaveAdmin','LeaveAdmin.ManageHolidays'),('LeaveAdmin','LeaveAdmin.ManageApprovalChains'),
    ('FleetAdminReviewer','Dashboard.View'),('FleetAdminReviewer','Documentation.View'),('FleetAdminReviewer','FleetRequest.ViewOwn'),
    ('FleetAdminReviewer','FleetRequest.Create'),('FleetAdminReviewer','FleetRequest.EditOwn'),('FleetAdminReviewer','FleetRequest.Submit'),
    ('FleetAdminReviewer','FleetRequest.Cancel'),('FleetAdminReviewer','FleetRequest.Copy'),('FleetAdminReviewer','FleetAdminReview.Approve'),
    ('FleetAdminReviewer','FleetAdminReview.Return'),('FleetAdminReviewer','FleetAdminReview.Reject'),('FleetAdminReviewer','FleetDashboard.View'),
    ('FleetAdminReviewer','FleetFeedback.Create'),('FleetAdminReviewer','FleetFeedback.ViewOwn'),('FleetAdminReviewer','FleetCalendar.View')
)
INSERT INTO role_permissions (role_id, permission_id)
SELECT role.id, permission.id
FROM baseline
JOIN roles role ON role.name = baseline.role_name AND role.is_active
JOIN permissions permission ON permission.code = baseline.permission_code AND permission.is_active
ON CONFLICT (role_id, permission_id) DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT role.id, permission.id
FROM roles role CROSS JOIN permissions permission
WHERE role.name = 'SuperAdmin' AND role.is_active AND permission.is_active
  AND permission.code NOT IN ('LeaveRequest.Create','LeaveCancellation.Create','LeaveCancellation.Submit','LeaveCancellation.CancelOwn')
ON CONFLICT (role_id, permission_id) DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT role.id, permission.id
FROM roles role CROSS JOIN permissions permission
WHERE role.name = 'Admin' AND role.is_active AND permission.is_active
  AND (
      permission.code LIKE 'Fleet%' OR
      permission.group_name IN ('Dashboard','UserManagement','DepartmentManagement','RoleManagement','LeaveRequest','LeaveApproval','LeaveCancellation','LeaveAdmin','LeaveBalance','System','SystemDiagnostics','Documentation','Announcement','AnnouncementNotification')
  )
  AND permission.code NOT IN ('LeaveRequest.Create','LeaveCancellation.Create','LeaveCancellation.Submit','LeaveCancellation.CancelOwn')
ON CONFLICT (role_id, permission_id) DO NOTHING;

DELETE FROM role_permissions role_permission
USING roles role, permissions permission
WHERE role_permission.role_id = role.id AND role_permission.permission_id = permission.id
  AND role.name = 'DepartmentHead' AND permission.code = 'FleetRequest.ViewAll';

WITH source (
    vehicle_code, registration_number, registration_province, vehicle_type_code,
    brand, model, manufacture_year, seat_capacity_total, passenger_capacity,
    fuel_type, status, is_active, note
) AS (
    VALUES
    ('NMH-FLEET-001', 'กค 2825', 'น่าน', 'AMBULANCE', 'TOYOTA', 'KDH222R-LEMDYT A1', 2010, 4, 4, 'DIESEL', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถพยาบาล\nประเภทต้นทาง: รถตู้\nจดทะเบียน: 7/5/2553\nพ.ร.บ. หมดอายุ: 17/10/2569\nหมายเหตุเดิม: พขร.'),
    ('NMH-FLEET-002', 'กค 4919', 'น่าน', 'AMBULANCE', 'TOYOTA', 'KDLL222R-LEMDYT A3', 2010, 4, 4, 'DIESEL', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถพยาบาล\nประเภทต้นทาง: รถตู้\nจดทะเบียน: 31/3/2554\nพ.ร.บ. หมดอายุ: 17/10/2569\nหมายเหตุเดิม: พขร.'),
    ('NMH-FLEET-003', 'กค 1669', 'น่าน', 'AMBULANCE', 'TOYOTA', 'COMMUTER', 2022, 4, 4, 'DIESEL', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถพยาบาล\nประเภทต้นทาง: รถตู้\nจดทะเบียน: 1/2/2566\nพ.ร.บ. หมดอายุ: 20/1/2570\nหมายเหตุเดิม: พขร.'),
    ('NMH-FLEET-004', 'กท 651', 'น่าน', 'AMBULANCE', 'TOYOTA', 'COMMUTER', 2025, 6, 6, 'DIESEL', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถพยาบาล\nประเภทต้นทาง: รถตู้\nจดทะเบียน: 5/3/2569\nพ.ร.บ. หมดอายุ: 23/12/2569\nหมายเหตุเดิม: พขร.'),
    ('NMH-FLEET-005', 'กจ 8963', 'น่าน', 'SEDAN', 'TOYOTA', 'Hilux Revo', 2018, 5, 5, 'DIESEL', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถยนต์นั่งส่วนบุคคล ไม่เกิน 7 คน\nประเภทต้นทาง: รถยนต์ 4 ประตู\nจดทะเบียน: 11/2/2562\nพ.ร.บ. หมดอายุ: 1/11/2569\nหมายเหตุเดิม: พขร./พขร.สำรอง'),
    ('NMH-FLEET-006', 'กข 7225', 'น่าน', 'SEDAN', 'TOYOTA', 'KUN25R-PRMSHT A1', 2005, 7, 7, 'DIESEL', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถยนต์นั่งส่วนบุคคล ไม่เกิน 7 คน\nประเภทต้นทาง: รถยนต์ 4 ประตู\nจดทะเบียน: 31/3/2549\nพ.ร.บ. หมดอายุ: 1/11/2569\nหมายเหตุเดิม: พขร./พขร.สำรอง'),
    ('NMH-FLEET-007', 'นข 416', 'น่าน', 'PICKUP', 'TOYOTA', NULL, NULL, 7, 7, 'DIESEL', 'DECOMMISSIONED', FALSE,
     E'รายละเอียดรถ: รถยนต์นั่งส่วนบุคคล เกิน 7 คน\nประเภทต้นทาง: กระบะ\nสถานะต้นทาง: แจ้งหยุดรถ\nจดทะเบียน: 20/7/2544\nพ.ร.บ. หมดอายุ: 1/11/2569'),
    ('NMH-FLEET-008', 'บง 948', 'น่าน', 'PICKUP', 'TOYOTA', 'Hilux', 1997, 2, 2, 'DIESEL', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถยนต์บรรทุกส่วนบุคคล\nประเภทต้นทาง: รถยนต์ 2 ประตู (cab)\nจดทะเบียน: 29/7/2540\nพ.ร.บ. หมดอายุ: 1/11/2569\nหมายเหตุเดิม: พขร./พขร.สำรอง'),
    ('NMH-FLEET-009', 'ม 0786', 'น่าน', 'VAN', 'TOYOTA', NULL, 1994, 12, 12, 'DIESEL', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถยนต์นั่งส่วนบุคคล เกิน 7 คน\nประเภทต้นทาง: รถยนต์ 2 ประตู\nจดทะเบียน: 3/6/2537\nพ.ร.บ. หมดอายุ: 1/11/2569\nหมายเหตุเดิม: รับ-ส่งขยะติดเชื้อ'),
    ('NMH-FLEET-010', 'นข 555', 'น่าน', 'VAN', 'TOYOTA', 'ไฮเอซ', 2005, 7, 7, 'DIESEL', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถยนต์นั่งส่วนบุคคล เกิน 7 คน\nประเภทต้นทาง: รถตู้\nความจุตามจำนวนที่นั่ง: 7 คน\nจดทะเบียน: 22/9/2548\nพ.ร.บ. หมดอายุ: 19/2/2570\nหมายเหตุเดิม: พขร./พขร.สำรอง'),
    ('NMH-FLEET-011', 'กธส 814', 'น่าน', 'MOTORCYCLE', 'Honda', 'WAVE 100', 2002, 2, 2, 'GASOLINE', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถจักรยานยนต์\nประเภทต้นทาง: รถจักรยานยนต์\nจดทะเบียน: 21/11/2545\nพ.ร.บ. หมดอายุ: 6/1/2570\nหมายเหตุเดิม: ทุกคัน'),
    ('NMH-FLEET-012', 'กธส 815', 'น่าน', 'MOTORCYCLE', 'Honda', 'WAVE 100', 2002, 2, 2, 'GASOLINE', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถจักรยานยนต์\nประเภทต้นทาง: รถจักรยานยนต์\nจดทะเบียน: 21/11/2545\nพ.ร.บ. หมดอายุ: 6/1/2570\nหมายเหตุเดิม: ทุกคัน'),
    ('NMH-FLEET-013', '1 กต 5238', 'น่าน', 'MOTORCYCLE', 'Honda', 'WAVE 125 I', 2012, 2, 2, 'GASOLINE', 'AVAILABLE', TRUE,
     E'รายละเอียดรถ: รถจักรยานยนต์\nประเภทต้นทาง: รถจักรยานยนต์\nจดทะเบียน: 10/8/2559\nพ.ร.บ. หมดอายุ: 13/11/2569\nหมายเหตุเดิม: ทุกคัน')
), resolved AS (
    SELECT s.*, vt.id AS vehicle_type_id
    FROM source s
    JOIN fleet_vehicle_types vt ON vt.code = s.vehicle_type_code
)
INSERT INTO fleet_vehicles (
    id, vehicle_code, registration_number, registration_province, vehicle_type_id,
    brand, model, manufacture_year, seat_capacity_total, passenger_capacity,
    fuel_type, current_mileage, owning_department_id, status, is_active, note,
    created_at, updated_at
)
SELECT
    gen_random_uuid(), vehicle_code, registration_number, registration_province, vehicle_type_id,
    brand, model, manufacture_year, seat_capacity_total, passenger_capacity,
    fuel_type, 0, NULL, status, is_active, note, NOW(), NOW()
FROM resolved
ON CONFLICT (registration_number) DO NOTHING;

WITH source(employee_code, fullname, license_expiry_date, restriction_note) AS (
    VALUES
        ('nm48003', 'นายอภิสิทธิ์ สารเถื่อนแก้ว', DATE '2029-05-04', 'เวรเช้าและเวร * เท่านั้น'),
        ('nm63004', 'นายวินัย ทับเกลี้ยง', DATE '2029-03-04', NULL),
        ('nm63003', 'นายฐิตพงศ์ ต๊ะทา', DATE '2028-01-07', NULL),
        ('nm66006', 'นายภราดร ธิเขียว', DATE '2029-02-02', NULL),
        ('nm68005', 'นายจิตติวัฒน์ สารเถื่อนแก้ว', DATE '2028-04-24', NULL)
)
INSERT INTO users (
    id, employee_code, fullname, username, password_hash, position, gender,
    department_id, is_active, created_at, updated_at
)
SELECT
    gen_random_uuid(), source.employee_code, source.fullname, source.employee_code,
    crypt(encode(gen_random_bytes(32), 'hex'), gen_salt('bf', 12)),
    'พนักงานขับรถ', 'MALE', NULL, TRUE, NOW(), NOW()
FROM source
WHERE NOT EXISTS (
    SELECT 1 FROM users user_account
    WHERE lower(user_account.employee_code) = lower(source.employee_code)
       OR lower(user_account.username) = lower(source.employee_code)
);

WITH target_users AS (
    SELECT id FROM users
    WHERE lower(employee_code) IN ('nm48003', 'nm63004', 'nm63003', 'nm66006', 'nm68005')
), target_roles AS (
    SELECT id FROM roles WHERE name IN ('Staff', 'พนักงานขับรถ') AND is_active
)
INSERT INTO user_roles (user_id, role_id)
SELECT target_users.id, target_roles.id
FROM target_users CROSS JOIN target_roles
ON CONFLICT (user_id, role_id) DO NOTHING;

WITH source(employee_code, license_expiry_date, restriction_note) AS (
    VALUES
        ('nm48003', DATE '2029-05-04', 'เวรเช้าและเวร * เท่านั้น'),
        ('nm63004', DATE '2029-03-04', NULL),
        ('nm63003', DATE '2028-01-07', NULL),
        ('nm66006', DATE '2029-02-02', NULL),
        ('nm68005', DATE '2028-04-24', NULL)
), resolved_drivers AS (
    SELECT user_account.id AS user_id, source.license_expiry_date, source.restriction_note
    FROM source
    JOIN users user_account ON lower(user_account.employee_code) = lower(source.employee_code)
)
INSERT INTO fleet_driver_profiles (
    id, user_id, license_number, license_type, license_expiry_date,
    can_drive_sedan, can_drive_pickup, can_drive_van, can_drive_ambulance,
    can_drive_other, driver_status, is_active, note, created_at, updated_at
)
SELECT
    gen_random_uuid(), driver.user_id, '', 'ขับรถทุกประเภทชนิดที่ 2', driver.license_expiry_date,
    TRUE, TRUE, TRUE, TRUE, FALSE, 'AVAILABLE', TRUE,
    concat_ws(E'\n',
        'ได้รับอนุญาตให้ขับรถ: รถพยาบาล/รถยนต์ส่วนบุคคล',
        CASE WHEN driver.restriction_note IS NOT NULL THEN 'ข้อจำกัด: ' || driver.restriction_note END,
        'เลขที่ใบขับขี่: ไม่ได้ระบุในข้อมูลต้นทาง'
    ),
    NOW(), NOW()
FROM resolved_drivers driver
ON CONFLICT (user_id) DO NOTHING;

DO $postcheck$
DECLARE
    vehicle_count integer;
    driver_count integer;
    missing_permissions integer;
    leave_type_count integer;
    policy_count integer;
BEGIN
    SELECT count(*) INTO vehicle_count
    FROM fleet_vehicles WHERE vehicle_code LIKE 'NMH-FLEET-%';

    SELECT count(*) INTO driver_count
    FROM fleet_driver_profiles profile
    JOIN users user_account ON user_account.id = profile.user_id
    WHERE lower(user_account.employee_code) IN ('nm48003', 'nm63004', 'nm63003', 'nm66006', 'nm68005');

    SELECT count(*) INTO missing_permissions
    FROM (VALUES
        ('FleetDriver.ViewOwn'), ('FleetDriver.ViewOwnJobs'), ('FleetDriver.Acknowledge'),
        ('FleetDriver.AcceptJob'), ('FleetDriver.DeclineJob'), ('FleetDriver.Start'),
        ('FleetDriver.StartTrip'), ('FleetDriver.Complete'), ('FleetDriver.CompleteTrip'),
        ('FleetTrip.Start'), ('FleetTrip.Complete'), ('FleetTrip.UploadAttachment'),
        ('FleetFeedback.Create'), ('FleetFeedback.ViewOwn'), ('FleetCalendar.View')
    ) expected(code)
    WHERE NOT EXISTS (
        SELECT 1
        FROM roles role
        JOIN role_permissions role_permission ON role_permission.role_id = role.id
        JOIN permissions permission ON permission.id = role_permission.permission_id
        WHERE role.name = 'พนักงานขับรถ' AND permission.code = expected.code
    );

    SELECT count(*) INTO leave_type_count
    FROM leave_types
    WHERE code IN ('SICK_LEAVE', 'PERSONAL_LEAVE', 'VACATION_LEAVE', 'MATERNITY_LEAVE',
                   'ORDINATION_LEAVE', 'STUDY_LEAVE', 'OTHER_LEAVE');

    SELECT count(*) INTO policy_count FROM leave_policy_rules WHERE is_active;

    IF vehicle_count <> 13 THEN
        RAISE EXCEPTION 'Expected 13 NMH Fleet vehicles, found %', vehicle_count;
    END IF;
    IF driver_count <> 5 THEN
        RAISE EXCEPTION 'Expected 5 Fleet driver profiles, found %', driver_count;
    END IF;
    IF missing_permissions <> 0 THEN
        RAISE EXCEPTION 'Fleet driver role is missing % required permissions', missing_permissions;
    END IF;
    IF leave_type_count <> 7 THEN
        RAISE EXCEPTION 'Expected 7 canonical leave types, found %', leave_type_count;
    END IF;
    IF policy_count = 0 THEN
        RAISE EXCEPTION 'No active leave policy rules were found.';
    END IF;
END
$postcheck$;

COMMIT;

SELECT vehicle_code, registration_number, registration_province, status, is_active
FROM fleet_vehicles
WHERE vehicle_code LIKE 'NMH-FLEET-%'
ORDER BY vehicle_code;

SELECT user_account.employee_code, user_account.fullname, profile.license_expiry_date,
       profile.driver_status, profile.is_active
FROM users user_account
JOIN fleet_driver_profiles profile ON profile.user_id = user_account.id
WHERE lower(user_account.employee_code) IN ('nm48003', 'nm63004', 'nm63003', 'nm66006', 'nm68005')
ORDER BY user_account.employee_code;
