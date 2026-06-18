import LockOutlinedIcon from "@mui/icons-material/LockOutlined";
import { Box, Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

export function UnauthorizedPage() {
  return (
    <Box sx={{ maxWidth: 560 }}>
      <Card>
        <CardContent>
          <Stack spacing={2} alignItems="flex-start">
            <LockOutlinedIcon color="primary" />
            <Typography variant="h5">ไม่มีสิทธิ์เข้าถึง</Typography>
            <Typography color="text.secondary">
              บัญชีของคุณยังไม่ได้รับสิทธิ์สำหรับหน้าหรือคำสั่งนี้ กรุณาติดต่อผู้ดูแลระบบ
            </Typography>
            <Button component={RouterLink} to="/dashboard" variant="contained">
              กลับไปหน้าแดชบอร์ด
            </Button>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
