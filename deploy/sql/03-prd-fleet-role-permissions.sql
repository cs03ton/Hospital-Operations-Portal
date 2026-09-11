-- Fleet permissions for existing PRD roles. PostgreSQL 13+ / DBeaver.
-- Run the WHOLE file as SQL Script, with Stop on error. Backup first.
-- On any error execute ROLLBACK; fix the reported issue, then rerun the whole file.
-- Does not create users/roles, change user_roles, or modify EF migration history.
BEGIN;
SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';

DO $pre$
DECLARE missing text;
BEGIN
 IF current_setting('server_version_num')::int < 130000 THEN
  RAISE EXCEPTION 'PostgreSQL 13+ required';
 END IF;
 SELECT string_agg(v.t || '.' || v.c, ', ') INTO missing
 FROM (VALUES
 ('roles','id'),('roles','name'),('roles','is_active'),
 ('permissions','id'),('permissions','code'),('permissions','name'),
 ('permissions','group_name'),('permissions','action'),('permissions','is_active'),('permissions','created_at'),
 ('role_permissions','role_id'),('role_permissions','permission_id'),
 ('users','id'),('users','username'),('users','is_active'),
 ('user_roles','user_id'),('user_roles','role_id')
 ) v(t,c)
 WHERE NOT EXISTS (SELECT 1 FROM information_schema.columns c
 WHERE c.table_schema='public' AND c.table_name=v.t AND c.column_name=v.c);
 IF missing IS NOT NULL THEN RAISE EXCEPTION 'Missing schema columns: %', missing; END IF;
END $pre$;

-- Prevent concurrent permission edits between validation and insertion.
LOCK TABLE roles, permissions, role_permissions IN SHARE ROW EXCLUSIVE MODE;
CREATE TEMP TABLE fleet_seed_permissions(code text PRIMARY KEY) ON COMMIT DROP;
INSERT INTO fleet_seed_permissions VALUES
('FleetRequest.ViewOwn'),
('FleetRequest.ViewDepartment'),
('FleetRequest.ViewAll'),
('FleetRequest.Create'),
('FleetRequest.EditOwn'),
('FleetRequest.Submit'),
('FleetRequest.Cancel'),
('FleetRequest.Copy'),
('FleetDispatch.View'),
('FleetDispatch.Assign'),
('FleetDispatch.Reassign'),
('FleetDispatch.Return'),
('FleetDispatch.Reject'),
('FleetAdminReview.Approve'),
('FleetAdminReview.Return'),
('FleetAdminReview.Reject'),
('FleetDirector.Approve'),
('FleetDirector.Return'),
('FleetDirector.Reject'),
('FleetDriver.ViewOwn'),
('FleetDriver.Acknowledge'),
('FleetDriver.Start'),
('FleetDriver.Complete'),
('FleetVehicle.Manage'),
('FleetDriver.Manage'),
('FleetReport.View'),
('FleetReport.Export'),
('FleetSettings.Manage'),
('FleetDelegation.Manage'),
('FleetDelegation.View'),
('FleetDispatch.ReplaceAssignment'),
('FleetDispatch.ReplaceApprovedAssignment'),
('FleetDriver.ViewOwnJobs'),
('FleetDriver.StartTrip'),
('FleetDriver.CompleteTrip'),
('FleetTrip.OverrideMileage'),
('FleetTrip.CloseByAdmin'),
('FleetCancellation.Review'),
('FleetDashboard.View'),
('FleetOutbox.View'),
('FleetTrip.OverrideComplete'),
('FleetHealth.View'),
('FleetHealth.Manage'),
('FleetMaintenance.View'),
('FleetMaintenance.Manage'),
('FleetMaintenance.Complete'),
('FleetMaintenance.Cancel'),
('FleetMaintenance.OverrideMileage'),
('FleetMaintenance.ManageTypes'),
('FleetMaintenance.ManageDocuments'),
('FleetMaintenance.UploadAttachment'),
('FleetCalendar.View'),
('FleetCapability.View'),
('FleetCapability.Manage'),
('FleetVehicleCapability.Manage'),
('FleetRequestCapability.ManageOwn'),
('FleetCompatibility.View'),
('FleetCompatibility.Override'),
('FleetEmergency.Create'),
('FleetEmergency.ViewOwn'),
('FleetEmergency.ViewQueue'),
('FleetEmergency.Dispatch'),
('FleetEmergency.BypassApproval'),
('FleetEmergency.Review'),
('FleetEmergency.ViewAudit'),
('FleetDriver.ViewJobs'),
('FleetDriver.AcceptJob'),
('FleetDriver.DeclineJob'),
('FleetTrip.Start'),
('FleetTrip.Complete'),
('FleetTrip.UploadAttachment'),
('FleetEmergencyPolicy.View'),
('FleetEmergencyPolicy.Manage'),
('FleetLineGroup.View'),
('FleetLineGroup.Manage'),
('FleetFeedback.Create'),
('FleetFeedback.ViewOwn'),
('FleetFeedback.ViewManagement'),
('FleetFeedback.ViewIdentity'),
('FleetFeedback.Manage');

