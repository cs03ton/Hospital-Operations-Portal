param(
    [Parameter(Mandatory = $true)][string]$SourcePath,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$rows = @(Import-Csv -LiteralPath $SourcePath -Delimiter "`t" -Encoding UTF8)
$expectedColumns = @('รหัสพนักงาน', 'คำนำหน้า', 'ชื่อ', 'นามสกุล', 'ตำแหน่งสายงาน', 'กลุ่มงาน', 'ประเภทพนักงาน', 'วันเริ่มงาน')
foreach ($column in $expectedColumns) {
    if ($rows.Count -eq 0 -or $column -notin $rows[0].PSObject.Properties.Name) { throw "Missing source column: $column" }
}

$monthNumbers = @{ 'ม.ค.'=1; 'ก.พ.'=2; 'มี.ค.'=3; 'เม.ย.'=4; 'พ.ค.'=5; 'มิ.ย.'=6; 'ก.ค.'=7; 'ส.ค.'=8; 'ก.ย.'=9; 'ต.ค.'=10; 'พ.ย.'=11; 'ธ.ค.'=12 }
$employmentTypes = @{ 'ข้าราชการ'='CIVIL_SERVANT'; 'พนักงานกระทรวงสาธารณสุข'='MOPH_EMPLOYEE'; 'ลูกจ้างชั่วคราว'='TEMPORARY_EMPLOYEE_DAILY' }
$departments = @{
    'การพยาบาล' = @{ Name = 'กลุ่มงานการพยาบาล'; Id = '019f7dd2-dbb1-75b5-90e8-924cb0c0b46c' }
    'องค์กรแพทย์' = @{ Name = 'กลุ่มงานการแพทย์'; Id = '019f7dd2-7da1-7c54-bef7-7249c63ea426' }
    'แพทย์แผนไทย' = @{ Name = 'กลุ่มงานการแพทย์แผนไทยและการแพทย์ทางเลือก'; Id = '019f7dd4-63a4-77cd-bd1a-3d4b25ad5de0' }
    'ทันตกรรม' = @{ Name = 'กลุ่มงานทันตกรรม'; Id = '019f7dd3-d665-7ecc-a7f5-8041545d7fce' }
    'บริหารทั่วไป' = @{ Name = 'กลุ่มงานบริหารทั่วไป'; Id = '019f7dd2-adb8-7033-ab3d-de5b4d9e3d3b' }
    'รังสีการแพทย์' = @{ Name = 'กลุ่มงานรังสีวิทยา'; Id = '019f7dd3-084f-70bf-a917-dba4b45336f5' }
    'ฟื้นฟู' = @{ Name = 'กลุ่มงานเวชกรรมฟื้นฟู'; Id = '019f7dd4-3706-7339-9d3c-dec502a33655' }
}
function Quote-Sql([string]$value) { return "'" + $value.Replace("'", "''") + "'" }

$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$values = foreach ($row in $rows) {
    $code = $row.'รหัสพนักงาน'.Trim()
    if ($code -notmatch '^nm[0-9]{5}$' -or -not $seen.Add($code)) { throw "Invalid or duplicate employee code: $code" }
    foreach ($column in $expectedColumns) { if ([string]::IsNullOrWhiteSpace($row.$column)) { throw "Missing $column for $code" } }
    if ($row.'วันเริ่มงาน' -notmatch '^(\d{1,2})-(.+)-(\d{2})$') { throw "Invalid start date for $code" }
    $day = [int]$Matches[1]; $monthText = $Matches[2]; $thaiYear = [int]$Matches[3]
    if (-not $monthNumbers.ContainsKey($monthText)) { throw "Unknown Thai month for $code" }
    # The source uses abbreviated Buddhist Era years (64 = 2564 BE = 2021 CE).
    $date = [datetime]::new((1414 + $thaiYear), $monthNumbers[$monthText], $day).ToString('yyyy-MM-dd')
    $type = $employmentTypes[$row.'ประเภทพนักงาน'.Trim()]
    if (-not $type) { throw "Unknown employment type for $code" }
    $department = $departments[$row.'กลุ่มงาน'.Trim()]
    if (-not $department) { throw "Unknown department for $code" }
    $gender = switch ($row.'คำนำหน้า'.Trim()) { 'นาย' { 'Male' } 'นาง' { 'Female' } 'นางสาว' { 'Female' } default { throw "Unknown title for $code" } }
    $fullname = ($row.'คำนำหน้า'.Trim() + $row.'ชื่อ'.Trim() + ' ' + $row.'นามสกุล'.Trim())
    '(' + ((@($code, $fullname, $row.'ตำแหน่งสายงาน'.Trim(), $department.Name, $gender, $type) | ForEach-Object { Quote-Sql $_ }) -join ', ') + ", DATE '$date')"
}
$valueSql = $values -join ",`n    "
$mappingSql = ($departments.Values | Sort-Object Name | ForEach-Object { '(' + (Quote-Sql $_.Name) + ", '" + $_.Id + "'::uuid)" }) -join ",`n        "
$sql = @"
-- HOP PRD employee import generated from the supplied 61-row roster.
-- DBeaver: execute as a SQL script with Stop on error; the final SELECT reports the result.
-- Username = employee code. New accounts only: password 1234, bcrypt cost 12, Staff role.
-- Temporary employees in this roster are mapped to TEMPORARY_EMPLOYEE_DAILY as confirmed.
-- Existing accounts are skipped; their profile, roles, and password are never overwritten.
-- Blank source status is treated as active. Canonical PRD departments must already exist.
BEGIN;
SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';

DO `$precheck`$
BEGIN
    IF to_regclass('public.users') IS NULL OR to_regclass('public.departments') IS NULL
       OR to_regclass('public.user_roles') IS NULL OR to_regclass('public.roles') IS NULL THEN
        RAISE EXCEPTION 'Required HOP tables are missing';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pgcrypto') THEN
        RAISE EXCEPTION 'pgcrypto is required; run the PRD master-data setup first';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM roles WHERE name = 'Staff' AND is_active) THEN
        RAISE EXCEPTION 'Active Staff role is required';
    END IF;
