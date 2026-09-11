CREATE TABLE departments(id uuid PRIMARY KEY,name text);
CREATE TABLE users(id uuid PRIMARY KEY,username text UNIQUE,department_id uuid REFERENCES departments(id),note text);
CREATE TABLE roles(id uuid PRIMARY KEY,name text);
CREATE TABLE user_roles(user_id uuid REFERENCES users(id),role_id uuid REFERENCES roles(id),PRIMARY KEY(user_id,role_id));
CREATE TABLE leave_requests(id uuid PRIMARY KEY,user_id uuid REFERENCES users(id),current_approver_id uuid REFERENCES users(id),request_number text);
CREATE TABLE leave_approvals(id uuid PRIMARY KEY,leave_request_id uuid REFERENCES leave_requests(id),approver_id uuid REFERENCES users(id));
CREATE TABLE leave_attachments(id uuid PRIMARY KEY,leave_request_id uuid REFERENCES leave_requests(id),uploaded_by_user_id uuid REFERENCES users(id),file_path text);
CREATE TABLE leave_balances(id uuid PRIMARY KEY,user_id uuid REFERENCES users(id),used_days numeric);
CREATE TABLE audit_logs(id uuid PRIMARY KEY,user_id uuid REFERENCES users(id),entity_name text,entity_id text,action text,detail text);
CREATE TABLE notifications(id uuid PRIMARY KEY,user_id uuid REFERENCES users(id),reference_id text,action_url text);
CREATE TABLE fleet_requests(id uuid PRIMARY KEY,created_by_user_id uuid REFERENCES users(id),note text);
CREATE TABLE extra_soft_refs(id uuid PRIMARY KEY,payload text);
CREATE TABLE approval_chains(id uuid PRIMARY KEY,department_id uuid REFERENCES departments(id));
CREATE TABLE approval_chain_steps(id uuid PRIMARY KEY,approval_chain_id uuid REFERENCES approval_chains(id),approver_user_id uuid REFERENCES users(id));
ALTER TABLE users ADD approval_chain_id uuid REFERENCES approval_chains(id);
ALTER TABLE leave_requests ADD approval_chain_id uuid REFERENCES approval_chains(id);
CREATE TABLE announcements(id uuid PRIMARY KEY,title text);
CREATE TABLE announcement_reads(id uuid PRIMARY KEY,user_id uuid REFERENCES users(id),announcement_id uuid REFERENCES announcements(id));
CREATE TABLE announcement_notification_deliveries(id uuid PRIMARY KEY,user_id uuid REFERENCES users(id),announcement_id uuid REFERENCES announcements(id));
CREATE TABLE line_delivery_logs(id uuid PRIMARY KEY,recipient_user_id uuid REFERENCES users(id),leave_request_id uuid REFERENCES leave_requests(id));
INSERT INTO departments VALUES ('20000000-0000-0000-0000-000000000001','Information Technology'),('20000000-0000-0000-0000-000000000002','Real department');
INSERT INTO users(id,username,department_id,note) VALUES
('10000000-0000-0000-0000-000000000001','staff03','20000000-0000-0000-0000-000000000001','test'),
('10000000-0000-0000-0000-000000000002','nm69003','20000000-0000-0000-0000-000000000001','test'),
('10000000-0000-0000-0000-000000000003','nm69001','20000000-0000-0000-0000-000000000001','test'),
('10000000-0000-0000-0000-000000000004','head01','20000000-0000-0000-0000-000000000001','test'),
('10000000-0000-0000-0000-000000000005','director01','20000000-0000-0000-0000-000000000001','test'),
('10000000-0000-0000-0000-000000000006','admin_support','20000000-0000-0000-0000-000000000001','test'),
('10000000-0000-0000-0000-000000000007','staff02','20000000-0000-0000-0000-000000000001','test'),
('10000000-0000-0000-0000-000000000008','staff01','20000000-0000-0000-0000-000000000001','test'),
('10000000-0000-0000-0000-000000000099','real_user','20000000-0000-0000-0000-000000000002','KEEP');
INSERT INTO roles VALUES('30000000-0000-0000-0000-000000000001','Staff');
INSERT INTO user_roles SELECT id,'30000000-0000-0000-0000-000000000001'::uuid FROM users;
INSERT INTO leave_requests(id,user_id,current_approver_id,request_number) VALUES
('40000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000099','TEST-001'),
('40000000-0000-0000-0000-000000000099','10000000-0000-0000-0000-000000000099',null,'REAL-001');
INSERT INTO leave_approvals VALUES('50000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000099');
INSERT INTO leave_attachments VALUES('60000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000001','uploads/test.pdf');
INSERT INTO leave_balances SELECT id,id,7 FROM users;
INSERT INTO audit_logs(id,user_id,entity_name,entity_id,action) VALUES('70000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000099','LeaveRequest','40000000-0000-0000-0000-000000000001','Approve');
INSERT INTO audit_logs VALUES('70000000-0000-0000-0000-000000000099','10000000-0000-0000-0000-000000000099','LeaveRequest','40000000-0000-0000-0000-000000000099','Approve','KEEP');
INSERT INTO notifications VALUES('80000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000099','40000000-0000-0000-0000-000000000001','/leave/40000000-0000-0000-0000-000000000001');
INSERT INTO fleet_requests VALUES('90000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000099','KEEP');
INSERT INTO approval_chains VALUES('a0000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000001');
INSERT INTO approval_chain_steps VALUES('a1000000-0000-0000-0000-000000000001','a0000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000004');
INSERT INTO announcements VALUES('b0000000-0000-0000-0000-000000000001','KEEP');
INSERT INTO announcement_reads SELECT gen_random_uuid(),id,'b0000000-0000-0000-0000-000000000001'::uuid FROM users;
INSERT INTO announcement_notification_deliveries SELECT gen_random_uuid(),id,'b0000000-0000-0000-0000-000000000001'::uuid FROM users;
INSERT INTO notifications SELECT gen_random_uuid(),id,'b0000000-0000-0000-0000-000000000001','/announcements/b0000000-0000-0000-0000-000000000001' FROM users;
INSERT INTO line_delivery_logs SELECT gen_random_uuid(),id,null FROM users;
