const defaultAppName = "Hospital Operations Portal";
const defaultHospitalName = "โรงพยาบาลนาหมื่น";
const defaultAppVersion = "0.1.0";
const defaultAppDeveloper = "งานเทคโนโลยีสารสนเทศ";

export const appName = import.meta.env.VITE_APP_NAME || defaultAppName;
export const hospitalName = import.meta.env.VITE_HOSPITAL_NAME || defaultHospitalName;
export const appVersion = import.meta.env.VITE_APP_VERSION || defaultAppVersion;
export const appDeveloper = import.meta.env.VITE_APP_DEVELOPER || defaultAppDeveloper;
const hideIneligibleLeaveTypes = (import.meta.env.VITE_HIDE_INELIGIBLE_LEAVE_TYPES ?? "true").toLowerCase() === "true";

export const appConfig = {
  appName,
  hospitalName,
  appVersion,
  appDeveloper,
  hideIneligibleLeaveTypes,
} as const;
