// @vitest-environment jsdom
import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import settings from "../../../../backend/Hop.Api/appsettings.json";
import { getMemoDepartmentPeople, type MemoOptions, type MemoPerson } from "../../api/correspondenceApi";
import { unresolvedMemoVariables } from "../../utils/memoContentTemplates";
import { MemoContentTemplatePicker } from "./MemoContentTemplatePicker";

vi.mock("../../api/correspondenceApi", async original => ({ ...await original<typeof import("../../api/correspondenceApi")>(), getMemoDepartmentPeople: vi.fn() }));

vi.mock("../common/AppDatePicker", () => ({ AppDatePicker: ({ label, value, onChange, helperText }: { label: string; value: string; onChange: (value: string) => void; helperText?: string }) => <label>{label}<input type="date" aria-label={label} value={value} onChange={e => onChange(e.target.value)} />{helperText}</label> }));
vi.mock("../common/AppTimePicker", () => ({ AppTimePicker: ({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) => <label>{label}<input type="time" aria-label={label} value={value} onChange={e => onChange(e.target.value)} /></label> }));
const template = settings.Correspondence.ContentTemplates.find(t => t.Code === "MEMO_OT_CLAIM")!;
const options: MemoOptions = { proposer: { id: "p", name: "ผู้เสนอทดสอบ", position: "นักวิชาการ", departmentId: "d" }, departments: [{ id: "d", name: "กลุ่มงานสุขภาพดิจิทัล" }, { id: "other", name: "กลุ่มงานบริหาร" }], heads: [], directors: [], closings: [], headComments: [], directorComments: [], contentTemplates: [{ code: template.Code, name: template.Name, subjectTemplate: template.SubjectTemplate, bodyTemplate: template.BodyTemplate, closingTemplate: template.ClosingTemplate, variables: template.Variables, isActive: template.IsActive }] };
beforeEach(() => { vi.mocked(getMemoDepartmentPeople).mockReset().mockResolvedValue([]); });
afterEach(cleanup);
const approvalConfig = settings.Correspondence.ContentTemplates.find(t => t.Code === "MEMO_OT_REQUEST")!;
const approval = { code: approvalConfig.Code, name: approvalConfig.Name, subjectTemplate: approvalConfig.SubjectTemplate, bodyTemplate: approvalConfig.BodyTemplate, closingTemplate: approvalConfig.ClosingTemplate, variables: approvalConfig.Variables, isActive: approvalConfig.IsActive };
const people: MemoPerson[] = [
  { id: "one", name: "นายหนึ่ง", position: "นักวิชาการ", departmentId: "d" },
  { id: "two", name: "นายสอง", position: "เจ้าหน้าที่", departmentId: "d" },
  { id: "three", name: "นายสาม", position: "พยาบาล", departmentId: "d" },
];
async function chooseApproval() {
  fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" }));
  fireEvent.click(await screen.findByRole("option", { name: approval.name }));
}

describe("work certification template", () => {
  it.each(["นาย", "พญ.", "พล.ต.", null])("uses database prefix %s without a duplicate prefix selector", async prefix => {
    const fullname = "กฤษฎา แสงวันเพ็ญ", name = `${prefix ?? ""}${fullname}`;
    const t = settings.Correspondence.ContentTemplates.find(t => t.Code === "MEMO_WORK_CERT")!;
    const cert = { code: t.Code, name: t.Name, subjectTemplate: t.SubjectTemplate, bodyTemplate: t.BodyTemplate, closingTemplate: t.ClosingTemplate, variables: t.Variables, isActive: t.IsActive };
    const apply = vi.fn();
    render(<MemoContentTemplatePicker options={{ ...options, proposer: { ...options.proposer, name, fullname, namePrefix: prefix, position: "นักวิชาการคอมพิวเตอร์" }, contentTemplates: [cert] }} departmentId="d" onApply={apply} onDraftChange={vi.fn()} />);
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" })); fireEvent.click(await screen.findByRole("option", { name: t.Name }));
    for (const [label, value] of [["วันเริ่ม รายการ 1", "2026-10-05"], ["เวลาเริ่ม รายการ 1", "08:30"], ["เวลาสิ้นสุด รายการ 1", "16:30"], ["รายละเอียดงาน", "แก้ไข config server"]]) fireEvent.change(screen.getByLabelText(label), { target: { value } });
    expect(screen.queryByRole("combobox", { name: "คำนำหน้าชื่อ" })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    const result = apply.mock.calls[apply.mock.calls.length - 1][0];
    expect(result.body).toHaveLength(3);
    expect(result.body[0].runs[0].text).toContain(`ด้วยข้าพเจ้า ${name} ตำแหน่งนักวิชาการคอมพิวเตอร์`);
    expect(result.body[0].runs[0].text).toContain("ได้ขึ้นปฏิบัติงานแก้ไข config server ตามวันและเวลาดังต่อไปนี้");
    expect(result.body[1].runs[0].text).toContain("1. วันที่ 5 ตุลาคม 2569 เวลา 08:30–16:30 น.");
    expect(result.body[1]).toMatchObject({ align: "left", indent: 1, ordered: false });
    expect(result.body[2].runs[0].text).toContain(`ในการนี้ ข้าพเจ้า ${name} ตำแหน่งนักวิชาการคอมพิวเตอร์ ขอรับรอง`);
    expect(JSON.stringify(result.body)).not.toContain("โดยปฏิบัติงาน"); expect(JSON.stringify(result.body)).not.toContain("นายนาย");
    expect(unresolvedMemoVariables(result.subject, result.body, result.closing)).toEqual([]);
    expect(options.proposer.name).toBe("ผู้เสนอทดสอบ");
  });
});
describe("OT work approval template", () => {
  it("initially selects own-department staff and allows adding and removing a person from another department", async () => {
    const foreign = { id: "external", name: "นางสาวต่างหน่วยงาน", position: "เจ้าหน้าที่บริหาร", departmentId: "other" };
    vi.mocked(getMemoDepartmentPeople).mockResolvedValue([...people, foreign]);
    const apply = vi.fn();
    render(<MemoContentTemplatePicker options={{ ...options, contentTemplates: [approval] }} departmentId="d" onApply={apply} onDraftChange={vi.fn()} />);
    await chooseApproval(); await screen.findByText("นายหนึ่ง · นักวิชาการ");
    expect(screen.queryByText(`${foreign.name} · ${foreign.position}`)).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("ผู้ควบคุมการปฏิบัติงาน"), { target: { value: "หัวหน้าทดสอบ" } });
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "ผู้ปฏิบัติงานในกลุ่มงาน" }));
    const option = await screen.findByRole("option", { name: /นางสาวต่างหน่วยงาน/ });
    expect(option).toHaveAttribute("aria-selected", "false"); fireEvent.click(option);
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    expect(JSON.stringify(apply.mock.calls[0][0].body)).toContain(`${foreign.name} ตำแหน่ง ${foreign.position}`);
    fireEvent.click(screen.getByRole("option", { name: /นางสาวต่างหน่วยงาน/ }));
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    expect(JSON.stringify(apply.mock.calls[1][0].body)).not.toContain(foreign.name);
  });
  it("defaults the supervisor from the department head and preserves manual edits until the department changes", async () => {
    vi.mocked(getMemoDepartmentPeople).mockResolvedValue(people);
    const heads: MemoPerson[] = [
      { id: "head", name: "นายหัวหน้า ดิจิ", position: "หัวหน้ากลุ่มงาน", departmentId: "d" },
      { id: "other-head", name: "นายหัวหน้า บริหาร", position: "หัวหน้ากลุ่มงาน", departmentId: "other" },
    ];
    const props = { options: { ...options, heads, contentTemplates: [approval] }, departmentId: "d", onApply: vi.fn(), onDraftChange: vi.fn() };
    const view = render(<MemoContentTemplatePicker {...props} />); await chooseApproval(); await screen.findByText("นายหนึ่ง · นักวิชาการ");
    const supervisor = screen.getByLabelText("ผู้ควบคุมการปฏิบัติงาน");
    expect(supervisor).toHaveValue("นายหัวหน้า ดิจิ"); expect(supervisor).not.toHaveAttribute("readonly");
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    expect(JSON.stringify(props.onApply.mock.calls[0][0].body)).toContain("โดยมี นายหัวหน้า ดิจิ เป็นผู้ควบคุม");
    fireEvent.change(supervisor, { target: { value: "นายผู้ควบคุมที่แก้ไข" } });
    fireEvent.change(screen.getByLabelText("ปี พ.ศ."), { target: { value: "2570" } });
    view.rerender(<MemoContentTemplatePicker {...props} options={{ ...props.options, heads: [{ ...heads[0], name: "หัวหน้าคนใหม่" }, heads[1]] }} />);
    expect(supervisor).toHaveValue("นายผู้ควบคุมที่แก้ไข");
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    expect(JSON.stringify(props.onApply.mock.calls[1][0].body)).toContain("โดยมี นายผู้ควบคุมที่แก้ไข เป็นผู้ควบคุม");
    view.rerender(<MemoContentTemplatePicker {...props} departmentId="other" />);
    expect(supervisor).toHaveValue("นายหัวหน้า บริหาร");
    view.rerender(<MemoContentTemplatePicker {...props} departmentId="unknown" />);
    expect(supervisor).toHaveValue("");
  });
  it("uses the chosen head when several exist without guessing, and keeps an explicitly cleared supervisor empty", async () => {
    const heads: MemoPerson[] = [
      { id: "head-a", name: "หัวหน้าคนที่หนึ่ง", position: "หัวหน้า", departmentId: "d" },
      { id: "head-b", name: "หัวหน้าคนที่สอง", position: "หัวหน้า", departmentId: "d" },
      { id: "outside", name: "หัวหน้าต่างกลุ่มงาน", position: "หัวหน้า", departmentId: "other" },
    ];
    const props = { options: { ...options, heads, contentTemplates: [approval] }, departmentId: "d", onApply: vi.fn(), onDraftChange: vi.fn() };
    const view = render(<MemoContentTemplatePicker {...props} />); await chooseApproval();
    const supervisor = screen.getByLabelText("ผู้ควบคุมการปฏิบัติงาน"); expect(supervisor).toHaveValue("");
    view.rerender(<MemoContentTemplatePicker {...props} headUserId="head-a" />); expect(supervisor).toHaveValue("หัวหน้าคนที่หนึ่ง");
    view.rerender(<MemoContentTemplatePicker {...props} headUserId="head-b" />); expect(supervisor).toHaveValue("หัวหน้าคนที่สอง");
    view.rerender(<MemoContentTemplatePicker {...props} headUserId="outside" />); expect(supervisor).toHaveValue("หัวหน้าต่างกลุ่มงาน");
    view.rerender(<MemoContentTemplatePicker {...props} headUserId="unknown" />); expect(supervisor).toHaveValue("");
    view.rerender(<MemoContentTemplatePicker {...props} headUserId="head-b" />);
    fireEvent.change(supervisor, { target: { value: "" } });
    view.rerender(<MemoContentTemplatePicker {...props} headUserId="head-a" />); expect(supervisor).toHaveValue("");
  });
  it("fills both department paragraphs, the live department roster and the chosen month's full range", async () => {
    vi.mocked(getMemoDepartmentPeople).mockResolvedValue(people);
    const t = settings.Correspondence.ContentTemplates.find(t => t.Code === "MEMO_OT_REQUEST")!;
    const approval = { code: t.Code, name: t.Name, subjectTemplate: t.SubjectTemplate, bodyTemplate: t.BodyTemplate, closingTemplate: t.ClosingTemplate, variables: t.Variables, isActive: t.IsActive };
    const apply = vi.fn(); render(<MemoContentTemplatePicker options={{ ...options, contentTemplates: [...options.contentTemplates!, approval] }} departmentId="d" onApply={apply} onDraftChange={vi.fn()} />);
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" })); fireEvent.click(await screen.findByRole("option", { name: t.Name }));
    await screen.findByText("นายหนึ่ง · นักวิชาการ");
    expect(getMemoDepartmentPeople).toHaveBeenCalledWith("d", expect.any(AbortSignal));
    expect(screen.queryByLabelText("รายชื่อผู้ปฏิบัติงานและตำแหน่ง")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("เลขที่หนังสืออ้างอิง")).not.toBeInTheDocument(); expect(screen.queryByLabelText("จำนวนเงิน (บาท)")).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("ปี พ.ศ."), { target: { value: "2569" } });
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เดือน" })); fireEvent.click(await screen.findByRole("option", { name: "พฤศจิกายน" }));
    fireEvent.change(screen.getByLabelText("ผู้ควบคุมการปฏิบัติงาน"), { target: { value: "นายหัวหน้า ดิจิ" } });
    expect(apply).not.toHaveBeenCalled(); fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    const content = apply.mock.calls[0][0]; expect(content.subject).toBe(t.SubjectTemplate);
    expect(content.body[0].runs[0].text).toContain("ด้วย กลุ่มงานสุขภาพดิจิทัล โรงพยาบาลนาหมื่น มีความจำเป็น");
    expect(content.body[1].runs[0].text).toContain("กลุ่มงานสุขภาพดิจิทัล โรงพยาบาลนาหมื่น จึงขออนุมัติ");
    expect(content.body.filter((p: { ordered: boolean }) => p.ordered)).toHaveLength(3);
    const text = content.body.map((p: { runs: { text: string }[] }) => p.runs.map(r => r.text).join("")).join("\n");
    for (const person of people) expect(text).toContain(`${person.name} ตำแหน่ง ${person.position}`);
    expect(text).toContain("ตั้งแต่วันที่ 1 พฤศจิกายน 2569 ถึงวันที่ 30 พฤศจิกายน 2569 โดยมี นายหัวหน้า ดิจิ เป็นผู้ควบคุม");
    expect(text).toContain("7.1 การปฏิบัติงานนอกเวลาราชการในวันทำการ"); expect(text).toContain("7.2 การปฏิบัติงานในวันหยุดราชการ");
    expect(content.closing).toBe("จึงเรียนมาเพื่อโปรดทราบและพิจารณาอนุมัติ");
    expect(unresolvedMemoVariables(content.subject, content.body, content.closing)).toEqual([]);
    fireEvent.change(screen.getByLabelText("ผู้ควบคุมการปฏิบัติงาน"), { target: { value: "" } }); fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    const missing = apply.mock.calls[1][0]; expect(unresolvedMemoVariables(missing.subject, missing.body, missing.closing)).toContain("{supervisor_name}");
  });
  it("discards an obsolete roster when the selected department changes and does not overwrite content automatically", async () => {
    let resolveOld!: (people: MemoPerson[]) => void;
    vi.mocked(getMemoDepartmentPeople).mockImplementationOnce(() => new Promise(resolve => { resolveOld = resolve; }))
      .mockResolvedValueOnce([{ id: "new", name: "ผู้ปฏิบัติงานบริหาร", position: "เจ้าหน้าที่บริหาร", departmentId: "other" }]);
    const props = { options: { ...options, contentTemplates: [approval] }, departmentId: "d", onApply: vi.fn(), onDraftChange: vi.fn() };
    const view = render(<MemoContentTemplatePicker {...props} />); await chooseApproval();
    const signal = vi.mocked(getMemoDepartmentPeople).mock.calls[0][1]!;
    expect(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" })).toBeDisabled();
    view.rerender(<MemoContentTemplatePicker {...props} departmentId="other" />);
    await screen.findByText("ผู้ปฏิบัติงานบริหาร · เจ้าหน้าที่บริหาร"); expect(signal.aborted).toBe(true);
    await act(async () => resolveOld(people));
    expect(screen.queryByText("นายหนึ่ง · นักวิชาการ")).not.toBeInTheDocument(); expect(props.onApply).not.toHaveBeenCalled();
    fireEvent.change(screen.getByLabelText("ปี พ.ศ."), { target: { value: "2567" } });
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เดือน" })); fireEvent.click(await screen.findByRole("option", { name: "กุมภาพันธ์" }));
    expect(screen.getByLabelText("วันเริ่มปฏิบัติงาน")).toHaveValue("2024-02-01"); expect(screen.getByLabelText("วันสิ้นสุดปฏิบัติงาน")).toHaveValue("2024-02-29");
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    const body = props.onApply.mock.calls[0][0].body;
    expect(body[0].runs[0].text).toContain("ด้วย กลุ่มงานบริหาร"); expect(body[1].runs[0].text).toContain("กลุ่มงานบริหาร โรงพยาบาลนาหมื่น");
    expect(body.filter((p: { ordered: boolean }) => p.ordered)).toHaveLength(1);
    expect(JSON.stringify(body)).not.toContain("นายหนึ่ง");
  });
  it("blocks failed, empty or incomplete rosters and allows retry or removing a person without a position", async () => {
    vi.mocked(getMemoDepartmentPeople).mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce([])
      .mockResolvedValue([...people, { id: "missing", name: "ยังไม่ระบุตำแหน่ง", position: null, departmentId: "d" }]);
    const props = { options: { ...options, contentTemplates: [approval] }, departmentId: "d", onApply: vi.fn(), onDraftChange: vi.fn() };
    const view = render(<MemoContentTemplatePicker {...props} />); await chooseApproval();
    await screen.findByText("โหลดรายชื่อบุคลากรไม่สำเร็จ กรุณาลองใหม่");
    expect(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: "ลองใหม่" })); await screen.findByText("ไม่พบบุคลากรที่เปิดใช้งาน");
    expect(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" })).toBeDisabled();
    view.rerender(<MemoContentTemplatePicker {...props} departmentId="other" />);
    await waitFor(() => expect(getMemoDepartmentPeople).toHaveBeenCalledTimes(3));
    view.rerender(<MemoContentTemplatePicker {...props} />);
    await screen.findByText("ยังไม่ระบุตำแหน่ง · ยังไม่มีตำแหน่ง");
    expect(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" })).toBeDisabled();
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "ผู้ปฏิบัติงานในกลุ่มงาน" }));
    fireEvent.click(await screen.findByRole("option", { name: /ยังไม่ระบุตำแหน่ง/ }));
    expect(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" })).toBeEnabled();
  });
});
describe("OT claim reference template", () => {
  it("defaults the range on selection, recalculates for month/year changes and preserves manual dates until the next change", async () => {
    const apply = vi.fn(); render(<MemoContentTemplatePicker options={options} departmentId="d" onApply={apply} onDraftChange={vi.fn()} />);
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" })); fireEvent.click(await screen.findByRole("option", { name: template.Name }));
    const start = screen.getByLabelText("วันเริ่มปฏิบัติงาน"), end = screen.getByLabelText("วันสิ้นสุดปฏิบัติงาน");
    expect((start as HTMLInputElement).value).toMatch(/^\d{4}-\d{2}-01$/); expect((end as HTMLInputElement).value).not.toBe("");
    fireEvent.change(screen.getByLabelText("ลงวันที่บันทึกข้อความอ้างอิง"), { target: { value: "2024-01-15" } });
    fireEvent.change(screen.getByLabelText("ปี พ.ศ."), { target: { value: "2567" } });
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เดือน" })); fireEvent.click(await screen.findByRole("option", { name: "กุมภาพันธ์" }));
    expect(start).toHaveValue("2024-02-01"); expect(end).toHaveValue("2024-02-29");
    fireEvent.change(screen.getByLabelText("ปี พ.ศ."), { target: { value: "2568" } });
    expect(start).toHaveValue("2025-02-01"); expect(end).toHaveValue("2025-02-28");
    fireEvent.change(start, { target: { value: "2025-02-04" } }); fireEvent.change(end, { target: { value: "2025-02-20" } });
    expect(start).toHaveValue("2025-02-04"); expect(end).toHaveValue("2025-02-20");
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เดือน" })); fireEvent.click(await screen.findByRole("option", { name: "เมษายน" }));
    expect(start).toHaveValue("2025-04-01"); expect(end).toHaveValue("2025-04-30");
    expect(screen.getByLabelText("ลงวันที่บันทึกข้อความอ้างอิง")).toHaveValue("2024-01-15"); expect(apply).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }));
    expect(apply.mock.calls[0][0].body[0].runs[0].text).toContain("ตั้งแต่วันที่ 1 เมษายน 2568 ถึงวันที่ 30 เมษายน 2568");
    fireEvent.change(screen.getByLabelText("ปี พ.ศ."), { target: { value: "256" } });
    expect(start).toHaveValue(""); expect(end).toHaveValue(""); expect(screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" })).toBeDisabled();
  });
  it("fills the actual configured template using the chosen reference, dates and HOP department", async () => {
    const apply = vi.fn(), props = { options, departmentId: "d", onApply: apply, onDraftChange: vi.fn() };
    const view = render(<MemoContentTemplatePicker {...props} />);
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เลือกเทมเพลต" }));
    fireEvent.click(await screen.findByRole("option", { name: template.Name }));
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เดือน" })); fireEvent.click(await screen.findByRole("option", { name: "ตุลาคม" }));
    for (const [label, value] of [["ปี พ.ศ.", "2569"], ["เลขที่หน่วยงานอ้างอิง", "307"], ["เลขทะเบียนส่งอ้างอิง", "92"], ["ลงวันที่บันทึกข้อความอ้างอิง", "2026-10-01"], ["วันเริ่มปฏิบัติงาน", "2026-10-01"], ["วันสิ้นสุดปฏิบัติงาน", "2026-10-31"], ["รายละเอียดงาน", "งานประกันสุขภาพ"], ["จำนวนเงิน (บาท)", "3780"]]) {
      fireEvent.change(screen.getByLabelText(new RegExp(`^${label.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}`)), { target: { value } });
    }
    expect(screen.getByLabelText("เลขที่หนังสืออ้างอิง")).toHaveValue("นน 0033.307/92");
    expect(screen.getByLabelText("เลขที่หนังสืออ้างอิง")).toHaveAttribute("readonly");
    expect(apply).not.toHaveBeenCalled();
    const button = screen.getByRole("button", { name: "เติมข้อความจากเทมเพลต" }); fireEvent.click(button);
    const result = apply.mock.calls[0][0];
    expect(result.subject).toContain("ประจำเดือน ตุลาคม 2569");
    expect(result).toMatchObject({ contentTemplateCode: "MEMO_OT_CLAIM", workMonth: "2026-10" });
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เดือน" }));
    fireEvent.click(await screen.findByRole("option", { name: "พฤศจิกายน" }));
    expect(apply).toHaveBeenCalledTimes(1);
    expect(result.workMonth).toBe("2026-10");
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "เดือน" }));
    fireEvent.click(await screen.findByRole("option", { name: "ตุลาคม" }));
    expect(result.body[0].runs[0].text).toContain("ตามบันทึกข้อความ ที่ นน 0033.307/92 ลงวันที่ 1 ตุลาคม 2569");
    expect(result.body[0].runs[0].text).toContain("ตั้งแต่วันที่ 1 ตุลาคม 2569 ถึงวันที่ 31 ตุลาคม 2569 เพื่อปฏิบัติงานด้านงานประกันสุขภาพ นั้น");
    expect(result.body[1].runs[0].text).toContain("3,780 บาท (สามพันเจ็ดร้อยแปดสิบบาทถ้วน)");
    expect(result.body[1].runs[0].text).toContain("ยินดีนำเงินส่งคืนตามจำนวนที่ผิดพลาด");
    expect(result.closing).toBe("จึงเรียนมาเพื่อโปรดทราบและพิจารณาอนุมัติ");
    expect(unresolvedMemoVariables(result.subject, result.body, result.closing)).toEqual([]);
    view.rerender(<MemoContentTemplatePicker {...props} departmentId="other" />); fireEvent.click(button);
    expect(apply.mock.calls[1][0].body[0].runs[0].text).toContain("ตามที่ กลุ่มงานบริหาร");
    fireEvent.change(screen.getByLabelText("วันสิ้นสุดปฏิบัติงาน"), { target: { value: "2026-09-30" } });
    expect(button).toBeDisabled(); expect(apply).toHaveBeenCalledTimes(2);
    fireEvent.change(screen.getByLabelText("วันสิ้นสุดปฏิบัติงาน"), { target: { value: "2026-10-31" } });
    fireEvent.change(screen.getByLabelText(/เลขที่หน่วยงานอ้างอิง/), { target: { value: "" } }); fireEvent.click(button);
    const incomplete = apply.mock.calls[2][0];
    expect(unresolvedMemoVariables(incomplete.subject, incomplete.body, incomplete.closing)).toContain("{reference_document_number}");
    expect(screen.getByLabelText("เลขที่หนังสืออ้างอิง")).toHaveValue("");
    fireEvent.change(screen.getByLabelText(/เลขที่หน่วยงานอ้างอิง/), { target: { value: "3a12" } });
    fireEvent.change(screen.getByLabelText(/เลขทะเบียนส่งอ้างอิง/), { target: { value: "2/9" } });
    expect(screen.getByLabelText("เลขที่หนังสืออ้างอิง")).toHaveValue("นน 0033.312/29");
    fireEvent.change(screen.getByLabelText(/เลขทะเบียนส่งอ้างอิง/), { target: { value: "29.1" } });
    expect(screen.getByLabelText("เลขที่หนังสืออ้างอิง")).toHaveValue("นน 0033.312/29.1");
  });
});
