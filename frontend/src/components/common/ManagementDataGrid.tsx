import {
  Box,
  Card,
  CardContent,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TableSortLabel,
  Typography,
} from "@mui/material";
import type { ReactNode } from "react";

export type GridSortDirection = "asc" | "desc";

export type ManagementDataGridColumn<T> = {
  key: string;
  label: string;
  align?: "left" | "center" | "right";
  width?: number | string;
  sortable?: boolean;
  render: (row: T) => ReactNode;
};

type ManagementDataGridProps<T> = {
  title?: string;
  subtitle?: string;
  toolbar?: ReactNode;
  columns: ManagementDataGridColumn<T>[];
  rows: T[];
  getRowId: (row: T) => string;
  isLoading?: boolean;
  emptyMessage?: string;
  page: number;
  pageSize: number;
  totalItems: number;
  sort: string;
  direction: GridSortDirection;
  onSortChange: (sort: string, direction: GridSortDirection) => void;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
};

export function ManagementDataGrid<T>({
  title,
  subtitle,
  toolbar,
  columns,
  rows,
  getRowId,
  isLoading,
  emptyMessage = "ไม่พบข้อมูล",
  page,
  pageSize,
  totalItems,
  sort,
  direction,
  onSortChange,
  onPageChange,
  onPageSizeChange,
}: ManagementDataGridProps<T>) {
  function handleSort(columnKey: string) {
    const nextDirection = sort === columnKey && direction === "asc" ? "desc" : "asc";
    onSortChange(columnKey, nextDirection);
  }

  return (
    <Card>
      <CardContent>
        {(title || subtitle || toolbar) && (
          <Stack spacing={2} sx={{ mb: 2 }}>
            {(title || subtitle) && (
              <Box>
                {title && <Typography variant="h6" fontWeight={800}>{title}</Typography>}
                {subtitle && <Typography variant="body2" color="text.secondary">{subtitle}</Typography>}
              </Box>
            )}
            {toolbar}
          </Stack>
        )}
        <TableContainer sx={{ maxHeight: 640 }}>
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow>
                {columns.map((column) => (
                  <TableCell key={column.key} align={column.align} width={column.width}>
                    {column.sortable ? (
                      <TableSortLabel
                        active={sort === column.key}
                        direction={sort === column.key ? direction : "asc"}
                        onClick={() => handleSort(column.key)}
                      >
                        {column.label}
                      </TableSortLabel>
                    ) : (
                      column.label
                    )}
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={columns.length}>กำลังโหลดข้อมูล...</TableCell>
                </TableRow>
              ) : rows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={columns.length}>{emptyMessage}</TableCell>
                </TableRow>
              ) : (
                rows.map((row) => (
                  <TableRow hover key={getRowId(row)}>
                    {columns.map((column) => (
                      <TableCell key={column.key} align={column.align}>
                        {column.render(row)}
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          component="div"
          count={totalItems}
          page={page - 1}
          rowsPerPage={pageSize}
          onPageChange={(_, value) => onPageChange(value + 1)}
          onRowsPerPageChange={(event) => onPageSizeChange(Number(event.target.value))}
          rowsPerPageOptions={[10, 20, 50, 100]}
          labelRowsPerPage="จำนวนต่อหน้า"
          labelDisplayedRows={({ from, to, count }) => `${from}-${to} จาก ${count}`}
        />
      </CardContent>
    </Card>
  );
}