CREATE TEMP TABLE fleet_seed_matrix(role_name text, code text, PRIMARY KEY(role_name,code)) ON COMMIT DROP;
INSERT INTO fleet_seed_matrix VALUES
('Staff','FleetRequest.ViewOwn'),
('Staff','FleetRequest.Create'),
('Staff','FleetRequest.EditOwn'),
('Staff','FleetRequest.Submit'),
('Staff','FleetRequest.Cancel'),
('Staff','FleetRequest.Copy'),
('Staff','FleetRequestCapability.ManageOwn'),
('Staff','FleetCompatibility.View'),
('Staff','FleetCalendar.View'),
('Staff','FleetFeedback.Create'),
('Staff','FleetFeedback.ViewOwn'),
('DepartmentHead','FleetRequest.ViewOwn'),
('DepartmentHead','FleetRequest.Create'),
('DepartmentHead','FleetRequest.EditOwn'),
('DepartmentHead','FleetRequest.Submit'),
('DepartmentHead','FleetRequest.Cancel'),
('DepartmentHead','FleetRequest.Copy'),
('DepartmentHead','FleetRequestCapability.ManageOwn'),
('DepartmentHead','FleetCompatibility.View'),
('DepartmentHead','FleetCalendar.View'),
('DepartmentHead','FleetFeedback.Create'),
('DepartmentHead','FleetFeedback.ViewOwn'),
('DepartmentHead','FleetRequest.ViewDepartment'),
('Director','FleetRequest.ViewOwn'),
('Director','FleetRequest.Create'),
('Director','FleetRequest.EditOwn'),
('Director','FleetRequest.Submit'),
('Director','FleetRequest.Cancel'),
('Director','FleetRequest.Copy'),
('Director','FleetRequestCapability.ManageOwn'),
('Director','FleetCompatibility.View'),
('Director','FleetCalendar.View'),
('Director','FleetFeedback.Create'),
('Director','FleetFeedback.ViewOwn'),
('Director','FleetRequest.ViewAll'),
('Director','FleetDirector.Approve'),
('Director','FleetDirector.Return'),
('Director','FleetDirector.Reject'),
('Director','FleetDashboard.View'),
('Director','FleetReport.View'),
('Director','FleetReport.Export'),
('Director','FleetFeedback.ViewManagement'),
('FleetAdminReviewer','FleetRequest.ViewOwn'),
('FleetAdminReviewer','FleetRequest.Create'),
('FleetAdminReviewer','FleetRequest.EditOwn'),
('FleetAdminReviewer','FleetRequest.Submit'),
('FleetAdminReviewer','FleetRequest.Cancel'),
('FleetAdminReviewer','FleetRequest.Copy'),
('FleetAdminReviewer','FleetRequestCapability.ManageOwn'),
('FleetAdminReviewer','FleetCompatibility.View'),
('FleetAdminReviewer','FleetCalendar.View'),
('FleetAdminReviewer','FleetFeedback.Create'),
('FleetAdminReviewer','FleetFeedback.ViewOwn'),
('FleetAdminReviewer','FleetAdminReview.Approve'),
('FleetAdminReviewer','FleetAdminReview.Return'),
('FleetAdminReviewer','FleetAdminReview.Reject'),
('FleetAdminReviewer','FleetDashboard.View'),
('พนักงานขับรถ','FleetDriver.ViewOwn'),
('พนักงานขับรถ','FleetDriver.ViewOwnJobs'),
('พนักงานขับรถ','FleetDriver.ViewJobs'),
('พนักงานขับรถ','FleetDriver.Acknowledge'),
('พนักงานขับรถ','FleetDriver.AcceptJob'),
('พนักงานขับรถ','FleetDriver.DeclineJob'),
('พนักงานขับรถ','FleetDriver.Start'),
('พนักงานขับรถ','FleetDriver.StartTrip'),
('พนักงานขับรถ','FleetDriver.Complete'),
('พนักงานขับรถ','FleetDriver.CompleteTrip'),
('พนักงานขับรถ','FleetTrip.Start'),
('พนักงานขับรถ','FleetTrip.Complete'),
('พนักงานขับรถ','FleetTrip.UploadAttachment'),
('พนักงานขับรถ','FleetFeedback.Create'),
('พนักงานขับรถ','FleetFeedback.ViewOwn'),
('พนักงานขับรถ','FleetCalendar.View'),
('LeaveAdmin','FleetFeedback.Create'),
('LeaveAdmin','FleetFeedback.ViewOwn'),
('Admin','FleetRequest.ViewOwn'),
('Admin','FleetRequest.ViewDepartment'),
('Admin','FleetRequest.ViewAll'),
('Admin','FleetRequest.Create'),
('Admin','FleetRequest.EditOwn'),
('Admin','FleetRequest.Submit'),
('Admin','FleetRequest.Cancel'),
('Admin','FleetRequest.Copy'),
('Admin','FleetDispatch.View'),
('Admin','FleetDispatch.Assign'),
('Admin','FleetDispatch.Reassign'),
('Admin','FleetDispatch.Return'),
('Admin','FleetDispatch.Reject'),
('Admin','FleetAdminReview.Approve'),
('Admin','FleetAdminReview.Return'),
('Admin','FleetAdminReview.Reject'),
('Admin','FleetDirector.Approve'),
('Admin','FleetDirector.Return'),
('Admin','FleetDirector.Reject'),
('Admin','FleetDriver.ViewOwn'),
('Admin','FleetDriver.Acknowledge'),
('Admin','FleetDriver.Start'),
('Admin','FleetDriver.Complete'),
('Admin','FleetVehicle.Manage'),
('Admin','FleetDriver.Manage'),
('Admin','FleetReport.View'),
('Admin','FleetReport.Export'),
('Admin','FleetSettings.Manage'),
('Admin','FleetDelegation.Manage'),
('Admin','FleetDelegation.View'),
('Admin','FleetDispatch.ReplaceAssignment'),
('Admin','FleetDispatch.ReplaceApprovedAssignment'),
('Admin','FleetDriver.ViewOwnJobs'),
('Admin','FleetDriver.StartTrip'),
('Admin','FleetDriver.CompleteTrip'),
('Admin','FleetTrip.OverrideMileage'),
('Admin','FleetTrip.CloseByAdmin'),
('Admin','FleetCancellation.Review'),
('Admin','FleetDashboard.View'),
('Admin','FleetOutbox.View'),
('Admin','FleetTrip.OverrideComplete'),
('Admin','FleetHealth.View'),
('Admin','FleetHealth.Manage'),
('Admin','FleetMaintenance.View'),
('Admin','FleetMaintenance.Manage'),
('Admin','FleetMaintenance.Complete'),
('Admin','FleetMaintenance.Cancel'),
('Admin','FleetMaintenance.OverrideMileage'),
('Admin','FleetMaintenance.ManageTypes'),
('Admin','FleetMaintenance.ManageDocuments'),
('Admin','FleetMaintenance.UploadAttachment'),
('Admin','FleetCalendar.View'),
('Admin','FleetCapability.View'),
('Admin','FleetCapability.Manage'),
('Admin','FleetVehicleCapability.Manage'),
('Admin','FleetRequestCapability.ManageOwn'),
('Admin','FleetCompatibility.View'),
('Admin','FleetCompatibility.Override'),
('Admin','FleetEmergency.Create'),
('Admin','FleetEmergency.ViewOwn'),
('Admin','FleetEmergency.ViewQueue'),
('Admin','FleetEmergency.Dispatch'),
('Admin','FleetEmergency.BypassApproval'),
('Admin','FleetEmergency.Review'),
('Admin','FleetEmergency.ViewAudit'),
('Admin','FleetDriver.ViewJobs'),
('Admin','FleetDriver.AcceptJob'),
('Admin','FleetDriver.DeclineJob'),
('Admin','FleetTrip.Start'),
('Admin','FleetTrip.Complete'),
('Admin','FleetTrip.UploadAttachment'),
('Admin','FleetEmergencyPolicy.View'),
('Admin','FleetEmergencyPolicy.Manage'),
('Admin','FleetLineGroup.View'),
('Admin','FleetLineGroup.Manage'),
('Admin','FleetFeedback.Create'),
('Admin','FleetFeedback.ViewOwn'),
('Admin','FleetFeedback.ViewManagement'),
('Admin','FleetFeedback.ViewIdentity'),
('Admin','FleetFeedback.Manage'),
('SuperAdmin','FleetRequest.ViewOwn'),
('SuperAdmin','FleetRequest.ViewDepartment'),
('SuperAdmin','FleetRequest.ViewAll'),
('SuperAdmin','FleetRequest.Create'),
('SuperAdmin','FleetRequest.EditOwn'),
('SuperAdmin','FleetRequest.Submit'),
('SuperAdmin','FleetRequest.Cancel'),
('SuperAdmin','FleetRequest.Copy'),
('SuperAdmin','FleetDispatch.View'),
('SuperAdmin','FleetDispatch.Assign'),
('SuperAdmin','FleetDispatch.Reassign'),
('SuperAdmin','FleetDispatch.Return'),
('SuperAdmin','FleetDispatch.Reject'),
('SuperAdmin','FleetAdminReview.Approve'),
('SuperAdmin','FleetAdminReview.Return'),
('SuperAdmin','FleetAdminReview.Reject'),
('SuperAdmin','FleetDirector.Approve'),
('SuperAdmin','FleetDirector.Return'),
('SuperAdmin','FleetDirector.Reject'),
('SuperAdmin','FleetDriver.ViewOwn'),
('SuperAdmin','FleetDriver.Acknowledge'),
('SuperAdmin','FleetDriver.Start'),
('SuperAdmin','FleetDriver.Complete'),
('SuperAdmin','FleetVehicle.Manage'),
('SuperAdmin','FleetDriver.Manage'),
('SuperAdmin','FleetReport.View'),
('SuperAdmin','FleetReport.Export'),
('SuperAdmin','FleetSettings.Manage'),
('SuperAdmin','FleetDelegation.Manage'),
('SuperAdmin','FleetDelegation.View'),
('SuperAdmin','FleetDispatch.ReplaceAssignment'),
('SuperAdmin','FleetDispatch.ReplaceApprovedAssignment'),
('SuperAdmin','FleetDriver.ViewOwnJobs'),
('SuperAdmin','FleetDriver.StartTrip'),
('SuperAdmin','FleetDriver.CompleteTrip'),
('SuperAdmin','FleetTrip.OverrideMileage'),
('SuperAdmin','FleetTrip.CloseByAdmin'),
('SuperAdmin','FleetCancellation.Review'),
('SuperAdmin','FleetDashboard.View'),
('SuperAdmin','FleetOutbox.View'),
('SuperAdmin','FleetTrip.OverrideComplete'),
('SuperAdmin','FleetHealth.View'),
('SuperAdmin','FleetHealth.Manage'),
('SuperAdmin','FleetMaintenance.View'),
('SuperAdmin','FleetMaintenance.Manage'),
('SuperAdmin','FleetMaintenance.Complete'),
('SuperAdmin','FleetMaintenance.Cancel'),
('SuperAdmin','FleetMaintenance.OverrideMileage'),
('SuperAdmin','FleetMaintenance.ManageTypes'),
('SuperAdmin','FleetMaintenance.ManageDocuments'),
('SuperAdmin','FleetMaintenance.UploadAttachment'),
('SuperAdmin','FleetCalendar.View'),
('SuperAdmin','FleetCapability.View'),
('SuperAdmin','FleetCapability.Manage'),
('SuperAdmin','FleetVehicleCapability.Manage'),
('SuperAdmin','FleetRequestCapability.ManageOwn'),
('SuperAdmin','FleetCompatibility.View'),
('SuperAdmin','FleetCompatibility.Override'),
('SuperAdmin','FleetEmergency.Create'),
('SuperAdmin','FleetEmergency.ViewOwn'),
('SuperAdmin','FleetEmergency.ViewQueue'),
('SuperAdmin','FleetEmergency.Dispatch'),
('SuperAdmin','FleetEmergency.BypassApproval'),
('SuperAdmin','FleetEmergency.Review'),
('SuperAdmin','FleetEmergency.ViewAudit'),
('SuperAdmin','FleetDriver.ViewJobs'),
('SuperAdmin','FleetDriver.AcceptJob'),
('SuperAdmin','FleetDriver.DeclineJob'),
('SuperAdmin','FleetTrip.Start'),
('SuperAdmin','FleetTrip.Complete'),
('SuperAdmin','FleetTrip.UploadAttachment'),
('SuperAdmin','FleetEmergencyPolicy.View'),
('SuperAdmin','FleetEmergencyPolicy.Manage'),
('SuperAdmin','FleetLineGroup.View'),
('SuperAdmin','FleetLineGroup.Manage'),
('SuperAdmin','FleetFeedback.Create'),
('SuperAdmin','FleetFeedback.ViewOwn'),
('SuperAdmin','FleetFeedback.ViewManagement'),
('SuperAdmin','FleetFeedback.ViewIdentity'),
('SuperAdmin','FleetFeedback.Manage');

