// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getMyProfile, type UserProfile } from "../../api/profileApi";
import { AppHeader } from "./AppHeader";

const state = vi.hoisted(() => ({ desktop: false }));
vi.mock("@mui/material", async (importOriginal) => ({
  ...await importOriginal<typeof import("@mui/material")>(),
  useMediaQuery: () => state.desktop,
}));
vi.mock("../../api/profileApi", () => ({ getMyProfile: vi.fn() }));
vi.mock("../../context/AuthContext", () => ({
  useAuth: () => ({ user: { id: "user-1", fullname: "Default Administrator", role: "SuperAdmin", profileImageUrl: "/old-image.png" }, logout: vi.fn() }),
}));
vi.mock("../notifications/NotificationBell", () => ({ NotificationBell: () => null }));
vi.mock("./PageBreadcrumbs", () => ({ PageBreadcrumbs: () => null }));
vi.mock("./PageTitle", () => ({ PageTitle: () => null }));

const profile = (url: string | null, hasProfileImage = Boolean(url)) => ({
  id: "user-1", fullname: "Default Administrator", username: "admin", profileImageUrl: url,
  hasProfileImage, gender: "Unspecified", roles: [], isActive: true, permissions: [],
}) as UserProfile;

function setup() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter><AppHeader drawerWidth={280} isSidebarCollapsed={false} onMobileMenuClick={() => {}} onToggleSidebar={() => {}} /></MemoryRouter>
    </QueryClientProvider>,
  );
  return { queryClient, ...view };
}

describe("AppHeader profile avatar", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    state.desktop = false;
  });

  it.each([false, true])("uses the uploaded image on mobile and desktop (desktop=%s)", async (desktop) => {
    state.desktop = desktop;
    vi.mocked(getMyProfile).mockResolvedValue(profile("/api/users/user-1/profile-image?v=2"));
    setup();
    await waitFor(() => screen.getAllByRole("img").forEach((avatar) =>
      expect(avatar).toHaveAttribute("src", "https://localhost:5000/api/users/user-1/profile-image?v=2")));
  });

  it("updates after replacement or deletion and falls back if the image fails", async () => {
    vi.mocked(getMyProfile).mockResolvedValue(profile("/api/users/user-1/profile-image?v=2"));
    const { queryClient } = setup();
    await waitFor(() => expect(screen.getAllByRole("img")[0]).toHaveAttribute("src", "https://localhost:5000/api/users/user-1/profile-image?v=2"));
    screen.getAllByRole("img").forEach((avatar) => fireEvent.error(avatar));
    await waitFor(() => expect(screen.queryAllByRole("img")).toHaveLength(0));
    expect(screen.getAllByText("D")).toHaveLength(2);

    queryClient.setQueryData(["me", "profile"], profile("/api/users/user-1/profile-image?v=3"));
    await waitFor(() => expect(screen.getAllByRole("img")[0]).toHaveAttribute("src", "https://localhost:5000/api/users/user-1/profile-image?v=3"));

    queryClient.setQueryData(["me", "profile"], profile(null, false));
    await waitFor(() => expect(screen.queryAllByRole("img")).toHaveLength(0));
    expect(screen.getAllByText("D")).toHaveLength(2);
  });
});
