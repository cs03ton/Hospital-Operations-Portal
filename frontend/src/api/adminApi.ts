import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type UserSummary = {
  id: string;
  employeeCode?: string | null;
  fullname: string;
  username: string;
  email?: string | null;
  roleIds: string[];
  roles: string[];
  departmentId?: string | null;
  department?: string | null;
  leaveApprovalRuleId?: string | null;
  leaveApprovalRuleName?: string | null;
  gender: string;
  employmentType?: string | null;
  employmentStartDate?: string | null;
  lineUserId?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
  lastLoginAt?: string | null;
};

export type SaveUserRequest = {
  employeeCode?: string | null;
  fullname: string;
  username?: string;
  password?: string;
  roleIds: string[];
  departmentId?: string | null;
  leaveApprovalRuleId?: string | null;
  gender?: string | null;
  employmentType?: string | null;
  employmentStartDate?: string | null;
  lineUserId?: string | null;
  isActive: boolean;
};

export type DepartmentSummary = {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
  usersCount: number;
};

export type SaveDepartmentRequest = {
  name: string;
  description?: string | null;
  isActive: boolean;
};

export type RoleSummary = {
  id: string;
  name: string;
  description?: string | null;
  isSystemRole: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
  usersCount: number;
  permissionsCount: number;
};

export type SaveRoleRequest = {
  name: string;
  description?: string | null;
  isActive: boolean;
};

export type PermissionSummary = {
  id: string;
  code: string;
  name: string;
  group: string;
  action: string;
  isActive: boolean;
  rolesCount: number;
  createdAt?: string | null;
  updatedAt?: string | null;
};

export type DeleteReferenceSummary = {
  label: string;
  count: number;
};

export type DeleteResult = {
  action: "Deleted" | "SoftDeleted" | "Blocked" | string;
  message: string;
  references: DeleteReferenceSummary[];
};

export type DashboardSummary = {
  totalUsers: number;
  totalDepartments: number;
  pendingApprovals: number;
  totalPendingLeaveRequests: number;
  openRepairRequests: number;
  activeBorrowRequests: number;
  inventoryItems: number;
  staffOnLeaveToday: number;
  staffOnLeaveThisWeek: number;
  staffOnLeaveThisMonth: number;
  myRemainingLeaveDays: number;
  myCoreLeaveBalances: DashboardLeaveBalance[];
  myLeaveRequestsTotal: number;
  myLeaveRequestsDraft: number;
  myLeaveRequestsPending: number;
  myLeaveRequestsReturnedForRevision: number;
  myLeaveRequestsApproved: number;
  myLeaveRequestsRejected: number;
  myLeaveRequestsCancelled: number;
  myLeaveCancellationRequestsPending: number;
  totalLeaveTypes: number;
  totalApprovalRules: number;
  totalHolidaysThisYear: number;
  totalAuditLogsToday: number;
  loginEventsToday: number;
  failedLoginEventsToday: number;
  permissionDeniedEventsToday: number;
  unreadNotifications: number;
  lineQueued: number;
  lineFailed: number;
  apiHealth: string;
  databaseStatus: string;
  applicationVersion: string;
  myPendingRequests: DashboardLeaveRequestGroup;
  departmentRequests: DashboardLeaveRequestGroup;
  myRecentLeaveRequests: DashboardLeaveRequestGroup;
  leaveCancellationSummary?: DashboardLeaveCancellationSummary | null;
};

export type DashboardLeaveBalance = {
  leaveTypeCode: string;
  leaveTypeName: string;
  entitledDays: number;
  usedDays: number;
  pendingDays: number;
  availableDays: number;
};

export type DashboardLeaveRequestGroup = {
  count: number;
  items: DashboardLeaveRequestItem[];
};

export type DashboardLeaveRequestItem = {
  id: string;
  requestNumber?: string | null;
  requesterName: string;
  leaveTypeName?: string | null;
  startDate: string;
  endDate: string;
  totalDays: number;
  status: string;
  currentApproverName?: string | null;
  createdAt: string;
  sourceType?: string | null;
  detailPath?: string | null;
};

