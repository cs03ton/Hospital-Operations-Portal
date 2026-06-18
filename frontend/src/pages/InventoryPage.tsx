import { PlaceholderModule } from "../components/PlaceholderModule";

export function InventoryPage() {
  return (
    <PlaceholderModule
      title="ระบบ Inventory"
      subtitle="โครงหน้าสำหรับทะเบียนทรัพย์สินและครุภัณฑ์"
      items={["ทะเบียนทรัพย์สิน", "ข้อมูลรับประกัน", "ติดตามสถานที่ใช้งาน"]}
    />
  );
}