-- Pre-check report: any listed row must be resolved before this script can proceed.
SELECT DISTINCT m.role_name, 'missing or inactive role' AS issue
FROM fleet_seed_matrix m LEFT JOIN roles r ON r.name=m.role_name
WHERE r.id IS NULL OR NOT r.is_active
UNION ALL
SELECT p.code, 'inactive permission' FROM permissions p
JOIN fleet_seed_permissions s ON s.code=p.code WHERE NOT p.is_active;

DO $validate$
DECLARE issues text;
BEGIN
 SELECT string_agg(name, ', ') INTO issues FROM (
 SELECT DISTINCT m.role_name AS name FROM fleet_seed_matrix m
 LEFT JOIN roles r ON r.name=m.role_name WHERE r.id IS NULL OR NOT r.is_active
 UNION
 SELECT p.code FROM permissions p JOIN fleet_seed_permissions s ON s.code=p.code WHERE NOT p.is_active
 ) problem;
 IF issues IS NOT NULL THEN RAISE EXCEPTION 'Missing/inactive roles or inactive permissions: %', issues; END IF;
 IF EXISTS (SELECT name FROM roles WHERE name IN (SELECT role_name FROM fleet_seed_matrix) GROUP BY name HAVING count(*)>1)
 OR EXISTS (SELECT code FROM permissions WHERE code IN (SELECT code FROM fleet_seed_permissions) GROUP BY code HAVING count(*)>1)
 THEN RAISE EXCEPTION 'Duplicate role names or permission codes; resolve before seeding'; END IF;