END
`$precheck`$;

CREATE TEMP TABLE employee_seed_source (
    employee_code text PRIMARY KEY, fullname text NOT NULL, position text NOT NULL,
    department_name text NOT NULL, gender text NOT NULL,
    employment_type text NOT NULL, employment_start_date date NOT NULL
) ON COMMIT DROP;

INSERT INTO employee_seed_source VALUES
    $valueSql;

CREATE TEMP TABLE employee_seed_departments (name text PRIMARY KEY, id uuid NOT NULL) ON COMMIT DROP;
INSERT INTO employee_seed_departments VALUES
        $mappingSql;

DO `$validate`$
BEGIN
    IF (SELECT count(*) FROM employee_seed_source) <> $($rows.Count) THEN
        RAISE EXCEPTION 'Expected $($rows.Count) employee records';
    END IF;
    IF EXISTS (
        SELECT 1 FROM employee_seed_departments m
        LEFT JOIN departments d ON d.id = m.id AND d.name = m.name AND d.is_active
        WHERE d.id IS NULL
    ) OR EXISTS (
        SELECT 1 FROM employee_seed_source s
        LEFT JOIN employee_seed_departments m ON m.name = s.department_name
        WHERE m.id IS NULL
    ) THEN
        RAISE EXCEPTION 'Canonical department ID/name missing or inactive; no changes made';
    END IF;
    IF EXISTS (
        SELECT 1 FROM employee_seed_source s JOIN users u
          ON lower(u.username) = lower(s.employee_code)
         AND (u.employee_code IS NULL OR lower(u.employee_code) <> lower(s.employee_code))
    ) THEN
        RAISE EXCEPTION 'Username collision with a different employee code; no changes made';
    END IF;
    IF EXISTS (
        SELECT 1 FROM employee_seed_source s JOIN users u
          ON lower(u.employee_code) = lower(s.employee_code)
         AND lower(u.username) <> lower(s.employee_code)
    ) THEN
        RAISE EXCEPTION 'Existing employee code has a different username; review manually';
    END IF;
END
`$validate`$;

CREATE TEMP TABLE employee_seed_inserted (id uuid PRIMARY KEY, employee_code text NOT NULL) ON COMMIT DROP;
WITH new_users AS (
    INSERT INTO users (id, employee_code, fullname, username, password_hash,
                       position, gender, employment_type, employment_start_date,
                       department_id, is_active, created_at)
    SELECT gen_random_uuid(), s.employee_code, s.fullname, s.employee_code,
           crypt('1234', gen_salt('bf', 12)), s.position, s.gender,
           s.employment_type, s.employment_start_date, d.id, TRUE, now()
    FROM employee_seed_source s
    JOIN employee_seed_departments d ON d.name = s.department_name
    WHERE NOT EXISTS (SELECT 1 FROM users u WHERE lower(u.employee_code) = lower(s.employee_code))
    RETURNING id, employee_code
)
INSERT INTO employee_seed_inserted SELECT id, employee_code FROM new_users;

INSERT INTO user_roles (user_id, role_id)
SELECT i.id, r.id FROM employee_seed_inserted i CROSS JOIN roles r
WHERE r.name = 'Staff' AND r.is_active
ON CONFLICT (user_id, role_id) DO NOTHING;

SELECT (SELECT count(*) FROM employee_seed_source) AS source_count,
       (SELECT count(*) FROM employee_seed_inserted) AS inserted_count,
       (SELECT count(*) FROM employee_seed_source) - (SELECT count(*) FROM employee_seed_inserted) AS existing_skipped_count,
       (SELECT count(*) FROM user_roles ur JOIN employee_seed_inserted i ON i.id = ur.user_id
        JOIN roles r ON r.id = ur.role_id WHERE r.name = 'Staff') AS new_staff_grants;
COMMIT;
"@
[System.IO.File]::WriteAllText($OutputPath, $sql, [System.Text.UTF8Encoding]::new($false))
Write-Output "Generated $($rows.Count) employees at $OutputPath"
