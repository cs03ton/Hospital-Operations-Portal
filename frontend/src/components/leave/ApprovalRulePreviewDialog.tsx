import { Alert, Chip, Dialog, DialogContent, DialogTitle, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from "@mui/material";
import type { ApprovalRulePreview } from "../../api/leaveApi";

type ApprovalRulePreviewDialogProps = {
  open: boolean;
  preview?: ApprovalRulePreview | null;
  onClose: () => void;
};

export function ApprovalRulePreviewDialog({ open, preview, onClose }: ApprovalRulePreviewDialogProps) {
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>ทดสอบกฎการอนุมัติวันลา</DialogTitle>
      <DialogContent dividers>
        {preview ? (
          <Stack spacing={2}>
            <Stack spacing={0.5}>
              <Typography variant="body2" color="text.secondary">ผู้ใช้งาน</Typography>
              <Typography fontWeight={700}>{preview.fullname ?? "-"}</Typography>
              <Typography variant="body2" color="text.secondary">กฎการอนุมัติ</Typography>
              <Typography fontWeight={700}>{preview.approvalRuleName ?? "-"}</Typography>
            </Stack>

            {preview.warnings.length > 0 && (
              <Alert severity="warning">
                {preview.warnings.join(" / ")}
              </Alert>
            )}

            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>ลำดับ</TableCell>
                  <TableCell>ขั้นอนุมัติ</TableCell>
                  <TableCell>ผู้อนุมัติ</TableCell>
                  <TableCell>บทบาท</TableCell>
                  <TableCell>สถานะ</TableCell>
                  <TableCell>คำเตือน</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {preview.steps.length ? preview.steps.map((step) => (
                  <TableRow key={`${step.stepOrder}-${step.stepName}`}>
                    <TableCell>{step.stepOrder}</TableCell>
                    <TableCell>{step.stepName}</TableCell>
                    <TableCell>{step.approverName ?? "-"}</TableCell>
                    <TableCell>{step.approverRoleName ?? "-"}</TableCell>
                    <TableCell>
                      <Chip size="small" color={step.warnings.length ? "warning" : "success"} label={step.status} />
                    </TableCell>
                    <TableCell>{step.warnings.length ? step.warnings.join(" / ") : "-"}</TableCell>
                  </TableRow>
                )) : (
                  <TableRow>
                    <TableCell colSpan={6}>ยังไม่มีขั้นอนุมัติในกฎนี้</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Stack>
        ) : (
          <Typography color="text.secondary">ยังไม่มีข้อมูล preview</Typography>
        )}
      </DialogContent>
    </Dialog>
  );
}