END $validate$;

-- Before: current Fleet mappings.
SELECT 'before' AS phase, r.name AS role_name, p.code
FROM roles r JOIN role_permissions rp ON rp.role_id=r.id
JOIN permissions p ON p.id=rp.permission_id
WHERE p.code LIKE 'Fleet%' ORDER BY r.name,p.code;

CREATE TEMP TABLE fleet_seed_result(operation text, affected bigint) ON COMMIT DROP;
-- Before: effective Fleet grants for active accounts.
SELECT 'before' AS phase, u.username,
 coalesce(string_agg(DISTINCT p.code, ', ' ORDER BY p.code),'') AS fleet_permissions
FROM users u LEFT JOIN user_roles ur ON ur.user_id=u.id
LEFT JOIN roles r ON r.id=ur.role_id AND r.is_active
LEFT JOIN role_permissions rp ON rp.role_id=r.id
LEFT JOIN permissions p ON p.id=rp.permission_id AND p.is_active AND p.code LIKE 'Fleet%'
WHERE u.is_active GROUP BY u.id,u.username ORDER BY u.username;

WITH added AS (
 INSERT INTO permissions(id,code,name,group_name,action,is_active,created_at)
 SELECT gen_random_uuid(), s.code, s.code, split_part(s.code,'.',1), split_part(s.code,'.',2), TRUE, NOW()
 FROM fleet_seed_permissions s WHERE NOT EXISTS(SELECT 1 FROM permissions p WHERE p.code=s.code)
 RETURNING id
)
INSERT INTO fleet_seed_result SELECT 'permissions_added',count(*) FROM added;

