export const DASHBOARD_POLL_INTERVAL_MS = 15_000;
export const NOTIFICATION_POLL_INTERVAL_MS = 30_000;

export const dashboardPollingOptions = {
  refetchInterval: DASHBOARD_POLL_INTERVAL_MS,
  refetchIntervalInBackground: false,
  refetchOnWindowFocus: true,
  refetchOnReconnect: true,
  staleTime: 0,
} as const;

export const notificationPollingOptions = {
  refetchInterval: NOTIFICATION_POLL_INTERVAL_MS,
  refetchIntervalInBackground: false,
  refetchOnWindowFocus: true,
  refetchOnReconnect: true,
} as const;
