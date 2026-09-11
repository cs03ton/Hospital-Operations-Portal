-- DBeaver: execute whole script; Stop on error; no per-statement auto-commit.
-- Stop API/workers and backup DB + files first. On error: ROLLBACK;
-- Temporary workspace only until the $delete$ block (cleanup script only).
BEGIN;

SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '180s';
CREATE TEMP TABLE cleanup_targets(username text PRIMARY KEY, expected_id uuid) ON COMMIT DROP;
INSERT INTO cleanup_targets VALUES
('staff03', NULL::uuid),
('nm69003', NULL::uuid),
('nm69001', NULL::uuid),
('head01', NULL::uuid),
('director01', NULL::uuid),
('admin_support', NULL::uuid),
('staff02', NULL::uuid),
('staff01', NULL::uuid);
-- CLEANUP ONLY: set expected_id above from preview. Never guess UUIDs.
CREATE TEMP TABLE cleanup_department(expected_id uuid) ON COMMIT DROP;
INSERT INTO cleanup_department VALUES(NULL::uuid);
-- CLEANUP ONLY: set department UUID from preview when department still exists.
CREATE TEMP TABLE cleanup_rows(
 rel oid, tab text, pos tid, data jsonb, selected boolean DEFAULT false,
 reason text, deleted boolean DEFAULT false, PRIMARY KEY(rel,pos)
) ON COMMIT DROP;
CREATE TEMP TABLE cleanup_issues(tab text, column_name text, row_id text, reason text) ON COMMIT DROP;
CREATE TEMP TABLE cleanup_tables(rel oid PRIMARY KEY,tab text) ON COMMIT DROP;

DO $snapshot$
DECLARE t record; missing text;
BEGIN
 SELECT string_agg(v.t||'.'||v.c, ', ') INTO missing
 FROM (VALUES ('users','id'),('users','username'),('users','department_id'),
 ('departments','id'),('departments','name'),('leave_requests','id'),('leave_requests','user_id'),
 ('approval_chains','id'),('approval_chains','department_id'),
 ('approval_chain_steps','id'),('approval_chain_steps','approval_chain_id'),
 ('notifications','user_id'),('announcement_reads','user_id'),
 ('announcement_notification_deliveries','user_id'),
 ('line_delivery_logs','recipient_user_id'),('line_delivery_logs','leave_request_id')) v(t,c)
 WHERE NOT EXISTS(SELECT 1 FROM information_schema.columns c WHERE c.table_schema='public' AND c.table_name=v.t AND c.column_name=v.c);
 IF missing IS NOT NULL THEN RAISE EXCEPTION 'Schema mismatch: %',missing; END IF;
 IF EXISTS(SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
 WHERE n.nspname='public' AND (c.relkind IN ('p','f') OR c.relispartition OR c.relrowsecurity))
 THEN RAISE EXCEPTION 'Partitioned/foreign/RLS tables require separate review'; END IF;
 IF EXISTS(SELECT 1 FROM pg_constraint f JOIN pg_class c ON c.oid=f.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace
 JOIN pg_class p ON p.oid=f.confrelid JOIN pg_namespace pn ON pn.oid=p.relnamespace
 WHERE f.contype='f' AND pn.nspname='public' AND n.nspname<>'public')
 THEN RAISE EXCEPTION 'Cross-schema incoming FK requires separate review'; END IF;
 -- Snapshot all public rows, not just known FK tables, to detect soft references.
 -- Data stays in temporary tables; reports never print tokens/passwords/payloads.
 FOR t IN SELECT c.oid,c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
 WHERE n.nspname='public' AND c.relkind='r' ORDER BY c.oid LOOP
   EXECUTE format('LOCK TABLE public.%I IN SHARE ROW EXCLUSIVE MODE',t.relname);
   INSERT INTO cleanup_tables VALUES(t.oid,t.relname);
 END LOOP;
 FOR t IN SELECT c.oid,c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
 WHERE n.nspname='public' AND c.relkind='r' ORDER BY c.oid LOOP
   EXECUTE format('INSERT INTO cleanup_rows(rel,tab,pos,data) SELECT %s,%L,ctid,to_jsonb(t) FROM public.%I t',t.oid,t.relname,t.relname);
 END LOOP;
