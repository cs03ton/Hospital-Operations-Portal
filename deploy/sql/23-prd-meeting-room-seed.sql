-- Meeting-room master data seed for DBeaver/PostgreSQL.
-- Room details were read from the local hop-postgres Docker database on 2026-09-24.
-- Run the entire file with Stop on error. On failure, ROLLBACK in this connection.
BEGIN;

DO $precheck$
BEGIN
    IF to_regclass('public.meeting_rooms') IS NULL THEN
        RAISE EXCEPTION 'meeting_rooms does not exist; apply meeting-room migrations first';
    END IF;
    IF to_regclass('public."__EFMigrationsHistory"') IS NULL OR NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20260914150000_AddMeetingRoomBooking'
    ) THEN
        RAISE EXCEPTION 'Meeting-room booking migration is not recorded';
    END IF;
END $precheck$;

CREATE TEMP TABLE meeting_room_seed_input (
    code varchar(50) NOT NULL,
    name varchar(200) NOT NULL,
    location varchar(500) NOT NULL,
    capacity integer NOT NULL,
    is_active boolean NOT NULL DEFAULT true
) ON COMMIT DROP;

INSERT INTO meeting_room_seed_input (code, name, location, capacity, is_active)
VALUES
    ('MEET-001', 'ห้องประชุมบริหาร', 'อาคารชั้น 2', 40, true),
    ('MEET-002', 'ห้องประชุมส่งเสริม', 'อาคารส่งเสริม', 10, true);

DO $validate$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM meeting_room_seed_input) THEN
        RAISE EXCEPTION 'No rooms supplied. Fill meeting_room_seed_input before running';
    END IF;
    IF EXISTS (SELECT 1 FROM meeting_room_seed_input
               WHERE code <> btrim(code) OR name <> btrim(name) OR location <> btrim(location)
                  OR btrim(code) = '' OR btrim(name) = '' OR btrim(location) = '' OR capacity < 1) THEN
        RAISE EXCEPTION 'Room code, name, location or capacity is invalid';
    END IF;
    IF EXISTS (SELECT 1 FROM meeting_room_seed_input GROUP BY lower(code) HAVING count(*) > 1) THEN
        RAISE EXCEPTION 'Duplicate room code in seed input';
    END IF;
    IF EXISTS (
        SELECT 1 FROM meeting_room_seed_input s JOIN meeting_rooms r ON lower(r.code) = lower(s.code)
        WHERE r.code <> s.code OR r.name <> s.name OR r.location <> s.location
           OR r.capacity <> s.capacity OR r.is_active <> s.is_active
    ) THEN
        RAISE EXCEPTION 'An existing room with the same code differs; review it manually rather than overwriting';
    END IF;
END $validate$;

-- Review these rows before COMMIT when running the script in DBeaver.
SELECT s.code, s.name, s.location, s.capacity, s.is_active,
       CASE WHEN r.id IS NULL THEN 'INSERT' ELSE 'ALREADY_MATCHES' END AS action
FROM meeting_room_seed_input s
LEFT JOIN meeting_rooms r ON r.code = s.code
ORDER BY s.code;

INSERT INTO meeting_rooms
    (id, code, name, location, capacity, is_active, concurrency_token, created_at, updated_at)
SELECT gen_random_uuid(), s.code, s.name, s.location, s.capacity, s.is_active,
       gen_random_uuid(), now(), now()
FROM meeting_room_seed_input s
WHERE NOT EXISTS (SELECT 1 FROM meeting_rooms r WHERE r.code = s.code);

DO $verify$
BEGIN
    IF EXISTS (
        SELECT 1 FROM meeting_room_seed_input s
        LEFT JOIN meeting_rooms r ON r.code = s.code
        WHERE r.id IS NULL OR r.name <> s.name OR r.location <> s.location
           OR r.capacity <> s.capacity OR r.is_active <> s.is_active
    ) THEN
        RAISE EXCEPTION 'Meeting-room seed verification failed';
    END IF;
END $verify$;

SELECT r.id, r.code, r.name, r.location, r.capacity, r.is_active
FROM meeting_rooms r JOIN meeting_room_seed_input s ON s.code = r.code
ORDER BY r.code;

COMMIT;
