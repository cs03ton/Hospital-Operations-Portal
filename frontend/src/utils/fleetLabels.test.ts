import { describe, expect, it } from "vitest";
import { getFleetStatusLabel } from "./fleetLabels";

describe("getFleetStatusLabel", () => {
  it.each([
    ["DRAFT", "แบบร่าง"],
    ["PENDING_DISPATCH", "รอจัดรถและคนขับ"],
    ["PENDING_ADMIN_REVIEW", "รอหัวหน้าฝ่ายบริหารตรวจสอบ"],
    ["PENDING_DIRECTOR", "รอผู้อำนวยการอนุมัติ"],
    ["PENDING_DRIVER_ACK", "รอคนขับรับทราบ"],
    ["READY", "พร้อมเดินทาง"],
    ["IN_PROGRESS", "กำลังปฏิบัติงาน"],
    ["COMPLETED", "เสร็จสิ้น"],
    ["ASSIGNED", "จัดรถและคนขับแล้ว"],
    ["REPLACED", "เปลี่ยนรถหรือคนขับแล้ว"],
    ["AVAILABLE", "พร้อมใช้งาน"],
    ["MAINTENANCE", "อยู่ระหว่างบำรุงรักษา"],
    ["TEMPORARILY_UNAVAILABLE", "ไม่พร้อมใช้งานชั่วคราว"],
    ["MATCH", "เหมาะสม"],
    ["PARTIAL_MATCH", "เหมาะสมบางส่วน"],
    ["NOT_MATCH", "ไม่เหมาะสม"],
    ["NORMAL", "ปกติ"],
    ["URGENT", "เร่งด่วน"],
    ["EMERGENCY", "ฉุกเฉิน"],
  ])("แปลสถานะ %s เป็นภาษาไทย", (status, expected) => {
    expect(getFleetStatusLabel(status)).toBe(expected);
  });
});
