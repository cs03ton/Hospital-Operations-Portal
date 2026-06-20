import { useEffect, useState } from "react";

const sidebarStorageKey = "hop.sidebar.collapsed";

export function useSidebarState() {
  const [isCollapsed, setIsCollapsed] = useState(() => localStorage.getItem(sidebarStorageKey) === "true");
  const [isMobileOpen, setIsMobileOpen] = useState(false);

  useEffect(() => {
    localStorage.setItem(sidebarStorageKey, String(isCollapsed));
  }, [isCollapsed]);

  return {
    isCollapsed,
    isMobileOpen,
    closeMobileSidebar: () => setIsMobileOpen(false),
    expandSidebar: () => setIsCollapsed(false),
    openMobileSidebar: () => setIsMobileOpen(true),
    toggleSidebar: () => setIsCollapsed((current) => !current),
  };
}