export type DashboardLeaveCancellationSummary = {
  total: number;
  pending: number;
  approved: number;
  rejected: number;
  cancelled: number;
  returnedForRevision: number;
  draft: number;
  pendingApprovalsForMe: number;
  approvedToday: number;
  rejectedToday: number;
  restoredDaysThisYear: number;
  restoredDaysTotal: number;
  averageApprovalHours?: number | null;
  approvalRate: number;
  rejectionRate: number;
  monthlyTrend: DashboardLeaveCancellationTrend[];
  byLeaveType: DashboardLeaveCancellationBreakdown[];
  byDepartment: DashboardLeaveCancellationBreakdown[];
  recentRequests: DashboardLeaveRequestGroup;
};

export type DashboardLeaveCancellationTrend = {
  month: string;
  requestCount: number;
  restoredDays: number;
};

export type DashboardLeaveCancellationBreakdown = {
  name: string;
  requestCount: number;
  restoredDays: number;
};

export type HealthComponent = {
  status: string;
  message?: string | null;
  latencyMs?: number | null;
  uptimeSeconds?: number | null;
  provider?: string | null;
};

export type StorageHealth = {
  status: string;
  writable: boolean;
  message?: string | null;
  path?: string | null;
};

export type LineHealth = {
  status: string;
  enabled: boolean;
  lastSuccessAt?: string | null;
  lastFailureAt?: string | null;
  message?: string | null;
  hasAccessToken?: boolean;
  hasChannelSecret?: boolean;
  lastError?: string | null;
};

export type QueueHealth = {
  status: string;
  lineRetryEnabled: boolean;
  approvalEscalationEnabled: boolean;
  pendingLineDeliveries: number;
  failedLineDeliveries: number;
  pendingRetries: number;
  lastLineSuccessAt?: string | null;
  lastLineFailureAt?: string | null;
  message?: string | null;
};

export type DiskHealth = {
  status: string;
  usedPercent?: number | null;
  message?: string | null;
  totalGb?: number | null;
  usedGb?: number | null;
  freeGb?: number | null;
};

export type MemoryHealth = {
  status: string;
  totalMb?: number | null;
  usedMb?: number | null;
  availableMb?: number | null;
  usedPercent?: number | null;
  message?: string | null;
};

export type CpuHealth = {
  status: string;
  processorCount: number;
  loadAverage?: string | null;
  message?: string | null;
};

export type BackupHealth = {
  status: string;
  lastBackupAt?: string | null;
  message?: string | null;
  lastRestoreTestAt?: string | null;
  backupDirectory?: string | null;
  latestBackupSizeBytes?: number | null;
  latestBackupFile?: string | null;
};

export type LeaveCancellationHealth = {
  status: string;
  pendingApproval: number;
  failedNotification: number;
  failedReferenceIntegrity: number;
  failedBalanceRestore: number;
  message?: string | null;
};

export type AdminHealth = {
  overallStatus: string;
  checkedAt: string;
  api: HealthComponent;
  database: HealthComponent;
  storage: StorageHealth;
  line: LineHealth;
  queue: QueueHealth;
  disk: DiskHealth;
  memory: MemoryHealth;
  cpu: CpuHealth;
  backup: BackupHealth;
  leaveCancellation: LeaveCancellationHealth;
  version: string;
  environment: string;
  currentTimeServer: string;
  gitCommit?: string | null;
  timezone?: string | null;
};

export type DiagnosticServiceStatus = {
  key: string;
  label: string;
  status: string;
  message?: string | null;
  latencyMs?: number | null;
  details?: Record<string, string | null> | null;
};

export type DiagnosticInfo = {
  status: string;
  message?: string | null;
  timestamp?: string | null;
  reference?: string | null;
};

export type DiagnosticsSummary = {
  checkedAt: string;
  environment: string;
  version: string;
  gitCommit?: string | null;
  services: Record<string, DiagnosticServiceStatus>;
  recentErrors: RecentError[];
  lastDeploy: DiagnosticInfo;
  lastMigration: DiagnosticInfo;
};

export type DiagnosticTestResult = {
  runId: string;
  diagnosticType: string;
  status: string;
  message: string;
  referenceId: string;
  durationMs: number;
};

