import BuildOutlinedIcon from "@mui/icons-material/BuildOutlined";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import EventAvailableOutlinedIcon from "@mui/icons-material/EventAvailableOutlined";
import MeetingRoomOutlinedIcon from "@mui/icons-material/MeetingRoomOutlined";

export const quickServiceRoutes = {
  leave: "/leave/create",
  fleet: "/fleet/requests/create",
  repair: "/repairs/new",
  meetingRoom: "/meeting-rooms/new",
} as const;

export const quickServicePermissions = {
  leave: "LeaveRequest.Create",
  fleet: "FleetRequest.Create",
  repair: "RepairManagement.Create",
  meetingRoom: "MeetingRoom.Booking.Create",
} as const;

export const quickServices = [
  { key: "meetingRoom", label: "จองห้องประชุม", path: quickServiceRoutes.meetingRoom, permission: quickServicePermissions.meetingRoom, icon: MeetingRoomOutlinedIcon, color: "#8B5EC6" },
  { key: "repair", label: "แจ้งซ่อม", path: quickServiceRoutes.repair, permission: quickServicePermissions.repair, icon: BuildOutlinedIcon, color: "#E58A3C" },
  { key: "fleet", label: "เพิ่มคำขอรถ", path: quickServiceRoutes.fleet, permission: quickServicePermissions.fleet, icon: DirectionsCarOutlinedIcon, color: "#3985BD" },
  { key: "leave", label: "เพิ่มคำขอลา", path: quickServiceRoutes.leave, permission: quickServicePermissions.leave, icon: EventAvailableOutlinedIcon, color: "#27885D" },
] as const;

export function visibleQuickServices(permissions: readonly string[], role?: string, fleetAvailable = false) {
  const permissionSet = new Set(permissions);
  return quickServices.filter((service) => permissionSet.has(service.permission)
    && (service.key !== "leave" || (role !== "Admin" && role !== "SuperAdmin"))
    && (service.key !== "fleet" || fleetAvailable));
}
