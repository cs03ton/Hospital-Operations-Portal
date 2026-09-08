import { Box, TablePagination } from "@mui/material";

type ListPaginationProps = {
  page: number;
  pageSize: number;
  totalItems: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  pageSizeOptions?: number[];
  disabled?: boolean;
};

/** Shared one-based pagination used by HOP list pages. */
export function ListPagination({
  page,
  pageSize,
  totalItems,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 20, 50, 100],
  disabled = false,
}: ListPaginationProps) {
  const safePage = Math.max(1, page);

  return (
    <Box sx={{ overflowX: "auto" }}>
      <TablePagination
        component="div"
        count={Math.max(0, totalItems)}
        page={safePage - 1}
        rowsPerPage={pageSize}
        onPageChange={(_, value) => onPageChange(value + 1)}
        onRowsPerPageChange={(event) => onPageSizeChange(Number(event.target.value))}
        rowsPerPageOptions={pageSizeOptions}
        disabled={disabled}
        showFirstButton
        showLastButton
        labelRowsPerPage="จำนวนรายการต่อหน้า"
        labelDisplayedRows={({ from, to, count }) => `แสดง ${from}-${to} จาก ${count} รายการ`}
        getItemAriaLabel={(type) => {
          if (type === "first") return "ไปหน้าแรก";
          if (type === "last") return "ไปหน้าสุดท้าย";
          if (type === "next") return "ไปหน้าถัดไป";
          return "ไปหน้าก่อนหน้า";
        }}
        sx={{
          minWidth: { xs: 320, sm: "auto" },
          ".MuiTablePagination-toolbar": {
            flexWrap: "wrap",
            rowGap: 1,
            justifyContent: "flex-end",
            px: 0,
          },
        }}
      />
    </Box>
  );
}