export type DiagnosticRun = {
  id: string;
  diagnosticType: string;
  status: string;
  startedAt: string;
  completedAt?: string | null;
  durationMs?: number | null;
  resultSummary?: string | null;
  referenceId?: string | null;
  createdBy?: string | null;
  errorMessage?: string | null;
};

export type RecentError = {
  timestamp: string;
  module: string;
  message: string;
  referenceId?: string | null;
  actor?: string | null;
  requestPath?: string | null;
  statusCode?: string | null;
};

export type DiagnosticsLogQuery = {
  source?: string;
  severity?: string;
  search?: string;
  page?: number;
  pageSize?: number;
};

export type DiagnosticsLogLine = {
  timestamp?: string | null;
  severity: string;
  message: string;
};

export type DiagnosticsLogResponse = {
  source: string;
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  items: DiagnosticsLogLine[];
};

export type SupportBundleRequest = {
  includeAppLogs: boolean;
  includeNginxLogs: boolean;
  includePostgresLogs: boolean;
  includeHealth: boolean;
  includeDeployInfo: boolean;
  includeMigrationInfo: boolean;
  includeLineSummary: boolean;
  includeBackupSummary: boolean;
  timeRangeHours: number;
  reason: string;
};

export type SupportBundle = {
  id: string;
  fileName: string;
  fileSizeBytes: number;
  checksum?: string | null;
  expiresAt: string;
  status: string;
  createdAt: string;
  downloadUrl?: string | null;
  reason?: string | null;
  createdBy?: string | null;
  downloadedAt?: string | null;
  deletedAt?: string | null;
};

export type BackupRun = {
  id: string;
  backupType: string;
  status: string;
  fileName: string;
  relativePath: string;
  fileSizeBytes: number;
  checksum?: string | null;
  startedAt: string;
  completedAt?: string | null;
  durationMs?: number | null;
  errorMessage?: string | null;
  createdBy?: string | null;
  verifiedAt?: string | null;
  verifiedBy?: string | null;
  deletedAt?: string | null;
};

export type BackupRunDetail = {
  backup: BackupRun;
  logSummary: string;
  canRestore: boolean;
  isVerified: boolean;
  restoreWarnings: string[];
  restoreErrors: string[];
};

export type BackupRetentionPolicy = {
  dailyDays: number;
  weeklyWeeks: number;
  monthlyMonths: number;
  keepVerified: boolean;
  keepFailedDays: number;
};

export type BackupOverview = {
  lastSuccessfulBackup?: BackupRun | null;
  lastFailedBackup?: BackupRun | null;
  lastVerifiedBackup?: BackupRun | null;
  lastRestoreTest?: RestoreRun | null;
  totalBackupSizeBytes: number;
  backupRoot: string;
  retentionPolicy: BackupRetentionPolicy;
};

export type BackupQuery = {
  page?: number;
  pageSize?: number;
  type?: string;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  search?: string;
  sort?: string;
  direction?: "asc" | "desc";
};

export type RestorePreview = {
  canRestore: boolean;
  warnings: string[];
  errors: string[];
  backupInfo: BackupRun;
  currentEnvironment: string;
  recommendedMode: string;
  freeDiskBytes?: number | null;
};

export type RestoreRequest = {
  confirmationText: string;
  reason: string;
  restoreDatabase: boolean;
  restoreStorage: boolean;
  restoreMode: string;
  targetDatabase?: string | null;
};

export type RestoreRun = {
  id: string;
  backupRunId: string;
  backupFileName: string;
  restoreType: string;
  targetEnvironment: string;
  targetDatabase?: string | null;
  status: string;
  reason: string;
  startedAt: string;
  completedAt?: string | null;
  durationMs?: number | null;
  errorMessage?: string | null;
  createdBy?: string | null;
  confirmationMethod: string;
  preRestoreBackupRunId?: string | null;
};

export type BackupVerification = {
  backupId: string;
  status: string;
  message: string;
  checksum?: string | null;
  verifiedAt: string;
};

