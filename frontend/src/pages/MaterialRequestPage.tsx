import { PlaceholderModule } from "../components/PlaceholderModule";

export function MaterialRequestPage() {
  return (
    <PlaceholderModule
      title="ระบบเบิกวัสดุ"
      subtitle="โครงหน้าสำหรับขอเบิกวัสดุและติดตามสถานะ"
      items={["แบบฟอร์มขอเบิก", "สถานะการอนุมัติ", "ประวัติการขอเบิก"]}
    />
  );
}
