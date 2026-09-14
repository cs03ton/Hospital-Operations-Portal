export type ExecutiveView = "overview" | "leave" | "fleet" | "repair";

export const executiveViewOptions: Array<{ value: ExecutiveView; label: string }> = [
  { value: "overview", label: "ภาพรวม" },
  { value: "leave", label: "ระบบลา" },
  { value: "fleet", label: "ระบบขอรถ" },
  { value: "repair", label: "ระบบแจ้งซ่อม" },
];

export function parseExecutiveView(value: string | null): ExecutiveView {
  return executiveViewOptions.some((option) => option.value === value)
    ? value as ExecutiveView
    : "overview";
}