export type RetentionPreviewItem = {
  backupId: string;
  fileName: string;
  createdAt: string;
  type: string;
  status: string;
  action: string;
  reason: string;
  fileSizeBytes: number;
};

export type RetentionPreview = {
  totalFiles: number;
  keep: number;
  delete: number;
  freedBytes: number;
  items: RetentionPreviewItem[];
};

export type ApplyRetentionRequest = {
  reason: string;
  confirmationText: string;
};

export type ApplyRetentionResponse = {
  deletedCount: number;
  freedBytes: number;
  items: RetentionPreviewItem[];
};

export type AdminDashboard = {
  users: AdminDashboardUsers;
  departments: AdminDashboardDepartments;
  roles: AdminDashboardRoles;
  line: AdminDashboardLine;
  leave: AdminDashboardLeave;
  health: AdminDashboardHealth;
  audit: AdminDashboardAudit;
};

export type AdminDashboardUsers = {
  total: number;
  active: number;
  inactive: number;
  missingLineBinding: number;
  missingEmploymentType: number;
  missingApprovalRule: number;
};

export type AdminDashboardDepartments = {
  total: number;
  withoutHead: number;
  withoutUsers: number;
};

export type AdminDashboardRoles = {
  total: number;
  permissions: number;
  unusedRoles: number;
  importantPermissionsUnassigned: number;
};

export type AdminDashboardLine = {
  enabled: boolean;
  boundUsers: number;
  unboundUsers: number;
  lastFailedDeliveryAt?: string | null;
};

export type AdminDashboardLeave = {
  pendingApprovals: number;
  todayRequests: number;
  missingBalances: number;
  missingApprovalRules: number;
};

export type AdminDashboardHealth = {
  overallStatus: string;
  api: HealthComponent;
  database: HealthComponent;
  storage: StorageHealth;
  line: LineHealth;
  disk: DiskHealth;
  backup: BackupHealth;
};

export type AdminDashboardAudit = {
  recentFailedLogins: number;
  recentPermissionDenied: number;
  recentAdminActions: AdminDashboardAuditAction[];
};

export type AdminDashboardAuditAction = {
  createdAt: string;
  action: string;
  entityName: string;
  result: string;
  actorName?: string | null;
};

export type ExecutiveKpis = {
  totalActiveUsers: number;
  presentToday: number;
  onLeaveToday: number;
  pendingApprovals: number;
  directorPendingApprovals: number;
  approvedToday: number;
  rejectedToday: number;
  leaveRate: number;
  approvalSlaHours?: number | null;
};

export type ExecutiveTodaySummary = {
  totalLeaveToday: number;
  sickLeaveToday: number;
  personalLeaveToday: number;
  vacationLeaveToday: number;
  pendingApprovals: number;
  approvedToday: number;
  rejectedToday: number;
  topDepartmentToday?: string | null;
};

export type ExecutiveMonthlyTrend = {
  month: string;
  sickLeaveDays: number;
  personalLeaveDays: number;
  vacationLeaveDays: number;
  totalDays: number;
};

export type ExecutiveDepartmentLeave = {
  departmentName: string;
  userCount: number;
  totalDays: number;
};

export type ExecutiveLeaveType = {
  leaveTypeCode: string;
  leaveTypeName: string;
  requestCount: number;
  totalDays: number;
};

export type ExecutiveYearlySummary = {
  fiscalYear: number;
  leaveTypeCode: string;
  leaveTypeName: string;
  usedDays: number;
};

export type ExecutiveSystemHealth = {
  api: HealthComponent;
  database: HealthComponent;
  storage: StorageHealth;
  line: LineHealth;
  disk: DiskHealth;
  backup: BackupHealth;
  version: string;
  environment: string;
};

export type ExecutiveDashboard = {
  kpis: ExecutiveKpis;
  todaySummary: ExecutiveTodaySummary;
  monthlyTrend: ExecutiveMonthlyTrend[];
  leaveByDepartment: ExecutiveDepartmentLeave[];
  leaveByType: ExecutiveLeaveType[];
  yearlySummary: ExecutiveYearlySummary[];
  systemHealth: ExecutiveSystemHealth;
};