END $snapshot$;

-- Identify only the explicit usernames, never all users of a department.
UPDATE cleanup_rows r SET selected=true,reason='target account'
WHERE tab='users' AND EXISTS(SELECT 1 FROM cleanup_targets t WHERE t.username=r.data->>'username');
UPDATE cleanup_rows SET selected=true,reason='target department'
WHERE tab='departments' AND data->>'name'='Information Technology';
INSERT INTO cleanup_issues
SELECT 'users','username',data->>'username','Duplicate target username'
FROM cleanup_rows WHERE tab='users' AND selected GROUP BY data->>'username' HAVING count(*)>1;
INSERT INTO cleanup_issues
SELECT 'departments','name','Information Technology','Ambiguous department name'
WHERE (SELECT count(*) FROM cleanup_rows WHERE tab='departments' AND selected)>1;

CREATE TEMP TABLE cleanup_user_ids ON COMMIT DROP AS
SELECT data->>'id' AS id FROM cleanup_rows WHERE tab='users' AND selected;
UPDATE cleanup_rows SET selected=true,reason='leave owned by target account'
WHERE tab='leave_requests' AND data->>'user_id' IN(SELECT id FROM cleanup_user_ids);
CREATE TEMP TABLE cleanup_leave_ids ON COMMIT DROP AS
SELECT data->>'id' AS id FROM cleanup_rows WHERE tab='leave_requests' AND selected;
UPDATE cleanup_rows SET selected=true,reason='cancellation of target leave'
WHERE tab='leave_cancellation_requests'
AND data->>'original_leave_request_id' IN(SELECT id FROM cleanup_leave_ids)
AND data->>'requester_user_id' IN(SELECT id FROM cleanup_user_ids);
CREATE TEMP TABLE cleanup_cancel_ids ON COMMIT DROP AS
SELECT data->>'id' AS id FROM cleanup_rows WHERE tab='leave_cancellation_requests' AND selected;

UPDATE cleanup_rows SET selected=true,reason='target account data'
WHERE tab IN('user_roles','refresh_tokens','line_user_bindings','line_pairing_codes','line_connect_tokens',
 'leave_balances','leave_balance_adjustments','leave_balance_snapshots','leave_balance_transactions')
AND data->>'user_id' IN(SELECT id FROM cleanup_user_ids);
UPDATE cleanup_rows SET selected=true,reason='target leave child'
WHERE tab IN('leave_attachments','leave_approvals','approval_override_logs','line_delivery_logs')
AND data->>'leave_request_id' IN(SELECT id FROM cleanup_leave_ids);
UPDATE cleanup_rows SET selected=true,reason='target cancellation child'
WHERE tab='leave_cancellation_approvals'
AND data->>'leave_cancellation_request_id' IN(SELECT id FROM cleanup_cancel_ids);
UPDATE cleanup_rows SET selected=true,reason='target leave approval log'
WHERE tab='approval_logs' AND data->>'request_type' IN('Leave','LeaveRequest','LeaveCancellation','LeaveCancellationRequest')
AND (data->>'request_id' IN(SELECT id FROM cleanup_leave_ids) OR data->>'request_id' IN(SELECT id FROM cleanup_cancel_ids));
-- Select known soft-reference records only by the entity they describe.
UPDATE cleanup_rows SET selected=true,reason='notification of target leave'
WHERE tab='notifications' AND (data->>'reference_id' IN(SELECT id FROM cleanup_leave_ids)
 OR data->>'reference_id' IN(SELECT id FROM cleanup_cancel_ids));
