import { createContext, useContext, useMemo } from "react";
import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";

type PermissionContextValue = {
  permissions: string[];
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  hasAllPermissions: (permissions: string[]) => boolean;
};

type PermissionGuardProps = {
  children: ReactNode;
  permission?: string;
  permissions?: string[];
  requireAll?: boolean;
  fallback?: ReactNode;
  redirectTo?: string;
};

const PermissionContext = createContext<PermissionContextValue | null>(null);

export function PermissionProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const permissions = user?.permissions ?? [];

  const value = useMemo<PermissionContextValue>(() => {
    const permissionSet = new Set(permissions);

    return {
      permissions,
      hasPermission: (permission) => permissionSet.has(permission),
      hasAnyPermission: (items) => items.some((permission) => permissionSet.has(permission)),
      hasAllPermissions: (items) => items.every((permission) => permissionSet.has(permission)),
    };
  }, [permissions]);

  return <PermissionContext.Provider value={value}>{children}</PermissionContext.Provider>;
}

export function PermissionGuard({
  children,
  permission,
  permissions,
  requireAll = false,
  fallback = null,
  redirectTo,
}: PermissionGuardProps) {
  const { hasPermission, hasAnyPermission, hasAllPermissions } = usePermission();
  const requiredPermissions = permissions ?? (permission ? [permission] : []);
  const allowed =
    requiredPermissions.length === 0 ||
    (requireAll ? hasAllPermissions(requiredPermissions) : hasAnyPermission(requiredPermissions));

  if (allowed) {
    return <>{children}</>;
  }

  if (redirectTo) {
    return <Navigate to={redirectTo} replace />;
  }

  return <>{fallback}</>;
}

export function usePermission() {
  const context = useContext(PermissionContext);
  if (!context) {
    throw new Error("usePermission must be used inside PermissionProvider.");
  }

  return context;
}
