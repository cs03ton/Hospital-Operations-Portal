import { Breadcrumbs, Link, Typography } from "@mui/material";
import { Link as RouterLink, useLocation } from "react-router-dom";
import { getPageBreadcrumbs } from "../../config/pageTitleConfig";

export function PageBreadcrumbs() {
  const location = useLocation();
  const breadcrumbs = getPageBreadcrumbs(location.pathname);

  if (breadcrumbs.length <= 1) {
    return null;
  }

  return (
    <Breadcrumbs aria-label="breadcrumb" sx={{ mb: 0.25 }}>
      {breadcrumbs.map((item, index) => {
        const isLast = index === breadcrumbs.length - 1;
        if (isLast || !item.path) {
          return (
            <Typography key={`${item.label}-${index}`} variant="caption" color="text.secondary">
              {item.label}
            </Typography>
          );
        }

        return (
          <Link
            key={`${item.label}-${index}`}
            component={RouterLink}
            to={item.path}
            underline="hover"
            variant="caption"
            color="text.secondary"
          >
            {item.label}
          </Link>
        );
      })}
    </Breadcrumbs>
  );
}
