param([string]$Container = 'hop-postgres', [string]$DbUser = 'hop_user')
$ErrorActionPreference = 'Stop'
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'test-users-cleanup-fixture.sql')
$preview = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'deploy/sql/04-prd-test-users-preview.sql')
$template = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'deploy/sql/05-prd-test-users-cleanup.sql')
$confirmed = $template
$names = @('staff03','nm69003','nm69001','head01','director01','admin_support','staff02','staff01')
for ($i=0; $i -lt $names.Count; $i++) {
 $id = '10000000-0000-0000-0000-' + ($i+1).ToString('D12')
 $confirmed = $confirmed.Replace("('$($names[$i])', NULL::uuid)", "('$($names[$i])', '$id'::uuid)")
}
$confirmed = $confirmed.Replace('INSERT INTO cleanup_department VALUES(NULL::uuid);', "INSERT INTO cleanup_department VALUES('20000000-0000-0000-0000-000000000001'::uuid);")
function Invoke-QASql([string]$Db, [string]$Sql, [bool]$FailExpected = $false) {
 $out = $Sql | & docker exec -i $Container psql -U $DbUser -d $Db -v ON_ERROR_STOP=1 2>&1
 $code = $LASTEXITCODE
 $text = $out -join "\n"
 if ($FailExpected) {
  if ($code -eq 0 -or $text -notmatch 'ERROR:') { throw "Expected failure did not occur: $text" }
 } elseif ($code -ne 0) { throw $text }
 return $text
}
$cases = @(
 @{Name='success'; Setup=''; Error=$null},
 @{Name='moved_admin'; Setup="UPDATE users SET username='admin' WHERE username='real_user';"; Error=$null},
 @{Name='shared_chain_user'; Setup="UPDATE users SET approval_chain_id='a0000000-0000-0000-0000-000000000001' WHERE username='real_user';"; Error='Cleanup blocked'},
 @{Name='shared_chain_leave'; Setup="UPDATE leave_requests SET approval_chain_id='a0000000-0000-0000-0000-000000000001' WHERE request_number='REAL-001';"; Error='Cleanup blocked'},
 @{Name='shared_chain_config'; Setup="INSERT INTO extra_soft_refs VALUES(gen_random_uuid(),'chain=a0000000-0000-0000-0000-000000000001');"; Error='Cleanup blocked'},
 @{Name='other_chain'; Setup="UPDATE approval_chains SET department_id='20000000-0000-0000-0000-000000000002';"; Error='Cleanup blocked'},
 @{Name='shared_line_leave'; Setup="INSERT INTO line_delivery_logs VALUES(gen_random_uuid(),'10000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000099');"; Error='Cleanup blocked'},
 @{Name='shared_audit'; Setup="INSERT INTO audit_logs(id,user_id,entity_name,entity_id,action) VALUES(gen_random_uuid(),'10000000-0000-0000-0000-000000000001','LeaveRequest','40000000-0000-0000-0000-000000000099','Approve');"; Error=$null},
 @{Name='audit_detail'; Setup="INSERT INTO audit_logs VALUES(gen_random_uuid(),null,'Other',null,'Update','target=10000000-0000-0000-0000-000000000001');"; Error=$null},
 @{Name='audit_entity'; Setup="INSERT INTO audit_logs VALUES(gen_random_uuid(),null,'Other','a0000000-0000-0000-0000-000000000001','Update',null);"; Error=$null},
 @{Name='audit_incoming_reference'; Setup="CREATE TABLE audit_ref(id uuid PRIMARY KEY, audit_id uuid REFERENCES audit_logs(id)); INSERT INTO audit_ref VALUES(gen_random_uuid(),'70000000-0000-0000-0000-000000000001');"; Error='Cleanup blocked'},
 @{Name='blank_identity'; Setup=''; Error='Fill all 8'},
 @{Name='identity_mismatch'; Setup="UPDATE users SET id='10000000-0000-0000-0000-000000000088' WHERE username='not-present'; UPDATE users SET username='renamed' WHERE username='staff01';"; Error='identity mismatch'},
 @{Name='shared_leave'; Setup="UPDATE leave_requests SET current_approver_id='10000000-0000-0000-0000-000000000001' WHERE request_number='REAL-001';"; Error='Cleanup blocked'},
 @{Name='shared_fleet'; Setup="UPDATE fleet_requests SET created_by_user_id='10000000-0000-0000-0000-000000000001';"; Error='Cleanup blocked'},
 @{Name='soft_reference'; Setup="INSERT INTO extra_soft_refs VALUES(gen_random_uuid(),'target=10000000-0000-0000-0000-000000000001');"; Error='Cleanup blocked'},
 @{Name='department_member'; Setup="UPDATE users SET department_id='20000000-0000-0000-0000-000000000001' WHERE username='real_user';"; Error='Cleanup blocked'},
 @{Name='schema_mismatch'; Setup='ALTER TABLE users RENAME COLUMN department_id TO unexpected_department;'; Error='Schema mismatch'},
 @{Name='trigger_change'; Setup=@'
CREATE FUNCTION public.qa_side_effect() RETURNS trigger LANGUAGE plpgsql AS $f$
BEGIN UPDATE public.users SET note='changed' WHERE username='real_user'; RETURN OLD; END $f$;
CREATE TRIGGER qa_trigger AFTER DELETE ON public.leave_requests FOR EACH ROW EXECUTE FUNCTION public.qa_side_effect();
'@; Error='Retained data changed'}
)
foreach ($case in $cases) {
 $db = 'hop_cleanup_test_' + [guid]::NewGuid().ToString('N')
 & docker exec $Container createdb -U $DbUser $db
 if ($LASTEXITCODE -ne 0) { throw 'Cannot create isolated test database' }
 try {
  Invoke-QASql $db $fixture | Out-Null
  if ($case.Setup) { Invoke-QASql $db $case.Setup | Out-Null }
  if ($case.Name -eq 'success') {
   Invoke-QASql $db $preview | Out-Null
   Invoke-QASql $db 'DO $a$ BEGIN IF (SELECT count(*) FROM users)<>9 THEN RAISE EXCEPTION ''preview mutated users''; END IF; END $a$;' | Out-Null
  }
  $sql = if ($case.Name -eq 'blank_identity') { $template } else { $confirmed }
  $result = Invoke-QASql $db $sql ([bool]$case.Error)
  if ($case.Error) {
   if ($result -notmatch $case.Error) { throw "Wrong failure: $result" }
   Invoke-QASql $db 'DO $a$ BEGIN IF (SELECT count(*) FROM users)<>9 OR (SELECT count(*) FROM leave_requests)<>2 THEN RAISE EXCEPTION ''partial deletion''; END IF; END $a$;' | Out-Null
  } else {
   Invoke-QASql $db @'
DO $a$ BEGIN
 IF (SELECT count(*) FROM users)<>1 OR (SELECT count(*) FROM leave_requests)<>1
 OR (SELECT note FROM users WHERE id='10000000-0000-0000-0000-000000000099') IS DISTINCT FROM 'KEEP'
 OR (SELECT used_days FROM leave_balances LIMIT 1)<>7
 OR (SELECT count(*) FROM fleet_requests WHERE note='KEEP')<>1
 OR (SELECT count(*) FROM departments)<>1 OR (SELECT count(*) FROM roles)<>1
 OR (SELECT count(*) FROM approval_chains)<>0 OR (SELECT count(*) FROM approval_chain_steps)<>0
 OR (SELECT count(*) FROM announcements WHERE title='KEEP')<>1
 OR (SELECT count(*) FROM announcement_reads)<>1
 OR (SELECT count(*) FROM announcement_notification_deliveries)<>1
 OR (SELECT count(*) FROM notifications)<>1 OR (SELECT count(*) FROM line_delivery_logs)<>1
 OR (SELECT count(*) FROM audit_logs)<>1
 OR (SELECT detail FROM audit_logs LIMIT 1) IS DISTINCT FROM 'KEEP'
 THEN RAISE EXCEPTION 'preserved data mismatch'; END IF;
END $a$;
'@ | Out-Null
   Invoke-QASql $db $confirmed | Out-Null
  }
  Write-Output "PASS $($case.Name)"
 } finally {
  # Only remove the unique database just created by this iteration.
  if ($db -notmatch '^hop_cleanup_test_[0-9a-f]{32}$') { throw 'Unexpected QA database name' }
  & docker exec $Container dropdb -U $DbUser $db
  if ($LASTEXITCODE -ne 0) { throw "QA database cleanup failed: $db" }
 }
}
