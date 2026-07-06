export const genderLabels: Record<string, string> = {
  Male: "ชาย",
  Female: "หญิง",
  Unknown: "ไม่ระบุ",
};

export const genderOptions = Object.entries(genderLabels).map(([value, label]) => ({
  value,
  label,
}));

export function getGenderLabel(value?: string | null) {
  if (!value) {
    return genderLabels.Unknown;
  }

  return genderLabels[value] ?? value;
}
