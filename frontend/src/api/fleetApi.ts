import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type FleetPassenger = { id?: string; userId?: string | null; fullName: string; positionOrOrganization?: string | null; phone?: string | null; passengerType: "EMPLOYEE" | "EXTERNAL"; isRequester: boolean; sortOrder: number };
export type FleetHistory = { id: string; fromStatus?: string | null; toStatus: string; action: string; returnTarget?: string | null; reason?: string | null; actorUserId: string; actorName?: string | null; createdAt: string };
export type FleetAssignment = { id: string; vehicleId: string; vehicleCode: string; registrationNumber: string; driverUserId: string; driverName: string; assignedAt: string; assignmentStatus: string; isActive: boolean; concurrencyToken: string; replacementOfAssignmentId?: string | null };
export type FleetRequest = { id: string; requestNo: string; requesterUserId: string; requesterName: string; requesterDepartmentId?: string | null; requesterDepartmentName?: string | null; requestDate: string; purpose: string; missionType: string; requestedVehicleTypeId?: string | null; requestedVehicleTypeName?: string | null; destination: string; contactPersonName: string; contactPhone: string; departureAt: string; expectedReturnAt: string; passengerCount: number; specialRequirement?: string | null; isUrgent: boolean; urgentReason?: string | null; status: string; returnTarget?: string | null; submittedAt?: string | null; cancelledAt?: string | null; cancellationReason?: string | null; createdAt: string; updatedAt?: string | null; concurrencyToken: string; passengers: FleetPassenger[]; statusHistories: FleetHistory[]; activeAssignment?: FleetAssignment | null };
export type SaveFleetRequest = Omit<FleetRequest, "id" | "requestNo" | "requesterUserId" | "requesterName" | "requesterDepartmentId" | "requesterDepartmentName" | "requestDate" | "requestedVehicleTypeName" | "status" | "returnTarget" | "submittedAt" | "cancelledAt" | "cancellationReason" | "createdAt" | "updatedAt" | "concurrencyToken" | "statusHistories" | "activeAssignment"> & { concurrencyToken?: string };
export type AvailabilityItem = { id: string; code: string; name: string; isAvailable: boolean; reasons: string[]; monthTripCount: number; lastAssignmentAt?: string | null };
export type FleetAvailability = { vehicles: AvailabilityItem[]; drivers: AvailabilityItem[] };
export type FleetVehicleType = { id: string; code: string; name: string; description?: string | null; sortOrder: number; isActive: boolean };
export type FleetPersonnelOption = { id: string; fullName: string; employeeCode?: string | null; departmentId?: string | null; departmentName?: string | null };

