import ErrorOutlineOutlinedIcon from "@mui/icons-material/ErrorOutlineOutlined";
import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import { Box, Button, Card, CardContent, Stack, Typography } from "@mui/material";
import React from "react";

type ErrorBoundaryState = {
  hasError: boolean;
  referenceId: string;
  error?: Error;
};

export class ErrorBoundary extends React.Component<React.PropsWithChildren, ErrorBoundaryState> {
  state: ErrorBoundaryState = {
    hasError: false,
    referenceId: "",
  };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return {
      hasError: true,
      referenceId: createReferenceId(),
      error,
    };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    console.error("Frontend error boundary caught an error", {
      referenceId: this.state.referenceId,
      error,
      componentStack: info.componentStack,
    });
  }

  render() {
    if (!this.state.hasError) {
      return this.props.children;
    }

    const showDetail = import.meta.env.DEV;

    return (
      <Box sx={{ minHeight: "100vh", display: "grid", placeItems: "center", p: 2, bgcolor: "background.default" }}>
        <Card sx={{ maxWidth: 620, width: "100%" }}>
          <CardContent>
            <Stack spacing={2.25} alignItems="flex-start">
              <ErrorOutlineOutlinedIcon color="error" sx={{ fontSize: 42 }} />
              <Box>
                <Typography variant="h4" color="primary" gutterBottom>
                  เกิดข้อผิดพลาด
                </Typography>
                <Typography color="text.secondary">
                  กรุณาลองใหม่อีกครั้ง หรือแจ้งผู้ดูแลระบบ
                </Typography>
              </Box>
              <Typography fontWeight={800}>Reference ID: {this.state.referenceId}</Typography>
              {showDetail && this.state.error && (
                <Box sx={{ maxWidth: "100%", overflow: "auto", bgcolor: "grey.100", borderRadius: 1, p: 1.5 }}>
                  <Typography component="pre" variant="caption">
                    {this.state.error.stack || this.state.error.message}
                  </Typography>
                </Box>
              )}
              <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                <Button href="/dashboard" variant="contained" startIcon={<HomeOutlinedIcon />}>
                  กลับหน้าหลัก
                </Button>
                <Button variant="outlined" startIcon={<RefreshOutlinedIcon />} onClick={() => window.location.reload()}>
                  โหลดใหม่
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      </Box>
    );
  }
}

function createReferenceId() {
  if ("crypto" in window && "randomUUID" in window.crypto) {
    return window.crypto.randomUUID();
  }

  return `FE-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}