UPDATE cleanup_rows SET selected=true,reason='personal unlinked notification'
WHERE tab='notifications' AND data->>'user_id' IN(SELECT id FROM cleanup_user_ids)
AND coalesce(data->>'reference_id','')='' AND coalesce(data->>'action_url','')='';
UPDATE cleanup_rows SET selected=true,reason='audit of deleted entity'
WHERE tab='audit_logs' AND (
 ((data->>'entity_name') IN('User','Users') AND data->>'entity_id' IN(SELECT id FROM cleanup_user_ids))
 OR ((data->>'entity_name') IN('LeaveRequest','LeaveRequests') AND data->>'entity_id' IN(SELECT id FROM cleanup_leave_ids))
 OR ((data->>'entity_name') IN('LeaveCancellation','LeaveCancellationRequest') AND data->>'entity_id' IN(SELECT id FROM cleanup_cancel_ids)));
UPDATE cleanup_rows SET selected=true,reason='authentication audit of target account'
WHERE tab='audit_logs' AND data->>'user_id' IN(SELECT id FROM cleanup_user_ids)
AND coalesce(data->>'entity_id','')='' AND data->>'action' IN('Login','LoginFailed','Logout','LoginSuccess');

-- Personal delivery/read history only; announcement content is retained.
UPDATE cleanup_rows SET selected=true,reason='target account notification history'
WHERE tab IN('notifications','announcement_reads','announcement_notification_deliveries')
AND data->>'user_id' IN(SELECT id FROM cleanup_user_ids);
UPDATE cleanup_rows SET selected=true,reason='target recipient delivery history'
WHERE tab='line_delivery_logs'
AND data->>'recipient_user_id' IN(SELECT id FROM cleanup_user_ids)
AND (coalesce(data->>'leave_request_id','')=''
 OR data->>'leave_request_id' IN(SELECT id FROM cleanup_leave_ids));

-- Candidates only: retained FK/soft references below block the entire transaction.
UPDATE cleanup_rows SET selected=true,reason='target department approval chain'
WHERE tab='approval_chains' AND data->>'department_id' IN(
 SELECT data->>'id' FROM cleanup_rows WHERE tab='departments' AND selected);
UPDATE cleanup_rows SET selected=true,reason='target department approval chain step'
WHERE tab='approval_chain_steps' AND data->>'approval_chain_id' IN(
 SELECT data->>'id' FROM cleanup_rows WHERE tab='approval_chains' AND selected);

-- Do not remove balance history pointing to a retained leave.
INSERT INTO cleanup_issues
SELECT r.tab,'reference_id',r.data->>'id','Target balance transaction references a retained leave'
FROM cleanup_rows r WHERE r.selected AND r.tab='leave_balance_transactions'
AND EXISTS(SELECT 1 FROM cleanup_rows k WHERE k.tab IN('leave_requests','leave_cancellation_requests')
 AND NOT k.selected AND k.data->>'id'=r.data->>'reference_id');

-- Explicitly approved: remove audit history mentioning deletion targets,
-- even when the event describes changes to retained business data.
-- Freeze this set before selecting more audits; do not recursively expand scope.
CREATE TEMP TABLE cleanup_audit_targets ON COMMIT DROP AS
SELECT DISTINCT lower(data->>'id') AS id FROM cleanup_rows WHERE selected AND data ? 'id';
INSERT INTO cleanup_audit_targets
SELECT DISTINCT lower(v.value) FROM cleanup_rows r CROSS JOIN LATERAL jsonb_each_text(r.data) v
WHERE r.selected AND r.tab IN('leave_requests','leave_cancellation_requests')
AND v.key IN('request_number','cancellation_request_number') AND coalesce(v.value,'')<>'';
UPDATE cleanup_rows r SET selected=true,reason='approved audit history referencing deletion target'
WHERE r.tab='audit_logs' AND NOT r.selected
AND EXISTS(
 SELECT 1 FROM jsonb_each_text(r.data) v
 JOIN cleanup_audit_targets t ON position(t.id IN lower(coalesce(v.value,'')))>0
 WHERE v.key IN('user_id','entity_id','detail') AND coalesce(t.id,'')<>'');