export type ExecutiveDashboardQuery = {
  trendMonth?: number;
  trendYear?: number;
  fiscalYear?: number;
};

export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type AuditLogSummary = {
  id: string;
  userId?: string | null;
  username?: string | null;
  fullname?: string | null;
  action: string;
  resource: string;
  resourceId?: string | null;
  detail?: string | null;
  ipAddress?: string | null;
  result: string;
  timestamp: string;
};

export type AuditLogQuery = {
  page?: number;
  pageSize?: number;
  search?: string;
  userId?: string;
  action?: string;
  from?: string;
  to?: string;
};

export type ManagementQuery = {
  page?: number;
  pageSize?: number;
  search?: string;
  sort?: string;
  direction?: "asc" | "desc";
  status?: string;
  departmentId?: string;
  roleId?: string;
  employmentType?: string;
  hasLine?: boolean;
  module?: string;
};

export type SystemSettings = {
  hospitalName: string;
  hospitalLogoPath: string;
  footerText: string;
  footerDeveloper: string;
  themePrimaryColor: string;
  themeSecondaryColor: string;
  applicationVersion: string;
  pdfTemplateConfigPath: string;
  pdfFontPath: string;
  pdfFontFamily: string;
  pdfFontSize: number;
  pdfLineHeight: number;
  lineEnabled: boolean;
  lineChannelAccessTokenConfigured: boolean;
  lineEndpoint: string;
};

export type LineSettings = {
  enabled: boolean;
  channelId?: string | null;
  hasChannelSecret: boolean;
  hasAccessToken: boolean;
  channelSecretConfigured: boolean;
  channelAccessTokenConfigured: boolean;
  testUserId?: string | null;
  endpoint: string;
};

export type LineTestSendRequest = {
  toUserId?: string | null;
  message: string;
};

export type LineTestSendResponse = {
  success: boolean;
  message: string;
  deliveryStatus?: string | null;
  deliveryLogId?: string | null;
  httpStatusCode?: number | null;
  responseTimeMs?: number | null;
  error?: string | null;
};

export type LineChecklistItem = {
  label: string;
  passed: boolean;
  recommendation: string;
};

export type LineOperationsStatus = {
  enabled: boolean;
  connectionStatus: string;
  channelIdMasked?: string | null;
  hasChannelSecret: boolean;
  hasAccessToken: boolean;
  hasTestUserId: boolean;
  testUserIdMasked?: string | null;
  endpoint: string;
  environment: string;
  webhookActive: boolean;
  botName?: string | null;
  lastSuccessfulDelivery?: string | null;
  lastFailedDelivery?: string | null;
  queueLength: number;
  pendingRetry: number;
  averageResponseTimeMs?: number | null;
  checklist: LineChecklistItem[];
};

export type LineConnectionValidation = {
  success: boolean;
  message: string;
  httpStatusCode?: number | null;
  responseTimeMs: number;
  botName?: string | null;
  checklist: LineChecklistItem[];
};

export type LineDeliveryLog = {
  id: string;
  date: string;
  recipient: string;
  module: string;
  event: string;
  status: string;
  retry: number;
  durationMs?: number | null;
  error?: string | null;
  requestType?: string | null;
  sanitizedRecipient?: string | null;
  payloadPreview?: string | null;
  httpStatusCode?: number | null;
  responseBody?: string | null;
};

export type LineNotificationSimulatorRequest = {
  userId: string;
  eventType: string;
  message?: string | null;
};

export type LineFlexPreview = {
  payload: string;
  validation: LineChecklistItem[];
};

export type LineFlexValidateRequest = {
  payload: string;
};

export type LineFlexValidateResponse = {
  isValid: boolean;
  message: string;
  checks: LineChecklistItem[];
};

export type LineFlexTestSendRequest = {
  toUserId?: string | null;
  leaveRequestId?: string | null;
  payload?: string | null;
  variant?: string | null;
  avatarMode?: string | null;
};

export type LineUserBinding = {
  id: string;
  lineUserIdMasked: string;
  displayName?: string | null;
  pictureUrl?: string | null;
  userId?: string | null;
  fullname?: string | null;
  username?: string | null;
  status: string;
  lastEventType?: string | null;
  lastEventAt?: string | null;
  boundAt?: string | null;
  unboundAt?: string | null;
  createdAt: string;
};