WITH added AS (
 INSERT INTO role_permissions(role_id,permission_id)
 SELECT r.id,p.id FROM fleet_seed_matrix m
 JOIN roles r ON r.name=m.role_name
 JOIN permissions p ON p.code=m.code
 WHERE NOT EXISTS(SELECT 1 FROM role_permissions rp WHERE rp.role_id=r.id AND rp.permission_id=p.id)
 ON CONFLICT(role_id,permission_id) DO NOTHING
 RETURNING role_id
)
INSERT INTO fleet_seed_result SELECT 'mappings_added',count(*) FROM added;

WITH removed AS (
 DELETE FROM role_permissions rp USING roles r, permissions p
 WHERE rp.role_id=r.id AND rp.permission_id=p.id
 AND r.name='DepartmentHead' AND p.code='FleetRequest.ViewAll'
 RETURNING rp.role_id
)
INSERT INTO fleet_seed_result SELECT 'department_head_view_all_removed',count(*) FROM removed;

DO $post$
BEGIN
 IF EXISTS(SELECT 1 FROM fleet_seed_matrix m JOIN roles r ON r.name=m.role_name
 JOIN permissions p ON p.code=m.code
 WHERE NOT EXISTS(SELECT 1 FROM role_permissions rp WHERE rp.role_id=r.id AND rp.permission_id=p.id))
 THEN RAISE EXCEPTION 'Expected Fleet mappings are missing'; END IF;
