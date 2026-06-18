import { PlaceholderModule } from "../components/PlaceholderModule";

export function AdministrationPage() {
  return (
    <PlaceholderModule
      title="ตั้งค่าระบบ"
      subtitle="โครงหน้าสำหรับตั้งค่าผู้ดูแลระบบและตรวจสอบระบบ"
      items={["จัดการผู้ใช้", "จัดการบทบาทและสิทธิ์", "ตรวจสอบประวัติการใช้งาน"]}
    />
  );
}