export type LineUserBindingStats = {
  pendingConnectTokenCount: number;
  expiredConnectTokenCount: number;
  recentlyBoundUserCount: number;
};

export async function getUsers() {
  const response = await httpClient.get<ApiResponse<UserSummary[]>>("/api/users");
  return response.data.data;
}

export async function getUsersPaged(params: ManagementQuery = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<UserSummary>>>("/api/users", { params });
  return response.data.data;
}

export async function getUser(id: string) {
  const response = await httpClient.get<ApiResponse<UserSummary>>(`/api/users/${id}`);
  return response.data.data;
}

export async function createUser(payload: SaveUserRequest) {
  const response = await httpClient.post<ApiResponse<UserSummary>>("/api/users", payload);
  return response.data.data;
}

export async function updateUser(id: string, payload: SaveUserRequest) {
  const response = await httpClient.put<ApiResponse<UserSummary>>(`/api/users/${id}`, payload);
  return response.data.data;
}

export async function deleteUser(id: string) {
  const response = await httpClient.delete<ApiResponse<DeleteResult>>(`/api/users/${id}`);
  return response.data.data;
}

export const deactivateUser = deleteUser;

export async function getDepartments() {
  const response = await httpClient.get<ApiResponse<DepartmentSummary[]>>("/api/departments");
  return response.data.data;
}

export async function getDepartmentsPaged(params: ManagementQuery = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<DepartmentSummary>>>("/api/departments", { params });
  return response.data.data;
}

export async function getDepartment(id: string) {
  const response = await httpClient.get<ApiResponse<DepartmentSummary>>(`/api/departments/${id}`);
  return response.data.data;
}

export async function createDepartment(payload: SaveDepartmentRequest) {
  const response = await httpClient.post<ApiResponse<DepartmentSummary>>("/api/departments", payload);
  return response.data.data;
}

export async function updateDepartment(id: string, payload: SaveDepartmentRequest) {
  const response = await httpClient.put<ApiResponse<DepartmentSummary>>(`/api/departments/${id}`, payload);
  return response.data.data;
}

export async function deleteDepartment(id: string) {
  const response = await httpClient.delete<ApiResponse<DeleteResult>>(`/api/departments/${id}`);
  return response.data.data;
}

export async function getRoles() {
  const response = await httpClient.get<ApiResponse<RoleSummary[]>>("/api/roles");
  return response.data.data;
}

export async function getRolesPaged(params: ManagementQuery = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<RoleSummary>>>("/api/roles", { params });
  return response.data.data;
}

export async function getRole(id: string) {
  const response = await httpClient.get<ApiResponse<RoleSummary>>(`/api/roles/${id}`);
  return response.data.data;
}

export async function createRole(payload: SaveRoleRequest) {
  const response = await httpClient.post<ApiResponse<RoleSummary>>("/api/roles", payload);
  return response.data.data;
}

export async function updateRole(id: string, payload: SaveRoleRequest) {
  const response = await httpClient.put<ApiResponse<RoleSummary>>(`/api/roles/${id}`, payload);
  return response.data.data;
}

export async function deleteRole(id: string) {
  const response = await httpClient.delete<ApiResponse<DeleteResult>>(`/api/roles/${id}`);
  return response.data.data;
}

export const deactivateRole = deleteRole;

export async function getPermissions() {
  const response = await httpClient.get<ApiResponse<PermissionSummary[]>>("/api/permissions");
  return response.data.data;
}

export async function getPermissionsPaged(params: ManagementQuery = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<PermissionSummary>>>("/api/permissions", { params });
  return response.data.data;
}

export async function deletePermission(id: string) {
  const response = await httpClient.delete<ApiResponse<DeleteResult>>(`/api/permissions/${id}`);
  return response.data.data;
}

export async function getRolePermissions(roleId: string) {
  const response = await httpClient.get<ApiResponse<PermissionSummary[]>>(
    `/api/roles/${roleId}/permissions`,
  );
  return response.data.data;
}

