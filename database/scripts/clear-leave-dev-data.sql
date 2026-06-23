/*
  DEVELOPMENT ONLY - DO NOT RUN ON PRODUCTION.

  This script clears Leave Management data for local development / QA reset.
  It intentionally DOES NOT delete:
  - users
  - departments
  - roles
  - permissions
  - audit_logs

  Recommended command:
  docker exec -i hop-postgres psql -U hop_user -d hop_db < database/scripts/clear-leave-dev-data.sql
*/

BEGIN;

DELETE FROM approval_override_logs;
DELETE FROM line_delivery_logs;
DELETE FROM leave_attachments;
DELETE FROM leave_approvals;
DELETE FROM approval_delegations;
DELETE FROM approval_escalation_rules;
DELETE FROM approval_chain_steps;
DELETE FROM approval_chains;
DELETE FROM leave_balance_adjustments;
DELETE FROM leave_balances;
DELETE FROM notifications
WHERE title ILIKE '%ลา%'
   OR message ILIKE '%ลา%';
DELETE FROM leave_requests;
DELETE FROM leave_holidays;
DELETE FROM leave_types;

COMMIT;
