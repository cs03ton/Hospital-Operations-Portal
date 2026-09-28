import AppsOutlinedIcon from "@mui/icons-material/AppsOutlined";
import CloseOutlinedIcon from "@mui/icons-material/CloseOutlined";
import { Box, ClickAwayListener, Fab, Grow, Tooltip, Typography, useMediaQuery } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { getFleetRolloutAccess } from "../../api/fleetApi";
import { quickServicePermissions, visibleQuickServices } from "../../config/quickServices";
import { useAuth } from "../../context/AuthContext";

export function QuickServiceFab() {
  const { user } = useAuth();
  const theme = useTheme();
  const navigate = useNavigate();
  const location = useLocation();
  const reducedMotion = useMediaQuery("(prefers-reduced-motion: reduce)");
  const [open, setOpen] = useState(false);
  const fabRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const hasFleetPermission = Boolean(user?.permissions?.includes(quickServicePermissions.fleet));
  const fleetAccess = useQuery({
    queryKey: ["fleet-rollout-access"], queryFn: getFleetRolloutAccess,
    enabled: hasFleetPermission, staleTime: 60_000, retry: 1,
  });
  const actions = visibleQuickServices(user?.permissions ?? [], user?.role, fleetAccess.data?.isAllowed === true);

  useEffect(() => { setOpen(false); }, [location.pathname]);
  useEffect(() => {
    if (!open) return;
    const onEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
        fabRef.current?.focus();
      }
    };
    document.addEventListener("keydown", onEscape);
    return () => document.removeEventListener("keydown", onEscape);
  }, [open]);

  if (actions.length === 0) return null;

  function handleAction(path: string) {
    setOpen(false);
    navigate(path);
  }

  function handleMenuKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
    const buttons = Array.from(menuRef.current?.querySelectorAll<HTMLButtonElement>('button[role="menuitem"]') ?? []);
    if (!buttons.length) return;
    event.preventDefault();
    const current = buttons.indexOf(document.activeElement as HTMLButtonElement);
    const next = event.key === "ArrowDown" ? (current + 1) % buttons.length : (current - 1 + buttons.length) % buttons.length;
    buttons[next].focus();
  }

  return (
    <ClickAwayListener onClickAway={() => setOpen(false)}>
      <Box sx={{ position: "fixed", right: { xs: 16, sm: 24 }, bottom: "calc(24px + env(safe-area-inset-bottom, 0px))", zIndex: theme.zIndex.appBar - 1, display: "flex", flexDirection: "column", alignItems: "flex-end", gap: 1.25 }}>
        <Grow in={open} timeout={reducedMotion ? 0 : 190} style={{ transformOrigin: "bottom right" }} onEntered={() => menuRef.current?.querySelector("button")?.focus()} unmountOnExit>
          <Box id="quick-service-menu" ref={menuRef} role="menu" aria-label="บริการด่วน" onKeyDown={handleMenuKeyDown}
            sx={{ display: "flex", flexDirection: "column", alignItems: "stretch", gap: 1, maxWidth: "calc(100vw - 32px)", maxHeight: "calc(100dvh - 120px)", overflowY: "auto", p: 1, bgcolor: "background.default", border: "1px solid", borderColor: alpha(theme.palette.secondary.main, 0.42), borderRadius: "18px", boxShadow: `0 14px 34px ${alpha(theme.palette.primary.dark, 0.16)}` }}>
            {actions.map(({ key, label, path, icon: Icon, color }) => (
              <Box component="button" type="button" role="menuitem" aria-label={label} key={key} onClick={() => handleAction(path)}
                sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 1.5, minWidth: { xs: 184, sm: 208 }, maxWidth: "calc(100vw - 48px)", minHeight: { xs: 48, sm: 52 }, px: 1.25, py: 0.75, bgcolor: "background.paper", color: "text.primary", border: "1px solid", borderColor: alpha(theme.palette.primary.main, 0.25), borderRadius: "14px", boxShadow: `0 3px 10px ${alpha(theme.palette.primary.dark, 0.08)}`, cursor: "pointer", font: "inherit", transition: reducedMotion ? "none" : "transform 190ms ease, background-color 190ms ease, color 190ms ease, border-color 190ms ease, box-shadow 190ms ease", "@media (hover: hover) and (pointer: fine)": { "&:hover": { transform: "translateY(-2px)", bgcolor: alpha(color, 0.07), color: "primary.dark", borderColor: alpha(color, 0.58), boxShadow: `0 9px 20px ${alpha(theme.palette.primary.dark, 0.16)}` }, "&:hover .quick-service-icon": { bgcolor: alpha(color, 0.22), borderColor: alpha(color, 0.48), transform: "scale(1.05)" } }, "&:focus-visible": { outline: `3px solid ${theme.palette.primary.main}`, outlineOffset: 2, bgcolor: alpha(color, 0.07), borderColor: alpha(color, 0.58) } }}>
                <Typography component="span" variant="body2" fontWeight={700} noWrap>{label}</Typography>
                <Box component="span" className="quick-service-icon" sx={{ display: "grid", placeItems: "center", width: 34, height: 34, flexShrink: 0, borderRadius: "50%", bgcolor: alpha(color, 0.13), border: "1px solid", borderColor: alpha(color, 0.25), color, transition: reducedMotion ? "none" : "transform 190ms ease, background-color 190ms ease, border-color 190ms ease" }}><Icon fontSize="small" /></Box>
              </Box>
            ))}
          </Box>
        </Grow>
        <Tooltip title={open ? "ปิดบริการด่วน" : "บริการด่วน"} placement="left">
          <Fab ref={fabRef} color="primary" aria-label={open ? "ปิดบริการด่วน" : "บริการด่วน"} aria-haspopup="menu" aria-expanded={open} aria-controls={open ? "quick-service-menu" : undefined}
            onClick={() => { setOpen((value) => !value); if (open) fabRef.current?.focus(); }}
            sx={{ width: { xs: 56, sm: 60 }, height: { xs: 56, sm: 60 }, bgcolor: "primary.main", color: "primary.contrastText", border: "2px solid", borderColor: "background.paper", boxShadow: `0 8px 22px ${alpha(theme.palette.primary.dark, 0.22)}`, transition: reducedMotion ? "none" : "transform 190ms ease, background-color 190ms ease, border-color 190ms ease, box-shadow 190ms ease", "@media (hover: hover) and (pointer: fine)": { "&:hover": { bgcolor: "primary.dark", borderColor: alpha(theme.palette.secondary.main, 0.8), transform: "translateY(-3px)", boxShadow: `0 14px 28px ${alpha(theme.palette.primary.dark, 0.28)}` } }, "&:focus-visible": { outline: `3px solid ${theme.palette.secondary.main}`, outlineOffset: 3 } }}>
            {open ? <CloseOutlinedIcon /> : <AppsOutlinedIcon />}
          </Fab>
        </Tooltip>
      </Box>
    </ClickAwayListener>
  );
}