export async function updateRolePermissions(roleId: string, permissionIds: string[]) {
  const response = await httpClient.put<ApiResponse<PermissionSummary[]>>(
    `/api/roles/${roleId}/permissions`,
    { permissionIds },
  );
  return response.data.data;
}

export async function getDashboardSummary() {
  const response = await httpClient.get<ApiResponse<DashboardSummary>>("/api/dashboard/summary");
  return response.data.data;
}

export async function getAdminHealth() {
  const response = await httpClient.get<ApiResponse<AdminHealth>>("/api/admin/health");
  return response.data.data;
}

export async function getDiagnosticsSummary() {
  const response = await httpClient.get<ApiResponse<DiagnosticsSummary>>("/api/admin/diagnostics/summary");
  return response.data.data;
}

export async function runDiagnosticTest(diagnosticType: string) {
  const response = await httpClient.post<ApiResponse<DiagnosticTestResult>>(`/api/admin/diagnostics/test/${diagnosticType}`);
  return response.data.data;
}

export async function getDiagnosticsLogs(params: DiagnosticsLogQuery = {}) {
  const response = await httpClient.get<ApiResponse<DiagnosticsLogResponse>>("/api/admin/diagnostics/logs", { params });
  return response.data.data;
}

export async function getDiagnosticsRecentErrors() {
  const response = await httpClient.get<ApiResponse<RecentError[]>>("/api/admin/diagnostics/recent-errors");
  return response.data.data;
}

export async function getDiagnosticsHistory() {
  const response = await httpClient.get<ApiResponse<DiagnosticRun[]>>("/api/admin/diagnostics/history");
  return response.data.data;
}

export async function createSupportBundle(payload: SupportBundleRequest) {
  const response = await httpClient.post<ApiResponse<SupportBundle>>("/api/admin/diagnostics/support-bundle", payload);
  return response.data.data;
}

export async function getSupportBundles() {
  const response = await httpClient.get<ApiResponse<SupportBundle[]>>("/api/admin/diagnostics/support-bundles");
  return response.data.data;
}

export function getSupportBundleDownloadUrl(id: string) {
  const baseUrl = httpClient.defaults.baseURL ?? "";
  return `${baseUrl}/api/admin/diagnostics/support-bundle/${id}/download`;
}

export async function downloadSupportBundle(id: string) {
  const response = await httpClient.get(`/api/admin/diagnostics/support-bundle/${id}/download`, { responseType: "blob" });
  return response.data as Blob;
}

export async function getBackupOverview() {
  const response = await httpClient.get<ApiResponse<BackupOverview>>("/api/admin/backups/overview");
  return response.data.data;
}

export async function getBackups(params: BackupQuery = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<BackupRun>>>("/api/admin/backups", { params });
  return response.data.data;
}

export async function getBackup(id: string) {
  const response = await httpClient.get<ApiResponse<BackupRunDetail>>(`/api/admin/backups/${id}`);
  return response.data.data;
}

export async function verifyBackup(id: string) {
  const response = await httpClient.post<ApiResponse<BackupVerification>>(`/api/admin/backups/${id}/verify`);
  return response.data.data;
}

export async function previewRestore(id: string) {
  const response = await httpClient.post<ApiResponse<RestorePreview>>(`/api/admin/backups/${id}/restore-preview`);
  return response.data.data;
}

export async function confirmRestore(id: string, payload: RestoreRequest) {
  const response = await httpClient.post<ApiResponse<RestoreRun>>(`/api/admin/backups/${id}/restore`, payload);
  return response.data.data;
}

export async function getRestoreRuns(params: BackupQuery = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<RestoreRun>>>("/api/admin/backups/restore-runs", { params });
  return response.data.data;
}

export async function previewRetention() {
  const response = await httpClient.post<ApiResponse<RetentionPreview>>("/api/admin/backups/retention/preview");
  return response.data.data;
}

export async function applyRetention(payload: ApplyRetentionRequest) {
  const response = await httpClient.post<ApiResponse<ApplyRetentionResponse>>("/api/admin/backups/retention/apply", payload);
  return response.data.data;
}