const unwrap = <T>(response: { data: ApiResponse<T> }) => response.data.data;
export async function getMyFleetRequests() { return unwrap(await httpClient.get<ApiResponse<FleetRequest[]>>("/fleet/requests/mine")); }
export type FleetRequestPage = { items: FleetRequest[]; page: number; pageSize: number; totalItems: number; totalPages: number };
export type FleetRequestQuery = { page?: number; pageSize?: number; search?: string; status?: string; dateFrom?: string; dateTo?: string; sortBy?: "createdAt" | "requestNo" | "departureAt" | "status"; sortDirection?: "asc" | "desc" };
export async function getMyFleetRequestsPaged(params: FleetRequestQuery) { return unwrap(await httpClient.get<ApiResponse<FleetRequestPage>>("/fleet/requests/mine/paged", { params })); }
export async function getDispatcherQueue() { return unwrap(await httpClient.get<ApiResponse<FleetRequest[]>>("/fleet/requests/dispatcher/queue")); }
export async function getFleetRequest(id: string) { return unwrap(await httpClient.get<ApiResponse<FleetRequest>>(`/fleet/requests/${id}`)); }
export async function createFleetRequest(data: SaveFleetRequest) { return unwrap(await httpClient.post<ApiResponse<FleetRequest>>("/fleet/requests", data)); }
export async function updateFleetRequest(id: string, data: SaveFleetRequest) { return unwrap(await httpClient.put<ApiResponse<FleetRequest>>(`/fleet/requests/${id}`, data)); }
export async function transitionFleetRequest(id: string, action: "submit" | "cancel" | "return" | "reject", concurrencyToken: string, reason?: string) { return unwrap(await httpClient.post<ApiResponse<FleetRequest>>(`/fleet/requests/${id}/${action}`, { concurrencyToken, reason })); }
export async function getFleetAvailability(id: string) { return unwrap(await httpClient.get<ApiResponse<FleetAvailability>>(`/fleet/requests/${id}/availability`)); }
export async function assignFleetRequest(id: string, vehicleId: string, driverUserId: string, concurrencyToken: string, reason?: string) { return unwrap(await httpClient.post<ApiResponse<FleetRequest>>(`/fleet/requests/${id}/assign`, { vehicleId, driverUserId, concurrencyToken, reason })); }
export async function getFleetVehicleTypes() { return unwrap(await httpClient.get<ApiResponse<FleetVehicleType[]>>("/fleet/vehicle-types")); }
export async function getFleetPersonnelOptions(search = "") { return unwrap(await httpClient.get<ApiResponse<FleetPersonnelOption[]>>("/fleet/requests/personnel-options", { params: { search: search || undefined } })); }
export type FleetQueueItem = { id: string; requestNo: string; status: string; requesterName: string; requesterDepartmentName?: string | null; purpose: string; missionType: string; destination: string; departureAt: string; expectedReturnAt: string; passengerCount: number; vehicle?: string | null; driver?: string | null; concurrencyToken: string };
export async function getFleetWorkflowQueue(kind: "admin-review" | "director-approval") { return unwrap(await httpClient.get<ApiResponse<FleetQueueItem[]>>(`/fleet/${kind}`)); }
export async function fleetWorkflowAction(kind: "admin-review" | "director-approval", id: string, action: "approve" | "return" | "reject", concurrencyToken: string, reason?: string, returnTarget?: string) { return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/${kind}/${id}/${action}`, { concurrencyToken, reason, returnTarget })); }
export async function getDriverJobs() { return unwrap(await httpClient.get<ApiResponse<Array<{ id: string; requestNo: string; priority:string; status: string; origin?:string; destination: string; departureAt: string; expectedReturnAt: string;passengerCount:number;vehicle?:string;requiredCapabilities:string[]; concurrencyToken: string }>>>("/fleet/driver-jobs")); }
export type DriverJobDetail={id:string;requestNo:string;priority:string;status:string;purpose:string;origin?:string;destination:string;passengerCount:number;emergencyReason?:string;concurrencyToken:string;vehicle:{id:string;vehicleCode:string;registrationNumber:string;currentMileage:number};trip?:{id:string;startMileage:number;endMileage?:number|null;actualStartAt:string;actualEndAt?:string|null;concurrencyToken:string}|null;externalMapUrl:string};
export async function getDriverJob(id:string){return unwrap(await httpClient.get<ApiResponse<DriverJobDetail>>(`/fleet/driver-jobs/${id}`));}
export async function driverJobAction(id: string, action: "accept" | "decline", concurrencyToken: string, reason?: string) { return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/driver-jobs/${id}/${action}`, { concurrencyToken, reason })); }
export type FleetKpiSummary = { totalRequests: number; completedRequests: number; rejectedRequests: number; cancelledRequests: number; approvalRate: number; rejectionRate: number; cancellationRate: number; tripCompletionRate: number; driverAcceptanceRate: number; assignmentReplacementCount: number; inAppDeliverySuccessRate: number; lineDeliverySuccessRate: number; outboxFailureCount: number };
export async function getFleetKpis(params: URLSearchParams) { return unwrap(await httpClient.get<ApiResponse<FleetKpiSummary>>("/fleet/dashboard/summary", { params })); }
export type FleetDepartmentReport = { requesterDepartmentId?: string | null; department: string; requestCount: number; completedCount: number; cancelledCount: number };
export type FleetDriverReport = { driverUserId: string; driver: string; jobCount: number; completedTrips: number };
export type FleetRouteReport = { destination: string; requestCount: number; completedTripCount: number };
export async function getFleetDepartmentReport(params: URLSearchParams) { return unwrap(await httpClient.get<ApiResponse<FleetDepartmentReport[]>>("/fleet/dashboard/departments", { params })); }
export async function getFleetDriverReport(params: URLSearchParams) { return unwrap(await httpClient.get<ApiResponse<FleetDriverReport[]>>("/fleet/dashboard/drivers", { params })); }
export async function getFleetRouteReport(params: URLSearchParams) { return unwrap(await httpClient.get<ApiResponse<FleetRouteReport[]>>("/fleet/dashboard/routes", { params })); }
export type FleetDashboardCapabilities = { canViewOwnRequests:boolean;canViewOwnTrips:boolean;canDispatch:boolean;canAdminReview:boolean;canDirectorApprove:boolean;canViewCalendar:boolean;canViewReports:boolean;canManageFleet:boolean;delegatedPermissions:string[] };
export type FleetDashboardBadges = { dispatchQueue:number;reviewQueue:number;approvalQueue:number;myDriverJobs:number };
export type FleetDashboardRequestPreview = { id:string;requestNo:string;departureAt:string;destination:string;status:string;vehicle?:string|null;driver?:string|null };
export type FleetDashboardRequester = { statusCounts:Record<string,number>;nextTrip?:FleetDashboardRequestPreview|null;actionRequired:number };
export type FleetDashboardDriver = { todayJobs:number;upcomingJobs:number;actionRequired:number;completedThisMonth:number;distanceThisMonth:number };
export type FleetDashboardDispatcher = { pendingDispatch:number;returnedToDispatcher:number;availableVehicles:number;busyVehicles:number;availableDrivers:number;busyDrivers:number;overdueJobs:number };
export type FleetDashboardApproval = { pending:number;urgent:number;nearDeparture:number;completedToday:number;returnedToday:number;rejectedToday:number };
export type FleetDashboardAdmin = { requestsThisMonth:number;tripsThisMonth:number;distanceThisMonth:number;cancelledThisMonth:number;overdueJobs:number;failedOutbox:number };
export type FleetDashboardFeedback = { feedbackCount:number;responseRate:number;overallAverage?:number|null;safetyAverage?:number|null;incidentCount:number;attentionCount:number;attentionThreshold:number };
export type FleetDashboardData = { capabilities:FleetDashboardCapabilities;badges:FleetDashboardBadges;shared:{todayJobs:number;availableVehicles:number;inUseVehicles:number;unavailableVehicles:number};requester:FleetDashboardRequester|null;driver:FleetDashboardDriver|null;dispatcher:FleetDashboardDispatcher|null;adminReviewer:FleetDashboardApproval|null;director:FleetDashboardApproval|null;admin:FleetDashboardAdmin|null;feedback?:FleetDashboardFeedback|null;generatedAt:string };
export const FLEET_DASHBOARD_QUERY_KEY = ["fleet-dashboard"] as const;
export async function getFleetDashboard() { return unwrap(await httpClient.get<ApiResponse<FleetDashboardData>>("/fleet/dashboard")); }
export async function exportFleetReport(params: URLSearchParams) { return (await httpClient.get<Blob>("/fleet/dashboard/export", { params, responseType: "blob" })).data; }
export type MaintenanceItem = { id: string; vehicleId: string; vehicle: string; maintenanceTypeId: string; type: string; dueDate?: string; dueMileage?: number; status: string; dueState: string; concurrencyToken: string };
export async function getFleetMaintenance(params?: Record<string, unknown>) { return unwrap(await httpClient.get<ApiResponse<{ items: MaintenanceItem[]; total: number }>>("/fleet/maintenance", { params })); }
export type MaintenanceType = { id: string; code: string; name: string; isDateBased: boolean; isMileageBased: boolean; blocksAvailabilityWhenOverdue: boolean; isActive: boolean };
export type FleetVehicleOption = { id: string; vehicleCode: string; registrationNumber: string; currentMileage: number; isActive: boolean };
export type MaintenanceDetail = { id: string; vehicleId: string; maintenanceTypeId: string; dueDate?: string|null; dueMileage?: number|null; reminderDays?: number|null; reminderMileage?: number|null; recurrenceDays?: number|null; recurrenceMileage?: number|null; status: string; notes?: string|null; concurrencyToken: string; records: Array<{ id:string; completedMileage?:number; cost?:number; vendor?:string; result?:string; attachments:Array<{id:string;originalFileName:string;contentType:string;fileSize:number;isDeleted:boolean}> }> };
export async function getFleetMaintenanceDetail(id: string) { return unwrap(await httpClient.get<ApiResponse<MaintenanceDetail>>(`/fleet/maintenance/${id}`)); }
export async function createFleetMaintenance(data: Record<string, unknown>) { return unwrap(await httpClient.post<ApiResponse<{id:string;concurrencyToken:string}>>("/fleet/maintenance", data)); }
export async function updateFleetMaintenance(id:string,data:Record<string,unknown>){return unwrap(await httpClient.put<ApiResponse<{id:string;concurrencyToken:string}>>(`/fleet/maintenance/${id}`,data));}
export async function getMaintenanceTypes(){return unwrap(await httpClient.get<ApiResponse<MaintenanceType[]>>("/fleet/maintenance-types"));}
export async function getFleetVehicles(){return unwrap(await httpClient.get<ApiResponse<FleetVehicleOption[]>>("/fleet/vehicles"));}
export async function maintenanceAction(id: string, action: "start" | "complete" | "cancel", data: Record<string, unknown>) { return unwrap(await httpClient.post<ApiResponse<Record<string, unknown>>>(`/fleet/maintenance/${id}/${action}`, data)); }
export async function uploadMaintenanceAttachment(recordId:string,file:File){const form=new FormData();form.append("file",file);return unwrap(await httpClient.post<ApiResponse<Record<string,unknown>>>(`/fleet/maintenance/${recordId}/attachments`,form));}
export async function deleteMaintenanceAttachment(id:string){return unwrap(await httpClient.delete<ApiResponse<{id:string}>>(`/fleet/maintenance/attachments/${id}`));}
export function maintenanceAttachmentUrl(id:string){return `/api/fleet/maintenance/attachments/${id}`;}
export type VehicleDocument = { id:string;vehicleId:string;documentType:string;documentNumber?:string|null;issuedAt?:string|null;expiresAt?:string|null;provider?:string|null;isRequired:boolean;concurrencyToken:string };
export async function getVehicleDocuments(vehicleId:string){return unwrap(await httpClient.get<ApiResponse<VehicleDocument[]>>(`/fleet/vehicles/${vehicleId}/documents`));}
export async function createVehicleDocument(vehicleId:string,data:Record<string,unknown>){return unwrap(await httpClient.post<ApiResponse<Record<string,unknown>>>(`/fleet/vehicles/${vehicleId}/documents`,data));}
export async function updateVehicleDocument(id:string,data:Record<string,unknown>){return unwrap(await httpClient.put<ApiResponse<Record<string,unknown>>>(`/fleet/vehicle-documents/${id}`,data));}
export async function disableVehicleDocument(id:string,concurrencyToken:string,reason:string){return unwrap(await httpClient.post<ApiResponse<Record<string,unknown>>>(`/fleet/vehicle-documents/${id}/disable`,{concurrencyToken,reason}));}
export type FleetCalendarEvent = { id: string; eventType: string; title: string; startAt: string; endAt: string; isAllDay: boolean; status: string; severity: string; detailUrl: string; requestId?:string|null;assignmentId?:string|null;tripId?:string|null;vehicleId?:string|null;driverUserId?:string|null;metadata:Record<string,unknown> };
export async function getFleetCalendar(params: URLSearchParams) { return unwrap(await httpClient.get<ApiResponse<FleetCalendarEvent[]>>("/fleet/calendar", { params })); }
export type FleetDelegation = { id: string; delegatorUserId: string; delegatorName: string; delegateUserId: string; delegateName: string; scope: "FLEET"; requiredPermissionCode: string; startAt: string; endAt: string; isActive: boolean; reason: string; concurrencyToken: string };
export type SaveFleetDelegation = Omit<FleetDelegation, "id" | "delegatorName" | "delegateName" | "scope" | "concurrencyToken"> & { concurrencyToken?: string };
export async function getFleetDelegations() { return unwrap(await httpClient.get<ApiResponse<FleetDelegation[]>>("/fleet/delegations")); }
export async function createFleetDelegation(data: SaveFleetDelegation) { return unwrap(await httpClient.post<ApiResponse<FleetDelegation>>("/fleet/delegations", data)); }
export async function updateFleetDelegation(id: string, data: SaveFleetDelegation) { return unwrap(await httpClient.put<ApiResponse<FleetDelegation>>(`/fleet/delegations/${id}`, data)); }
export async function disableFleetDelegation(id: string, concurrencyToken: string) { await httpClient.delete(`/fleet/delegations/${id}`, { params: { concurrencyToken } }); }
export async function replaceFleetAssignment(id: string, data: { vehicleId?: string; driverUserId?: string; requestConcurrencyToken: string; assignmentConcurrencyToken: string; reason: string }) { return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/assignments/${id}/replace`, data)); }
export type FleetRollout = { mode: "Disabled" | "UATOnly" | "Enabled"; uatUserIds: string[]; uatRoleCodes: string[]; concurrencyToken?: string | null; isDatabaseOverride: boolean; isAllowed: boolean; reason: string };
export async function getFleetRolloutAccess() { return unwrap(await httpClient.get<ApiResponse<FleetRollout>>("/fleet/rollout/access")); }
export async function getFleetRolloutStatus() { return unwrap(await httpClient.get<ApiResponse<FleetRollout>>("/fleet/rollout/status")); }
export async function updateFleetRollout(data: { mode: string; uatUserIds: string[]; uatRoleCodes: string[]; concurrencyToken?: string | null; reason: string }) { return unwrap(await httpClient.put<ApiResponse<unknown>>("/fleet/rollout", data)); }
export type FleetHealthMetric = { key: string; label: string; value: number; severity: "Healthy" | "Warning" | "Critical"; actionUrl?: string | null };
export type FleetHealthIssue = { code: string; severity: "Warning" | "Critical"; message: string; entityType: string; entityId?: string | null; detectedAt: string; actionUrl?: string | null };
export type FleetHealth = { status: "Healthy" | "Warning" | "Critical"; generatedAt: string; metrics: FleetHealthMetric[]; warningCount: number; criticalCount: number };
export async function getFleetHealth() { return unwrap(await httpClient.get<ApiResponse<FleetHealth>>("/fleet/health")); }
export async function getFleetHealthIssues(severity?: string) { return unwrap(await httpClient.get<ApiResponse<FleetHealthIssue[]>>("/fleet/health/issues", { params: { severity: severity || undefined } })); }
export async function getFleetPermissionDiagnostics() { return unwrap(await httpClient.get<ApiResponse<unknown>>("/fleet/diagnostics/permissions")); }
export type FleetCapability = { id:string;code:string;name:string;description?:string;category:string;dataType:"BOOLEAN"|"NUMBER"|"TEXT"|"ENUM";unit?:string;minimumNumericValue?:number|null;maximumNumericValue?:number|null;enumOptionsJson?:string|null;isRequiredSafetyCapability:boolean;isActive:boolean;sortOrder:number;concurrencyToken:string };
export async function getFleetCapabilities(search="",params:Record<string,unknown>={}){return unwrap(await httpClient.get<ApiResponse<{items:FleetCapability[];total:number;page:number;pageSize:number}>>("/fleet/capabilities",{params:{search,...params}}));}
export async function saveFleetCapability(id:string|null,data:Record<string,unknown>){return unwrap(id?await httpClient.put<ApiResponse<FleetCapability>>(`/fleet/capabilities/${id}`,data):await httpClient.post<ApiResponse<FleetCapability>>("/fleet/capabilities",data));}
export async function disableFleetCapability(id:string,concurrencyToken:string,reason:string){return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/capabilities/${id}/disable`,{concurrencyToken,reason}));}
export type TypedCapabilityValue={id?:string;capabilityId:string;booleanValue?:boolean|null;numericValue?:number|null;textValue?:string|null;enumValue?:string|null;effectiveFrom?:string|null;effectiveTo?:string|null;isActive:boolean;concurrencyToken?:string};
export async function getVehicleCapabilities(vehicleId:string){return unwrap(await httpClient.get<ApiResponse<TypedCapabilityValue[]>>(`/fleet/vehicles/${vehicleId}/capabilities`));}
export async function saveVehicleCapabilities(vehicleId:string,rows:TypedCapabilityValue[]){return unwrap(await httpClient.put<ApiResponse<unknown>>(`/fleet/vehicles/${vehicleId}/capabilities`,rows));}
export type RequiredCapabilityValue={capabilityId:string;operator:string;requiredBooleanValue?:boolean|null;requiredNumericValue?:number|null;requiredTextValue?:string|null;requiredEnumValue?:string|null;isMandatory:boolean;notes?:string|null};
export async function getRequestCapabilities(requestId:string){return unwrap(await httpClient.get<ApiResponse<RequiredCapabilityValue[]>>(`/fleet/requests/${requestId}/required-capabilities`));}
export async function saveRequestCapabilities(requestId:string,rows:RequiredCapabilityValue[]){return unwrap(await httpClient.put<ApiResponse<{count:number;requestConcurrencyToken:string}>>(`/fleet/requests/${requestId}/required-capabilities`,rows));}
export async function getFleetCompatibility(requestId:string,vehicleId?:string){return unwrap(await httpClient.get<ApiResponse<Array<{requestId:string;vehicleId:string;status:string;capabilities:Array<{capabilityId:string;code:string;requirement:string;status:string;reason:string;isMandatory:boolean;isSafety:boolean}>}>>>(`/fleet/requests/${requestId}/compatibility`,{params:{vehicleId}}));}
export async function overrideFleetCompatibility(requestId:string,data:Record<string,unknown>){return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/requests/${requestId}/compatibility-override`,data));}
export type EmergencyRequest={id:string;requestNo:string;priority:string;status:string;purpose:string;destination:string;incidentLocation:string;departureAt:string;expectedReturnAt:string;passengerCount:number;contactPhone:string;emergencyReason:string;requiresPostReview:boolean;concurrencyToken:string};
export async function createEmergencyRequest(data:Record<string,unknown>){return unwrap(await httpClient.post<ApiResponse<EmergencyRequest>>("/fleet/emergency-requests",data));}
export async function submitEmergencyRequest(id:string,concurrencyToken:string,reason?:string){return unwrap(await httpClient.post<ApiResponse<EmergencyRequest>>(`/fleet/emergency-requests/${id}/submit`,{concurrencyToken,reason}));}
export async function getEmergencyQueue(priority?:string){return unwrap(await httpClient.get<ApiResponse<{items:EmergencyRequest[];total:number;page:number;pageSize:number}>>("/fleet/emergency-requests/queue",{params:{priority:priority||undefined}}));}
export async function emergencyAction(id:string,action:"dispatch"|"bypass-approval"|"post-review",data:Record<string,unknown>){return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/emergency-requests/${id}/${action}`,data));}
export async function uploadTripAttachment(requestId:string,file:File,idempotencyKey:string){const form=new FormData();form.append("file",file);return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/trips/${requestId}/attachments`,form,{headers:{"Idempotency-Key":idempotencyKey}}));}
export type TripAttachment={id:string;originalFileName:string;contentType:string;fileSize:number;createdAt:string};
export async function getTripAttachments(requestId:string){return unwrap(await httpClient.get<ApiResponse<TripAttachment[]>>(`/fleet/trips/${requestId}/attachments`));}
export function tripAttachmentDownloadUrl(id:string){return `/api/fleet/trips/attachments/${id}/download`;}
export async function deleteTripAttachment(id:string){return unwrap(await httpClient.delete<ApiResponse<unknown>>(`/fleet/trips/attachments/${id}`));}
export async function startFleetTrip(id:string,data:Record<string,unknown>,idempotencyKey:string){return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/trips/${id}/start`,data,{headers:{"Idempotency-Key":idempotencyKey}}));}
export async function completeFleetTrip(id:string,data:Record<string,unknown>,idempotencyKey:string){return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/trips/${id}/complete`,data,{headers:{"Idempotency-Key":idempotencyKey}}));}
export type EmergencyPolicy={id:string;code:string;name:string;priority:"URGENT"|"EMERGENCY";responseTargetMinutes:number;dispatchTargetMinutes:number;driverAcknowledgementTargetMinutes:number;approvalBypassAllowed:boolean;postReviewRequired:boolean;isActive:boolean;effectiveFrom:string;effectiveTo?:string|null;concurrencyToken:string};
export async function getEmergencyPolicies(params:Record<string,unknown>={}){return unwrap(await httpClient.get<ApiResponse<{items:EmergencyPolicy[];total:number}>>("/fleet/emergency-policies",{params}));}
export async function saveEmergencyPolicy(id:string|null,data:Record<string,unknown>){return unwrap(id?await httpClient.put<ApiResponse<EmergencyPolicy>>(`/fleet/emergency-policies/${id}`,data):await httpClient.post<ApiResponse<EmergencyPolicy>>("/fleet/emergency-policies",data));}
export async function disableEmergencyPolicy(id:string,concurrencyToken:string,reason:string){return unwrap(await httpClient.post<ApiResponse<unknown>>(`/fleet/emergency-policies/${id}/disable`,{concurrencyToken,reason}));}
export type EmergencyReviewItem={id:string;requestNo:string;priority:string;status:string;createdAt:string;requiresPostReview:boolean;emergencyPolicyCode:string;responseTargetMinutes:number;slaBreached:boolean;bypassUsed:boolean};
export async function getEmergencyReviews(params:Record<string,unknown>={}){return unwrap(await httpClient.get<ApiResponse<{items:EmergencyReviewItem[];total:number}>>("/fleet/emergency-reviews",{params}));}
export async function getEmergencyReview(id:string){return unwrap(await httpClient.get<ApiResponse<Record<string,unknown>>>(`/fleet/emergency-reviews/${id}`));}

export type FleetLineGroupEventSubscription = { eventType: string; isEnabled: boolean };
export type FleetLineGroup = {
  id: string; displayName: string; groupIdMasked: string; status: "Pending" | "Active" | "Disabled";
  module: "FLEET"; attentionRequired: boolean; attentionReason?: string | null; firstDetectedAt: string;
  lastDetectedAt: string; confirmedAt?: string | null; disabledAt?: string | null; concurrencyToken: string;
  deliveryProvider?: "LINE_MESSAGING_API" | "CUSTOM_ENDPOINT"; endpointUrl?: string | null; clientId?: string | null; hasClientSecret?: boolean;
  events: FleetLineGroupEventSubscription[];
};
export type FleetLineGroupDelivery = {
  id: string; eventType: string; destinationType: "GROUP"; destinationIdMasked: string; requestId: string;
  status: string; attemptCount: number; sentAt?: string | null; failedAt?: string | null; errorCode?: string | null;
  errorMessage?: string | null; correlationId: string; createdAt: string;
};
export type FleetLineGroupDeliveries = { items: FleetLineGroupDelivery[]; page: number; pageSize: number; totalItems: number; totalPages: number };
export async function getFleetLineGroups(params?: { status?: string; search?: string }) {
  return unwrap(await httpClient.get<ApiResponse<FleetLineGroup[]>>("/fleet/line-groups", { params }));
}
export type SaveFleetLineGroupEndpoint = { displayName: string; groupId: string; endpointUrl: string; clientId: string; clientSecret?: string; concurrencyToken?: string };
export async function createFleetLineGroupEndpoint(data: SaveFleetLineGroupEndpoint) {
  return unwrap(await httpClient.post<ApiResponse<{ id: string; status: string; concurrencyToken: string }>>("/fleet/line-groups", data));
}
export async function updateFleetLineGroupEndpoint(id: string, data: SaveFleetLineGroupEndpoint) {
  return unwrap(await httpClient.put<ApiResponse<{ id: string; status: string; concurrencyToken: string }>>(`/fleet/line-groups/${id}/configuration`, data));
}
export async function confirmFleetLineGroup(id: string, concurrencyToken: string, reason?: string) {
  return unwrap(await httpClient.post<ApiResponse<{ id: string; status: string; concurrencyToken: string }>>(`/fleet/line-groups/${id}/confirm`, { concurrencyToken, reason }));
}
export async function disableFleetLineGroup(id: string, concurrencyToken: string, reason: string) {
  return unwrap(await httpClient.post<ApiResponse<{ id: string; status: string; concurrencyToken: string }>>(`/fleet/line-groups/${id}/disable`, { concurrencyToken, reason }));
}
export async function updateFleetLineGroupSubscriptions(id: string, concurrencyToken: string, events: Record<string, boolean>) {
  return unwrap(await httpClient.put<ApiResponse<{ id: string; concurrencyToken: string }>>(`/fleet/line-groups/${id}/subscriptions`, { concurrencyToken, events }));
}
export async function testFleetLineGroup(id: string, message?: string) {
  return unwrap(await httpClient.post<ApiResponse<{ id: string; status: string; attemptCount: number; errorCode?: string | null }>>(`/fleet/line-groups/${id}/test`, { message }));
}
export async function getFleetLineGroupDeliveries(id: string, page = 1, pageSize = 20) {
  return unwrap(await httpClient.get<ApiResponse<FleetLineGroupDeliveries>>(`/fleet/line-groups/${id}/deliveries`, { params: { page, pageSize } }));
}

export type FleetFeedbackStatus = "AVAILABLE" | "SUBMITTED" | "EXPIRED" | "NOT_ELIGIBLE";
export type FleetFeedbackContext = {
  tripId: string; requestNo: string; tripDate: string; destination: string; vehicleDisplay: string;
  driverDisplay: string; completedAt: string; feedbackDeadline: string; canSubmitFeedback: boolean;
  feedbackStatus: FleetFeedbackStatus;
};
export type SubmitFleetFeedback = {
  punctualityRating: number; safetyRating: number; serviceRating: number; overallRating: number;
  vehicleConditionRating: number; vehicleCleanlinessRating: number; hasIncident: boolean;
  incidentCategory?: string | null; comment?: string | null;
};
export type FleetFeedbackOwn = SubmitFleetFeedback & { id: string; tripId: string; requestNo: string; submittedAt: string };
export async function getFleetFeedbackContext(tripId: string) {
  return unwrap(await httpClient.get<ApiResponse<FleetFeedbackContext>>(`/fleet/trips/${tripId}/feedback-context`));
}
export async function getFleetFeedbackEligibleTrips() {
  return unwrap(await httpClient.get<ApiResponse<FleetFeedbackContext[]>>("/fleet/my-feedback-eligible-trips"));
}
export async function getMyFleetFeedbackTrips() {
  return unwrap(await httpClient.get<ApiResponse<FleetFeedbackContext[]>>("/fleet/my-feedback-trips"));
}
export async function submitFleetFeedback(tripId: string, data: SubmitFleetFeedback) {
  return unwrap(await httpClient.post<ApiResponse<FleetFeedbackOwn>>(`/fleet/trips/${tripId}/feedback`, data));
}
export async function getMyFleetFeedback(tripId: string) {
  return unwrap(await httpClient.get<ApiResponse<FleetFeedbackOwn>>(`/fleet/trips/${tripId}/feedback/me`));
}
export type FleetFeedbackTripSummary = {
  tripId:string;fleetRequestId:string;requestNo:string;completedAt:string;destination:string;vehicleId:string;vehicle:string;
  driverUserId:string;driver:string;department:string;missionType:string;eligibleParticipants:number;feedbackCount:number;
  responseRate:number;overallAverage?:number|null;safetyAverage?:number|null;punctualityAverage?:number|null;
  serviceAverage?:number|null;vehicleConditionAverage?:number|null;vehicleCleanlinessAverage?:number|null;incidentCount:number;
};
export type FleetFeedbackDriverSummary = {driverUserId:string;driver:string;trips:number;eligibleParticipants:number;feedbackCount:number;responseRate:number;punctualityAverage?:number|null;safetyAverage?:number|null;serviceAverage?:number|null;overallAverage?:number|null;incidentCount:number};
export type FleetFeedbackVehicleSummary = {vehicleId:string;vehicle:string;registrationNumber:string;trips:number;feedbackCount:number;vehicleConditionAverage?:number|null;vehicleCleanlinessAverage?:number|null;incidentCount:number};
export type FleetFeedbackManagementSummary = {trips:number;eligibleParticipants:number;feedbackCount:number;responseRate:number;overallAverage?:number|null;safetyAverage?:number|null;incidentCount:number;attentionCount:number;attentionThreshold:number};
export type FleetFeedbackAttention = {feedbackId:string;tripId:string;fleetRequestId:string;requestNo:string;completedAt:string;destination:string;vehicle:string;driverUserId:string;driver:string;overallRating:number;safetyRating:number;hasIncident:boolean;incidentCategory?:string|null;comment?:string|null;submittedAt:string};
export type FleetFeedbackTrendPoint = {year:number;month:number;feedbackCount:number;overallAverage?:number|null;safetyAverage?:number|null;punctualityAverage?:number|null};
export type FleetFeedbackReportPage<T> = {items:T[];page:number;pageSize:number;totalItems:number;totalPages:number};
export async function getFleetFeedbackTripReport(params:Record<string,unknown>){return unwrap(await httpClient.get<ApiResponse<FleetFeedbackReportPage<FleetFeedbackTripSummary>>>("/fleet/reports/feedback/trips",{params}));}
export async function getFleetFeedbackDriverReport(params:Record<string,unknown>){return unwrap(await httpClient.get<ApiResponse<FleetFeedbackReportPage<FleetFeedbackDriverSummary>>>("/fleet/reports/feedback/drivers",{params}));}
export async function getFleetFeedbackVehicleReport(params:Record<string,unknown>){return unwrap(await httpClient.get<ApiResponse<FleetFeedbackReportPage<FleetFeedbackVehicleSummary>>>("/fleet/reports/feedback/vehicles",{params}));}
export async function getFleetFeedbackManagementSummary(params:Record<string,unknown>){return unwrap(await httpClient.get<ApiResponse<FleetFeedbackManagementSummary>>("/fleet/reports/feedback/summary",{params}));}
export async function getFleetFeedbackAttention(params:Record<string,unknown>){return unwrap(await httpClient.get<ApiResponse<FleetFeedbackReportPage<FleetFeedbackAttention>>>("/fleet/reports/feedback/attention",{params}));}
export async function getFleetFeedbackDriverTrend(driverUserId:string,params:Record<string,unknown>){return unwrap(await httpClient.get<ApiResponse<FleetFeedbackTrendPoint[]>>(`/fleet/reports/feedback/drivers/${driverUserId}/trend`,{params}));}
