namespace Hop.Api.Data;

// Versioned additive seed; never assign technicians to user accounts automatically.
public static class RepairSeed
{
    public const string Sql = """
        INSERT INTO repair_teams(code,name,responsible_department) VALUES
         ('IT','IT','กลุ่มงานสุขภาพดิจิทัล'),('GENERAL','ช่างทั่วไป','กลุ่มงานบริหาร')
         ON CONFLICT (code) DO NOTHING;
        INSERT INTO repair_categories(id,name,team_code,is_active,concurrency_token)
         SELECT gen_random_uuid(),v.name,v.team,true,gen_random_uuid() FROM (VALUES
         ('คอมพิวเตอร์','IT'),('เครื่องพิมพ์','IT'),('เครือข่าย','IT'),('โปรแกรม','IT'),('เครื่องมือแพทย์','IT'),
         ('ไฟฟ้า','GENERAL'),('ประปา','GENERAL'),('เครื่องปรับอากาศ','GENERAL'),('อาคาร','GENERAL'),
         ('สุขภัณฑ์','GENERAL'),('อุปกรณ์ทั่วไป','GENERAL')) v(name,team)
         ON CONFLICT (name) DO NOTHING;
        INSERT INTO roles(id,name,description,is_system_role,is_active,created_at)
         SELECT gen_random_uuid(),v.name,'Repair team access',true,true,now()
         FROM (VALUES ('ช่าง IT'),('ช่างทั่วไป')) v(name) WHERE NOT EXISTS(SELECT 1 FROM roles r WHERE r.name=v.name);
        INSERT INTO permissions(id,code,name,group_name,action,is_active,created_at)
         SELECT gen_random_uuid(),'RepairManagement.'||v.action,v.label,'RepairManagement',v.action,true,now()
         FROM (VALUES ('ViewOwn','ดูงานแจ้งซ่อมของตนเอง'),('Create','แจ้งซ่อม'),('WorkIT','คิวทีม IT'),
         ('WorkGeneral','คิวทีมช่างทั่วไป'),('ViewAll','ดูงานซ่อมทั้งหมด'),('Manage','จัดการระบบแจ้งซ่อม')) v(action,label)
         ON CONFLICT (code) DO NOTHING;
        INSERT INTO role_permissions(role_id,permission_id)
         SELECT r.id,p.id FROM roles r CROSS JOIN permissions p WHERE r.is_active AND p.is_active AND (
         (r.name IN('Staff','DepartmentHead','Director','Admin','SuperAdmin','LeaveAdmin','FleetAdminReviewer','พนักงานขับรถ','ช่าง IT','ช่างทั่วไป')
         AND p.code IN('RepairManagement.ViewOwn','RepairManagement.Create'))
         OR (r.name IN('Admin','SuperAdmin') AND p.code IN('RepairManagement.ViewAll','RepairManagement.Manage'))
         OR (r.name='ช่าง IT' AND p.code='RepairManagement.WorkIT')
         OR (r.name='ช่างทั่วไป' AND p.code='RepairManagement.WorkGeneral')
         OR (r.name IN('ช่าง IT','ช่างทั่วไป') AND p.code='Documentation.View'))
         ON CONFLICT DO NOTHING;
        INSERT INTO line_group_destinations(id,line_group_id,display_name,status,module,delivery_provider,
         attention_required,first_detected_at,last_detected_at,concurrency_token)
         SELECT gen_random_uuid(),'REPAIR_'||v.code,'แจ้งซ่อม '||v.label,'Disabled','REPAIR_'||v.code,'CUSTOM_ENDPOINT',
         false,now(),now(),gen_random_uuid() FROM (VALUES ('IT','IT'),('GENERAL','ช่างทั่วไป')) v(code,label)
         WHERE NOT EXISTS(SELECT 1 FROM line_group_destinations d WHERE d.module='REPAIR_'||v.code)
         ON CONFLICT DO NOTHING;
        """;
}
