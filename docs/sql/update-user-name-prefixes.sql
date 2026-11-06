-- PostgreSQL / DBeaver: run the complete script with Alt+X.
-- Requires user-name-prefix.sql to have been installed first.
-- Updates name_prefix only; fullname and username remain unchanged.
BEGIN;
SET LOCAL lock_timeout = '10s';

CREATE TEMP TABLE hop_prefix_updates (
    username text PRIMARY KEY,
    name_prefix varchar(100) NOT NULL
) ON COMMIT DROP;

INSERT INTO hop_prefix_updates (username, name_prefix) VALUES
('nm59002', 'นาย'),
('nm64002', 'นาย'),
('nm68002', 'นางสาว'),
('nm67002', 'นางสาว'),
('nm44001', 'นาย'),
('nm61001', 'นางสาว'),
('nm42001', 'นาง'),
('nm55001', 'นางสาว'),
('nm62001', 'นางสาว'),
('nm66007', 'นางสาว'),
('nm52002', 'นางสาว'),
('nm48002', 'นาง'),
('nm63001', 'นาย'),
('nm56002', 'นาง'),
('nm53003', 'นางสาว'),
('nm48003', 'นาย'),
('nm35001', 'นางสาว'),
('nm34001', 'นางสาว'),
('nm37001', 'นาง'),
('nm63002', 'นาย'),
('nm30001', 'นาง'),
('nm38001', 'นาง'),
('nm35002', 'นาง'),
('nm34002', 'นาง'),
('nm57001', 'นาง'),
('nm56001', 'นางสาว'),
('nm42002', 'นาง'),
('nm44002', 'นาง'),
('nm52003', 'นาง'),
('nm52004', 'นาง'),
('nm48001', 'นาง'),
('nm41001', 'นาง'),
('nm41002', 'นาง'),
('nm47001', 'นางสาว'),
('nm41003', 'นาง'),
('nm59003', 'นางสาว'),
('nm62002', 'นางสาว'),
('nm53002', 'นางสาว'),
('nm58001', 'นางสาว'),
('nm48004', 'นาง'),
('nm60001', 'นาย'),
('nm52001', 'นาย'),
('nm60003', 'นางสาว'),
('nm45002', 'นาง'),
('nm57002', 'นางสาว'),
('nm59001', 'นางสาว'),
('nm51001', 'นางสาว'),
('nm65001', 'นางสาว'),
('nm68006', 'นางสาว'),
('nm63003', 'นาย'),
('nm63004', 'นาย'),
('nm55002', 'นาง'),
('nm64003', 'นางสาว'),
('nm66002', 'นางสาว'),
('nm67001', 'นาง'),
('nm68003', 'นาง'),
('nm69001', 'นาย'),
('nm45003', 'นางสาว'),
('nm64009', 'นางสาว'),
('nm45004', 'นาง'),
('nm63005', 'นาย'),
('nm54001', 'นาง'),
('nm62003', 'นาง'),
('nm45005', 'นาย'),
('nm65002', 'นางสาว'),
('nm40001', 'นาง'),
('nm40002', 'นางสาว'),
('nm55003', 'นางสาว'),
('nm45001', 'นาง'),
('nm49001', 'นาง'),
('nm45006', 'นาง'),
('nm43001', 'นาง'),
('nm55004', 'นาง'),
('nm60002', 'นาง'),
('nm50001', 'นาง'),
('nm53001', 'นาง'),
('nm66001', 'นาง'),
('nm65003', 'นาย'),
('nm66005', 'นางสาว'),
('nm68004', 'นางสาว'),
('nm68001', 'นางสาว'),
('nm69004', 'นางสาว'),
('nm69008', 'นางสาว'),
('nm47002', 'นาง'),
('nm63006', 'นางสาว'),
('nm64004', 'นาย'),
('nm64005', 'นาย'),
('nm64006', 'นาง'),
('nm64007', 'นางสาว'),
('nm64001', 'นาง'),
('nm64008', 'นางสาว'),
('nm66003', 'นางสาว'),
('nm66004', 'นาง'),
('nm66006', 'นาย'),
('nm69005', 'นาย'),
('nm68005', 'นาย'),
('nm69003', 'นาย'),
('nm69002', 'นาย'),
('nm69006', 'นาย'),
('nm69007', 'นาย');

-- Stop the whole transaction if any username is missing or ambiguous.
DO $validation$
DECLARE invalid_usernames text;
BEGIN
    SELECT string_agg(m.username, ', ' ORDER BY m.username)
    INTO invalid_usernames
    FROM hop_prefix_updates m
    WHERE (SELECT count(*) FROM public.users u WHERE u.username = m.username) <> 1;

    IF invalid_usernames IS NOT NULL THEN
        RAISE EXCEPTION 'Expected exactly one user for each username. Missing or duplicate usernames: %', invalid_usernames;
    END IF;
END
$validation$;

-- Preview old and new values in the result grid.
SELECT u.employee_code, u.username, u.fullname,
       u.name_prefix AS old_name_prefix, m.name_prefix AS new_name_prefix
FROM public.users u
JOIN hop_prefix_updates m ON m.username = u.username
ORDER BY u.username;

-- Repeat executions do not rewrite values that already match.
UPDATE public.users AS u
SET name_prefix = m.name_prefix
FROM hop_prefix_updates AS m
WHERE u.username = m.username
  AND u.name_prefix IS DISTINCT FROM m.name_prefix
RETURNING u.employee_code, u.name_prefix, u.fullname, u.username;

-- Verify all supplied usernames have the requested prefix before committing.
SELECT count(*) AS requested_users,
       count(*) FILTER (WHERE u.name_prefix = m.name_prefix) AS matching_users
FROM hop_prefix_updates m
JOIN public.users u ON u.username = m.username;

COMMIT;

-- On any error, execute ROLLBACK; before correcting and rerunning the script.
