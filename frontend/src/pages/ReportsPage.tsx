import { PlaceholderModule } from "../components/PlaceholderModule";

export function ReportsPage() {
  return (
    <PlaceholderModule
      title="รายงาน"
      subtitle="โครงหน้าสำหรับรายงานปฏิบัติการและสรุปผู้บริหาร"
      items={["รายงานมาตรฐาน", "สถิติรายเดือน", "สรุปข้อมูลสำหรับส่งออก"]}
    />
  );
}
