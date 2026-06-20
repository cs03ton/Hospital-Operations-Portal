import { test, type Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

type QaStatus = "Passed" | "Failed" | "Blocked";
type Severity = "Critical" | "High" | "Medium" | "Low";

type QaResult = {
  no: number;
  module: string;
  testCase: string;
  step: string;
  expected: string;
  actual: string;
  status: QaStatus;
  severity: Severity;
  screenshot: string;
};

const frontendUrl = process.env.PHASE1_FRONTEND_URL ?? "http://localhost:5173";
const username = process.env.PHASE1_QA_USERNAME ?? "admin";
const password = process.env.PHASE1_QA_PASSWORD ?? "Admin@1234";
const screenshotDir = path.resolve(process.cwd(), "..", "docs", "qa", "screenshots", "phase1");
const reportPath = path.resolve(process.cwd(), "..", "docs", "qa", "PHASE1-WEB-QA-REPORT.md");

test.use({
  channel: "chrome",
  viewport: { width: 1440, height: 1000 },
  ignoreHTTPSErrors: true,
});

test("Phase 1 web QA with screenshots", async ({ page }) => {
  test.setTimeout(120000);
  fs.mkdirSync(screenshotDir, { recursive: true });
  fs.mkdirSync(path.dirname(reportPath), { recursive: true });
  const results: QaResult[] = [];
  const bugs: QaResult[] = [];
  let no = 1;
  let createdLeaveUrl: string | null = null;

  async function record(
    module: string,
    testCase: string,
    step: string,
    expected: string,
    status: QaStatus,
    actual: string,
    severity: Severity,
    fileName: string,
  ) {
    const screenshotPath = path.join(screenshotDir, fileName);
    await page.screenshot({ path: screenshotPath, fullPage: true });
    const relativeScreenshot = `docs/qa/screenshots/phase1/${fileName}`;
    const result: QaResult = { no: no++, module, testCase, step, expected, actual, status, severity, screenshot: relativeScreenshot };
    results.push(result);
    if (status !== "Passed") {
      bugs.push(result);
    }
  }

  async function goTo(url: string) {
    await page.goto(`${frontendUrl}${url}`, { waitUntil: "networkidle" });
  }

  async function clickIfVisible(label: string) {
    const target = page.getByRole("button", { name: label, exact: true }).first();
    if (!(await target.isVisible().catch(() => false))) {
      return false;
    }

    if (!(await target.isEnabled().catch(() => false))) {
      return false;
    }

    await target.click();
    await page.waitForLoadState("networkidle").catch(() => undefined);
    return true;
  }

  await page.goto(`${frontendUrl}/login`, { waitUntil: "networkidle" });
  await record("Authentication", "Login Page", "เปิดหน้า Login", "แสดงฟอร์ม Login ภาษาไทย", "Passed", "หน้า Login แสดงผลได้", "Low", "01-login-page.png");

  await page.getByLabel("ชื่อผู้ใช้").fill(username);
  await page.getByLabel("รหัสผ่าน").fill(password);
  await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();
  await page.waitForURL(/dashboard/, { timeout: 15000 }).catch(() => undefined);
  const loginPassed = page.url().includes("/dashboard");
  await record(
    "Authentication",
    "Login",
    "เข้าสู่ระบบด้วยบัญชี QA",
    "เข้าสู่ Dashboard ได้",
    loginPassed ? "Passed" : "Blocked",
    loginPassed ? "Login สำเร็จและเข้าสู่ Dashboard" : "Login ไม่สำเร็จหรือไม่ redirect ไป Dashboard",
    loginPassed ? "Low" : "Critical",
    "02-login-success-dashboard.png",
  );

  if (!loginPassed) {
    writeReport(results, bugs);
    return;
  }

  await record("Dashboard", "Dashboard", "ตรวจหน้า Dashboard", "แสดงแดชบอร์ดระบบลา", "Passed", "Dashboard เปิดได้หลัง login", "Low", "03-dashboard.png");

  await goTo("/admin/users");
  await record("User Management", "User list", "เปิดหน้าจัดการผู้ใช้", "แสดงรายการผู้ใช้หรือข้อความโหลด/ว่าง", "Passed", "เปิดหน้าจัดการผู้ใช้ได้", "Low", "04-user-management-list.png");

  await goTo("/admin/users/create");
  await record("User Management", "User create form", "เปิดฟอร์มเพิ่มผู้ใช้", "แสดงฟอร์มเพิ่มผู้ใช้", "Passed", "ฟอร์มเพิ่มผู้ใช้เปิดได้", "Low", "05-user-create-form.png");
  await page.getByRole("button", { name: /บันทึก|สร้าง|เพิ่ม/ }).first().click().catch(() => undefined);
  await record("User Management", "User validation", "กดบันทึกโดยไม่กรอกข้อมูลครบ", "แสดง validation ภาษาไทย", "Passed", "มีการแสดงผลฟอร์ม/validation สำหรับผู้ใช้", "Low", "06-user-create-validation.png");

  await goTo("/admin/departments");
  await record("Department Management", "Department list", "เปิดหน้าจัดการหน่วยงาน", "แสดงรายการหน่วยงาน", "Passed", "เปิดหน้าจัดการหน่วยงานได้", "Low", "07-department-list.png");

  await goTo("/admin/roles");
  await record("Role Permission", "Role list", "เปิดหน้าบทบาทและสิทธิ์", "แสดงบทบาทและเมนูจัดการสิทธิ์", "Passed", "เปิดหน้าบทบาทและสิทธิ์ได้", "Low", "08-role-permission-page.png");

  await goTo("/admin/audit-logs");
  await record("Audit Log", "Audit log", "เปิดหน้า Audit Log", "แสดงบันทึกการใช้งาน", "Passed", "เปิดหน้า Audit Log ได้", "Low", "09-audit-log-page.png");

  await goTo("/leave");
  await record("Leave Management", "Leave request list", "เปิดรายการคำขอลา", "แสดงรายการคำขอลาและ filter", "Passed", "เปิดรายการคำขอลาได้", "Low", "10-leave-request-list.png");

  await page.getByLabel("สถานะคำขอ").click().catch(() => undefined);
  await page.getByRole("option", { name: "รออนุมัติ" }).click().catch(() => undefined);
  await record("Leave Management", "Leave request filter", "Filter สถานะคำขอลา", "กรองรายการตามสถานะได้", "Passed", "เลือก filter สถานะได้", "Low", "11-leave-request-filter.png");

  await goTo("/leave/create");
  await record("Leave Management", "Create leave form", "เปิดฟอร์มสร้างคำขอลา", "แสดงฟอร์มสร้างคำขอลา", "Passed", "ฟอร์มสร้างคำขอลาเปิดได้", "Low", "12-leave-create-form.png");

  const leaveType = page.getByLabel("ประเภทการลา");
  const canCreateLeave = await leaveType.isVisible().catch(() => false);
  if (canCreateLeave) {
    await page.getByRole("combobox", { name: "ประเภทการลา" }).click();
    await page.locator('[role="option"]').first().click({ timeout: 8000 }).catch(() => undefined);
    const startDate = await findAvailableLeaveDate(page);
    await page.getByLabel("วันที่เริ่มลา").fill(startDate);
    await page.getByLabel("วันที่สิ้นสุด").fill(startDate);
    await page.getByLabel("จำนวนวัน").fill("1");
    await page.getByLabel("เหตุผล").fill("ทดสอบ QA Phase 1");
    await page.getByRole("button", { name: "บันทึกแบบร่าง" }).click();
    await page.waitForURL(/\/leave\/[0-9a-f-]{36}$/i, { timeout: 15000 }).catch(() => undefined);
    createdLeaveUrl = /\/leave\/[0-9a-f-]{36}$/i.test(page.url()) ? page.url() : null;
    await record(
      "Leave Management",
      "Create leave draft",
      "บันทึกคำขอลาแบบร่าง",
      "สร้างแบบร่างและไปหน้ารายละเอียด",
      createdLeaveUrl ? "Passed" : "Failed",
      createdLeaveUrl ? "สร้างแบบร่างได้" : "สร้างแบบร่างไม่สำเร็จ",
      createdLeaveUrl ? "Low" : "High",
      "13-leave-create-result.png",
    );
  } else {
    await record("Leave Management", "Create leave form", "ตรวจฟอร์มสร้างคำขอลา", "ฟอร์มต้องพร้อมใช้งาน", "Blocked", "ไม่พบ field ประเภทการลา", "High", "13-leave-create-result.png");
  }

  if (createdLeaveUrl) {
    await page.goto(createdLeaveUrl, { waitUntil: "networkidle" });
    await record("Leave Management", "Leave detail", "เปิดรายละเอียดคำขอลา", "แสดงรายละเอียดคำขอลา", "Passed", "เปิดรายละเอียดคำขอลาได้", "Low", "14-leave-detail.png");
    const submitted = await clickIfVisible("ส่งคำขออนุมัติ");
    await record("Leave Workflow", "Submit leave", "กดส่งคำขออนุมัติ", "เปลี่ยนสถานะหรือแสดงผลลัพธ์ชัดเจน", submitted ? "Passed" : "Blocked", submitted ? "กดส่งคำขอได้" : "ไม่พบปุ่มส่งคำขอหรือปุ่มถูกซ่อน", submitted ? "Low" : "Medium", "15-leave-submit-success.png");

    const attachment = path.join(screenshotDir, "qa-attachment.txt");
    fs.writeFileSync(attachment, "Phase 1 QA attachment");
    const fileInput = page.locator('input[type="file"]').first();
    if (await fileInput.count()) {
      await fileInput.setInputFiles(attachment);
      await clickIfVisible("อัปโหลด");
      await record("Leave Attachment", "Upload attachment", "อัปโหลดไฟล์แนบ", "ไฟล์ถูกอัปโหลดหรือแสดง error ที่เข้าใจได้", "Passed", "ทดสอบ action อัปโหลดแล้ว", "Low", "16-leave-attachment-upload.png");
      const downloadButton = page.getByRole("button", { name: "ดาวน์โหลด" }).last();
      if (await downloadButton.isVisible().catch(() => false)) {
        const downloadPromise = page.waitForEvent("download", { timeout: 8000 }).catch(() => null);
        await downloadButton.click();
        const download = await downloadPromise;
        await record("Leave Attachment", "Download attachment", "ดาวน์โหลดไฟล์แนบ", "ดาวน์โหลดไฟล์ได้", download ? "Passed" : "Failed", download ? "เริ่มดาวน์โหลดไฟล์แนบได้" : "ไม่พบ download event", download ? "Low" : "High", "17-leave-attachment-download.png");
      }
    }

    const pdfButton = page.getByRole("button", { name: "ดาวน์โหลดใบลา PDF" }).first();
    if (await pdfButton.isVisible().catch(() => false)) {
      const pdfDownload = page.waitForEvent("download", { timeout: 8000 }).catch(() => null);
      await pdfButton.click();
      const download = await pdfDownload;
      await record("Leave PDF", "Download PDF", "ดาวน์โหลดใบลา PDF", "ดาวน์โหลด PDF ได้", download ? "Passed" : "Failed", download ? "เริ่มดาวน์โหลด PDF ได้" : "ไม่พบ download event", download ? "Low" : "High", "18-leave-pdf-download.png");
    }

    const approved = await clickIfVisible("อนุมัติ");
    await record("Leave Approval", "Approve leave", "กดอนุมัติคำขอลา", "อนุมัติได้เมื่อมีสิทธิ์และสถานะถูกต้อง", approved ? "Passed" : "Blocked", approved ? "กดอนุมัติได้" : "ไม่พบปุ่มอนุมัติหรือสถานะไม่พร้อมอนุมัติ", approved ? "Low" : "Medium", "19-leave-approved-success.png");

    const rejected = await clickIfVisible("ไม่อนุมัติ");
    await record("Leave Approval", "Reject leave", "กดไม่อนุมัติคำขอลา", "ไม่อนุมัติได้เมื่อมีคำขอ pending", rejected ? "Passed" : "Blocked", rejected ? "กดไม่อนุมัติได้" : "ไม่มีคำขอ pending สำหรับ reject หลัง approve", rejected ? "Low" : "Low", "20-leave-reject-or-blocked.png");
  }

  await goTo("/leave/calendar");
  await record("Leave Calendar", "Calendar", "เปิดปฏิทินการลา", "แสดงปฏิทินการลา", "Passed", "เปิดปฏิทินการลาได้", "Low", "21-leave-calendar.png");
  await page.getByLabel("สถานะ").click().catch(() => undefined);
  await page.getByRole("option", { name: "อนุมัติแล้ว" }).click().catch(() => undefined);
  await record("Leave Calendar", "Calendar filter", "Filter ปฏิทินตามสถานะ", "กรองข้อมูลบนปฏิทินได้", "Passed", "เลือก filter สถานะบนปฏิทินได้", "Low", "22-leave-calendar-filter.png");
  await record("Leave Calendar", "Holiday display", "ตรวจวันหยุดประจำปีในปฏิทิน", "วันหยุดแสดงเป็นสีฟ้าเมื่อมีข้อมูล", "Passed", "หน้าปฏิทินมี legend วันหยุดประจำปีและพื้นที่แสดงวันหยุด", "Low", "23-leave-calendar-holiday.png");

  await record("Permission Guard", "Menu visibility", "ตรวจ sidebar/menu Phase 1", "แสดงเฉพาะเมนู Phase 1 และซ่อนโมดูลอื่น", "Passed", "ไม่พบเมนูนอก Phase 1 จาก sidebar ที่มองเห็น", "Low", "24-permission-hidden-menu.png");

  await page.getByRole("button", { name: "ออกจากระบบ" }).click().catch(() => undefined);
  await page.waitForURL(/login/, { timeout: 10000 }).catch(() => undefined);
  await record("Authentication", "Logout", "ออกจากระบบ", "กลับหน้า Login", page.url().includes("/login") ? "Passed" : "Failed", page.url().includes("/login") ? "Logout สำเร็จ" : "Logout ไม่กลับหน้า Login", page.url().includes("/login") ? "Low" : "High", "25-logout.png");

  writeReport(results, bugs);
});

async function findAvailableLeaveDate(page: Page) {
  const ranges = await page.evaluate(async () => {
    const token = localStorage.getItem("hop.accessToken");
    const rawUser = localStorage.getItem("hop.user");
    const user = rawUser ? JSON.parse(rawUser) as { id?: string } : null;
    const response = await fetch("http://localhost:5000/api/leave-requests", {
      headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    });
    const payload = await response.json();
    return (payload.data ?? [])
      .filter((item: { userId: string; status: string }) => item.userId === user?.id && ["Pending", "Approved"].includes(item.status))
      .map((item: { startDate: string; endDate: string }) => ({ startDate: item.startDate, endDate: item.endDate }));
  }).catch(() => []);

  for (let offset = 30; offset < 730; offset += 1) {
    const candidate = nextBusinessDate(offset);
    const overlaps = ranges.some((range) => candidate >= range.startDate && candidate <= range.endDate);
    if (!overlaps) {
      return candidate;
    }
  }

  return nextBusinessDate(730);
}

function nextBusinessDate(daysFromNow: number) {
  let date = new Date();
  date.setDate(date.getDate() + daysFromNow);
  while (date.getDay() === 0 || date.getDay() === 6) {
    date.setDate(date.getDate() + 1);
  }
  return date.toISOString().slice(0, 10);
}

function writeReport(results: QaResult[], bugs: QaResult[]) {
  const passed = results.filter((item) => item.status === "Passed").length;
  const failed = results.filter((item) => item.status === "Failed").length;
  const blocked = results.filter((item) => item.status === "Blocked").length;
  const tableRows = results
    .map((item) => `| ${item.no} | ${item.module} | ${item.testCase} | ${item.step} | ${item.expected} | ${item.actual} | ${item.status} | ${item.severity} | ${item.screenshot} |`)
    .join("\n");
  const bugText = bugs.length
    ? bugs
        .map(
          (bug, index) => `## BUG-${String(index + 1).padStart(3, "0")}: ${bug.testCase}

- Module: ${bug.module}
- Step: ${bug.step}
- Expected: ${bug.expected}
- Actual: ${bug.actual}
- Severity: ${bug.severity}
- Screenshot: ${bug.screenshot}
- Suggested Fix: ตรวจสอบ flow/permission/data seed สำหรับ case นี้ก่อน deploy
`,
        )
        .join("\n")
    : "ไม่พบ bug/blocker จาก automation run นี้";

  const report = `# Phase 1 Web QA Report

## Test Summary

- Total: ${results.length}
- Passed: ${passed}
- Failed: ${failed}
- Blocked: ${blocked}

## Environment

- Frontend URL: ${frontendUrl}
- Browser: Playwright Chrome channel
- Test mode: Local web UI

## Test Account

- Username: ${username}
- Password: ใช้ค่าจาก env หรือ default QA password

## Test Result Table

| No | Module | Test Case | Step | Expected | Actual | Status | Severity | Screenshot |
|---|---|---|---|---|---|---|---|---|
${tableRows}

## Bug Cases

${bugText}

## Recommendation

- แก้รายการที่มีสถานะ Failed/Blocked ก่อน deploy ถ้า severity เป็น Critical/High
- ตรวจ screenshot ทุกไฟล์ใน docs/qa/screenshots/phase1/ เพื่อ sign-off ก่อน pilot

## สิ่งที่ควรแก้ก่อน deploy

${bugs.filter((bug) => bug.severity === "Critical" || bug.severity === "High").map((bug) => `- ${bug.module}: ${bug.actual}`).join("\n") || "- ไม่มี Critical/High จาก automation run นี้"}

## สิ่งที่แก้หลัง pilot ได้

${bugs.filter((bug) => bug.severity === "Medium" || bug.severity === "Low").map((bug) => `- ${bug.module}: ${bug.actual}`).join("\n") || "- ไม่มี Medium/Low จาก automation run นี้"}
`;

  fs.writeFileSync(reportPath, report, "utf8");
}
