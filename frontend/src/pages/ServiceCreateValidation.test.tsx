// @vitest-environment jsdom
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, afterEach, describe, expect, it, vi } from "vitest";
import { renderFleet } from "../test/renderFleet";
import { RepairCreatePage } from "./RepairPages";
import { MeetingRoomCreatePage } from "./MeetingRoomPages";
import { getMyProfile } from "../api/profileApi";
import { repairCreate, repairOptions } from "../api/repairApi";
import { createMeetingBooking, getMeetingRooms, getMeetingPersonnelOptions } from "../api/meetingRoomApi";
import { notifyGlobal } from "../contexts/NotificationContext";
vi.mock("../context/AuthContext", () => ({ useAuth: () => ({ user: { id: "author", fullname: "ผู้ขอ ทดสอบ", department: "ทดสอบ" } }) }));
vi.mock("../contexts/NotificationContext", () => ({ notifyGlobal: vi.fn() }));
vi.mock("../api/profileApi", () => ({ getMyProfile: vi.fn() }));
vi.mock("../api/repairApi", async original => ({ ...await original<typeof import("../api/repairApi")>(), repairOptions: vi.fn(), repairCreate: vi.fn() }));
vi.mock("../api/meetingRoomApi", async original => ({ ...await original<typeof import("../api/meetingRoomApi")>(), getMeetingRooms: vi.fn(), getMeetingPersonnelOptions: vi.fn(), createMeetingBooking: vi.fn() }));
vi.mock("../components/common/AppDatePicker", () => ({ AppDatePicker: ({ label, value, onChange, helperText }: { label: string; value: string; onChange: (v: string) => void; helperText?: string }) => <label>{label}<input aria-label={label} value={value} onChange={e => onChange(e.target.value)} />{helperText}</label> }));
vi.mock("../components/common/AppTimePicker", () => ({ AppTimePicker: ({ label, value, onChange, helperText }: { label: string; value: string; onChange: (v: string) => void; helperText?: string }) => <label>{label}<input aria-label={label} value={value} onChange={e => onChange(e.target.value)} />{helperText}</label> }));
const change = (label: RegExp | string, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } });
async function choose(name: string, option: RegExp) { fireEvent.mouseDown(screen.getByRole("combobox", { name: new RegExp(name) })); fireEvent.click(await screen.findByRole("option", { name: option })); }
const repairSubmit = () => fireEvent.click(screen.getByRole("button", { name: "ส่งแจ้งซ่อม" }));
const meetingSubmit = () => fireEvent.click(screen.getByRole("button", { name: "ยืนยันการจอง" }));
describe("repair and meeting-room validation focus", () => {
  beforeEach(() => {
    vi.mocked(getMyProfile).mockResolvedValue({ id: "author", fullname: "ผู้ขอ ทดสอบ", phoneNumber: "0800000000", departmentName: "ทดสอบ" } as Awaited<ReturnType<typeof getMyProfile>>);
    vi.mocked(repairOptions).mockResolvedValue({ categories: [{ id: "it", name: "คอมพิวเตอร์", teamCode: "IT" }] } as Awaited<ReturnType<typeof repairOptions>>);
    vi.mocked(repairCreate).mockResolvedValue({ id: "saved", concurrencyToken: "token" } as Awaited<ReturnType<typeof repairCreate>>);
    vi.mocked(getMeetingRooms).mockResolvedValue([{ id: "room", name: "ห้องบริหาร", capacity: 1 }] as Awaited<ReturnType<typeof getMeetingRooms>>);
    vi.mocked(getMeetingPersonnelOptions).mockResolvedValue([{ id: "other", fullName: "ผู้ร่วมประชุม", departmentName: "ทดสอบ" }]);
    vi.mocked(createMeetingBooking).mockResolvedValue({ id: "booking" } as Awaited<ReturnType<typeof createMeetingBooking>>);
    Object.defineProperty(HTMLElement.prototype, "scrollIntoView", { configurable: true, value: vi.fn() });
  });
  afterEach(() => { vi.clearAllMocks(); });
  it("repair focuses category then missing and whitespace text fields without saving", async () => {
    renderFleet(<RepairCreatePage />); await screen.findByDisplayValue("0800000000"); repairSubmit();
    expect(screen.getByRole("combobox", { name: /ประเภทงาน/ })).toHaveFocus(); expect(notifyGlobal).toHaveBeenCalledWith("error", "กรุณาเลือกประเภทงาน");
    await choose("ประเภทงาน", /คอมพิวเตอร์/); change(/หัวข้อ/, " "); repairSubmit(); expect(screen.getByLabelText(/หัวข้อ/)).toHaveFocus();
    change(/หัวข้อ/, "คอมเสีย"); repairSubmit(); expect(screen.getByLabelText(/อาการและผลกระทบ/)).toHaveFocus();
    change(/อาการและผลกระทบ/, "เปิดไม่ได้"); repairSubmit(); expect(screen.getByLabelText(/อาคาร \/ ชั้น/)).toHaveFocus();
    expect(repairCreate).not.toHaveBeenCalled();
    change(/อาคาร \/ ชั้น/, "ห้องยา"); repairSubmit(); await waitFor(() => expect(repairCreate).toHaveBeenCalled());
  });
  it("repair sends missing-profile-phone focus to the profile link", async () => {
    vi.mocked(getMyProfile).mockResolvedValue({ id: "author", fullname: "ผู้ขอ", phoneNumber: "" } as Awaited<ReturnType<typeof getMyProfile>>);
    renderFleet(<RepairCreatePage />); await screen.findByText(/ยังไม่มีเบอร์โทรในข้อมูลส่วนตัว/); await choose("ประเภทงาน", /คอมพิวเตอร์/);
    change(/หัวข้อ/, "เสีย"); change(/อาการและผลกระทบ/, "เปิดไม่ได้"); change(/อาคาร \/ ชั้น/, "ห้องยา"); repairSubmit();
    expect(screen.getByRole("link", { name: "เพิ่มเบอร์โทร" })).toHaveFocus(); expect(repairCreate).not.toHaveBeenCalled();
  });
  it("meeting focuses room, invalid date, times and whitespace subject/purpose", async () => {
    renderFleet(<MeetingRoomCreatePage />); meetingSubmit(); expect(screen.getByRole("combobox", { name: /ห้องประชุม/ })).toHaveFocus();
    await choose("ห้องประชุม", /ห้องบริหาร/); change("วันที่ประชุม", ""); meetingSubmit(); expect(screen.getByLabelText("วันที่ประชุม")).toHaveFocus();
    change("วันที่ประชุม", "2026-11-10"); change("เวลาเริ่ม", ""); meetingSubmit(); expect(screen.getByLabelText("เวลาเริ่ม")).toHaveFocus();
    change("เวลาเริ่ม", "11:00"); meetingSubmit(); expect(screen.getByLabelText("เวลาสิ้นสุด")).toHaveFocus();
    change("เวลาสิ้นสุด", "12:00"); change(/หัวข้อประชุม/, " "); meetingSubmit(); expect(screen.getByLabelText(/หัวข้อประชุม/)).toHaveFocus();
    change(/หัวข้อประชุม/, "ประชุม"); meetingSubmit(); expect(screen.getByLabelText(/วัตถุประสงค์/)).toHaveFocus(); expect(createMeetingBooking).not.toHaveBeenCalled();
    change(/วัตถุประสงค์/, "หารือ"); meetingSubmit(); await waitFor(() => expect(createMeetingBooking).toHaveBeenCalled());
  });
  it("meeting validates HTTPS and room capacity with actionable focus", async () => {
    renderFleet(<MeetingRoomCreatePage />); await choose("ห้องประชุม", /ห้องบริหาร/); change(/หัวข้อประชุม/, "ประชุม"); change(/วัตถุประสงค์/, "หารือ");
    change("Link ประชุม (ถ้ามี)", "http://example.com"); meetingSubmit(); expect(screen.getByLabelText("Link ประชุม (ถ้ามี)")).toHaveFocus();
    change("Link ประชุม (ถ้ามี)", "https://example.com"); await choose("ผู้เข้าประชุม", /ผู้ร่วมประชุม/); meetingSubmit();
    expect(screen.getByRole("combobox", { name: "ผู้เข้าประชุม" })).toHaveFocus(); expect(createMeetingBooking).not.toHaveBeenCalled();
    expect(HTMLElement.prototype.scrollIntoView).toHaveBeenCalledWith({ behavior: "smooth", block: "center" });
  });
});
