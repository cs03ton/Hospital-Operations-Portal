import { act, render } from "@testing-library/react";
import { QueryClient, QueryClientProvider, focusManager, onlineManager, useQuery } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  DASHBOARD_POLL_INTERVAL_MS,
  NOTIFICATION_POLL_INTERVAL_MS,
  dashboardPollingOptions,
  notificationPollingOptions,
} from "./queryPolling";

function PollingProbe({ queryFn, notification = false }: { queryFn: () => Promise<string>; notification?: boolean }) {
  useQuery({
    queryKey: [notification ? "notification-probe" : "dashboard-probe"],
    queryFn,
    ...(notification ? notificationPollingOptions : dashboardPollingOptions),
  });
  return null;
}

function renderProbe(queryFn: () => Promise<string>, notification = false) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const view = render(
    <QueryClientProvider client={client}>
      <PollingProbe queryFn={queryFn} notification={notification} />
    </QueryClientProvider>,
  );
  return { client, ...view };
}

async function flushQuery() {
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
  });
}

describe("query polling", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    focusManager.setFocused(true);
    onlineManager.setOnline(true);
  });

  afterEach(() => {
    focusManager.setFocused(true);
    onlineManager.setOnline(true);
    vi.useRealTimers();
  });

  it("polls dashboard data every 15 seconds", async () => {
    const queryFn = vi.fn().mockResolvedValue("ok");
    const { client, unmount } = renderProbe(queryFn);
    await flushQuery();
    expect(queryFn).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(DASHBOARD_POLL_INTERVAL_MS);
    });

    expect(queryFn).toHaveBeenCalledTimes(2);
    unmount();
    client.clear();
  });

  it("polls notifications every 30 seconds", async () => {
    const queryFn = vi.fn().mockResolvedValue("ok");
    const { client, unmount } = renderProbe(queryFn, true);
    await flushQuery();
    expect(queryFn).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(NOTIFICATION_POLL_INTERVAL_MS);
    });

    expect(queryFn).toHaveBeenCalledTimes(2);
    unmount();
    client.clear();
  });

  it("pauses dashboard polling while unfocused and refetches on focus", async () => {
    const queryFn = vi.fn().mockResolvedValue("ok");
    const { client, unmount } = renderProbe(queryFn);
    await flushQuery();
    expect(queryFn).toHaveBeenCalledTimes(1);

    focusManager.setFocused(false);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(DASHBOARD_POLL_INTERVAL_MS * 2);
    });
    expect(queryFn).toHaveBeenCalledTimes(1);

    await act(async () => {
      focusManager.setFocused(true);
      await Promise.resolve();
    });
    expect(queryFn).toHaveBeenCalledTimes(2);
    unmount();
    client.clear();
  });

  it("refetches dashboard data after reconnect", async () => {
    const queryFn = vi.fn().mockResolvedValue("ok");
    const { client, unmount } = renderProbe(queryFn);
    await flushQuery();
    expect(queryFn).toHaveBeenCalledTimes(1);

    onlineManager.setOnline(false);
    await act(async () => {
      onlineManager.setOnline(true);
      await Promise.resolve();
    });

    expect(queryFn).toHaveBeenCalledTimes(2);
    unmount();
    client.clear();
  });
});
