\set ON_ERROR_STOP on

BEGIN;

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
ON CONFLICT (registration_number) DO UPDATE SET
    vehicle_code = EXCLUDED.vehicle_code,
    registration_province = EXCLUDED.registration_province,
    vehicle_type_id = EXCLUDED.vehicle_type_id,
    brand = EXCLUDED.brand,
    model = EXCLUDED.model,
    manufacture_year = EXCLUDED.manufacture_year,
    seat_capacity_total = EXCLUDED.seat_capacity_total,
    passenger_capacity = EXCLUDED.passenger_capacity,
    fuel_type = EXCLUDED.fuel_type,
    status = EXCLUDED.status,
    is_active = EXCLUDED.is_active,
    note = EXCLUDED.note,
    updated_at = NOW();

COMMIT;

SELECT vehicle_code, registration_number, registration_province, status, is_active
FROM fleet_vehicles
WHERE vehicle_code LIKE 'NMH-FLEET-%'
ORDER BY vehicle_code;
