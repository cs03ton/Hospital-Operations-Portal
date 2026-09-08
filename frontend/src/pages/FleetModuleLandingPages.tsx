import { Card, CardContent, Stack, Typography } from "@mui/material";
import { PageHeader } from "../components/PageHeader";
import { FleetPermissionGuard } from "../routes/FleetPermissionGuard";
import { FleetWorkflowQueuePage } from "./FleetWorkflowQueuePage";
import { FleetReportsPage } from "./FleetReportsPage";

export function FleetReportsLandingPage() {
  return <FleetReportsPage />;
}

export function FleetSettingsLandingPage() {
  return <Stack spacing={2}><PageHeader title="ตั้งค่า Fleet" subtitle="ศูนย์รวมการตั้งค่าและข้อมูลหลักของระบบรถ" /><Card><CardContent><Typography color="text.secondary">รายการตั้งค่าจะแสดงตามสิทธิ์ย่อยใน Milestone ถัดไป</Typography></CardContent></Card></Stack>;
}

export function FleetApprovalsLandingPage() {
  return <Stack spacing={3}>
    <FleetPermissionGuard denyMode="hide" permissions={["FleetAdminReview.Approve", "FleetAdminReview.Return", "FleetAdminReview.Reject"]}>
      <FleetWorkflowQueuePage kind="admin-review" />
    </FleetPermissionGuard>
    <FleetPermissionGuard denyMode="hide" permissions={["FleetDirector.Approve", "FleetDirector.Return", "FleetDirector.Reject"]}>
      <FleetWorkflowQueuePage kind="director-approval" />
    </FleetPermissionGuard>
  </Stack>;
}