CREATE TEMP TABLE cleanup_edges(parent_rel oid,parent_pos tid,child_rel oid,child_pos tid,fk text) ON COMMIT DROP;
DO $foreign_keys$
DECLARE f record; predicate text;
BEGIN
 FOR f IN SELECT oid,conname,conrelid,confrelid,conkey,confkey FROM pg_constraint
 WHERE contype='f' AND confrelid IN(SELECT DISTINCT rel FROM cleanup_rows WHERE selected) LOOP
   SELECT string_agg(format('c.data->>%L = p.data->>%L',ca.attname,pa.attname),' AND ') INTO predicate
   FROM unnest(f.conkey,f.confkey) k(ck,pk)
   JOIN pg_attribute ca ON ca.attrelid=f.conrelid AND ca.attnum=k.ck
   JOIN pg_attribute pa ON pa.attrelid=f.confrelid AND pa.attnum=k.pk;
   EXECUTE format('INSERT INTO cleanup_edges SELECT p.rel,p.pos,c.rel,c.pos,%L
     FROM cleanup_rows p JOIN cleanup_rows c ON %s
     WHERE p.rel=%s AND p.selected AND c.rel=%s',f.conname,predicate,f.confrelid,f.conrelid);
 END LOOP;
END $foreign_keys$;
INSERT INTO cleanup_issues
SELECT c.tab,e.fk,coalesce(c.data->>'id',c.pos::text),'Retained row references deletion target (FK)'
FROM cleanup_edges e JOIN cleanup_rows c ON c.rel=e.child_rel AND c.pos=e.child_pos WHERE NOT c.selected;

-- Scan all retained values, including JSON/text payloads, for deleted UUIDs.
-- Conservative matches are blockers, never permission to delete unrelated rows.
CREATE TEMP TABLE cleanup_ids ON COMMIT DROP AS
SELECT DISTINCT lower(data->>'id') id FROM cleanup_rows WHERE selected AND data ? 'id';
INSERT INTO cleanup_ids
SELECT DISTINCT lower(v.value) FROM cleanup_rows r CROSS JOIN LATERAL jsonb_each_text(r.data) v
WHERE r.selected AND r.tab IN('leave_requests','leave_cancellation_requests')
AND v.key IN('request_number','cancellation_request_number') AND coalesce(v.value,'')<>'';
INSERT INTO cleanup_issues
SELECT DISTINCT r.tab,v.key,coalesce(r.data->>'id',r.pos::text),'Retained row mentions deleted UUID (soft reference)'
FROM cleanup_rows r CROSS JOIN LATERAL jsonb_each_text(r.data) v
WHERE NOT r.selected AND EXISTS(SELECT 1 FROM cleanup_ids x
 WHERE position(x.id IN lower(coalesce(v.value,'')))>0);

-- Reports: export all result sets before any destructive run.
SELECT t.username,r.data->>'id' AS actual_id,t.expected_id,
 CASE WHEN r.rel IS NULL THEN 'absent' ELSE 'found' END AS state
FROM cleanup_targets t LEFT JOIN cleanup_rows r ON r.tab='users' AND r.data->>'username'=t.username ORDER BY t.username;
SELECT data->>'id' AS department_id,data->>'name' AS name FROM cleanup_rows WHERE tab='departments' AND selected;
SELECT tab,reason,count(*) AS rows_to_delete FROM cleanup_rows WHERE selected GROUP BY tab,reason ORDER BY tab;
SELECT DISTINCT tab,column_name,row_id,reason FROM cleanup_issues ORDER BY tab,column_name,row_id;
SELECT count(*) AS issue_count, count(DISTINCT (tab,row_id)) AS affected_rows FROM cleanup_issues;
SELECT tab,reason,count(*) AS issue_count,count(DISTINCT row_id) AS affected_rows
FROM cleanup_issues GROUP BY tab,reason ORDER BY tab,reason;
-- Manifest only. No filesystem changes. Check shared paths before removing files.
SELECT r.tab,r.data->>'id' AS row_id,v.key AS path_column,v.value AS stored_path,
 EXISTS(SELECT 1 FROM cleanup_rows k CROSS JOIN LATERAL jsonb_each_text(k.data) z
   WHERE NOT k.selected AND z.value=v.value) AS shared_with_retained_row
FROM cleanup_rows r CROSS JOIN LATERAL jsonb_each_text(r.data) v
WHERE r.selected AND r.tab IN('leave_attachments','users')
AND v.key IN('file_path','profile_image_path','profile_image_url') AND coalesce(v.value,'')<>'';

DO $identity$
BEGIN
 IF EXISTS(SELECT 1 FROM cleanup_targets WHERE expected_id IS NULL) THEN
  RAISE EXCEPTION 'Fill all 8 expected_id UUIDs from preview before cleanup';
 END IF;
 IF EXISTS(SELECT 1 FROM cleanup_rows r JOIN cleanup_targets t
 ON r.tab='users' AND (r.data->>'username'=t.username OR r.data->>'id'=t.expected_id::text)
 WHERE r.data->>'id'<>t.expected_id::text OR r.data->>'username'<>t.username)
 THEN RAISE EXCEPTION 'Username/UUID identity mismatch'; END IF;
 IF EXISTS(SELECT 1 FROM cleanup_rows r CROSS JOIN cleanup_department d
 WHERE r.tab='departments' AND r.selected AND (d.expected_id IS NULL OR d.expected_id::text<>r.data->>'id'))
 THEN RAISE EXCEPTION 'Fill/verify Information Technology department UUID from preview'; END IF;
 IF EXISTS(SELECT 1 FROM cleanup_issues) THEN
  RAISE EXCEPTION 'Cleanup blocked: % references/issues; inspect preview. No persistent rows deleted.',(SELECT count(*) FROM cleanup_issues);
 END IF;
END $identity$;

DO $delete$
DECLARE r record; n bigint; progressed boolean; mismatch bigint;
BEGIN
 -- Leaf-first deletion follows actual FK edges, including composite FKs.
 -- No CASCADE, no constraint disabling, no updates to retained rows.
 WHILE EXISTS(SELECT 1 FROM cleanup_rows WHERE selected AND NOT deleted) LOOP
  progressed := false;
  FOR r IN SELECT s.* FROM cleanup_rows s WHERE selected AND NOT deleted
   AND NOT EXISTS(SELECT 1 FROM cleanup_edges e JOIN cleanup_rows child
    ON child.rel=e.child_rel AND child.pos=e.child_pos
    WHERE e.parent_rel=s.rel AND e.parent_pos=s.pos AND NOT child.deleted
    AND NOT(e.child_rel=s.rel AND e.child_pos=s.pos))
   ORDER BY s.tab,s.pos LOOP
    EXECUTE format('DELETE FROM public.%I WHERE ctid=$1',r.tab) USING r.pos;
    GET DIAGNOSTICS n=ROW_COUNT;
    IF n<>1 THEN RAISE EXCEPTION 'Unexpected affected row count in %: %',r.tab,n; END IF;
    UPDATE cleanup_rows SET deleted=true WHERE rel=r.rel AND pos=r.pos;
    progressed := true;
  END LOOP;
  IF NOT progressed THEN RAISE EXCEPTION 'Cyclic references require manual review; rollback'; END IF;
 END LOOP;
 -- Compare complete retained row values, not just counts: detect trigger side effects.
 FOR r IN SELECT rel,tab FROM cleanup_tables LOOP
  EXECUTE format('SELECT count(*) FROM (
   (SELECT data FROM cleanup_rows WHERE rel=$1 AND NOT selected EXCEPT ALL SELECT to_jsonb(t) FROM public.%I t)
   UNION ALL
   (SELECT to_jsonb(t) FROM public.%I t EXCEPT ALL SELECT data FROM cleanup_rows WHERE rel=$1 AND NOT selected)
  ) differences',r.tab,r.tab) INTO mismatch USING r.rel;
  IF mismatch<>0 THEN RAISE EXCEPTION 'Retained data changed in %; rollback (% differences)',r.tab,mismatch; END IF;
 END LOOP;
END $delete$;
SELECT tab,count(*) AS deleted_rows FROM cleanup_rows WHERE deleted GROUP BY tab ORDER BY tab;
COMMIT;
