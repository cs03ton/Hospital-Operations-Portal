const defaultAppName = "Hospital Operations Portal";
const defaultHospitalName = "โรงพยาบาลนาหมื่น";

export const appName = import.meta.env.VITE_APP_NAME || defaultAppName;
export const hospitalName = import.meta.env.VITE_HOSPITAL_NAME || defaultHospitalName;

export const appConfig = {
  appName,
  hospitalName,
} as const;
