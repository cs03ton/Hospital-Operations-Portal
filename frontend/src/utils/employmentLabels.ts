export const employmentTypeLabels: Record<string, string> = {
  CIVIL_SERVANT: "ข้าราชการ",
  GOVERNMENT_EMPLOYEE: "พนักงานราชการ",
  MOPH_EMPLOYEE: "พนักงานกระทรวงสาธารณสุข",
  TEMPORARY_EMPLOYEE_MONTHLY: "ลูกจ้างชั่วคราวรายเดือน",
  TEMPORARY_EMPLOYEE_DAILY: "ลูกจ้างชั่วคราวรายวัน",
};

export const employmentTypeOptions = Object.entries(employmentTypeLabels).map(([value, label]) => ({
  value,
  label,
}));

export function getEmploymentTypeLabel(value?: string | null) {
  if (!value) {
    return "-";
  }

  return employmentTypeLabels[value] ?? value;
}
