export const roleLabels: Record<string, string> = {
  SuperAdmin: "ผู้ดูแลระบบสูงสุด",
  Admin: "ผู้ดูแลระบบ",
  Director: "ผู้อำนวยการ",
  DepartmentHead: "หัวหน้าหน่วยงาน",
  Staff: "เจ้าหน้าที่",
};

export function getRoleLabel(role?: string | null) {
  if (!role) {
    return "-";
  }

  return roleLabels[role] ?? role;
}

export function getRoleLabels(roles?: string[] | null) {
  if (!roles?.length) {
    return "-";
  }

  return roles.map(getRoleLabel).join(", ");
}