export async function getAdminDashboard() {
  const response = await httpClient.get<ApiResponse<AdminDashboard>>("/api/admin/dashboard");
  return response.data.data;
}

export async function getExecutiveDashboard(params?: ExecutiveDashboardQuery) {
  const response = await httpClient.get<ApiResponse<ExecutiveDashboard>>("/api/dashboard/executive", { params });
  return response.data.data;
}

export async function getAuditLogs(params: AuditLogQuery = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<AuditLogSummary>>>(
    "/api/audit-logs",
    { params },
  );
  return response.data.data;
}

export async function getAuditLog(id: string) {
  const response = await httpClient.get<ApiResponse<AuditLogSummary>>(`/api/audit-logs/${id}`);
  return response.data.data;
}

export async function downloadAuditLogExcel(params: AuditLogQuery = {}) {
  const response = await httpClient.get("/api/audit-logs/export-excel", { params, responseType: "blob" });
  return response.data as Blob;
}

export async function downloadAuditLogPdf(params: AuditLogQuery = {}) {
  const response = await httpClient.get("/api/audit-logs/export-pdf", { params, responseType: "blob" });
  return response.data as Blob;
}

export async function getSystemSettings() {
  const response = await httpClient.get<ApiResponse<SystemSettings>>("/api/system-settings");
  return response.data.data;
}

export async function getLineSettings() {
  const response = await httpClient.get<ApiResponse<LineSettings>>("/api/admin/line/settings");
  return response.data.data;
}

export async function getLineOperationsStatus() {
  const response = await httpClient.get<ApiResponse<LineOperationsStatus>>("/api/admin/line/operations-status");
  return response.data.data;
}

export async function validateLineConnection() {
  const response = await httpClient.post<ApiResponse<LineConnectionValidation>>("/api/admin/line/validate");
  return response.data.data;
}

export async function sendLineTestMessage(payload: LineTestSendRequest) {
  const response = await httpClient.post<ApiResponse<LineTestSendResponse>>("/api/admin/line/test-send", payload);
  return response.data.data;
}

export async function getLineTestHistory(params: { page?: number; pageSize?: number; search?: string } = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<LineDeliveryLog>>>("/api/admin/line/test-history", { params });
  return response.data.data;
}

export async function getLineDeliveryLogs(params: { page?: number; pageSize?: number; status?: string; search?: string } = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<LineDeliveryLog>>>("/api/admin/line/delivery-logs", { params });
  return response.data.data;
}

export async function simulateLineNotification(payload: LineNotificationSimulatorRequest) {
  const response = await httpClient.post<ApiResponse<LineTestSendResponse>>("/api/admin/line/simulate", payload);
  return response.data.data;
}

export async function getLineFlexPreview(params: { leaveRequestId?: string | null; variant?: string | null; avatarMode?: string | null } = {}) {
  const response = await httpClient.get<ApiResponse<LineFlexPreview>>("/api/admin/line/flex-preview", { params });
  return response.data.data;
}

export async function validateLineFlexPayload(payload: LineFlexValidateRequest) {
  const response = await httpClient.post<ApiResponse<LineFlexValidateResponse>>("/api/admin/line/validate-flex", payload);
  return response.data.data;
}

export async function sendLineTestFlex(payload: LineFlexTestSendRequest) {
  const response = await httpClient.post<ApiResponse<LineTestSendResponse>>("/api/admin/line/test-flex", payload);
  return response.data.data;
}

export async function getLineUsers(params: { page?: number; pageSize?: number; status?: string; search?: string } = {}) {
  const response = await httpClient.get<ApiResponse<PagedResponse<LineUserBinding>>>("/api/admin/line/line-users", { params });
  return response.data.data;
}

export async function getLineUserStats() {
  const response = await httpClient.get<ApiResponse<LineUserBindingStats>>("/api/admin/line/line-users/stats");
  return response.data.data;
}

export async function sendLineUserTestMessage(id: string, message = "ทดสอบการแจ้งเตือนจาก HOP") {
  const response = await httpClient.post<ApiResponse<LineTestSendResponse>>(`/api/admin/line/line-users/${id}/test-send`, { message });
  return response.data.data;
}