END $post$;

SELECT * FROM fleet_seed_result ORDER BY operation;
SELECT m.role_name,m.code AS missing_permission FROM fleet_seed_matrix m
JOIN roles r ON r.name=m.role_name JOIN permissions p ON p.code=m.code
WHERE NOT EXISTS(SELECT 1 FROM role_permissions rp WHERE rp.role_id=r.id AND rp.permission_id=p.id);
SELECT 'after' AS phase,r.name AS role_name,p.code
FROM roles r JOIN role_permissions rp ON rp.role_id=r.id JOIN permissions p ON p.id=rp.permission_id
WHERE p.code LIKE 'Fleet%' ORDER BY r.name,p.code;
-- Effective role-derived permissions; users without Fleet grants remain visible.
SELECT u.username,
 coalesce(string_agg(DISTINCT r.name, ', ' ORDER BY r.name),'') AS active_roles,
 coalesce(string_agg(DISTINCT p.code, ', ' ORDER BY p.code),'') AS fleet_permissions
FROM users u LEFT JOIN user_roles ur ON ur.user_id=u.id
LEFT JOIN roles r ON r.id=ur.role_id AND r.is_active
LEFT JOIN role_permissions rp ON rp.role_id=r.id
LEFT JOIN permissions p ON p.id=rp.permission_id AND p.is_active AND p.code LIKE 'Fleet%'
WHERE u.is_active GROUP BY u.id,u.username ORDER BY u.username;
COMMIT;
-- Sign out and sign in again. Rollout restrictions are independent of permissions.
